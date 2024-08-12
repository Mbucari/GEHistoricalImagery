using Keyhole;

namespace GEHistoricalImagery;

internal record DatedTile
{
	private const string ROOT_URL = "https://khmdb.google.com/flatfile?db=tm&f1-{0}-i.{1}-{2}";
	private const string ROOT_URL_NO_PROVIDER = "https://kh.google.com/flatfile?f1-{0}-i.{1}";
	public Tile Tile { get; }
	public int Epoch { get; }
	public DateOnly Date { get; }
	public int Provider { get; }
	public string TileUrl { get; }

	public DatedTile(Tile tile, QuadtreeImageryDatedTile datedTile)
	{
		Tile = tile;
		Provider = datedTile.Provider;
		Date = datedTile.Date.ToDate();
		Epoch = datedTile.DatedTileEpoch;

		TileUrl = string.Format(ROOT_URL, tile.QtPath, Epoch, datedTile.Date.ToString("x"));
	}

	public DatedTile(Tile tile, DateOnly tileDate, QuadtreeLayer imageryLayer)
	{
		Tile = tile;
		Provider = 0;
		Date = tileDate;
		Epoch = imageryLayer.LayerEpoch;

		TileUrl = string.Format(ROOT_URL_NO_PROVIDER, tile.QtPath, Epoch);
	}
}
