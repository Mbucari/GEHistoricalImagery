namespace Keyhole;

public interface IQuadtreeNode
{
	int CacheNodeEpoch { get; }
	IReadOnlyList<IQuadtreeLayer> Layer { get; }
	IReadOnlyList<IQuadtreeChannel> Channel { get; }
}
