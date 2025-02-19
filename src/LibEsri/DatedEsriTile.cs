namespace LibEsri;

public class DatedEsriTile
{
	/// <summary> The <see cref="LibGoogleEarth.KeyholeTile"/> covered by this image  </summary>
	public EsriTile Tile { get; }
	public DateOnly Date { get; }
	/// <summary> Url to the encrypted aerial image. </summary>
	public string AssetUrl { get; }

	public string Version { get; }

	internal DatedEsriTile(EsriTile tile, DateOnly date, Layer layer)
	{
		Tile = tile;
		Date = date;
		Version = layer.Title;
		AssetUrl = layer.GetAssetUrl(tile);
	}
}
