using LibMapCommon;
using OSGeo.GDAL;
using OSGeo.OGR;

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
	Geometry GetPolygonGeometry();
}

internal class TileDataset<TCoordinate>(IGeoTile<TCoordinate> tile) : ITileDataset, IDisposable
	where TCoordinate : IGeoCoordinate<TCoordinate>
{
	public Dataset? Dataset { get; init; }
	public IGeoTile<TCoordinate> Tile { get; } = tile;
	ITile ITileDataset.Tile => Tile;
	public DateOnly? LayerDate { get; init; }
	public DateOnly TileDate { get; init; }
	public byte[]? TileBytes { get; init; }
	public required string? Message { get; init; }

	public GeoTransform GetGeoTransform() => Tile.GetGeoTransform();

	public Geometry GetPolygonGeometry()
	{
		var ll = Tile.LowerLeft;
		var ur = Tile.UpperRight;
		return OgrExtensions.MakeRectangle(ll, ur.X - ll.X, ur.Y - ll.Y);
	}

	public GDALWarpAppOptions GetWarpOptions(RasterOptions rasterOptions, string targetSr)
		=> rasterOptions.GetWarpOptions<TCoordinate>(targetSr);

	public void Dispose()
		=> Dataset?.Dispose();
}