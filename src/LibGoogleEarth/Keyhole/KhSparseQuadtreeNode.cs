namespace Keyhole;

internal record KhSparseQuadtreeNode : ISparseQuadtreeNode
{
	public int Index { get; }
	public KhQuadtreeNode Node { get; }
	IQuadtreeNode ISparseQuadtreeNode.Node => Node;
	public KhSparseQuadtreeNode(int subIndex, KhQuadtreeNode node)
	{
		Index = subIndex;
		Node = node;
	}
}
