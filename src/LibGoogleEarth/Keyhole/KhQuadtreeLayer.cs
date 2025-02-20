namespace Keyhole;

internal class KhQuadtreeLayer : IQuadtreeLayer
{
	public QuadtreeLayer.Types.LayerType Type { get; }
	public int LayerEpoch { get; }
	public int Provider { get; }
	public KhQuadtreeLayer(QuadtreeLayer.Types.LayerType type, int layerEpoch, int provider)
	{
		Type = type;
		LayerEpoch = layerEpoch;
		Provider = provider;
	}
}
