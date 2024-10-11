using Keyhole;

namespace LibGoogleEarth;

/// <summary>
/// Represents a Google Earth aerial image tile from a specific date
/// </summary>
public record DatedTile : IEarthAsset<byte[]>
{
	private const string ROOT_URL = "https://khmdb.google.com/flatfile?db=tm&f1-{0}-i.{1}-{2}";
	private const string ROOT_URL_NO_PROVIDER = "https://kh.google.com/flatfile?f1-{0}-i.{1}";
	/// <summary> The <see cref="LibGoogleEarth.Tile"/> covered by this image  </summary>
	public Tile Tile { get; }
	/// <summary> The aerial image's epoch. </summary>
	public int Epoch { get; }
	public DateOnly Date { get; }
	/// <summary>
	/// The Google Earther image's provider number
	/// </summary>
	public int Provider { get; }
	/// <summary> Url to the encrypted aerial image. </summary>
	public string AssetUrl { get; }

	public bool Compressed => false;

	internal DatedTile(Tile tile, QuadtreeImageryDatedTile datedTile)
	{
		Tile = tile;
		Provider = datedTile.Provider;
		Date = datedTile.DateOnly;
		Epoch = datedTile.DatedTileEpoch;

		AssetUrl = string.Format(ROOT_URL, tile.Path, Epoch, datedTile.Date.ToString("x"));
	}

	internal DatedTile(Tile tile, DateOnly tileDate, QuadtreeLayer imageryLayer)
	{
		Tile = tile;
		Provider = 0;
		Date = tileDate;
		Epoch = imageryLayer.LayerEpoch;

		AssetUrl = string.Format(ROOT_URL_NO_PROVIDER, tile.Path, Epoch);
	}

	public byte[] Decode(byte[] bytes) => bytes;
}
