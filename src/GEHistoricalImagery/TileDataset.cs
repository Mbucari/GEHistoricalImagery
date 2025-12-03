using LibMapCommon;
using OSGeo.GDAL;

namespace GEHistoricalImagery;

internal abstract class TileDataset
{
	public DateOnly? LayerDate { get; init; }
	public DateOnly TileDate { get; init; }
	public abstract ITile Tile { get; }
	public byte[]? TileBytes { get; init; }
	public required string? Message { get; init; }
	public abstract GeoTransform GetGeoTransform();
	public abstract GDALWarpAppOptions GetWarpOptions(string targetSr);

	static TileDataset()
	{
		GdalLib.Register();
	}
}

internal class TileDataset<TCoordinate> : TileDataset, IDisposable
	where TCoordinate : IGeoCoordinate<TCoordinate>
{
	public Dataset? Dataset { get; init; }
	public override ITile<TCoordinate> Tile { get; }
	public TileDataset(ITile<TCoordinate> tile)
	{
		Tile = tile;
	}

	public override GeoTransform GetGeoTransform() => Tile.GetGeoTransform();

	public override GDALWarpAppOptions GetWarpOptions(string targetSr)
		=> EarthImage<TCoordinate>.GetWarpOptions(targetSr);

	public void Dispose()
		=> Dataset?.Dispose();
}