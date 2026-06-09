using LibMapCommon;

namespace LibEsri;

public class DatedEsriTile : IDatedElement
{
	public DateOnly LayerDate { get; }
	public DateOnly CaptureDate { get; }
	public Layer Layer { get; }
	public EsriTile Tile { get; }
	public DateOnly Date => CaptureDate;

	internal DatedEsriTile(DateOnly captureDate, Layer layer, EsriTile tile)
	{
		CaptureDate = captureDate;
		LayerDate = layer.Date;
		Layer = layer;
		Tile = tile;
	}
}
