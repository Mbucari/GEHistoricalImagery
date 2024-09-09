using Google.Protobuf;
using Keyhole;
using Keyhole.Dbroot;
using LibGoogleEarth.IO;
using Microsoft.Extensions.Caching.Memory;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LibGoogleEarth;

/// <summary>
/// A Google earth database instance
/// </summary>
public class DbRoot
{
	private static readonly HttpClient HttpClient = new();
	private const string QP2_URL = "https://khmdb.google.com/flatfile?db=tm&qp-{0}-q.{1}";
	private const string DBROOT_URL = "https://khmdb.google.com/dbRoot.v5?db=tm&hl=en&gl=us&output=proto";
	/// <summary> The database's local cache directory </summary>
	public DirectoryInfo CacheDir { get; }
	/// <summary>  The keyhole DbRoot protocol buffer  </summary>
	public DbRootProto DbRootBuffer { get; }
	private ReadOnlyMemory<byte> EncryptionData { get; }
	private MemoryCache PacketCache { get; } = new MemoryCache(new MemoryCacheOptions());

	private DbRoot(DirectoryInfo cacheDir, EncryptedDbRootProto dbRootEnc)
	{
		CacheDir = cacheDir;
		EncryptionData = dbRootEnc.EncryptionData.Memory;
		var bts = dbRootEnc.DbrootData.ToByteArray();
		Decrypt(bts);
		DbRootBuffer = DecodeBufferInternal(DbRootProto.Parser, bts);
	}

	/// <summary>
	/// Create a new instance of the Google Earth database
	/// </summary>
	/// <param name="cacheDir">path to the cache directory. (default is .\cache\</param>
	/// <returns>A new instance of the Google Earth database</returns>
	public static async Task<DbRoot> CreateAsync(string cacheDir = ".\\cache")
	{
		var uri = new Uri(DBROOT_URL);

		var cacheDirInfo = new DirectoryInfo(cacheDir);
		cacheDirInfo.Create();

		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		var dbRootFile = new FileInfo(Path.Combine(cacheDirInfo.FullName, Path.GetFileName(uri.AbsolutePath)));

		await using var mutex = await AsyncMutex.AcquireAsync("Global\\" + HashString(dbRootFile.FullName));

		if (dbRootFile.Exists)
			request.Headers.IfModifiedSince = dbRootFile.LastWriteTimeUtc;

		using var response = await HttpClient.SendAsync(request);

		byte[] dbRootBts;

		if (dbRootFile.Exists && response.StatusCode == System.Net.HttpStatusCode.NotModified)
			dbRootBts = File.ReadAllBytes(dbRootFile.FullName);
		else
		{
			response.EnsureSuccessStatusCode();
			dbRootBts = await response.Content.ReadAsByteArrayAsync();
			try
			{
				File.WriteAllBytes(dbRootFile.FullName, dbRootBts);

				if (response.Content.Headers.LastModified.HasValue)
					dbRootFile.LastWriteTimeUtc = response.Content.Headers.LastModified.Value.UtcDateTime;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Failed to Cache {dbRootFile.FullName}.");
				Console.Error.WriteLine(ex.Message);
			}
		}

		return new DbRoot(cacheDirInfo, EncryptedDbRootProto.Parser.ParseFrom(dbRootBts));
	}

	/// <summary>
	/// Gets a <see cref="TileNode"/> for a specified <see cref="Tile"/>
	/// </summary>
	/// <param name="tile">The tile to get</param>
	/// <returns>The <see cref="Tile"/>'s <see cref="TileNode"/></returns>
	public async Task<TileNode?> GetNodeAsync(Tile tile)
	{
		var packet = await GetQuadtreePacketAsync(tile);
		return
			packet?.SparseQuadtreeNode?.SingleOrDefault(n => n.Index == tile.SubIndex)?.Node is QuadtreeNode n
			? new TileNode(tile, n)
			: null;
	}

	/// <summary>
	/// Gets a <see cref="QuadtreePacket"/> which references a specified <see cref="Tile"/>
	/// </summary>
	/// <param name="tile">The tile to get</param>
	/// <returns>The <see cref="QuadtreePacket"/> which references the <see cref="TileNode"/></returns>
	public async Task<QuadtreePacket?> GetQuadtreePacketAsync(Tile tile)
	{
		var packet = await GetRootCachedAsync();

		if (packet == null)
			return null;

		foreach (var path in tile.Indices)
		{
			packet = await GetChildCachedAsync(packet, path);
			if (packet == null)
				return null;
		}
		return packet;
	}

	private async Task<QuadtreePacket?> GetRootCachedAsync()
	{
		return await PacketCache.GetOrCreateAsync(Tile.Root, loadRootPacketAsync);

		async Task<QuadtreePacket> loadRootPacketAsync(ICacheEntry _)
			=> await GetPacketAsync(Tile.Root, (int)DbRootBuffer.DatabaseVersion.QuadtreeVersion);
	}

	private async Task<QuadtreePacket?> GetChildCachedAsync(QuadtreePacket parentPacket, Tile path)
	{
		return await PacketCache.GetOrCreateAsync(path, loadChildPacketAsync);

		async Task<QuadtreePacket?> loadChildPacketAsync(ICacheEntry _)
		{
			var childNode
				= parentPacket.SparseQuadtreeNode
				.Where(n => n.Node.CacheNodeEpoch != 0)
				.SingleOrDefault(n => n.Index == path.SubIndex)?.Node;

			if (childNode is null) return null;

			var childPacket = await GetPacketAsync(path, childNode.CacheNodeEpoch);
			return childPacket;
		}
	}

	private async Task<QuadtreePacket> GetPacketAsync(Tile path, int epoch)
	{
		byte[] packetData = await DownloadBytesAsync(string.Format(QP2_URL, path, epoch));

		return DecodeBufferInternal(QuadtreePacket.Parser, packetData);
	}

	/// <summary>
	/// Download, decrypt and cache a file from Google Earth.
	/// </summary>
	/// <param name="url">The Google Earth asset Url</param>
	/// <returns>The decrypted asset's bytes</returns>
	public async Task<byte[]> DownloadBytesAsync(string url)
	{
		var uri = new Uri(url);
		var fileName = Path.Combine(CacheDir.FullName, Path.GetFileName(uri.PathAndQuery.Replace('?', '-')));
		await using var mutex = await AsyncMutex.AcquireAsync("Global\\" + HashString(fileName));

		if (File.Exists(fileName) && File.ReadAllBytes(fileName) is byte[] b && b.Length > 0)
			return b;
		else
		{
			var data = await HttpClient.GetByteArrayAsync(uri);
			Decrypt(data);

			try
			{
				File.WriteAllBytes(fileName, data);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Failed to Cache {uri.PathAndQuery}.");
				Console.Error.WriteLine(ex.Message);
			}
			return data;
		}
	}

	private void Decrypt(Span<byte> cipherText)
		=> Encode(EncryptionData.Span, cipherText);

	private static void Encode(ReadOnlySpan<byte> key, Span<byte> cipherText)
	{
		int off = 16;
		for (int j = 0; j < cipherText.Length; j++)
		{
			cipherText[j] ^= key[off++];

			if ((off & 7) == 0) off += 16;
			if (off >= key.Length) off = (off + 8) % 24;
		}
	}

	private static T DecodeBufferInternal<T>(MessageParser<T> parser, byte[] packet) where T : IMessage<T>
	{
		const int kPacketCompressHdrSize = 8;

		if (!TryGetDecompressBufferSize(packet, out var decompSz))
			throw new InvalidDataException("Failed to determine packet size.");

		var decompressed = GC.AllocateUninitializedArray<byte>(decompSz);
		using var compressedStream = new MemoryStream(packet[kPacketCompressHdrSize..], writable: false);

		using (var outputStream = new MemoryStream(decompressed))
		{
			using var decompressor = new ZLibStream(compressedStream, CompressionMode.Decompress);
			decompressor.CopyTo(outputStream);
		}
		return parser.ParseFrom(decompressed);

		static bool TryGetDecompressBufferSize(ReadOnlySpan<byte> buff, out int decompSz)
		{
			const uint kPktMagic = 0x7468deadu;
			const uint kPktMagicSwap = 0xadde6874u;

			var intBuf = MemoryMarshal.Cast<byte, uint>(buff);

			if (buff.Length >= kPacketCompressHdrSize)
			{
				if (intBuf[0] == kPktMagic)
				{
					decompSz = (int)intBuf[1];
					return true;
				}
				else if (intBuf[0] == kPktMagicSwap)
				{
					decompSz = (int)System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(intBuf[1]);
					return true;
				}
			}

			decompSz = 0;
			return false;
		}
	}

	private static string HashString(string s)
		=> Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(s)));
}
