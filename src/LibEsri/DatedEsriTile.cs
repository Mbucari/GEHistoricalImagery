namespace LibEsri;

public class DatedEsriTile
{
	public DateOnly LayerDate { get; }
	public DateOnly CaptureDate { get; }
	public Layer Layer { get; }
	public EsriTile Tile { get; }

	internal DatedEsriTile(DateOnly captureDate, Layer layer, EsriTile tile)
	{
		CaptureDate = captureDate;
		LayerDate = layer.Date;
		Layer = layer;
		Tile = tile;
	}
}
