namespace Keyhole;

public interface IQuadtreePacket
{
	int PacketEpoch { get; }
	IReadOnlyList<ISparseQuadtreeNode> SparseQuadtreeNode { get; }
}
