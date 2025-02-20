using LibGoogleEarth;
using System.Runtime.InteropServices;

namespace Keyhole;

internal class KhQuadTreePacket16 : IQuadtreePacket
{
	public int PacketEpoch { get; }
	public IReadOnlyList<ISparseQuadtreeNode> SparseQuadtreeNode => sparseNodes;

	private readonly KhSparseQuadtreeNode[] sparseNodes;

	private KhQuadTreePacket16(KhQuadTreePacketHeader header)
	{
		PacketEpoch = header.Version;
		sparseNodes = GC.AllocateUninitializedArray<KhSparseQuadtreeNode>(header.NumInstances);
	}

	public static KhQuadTreePacket16 ParseFrom(Span<byte> bytes, bool isRoot)
	{
		var header = KhQuadTreePacketHeader.ParseFrom(bytes);
		var p = new KhQuadTreePacket16(header);

		var quanta = MemoryMarshal.Cast<byte, KhQuadTreeQuantum16>(bytes[KhQuadTreePacketHeader.HEADER_SIZE..header.DataBufferOffset]);
		var channels = MemoryMarshal.Cast<byte, short>(bytes[header.DataBufferOffset..]);

		Traverse(quanta, channels, p.sparseNodes, 0, "", isRoot);

		return p;
	}

	private static int Traverse(Span<KhQuadTreeQuantum16> quanta, Span<short> channels, KhSparseQuadtreeNode[] collector, int node_index, string qt_path, bool isRoot)
	{
		if (node_index >= collector.Length)
			return node_index;

		var q = quanta[node_index];

		var channelTypes = channels.Slice(q.type_offset / sizeof(short), q.num_channels).ToArray();
		var channelVersions = channels.Slice(q.version_offset / sizeof(short), q.num_channels).ToArray();

		var subIndex
			= isRoot ? Util.GetRootSubIndex("0" + qt_path)
			: node_index > 0 ? Util.GetTreeSubIndex(qt_path)
			: 0;

		collector[node_index] = new KhSparseQuadtreeNode(subIndex, new KhQuadtreeNode(q, channelTypes, channelVersions));

		for (int i = 0; i < 4; i++)
		{
			if (q.children.GetBit(i))
			{
				var new_qt_path = qt_path + i.ToString();
				node_index = Traverse(quanta, channels, collector, node_index + 1, new_qt_path, isRoot);
			}
		}
		return node_index;
	}
}
