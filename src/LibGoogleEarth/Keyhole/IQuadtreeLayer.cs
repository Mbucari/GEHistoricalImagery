namespace Keyhole;

public interface IQuadtreeLayer
{
	QuadtreeLayer.Types.LayerType Type { get; }
	int LayerEpoch { get; }
	int Provider { get; }
}
