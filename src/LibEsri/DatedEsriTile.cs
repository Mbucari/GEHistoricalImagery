namespace LibEsri;

public class DatedEsriTile
{
	public DateOnly LayerDate { get; }
	public DateOnly CaptureDate { get; }

	internal DatedEsriTile(DateOnly captureDate, Layer layer)
	{
		CaptureDate = captureDate;
		LayerDate = layer.Date;
	}
}
