namespace Keyhole;

public partial class QuadtreePacket : IQuadtreePacket
{
	IReadOnlyList<ISparseQuadtreeNode> IQuadtreePacket.SparseQuadtreeNode => SparseQuadtreeNode;

	public partial class Types
	{
		public partial class SparseQuadtreeNode : ISparseQuadtreeNode
		{
			IQuadtreeNode ISparseQuadtreeNode.Node => Node;
		}
	}
}
