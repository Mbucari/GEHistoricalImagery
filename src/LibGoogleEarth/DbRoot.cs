using Keyhole;
using Keyhole.Dbroot;
using LibMapCommon;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace LibGoogleEarth;

public enum Database
{
	Default,
	TimeMachine,
	Sky,
	Moon,
	Mars
}

/// <summary>
/// A Google earth database instance
/// </summary>
public abstract class DbRoot
{
	private readonly CachedHttpClient HttpClient;
	/// <summary>  The keyhole DbRoot protocol buffer  </summary>
	public DbRootProto DbRootBuffer { get; }
	/// <summary> The google earth database </summary>
	public abstract Database Database { get; }
	private ReadOnlyMemory<byte> EncryptionData { get; }
	private MemoryCache PacketCache { get; } = new MemoryCache(new MemoryCacheOptions());

	private static readonly TimeSpan CacheCompactInterval = TimeSpan.FromSeconds(2);
	private static readonly MemoryCacheEntryOptions Options = new() { SlidingExpiration = CacheCompactInterval };
	private DateTime LastCacheComact;

	protected DbRoot(CachedHttpClient cachedHttpClient, EncryptedDbRootProto dbRootEnc)
	{
		HttpClient = cachedHttpClient;
		EncryptionData = dbRootEnc.EncryptionData.Memory;
		var bts = dbRootEnc.DbrootData.ToByteArray();
		Decrypt(bts);
		DbRootBuffer = DbRootProto.Parser.ParseFrom(DecompressBuffer(bts));
	}

	/// <summary>
	/// Create a new instance of the Google Earth database
	/// </summary>
	/// <param name="cacheDir">path to the cache directory. (default is .\cache\</param>
	/// <returns>A new instance of the Google Earth database</returns>
	public static async Task<DbRoot> CreateAsync(Database database, string? cacheDir)
	{
		var url = database is Database.Default ? DefaultDbRoot.DatabaseUrl : NamedDbRoot.DatabaseUrl(database);

		var cacheDirInfo = cacheDir is null ? null : new DirectoryInfo(cacheDir);
		cacheDirInfo?.Create();

		var cachedHttpClient = new CachedHttpClient(cacheDirInfo);

		byte[] dbRootBts = await cachedHttpClient.GetBytesIfNewer(url);

		var proto = EncryptedDbRootProto.Parser.ParseFrom(dbRootBts);

		return database is Database.Default
			? new DefaultDbRoot(cachedHttpClient, proto)
			: new NamedDbRoot(database, cachedHttpClient, proto);
	}

	/// <summary>
	/// Gets a <see cref="TileNode"/> for a specified <see cref="KeyholeTile"/>
	/// </summary>
	/// <param name="tile">The tile to get</param>
	/// <returns>The <see cref="KeyholeTile"/>'s <see cref="TileNode"/></returns>
	public async Task<TileNode?> GetNodeAsync(KeyholeTile tile)
	{
		var packet = await GetQuadtreePacketAsync(tile);
		return
			packet?.SparseQuadtreeNode?.SingleOrDefault(n => n.Index == tile.SubIndex)?.Node is IQuadtreeNode n
			? new TileNode(tile, n)
			: null;
	}


	[return: NotNullIfNotNull(nameof(terrainTile))]
	public async Task<T?> GetEarthAssetAsync<T>(IEarthAsset<T>? terrainTile)
	{
		if (terrainTile is null)
			return default;

		var rawAsset = await DownloadBytesAsync(terrainTile.AssetUrl);
		if (terrainTile.Compressed)
			rawAsset = DecompressBuffer(rawAsset);

		return terrainTile.Decode(rawAsset);
	}

	/// <summary>
	/// Gets a <see cref="QuadtreePacket"/> which references a specified <see cref="KeyholeTile"/>
	/// </summary>
	/// <param name="tile">The tile to get</param>
	/// <returns>The <see cref="QuadtreePacket"/> which references the <see cref="TileNode"/></returns>
	private async Task<IQuadtreePacket?> GetQuadtreePacketAsync(KeyholeTile tile)
	{
		if ((DateTime.UtcNow - LastCacheComact) > CacheCompactInterval)
		{
			lock (PacketCache)
			{
				//0% will remove all expired entries and nothing else.
				PacketCache.Compact(0);
				LastCacheComact = DateTime.UtcNow;
			}
		}
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

	private async Task<IQuadtreePacket?> GetRootCachedAsync()
	{
		return await PacketCache.GetOrCreateAsync(KeyholeTile.Root, loadRootPacketAsync, Options);

		async Task<IQuadtreePacket> loadRootPacketAsync(ICacheEntry _)
			=> await GetPacketAsync(KeyholeTile.Root, (int)DbRootBuffer.DatabaseVersion.QuadtreeVersion);
	}

	private async Task<IQuadtreePacket?> GetChildCachedAsync(IQuadtreePacket parentPacket, KeyholeTile path)
	{
		return await PacketCache.GetOrCreateAsync(path, loadChildPacketAsync, Options);

		async Task<IQuadtreePacket?> loadChildPacketAsync(ICacheEntry _)
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

	protected abstract Task<IQuadtreePacket> GetPacketAsync(KeyholeTile path, int epoch);

	/// <summary>
	/// Download, decrypt and cache a file from Google Earth.
	/// </summary>
	/// <param name="url">The Google Earth asset Url</param>
	/// <returns>The decrypted asset's bytes</returns>
	protected Task<byte[]> DownloadBytesAsync(string url)
		=> HttpClient.GetByteArrayAsync(url, Decrypt);

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

	protected static byte[] DecompressBuffer(byte[] compressedPacket)
	{
		const int kPacketCompressHdrSize = 8;

		if (!tryGetDecompressBufferSize(compressedPacket, out var decompSz))
			throw new InvalidDataException("Failed to determine packet size.");

		var decompressed = GC.AllocateUninitializedArray<byte>(decompSz);
		using var compressedStream = new MemoryStream(compressedPacket[kPacketCompressHdrSize..], writable: false);

		using (var outputStream = new MemoryStream(decompressed))
		{
			using var decompressor = new ZLibStream(compressedStream, CompressionMode.Decompress);
			decompressor.CopyTo(outputStream);
		}

		return decompressed;

		static bool tryGetDecompressBufferSize(ReadOnlySpan<byte> buff, out int decompSz)
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
}
