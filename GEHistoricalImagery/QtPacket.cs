using Keyhole;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;

namespace GEHistoricalImagery;

internal class QtPacket
{

	public string FullPath { get; }
	public QuadtreePacket Packet { get; }
	public QtPacket? Parent { get; private init; }
	public DbRoot DbRoot { get; }

	protected QtPacket(DbRoot dbRoot, QtPacket? parent, string path, QuadtreePacket packet)
	{
		DbRoot = dbRoot;
		Parent = parent;
		FullPath = Parent?.FullPath + path;
		Packet = packet;
	}

	public async Task<Node?> GetNodeAsync(Tile tile)
	{
		var path = tile.QtPath;
		var child = await GetChildAsync(path);
		if (child is null) return null;

		int remainderLen = path.Length - child.FullPath.Length;
		if (remainderLen > 4 || remainderLen < 0) return null;

		var remainder = path.Substring(child.FullPath.Length);

		int subIndex = child.GetNodeIndex(remainder);

		var cc = child.Packet.SparseQuadtreeNode.SingleOrDefault(n => n.Index == subIndex);
		return cc?.Node is QuadtreeNode n ? new Node(tile, n) : null;
	}

	public virtual async Task<QtPacket?> GetChildAsync(string quadTreePath)
	{
		ValidateQuadTreePath(quadTreePath);

		if (!quadTreePath.StartsWith(FullPath))
			return await Parent!.GetChildAsync(quadTreePath);

		if (quadTreePath.Length - FullPath.Length <= 4) return this;

		var sub = quadTreePath.Substring(FullPath.Length, 4);

		return
			await GetChildInternalAsync(sub) is QtPacket c
			? await c.GetChildAsync(quadTreePath)
			: null;
	}

	// Nodes have two numbering schemes:
	//
	// 1) "Subindex".  This numbering starts at the top of the tree
	// and goes left-to-right across each level, like this:
	//
	//                    0
	//                 /     \                           .
	//               1  86 171 256
	//            /     \                                .
	//          2  3  4  5 ...
	//        /   \                                      .
	//       6 7 8 9  ...
	//
	// Notice that the second row is weird in that it's not left-to-right
	// order.  HOWEVER, the root node in Keyhole is special in that it
	// doesn't have this weird ordering.  It looks like this:
	//
	//                    0
	//                 /     \                           .
	//               1  2  3  4
	//            /     \                                .
	//          5  6  7  8 ...
	//       /     \                                     .
	//     21 22 23 24  ...
	//
	// The mangling of the second row is controlled by a parameter to the
	// constructor.

	protected virtual int GetNodeIndex(string qtp)
	{
		ValidateQuadTreePath(qtp);
		if (qtp.Length > 4)
			throw new ArgumentException("Quad Tree Path mst be a string of 0 to 4 characters", nameof(qtp));

		if (qtp.Length == 0) return 0;

		int subIndex = 0;

		for (int i = 1; i < qtp.Length; i++)
		{
			subIndex *= 4;
			subIndex += qtp[i] - 0x30 + 1;
		}

		subIndex += (qtp[0] - 0x30) * 85 + 1;

		return subIndex;
	}

	protected static void ValidateQuadTreePath([NotNull] string? quadTreePath)
	{
		if (quadTreePath is null)
			throw new ArgumentException("Null Quad Tree Path", nameof(quadTreePath));

		foreach (var c in quadTreePath)
			if (!(c is '0' or '1' or '2' or '3'))
				throw new ArgumentException("Quad Tree Path can only contain the characters '0', '1', '2', and '3'", nameof(quadTreePath));
	}

	protected async Task<QtPacket?> GetChildInternalAsync(string subPath)
	{
		return await DbRoot.PacketCache.GetOrCreateAsync(FullPath + subPath, e => LoadChildPacketAsyncAsync(e, subPath, Packet));

		async Task<QtPacket?> LoadChildPacketAsyncAsync(ICacheEntry cacheEntry, string subPath, QuadtreePacket packet)
		{
			var subIndex = GetNodeIndex(subPath);

			var childNode
				= packet.SparseQuadtreeNode
				.Where(n => n.Node.CacheNodeEpoch != 0)
				.SingleOrDefault(n => n.Index == subIndex)?.Node;

			if (childNode is null) return null;

			var childPacket = await DbRoot.GetPacketAsync((string)cacheEntry.Key, childNode.CacheNodeEpoch);

			return
				childPacket is null ? null
				: new QtPacket(DbRoot, this, subPath, childPacket);
		}
	}
}
