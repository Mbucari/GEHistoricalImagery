using LibMapCommon;
using OSGeo.GDAL;

namespace GEHistoricalImagery;

internal interface ITileDataset
{
	DateOnly? LayerDate { get; init; }
	DateOnly TileDate { get; init; }
	ITile Tile { get; }
	byte[]? TileBytes { get; init; }
	string? Message { get; init; }
	GeoTransform GetGeoTransform();
	GDALWarpAppOptions GetWarpOptions(RasterOptions rasterOptions, string targetSr);
}

internal class TileDataset<TCoordinate>(ITile<TCoordinate> tile) : ITileDataset, IDisposable
	where TCoordinate : IGeoCoordinate<TCoordinate>
{
	public Dataset? Dataset { get; init; }
	public ITile<TCoordinate> Tile { get; } = tile;
	ITile ITileDataset.Tile => Tile;
	public DateOnly? LayerDate { get; init; }
	public DateOnly TileDate { get; init; }
	public byte[]? TileBytes { get; init; }
	public required string? Message { get; init; }

	public GeoTransform GetGeoTransform() => Tile.GetGeoTransform();

	public GDALWarpAppOptions GetWarpOptions(RasterOptions rasterOptions, string targetSr)
		=> rasterOptions.GetWarpOptions<TCoordinate>(targetSr);

	public void Dispose()
		=> Dataset?.Dispose();
}