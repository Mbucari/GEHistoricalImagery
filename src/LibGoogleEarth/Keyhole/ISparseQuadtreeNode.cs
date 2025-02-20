namespace Keyhole;

public interface ISparseQuadtreeNode
{
	int Index { get; }
	IQuadtreeNode Node { get; }
}
