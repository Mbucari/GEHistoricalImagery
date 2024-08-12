using Google.Protobuf;
using Keyhole;
using Keyhole.Dbroot;
using Microsoft.Extensions.Caching.Memory;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace GEHistoricalImagery;

internal class DbRoot
{
	private static readonly HttpClient HttpClient = new();
	private const string QP2_URL = "https://khmdb.google.com/flatfile?db=tm&qp-{0}-q.{1}";
	private const string DBROOT_URL = "https://khmdb.google.com/dbRoot.v5?db=tm&hl=en&gl=us&output=proto";

	private DbRootProto Buffer { get; }
	public DirectoryInfo CacheDir { get; }
	private ReadOnlyMemory<byte> EncryptionData { get; }

	private QtPacket? Root { get; set; }
	public MemoryCache PacketCache { get; } = new MemoryCache(new MemoryCacheOptions());

	private DbRoot(DirectoryInfo cacheDir, EncryptedDbRootProto dbRootEnc)
	{
		CacheDir = cacheDir;
		EncryptionData = dbRootEnc.EncryptionData.Memory;
		var bts = dbRootEnc.DbrootData.ToByteArray();
		Decrypt(bts);
		Buffer = DecodeBufferInternal(DbRootProto.Parser, bts);
	}

	public static async Task<DbRoot> CreateAsync(string cacheDir = ".\\cache")
	{
		var uri = new Uri(DBROOT_URL);

		var cacheDirInfo = new DirectoryInfo(cacheDir);
		cacheDirInfo.Create();

		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		var dbRootFile = new FileInfo(Path.Combine(cacheDirInfo.FullName, Path.GetFileName(uri.AbsolutePath)));

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

	public async Task<QtPacket> GetRootPacket()
		=> Root ??= new QtRoot(this, await GetPacketAsync("0", (int)Buffer.DatabaseVersion.QuadtreeVersion));

	public async Task<Node?> GetNodeAsync(Tile tile)
	{
		var root = await GetRootPacket();
		return await root.GetNodeAsync(tile);
	}

	public async Task<QuadtreePacket> GetPacketAsync(string qtPath, int epoch)
	{
		byte[] packetData = await DownloadBytesAsync(string.Format(QP2_URL, qtPath, epoch));

		return DecodeBufferInternal(QuadtreePacket.Parser, packetData);
	}

	public async Task<byte[]> DownloadBytesAsync(string url)
	{
		var uri = new Uri(url);
		var fileName = Path.Combine(CacheDir.FullName, Path.GetFileName(uri.PathAndQuery.Replace('?', '-')));

		if (File.Exists(fileName))
		{
			lock (this)
				return File.ReadAllBytes(fileName);
		}
		else
		{
			var data = await HttpClient.GetByteArrayAsync(uri);
			Decrypt(data);

			try
			{
				lock (this)
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

	private static T DecodeBufferInternal<T>(MessageParser<T> parser, byte[] packet) where T : IMessage<T>
	{
		if (!TryGetDecompressBufferSize(packet, out var decompSz))
			throw new InvalidDataException("Failed to determine packet size.");

		var decompressed = new byte[decompSz];
		using var compressedStream = new MemoryStream(packet, 8, packet.Length - 8, writable: false);

		using (var outputStream = new MemoryStream(decompressed))
		{
			using var decompressor = new ZLibStream(compressedStream, CompressionMode.Decompress);
			decompressor.CopyTo(outputStream);
		}
		return parser.ParseFrom(decompressed);
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

	private static bool TryGetDecompressBufferSize(ReadOnlySpan<byte> buff, out int decompSz)
	{
		const int kPacketCompressHdrSize = 8;
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
				uint len = intBuf[1];
				uint v = ((len >> 24) & 0x000000ffu) |
						   ((len >> 8) & 0x0000ff00u) |
						   ((len << 8) & 0x00ff0000u) |
						   ((len << 24) & 0xff000000u);
				decompSz = (int)v;
				return true;
			}
		}

		decompSz = 0;
		return false;
	}

	private class QtRoot : QtPacket
	{
		/*
		 * The root packet is 3-high, but all child nodes are 4 high.
		 */

		public override async Task<QtPacket?> GetChildAsync(string quadTreePath)
		{
			ValidateQuadTreePath(quadTreePath);
			if (quadTreePath.Length < 1 || quadTreePath[0] != '0')
				throw new ArgumentException("Paths must begin with '0'.", nameof(quadTreePath));
			if (quadTreePath.Length <= 4) return this;

			var sub = quadTreePath.Substring(1, 3);

			return
				await GetChildInternalAsync(sub) is QtPacket c
				? await c.GetChildAsync(quadTreePath)
				: null;
		}

		protected override int GetNodeIndex(string qtp)
		{
			ValidateQuadTreePath(qtp);
			if (qtp.Length > 3)
				throw new ArgumentException("Root Quad Tree Path mst be a string of 0 to 3 characters", nameof(qtp));

			if (qtp.Length == 0) return 0;

			int subIndex = 0;

			for (int i = 0; i < qtp.Length; i++)
			{
				subIndex *= 4;
				subIndex += qtp[i] - 0x30 + 1;
			}

			return subIndex;
		}

		public QtRoot(DbRoot dbRoot, QuadtreePacket packet)
			: base(dbRoot, null, "0", packet) { }
	}
}
