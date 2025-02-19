using LibEsri;
using LibMapCommon;
using OSGeo.GDAL;

namespace GEHistoricalImagery;

internal class EsriImage : EarthImage
{
	protected override int EpsgNumber => 3857;

	public EsriImage(Rectangle rectangle, int level, string? cacheFile = null)
	{
		double globalPixels = TILE_SZ * (1L << level);

		var upperLeft = rectangle.GetUpperLeft().ToWebMercator();
		(RasterX, RasterY) = upperLeft.GetGlobalPixelCoordinate(level);
		(var lrX, var lrY) = rectangle.GetLowerRight().ToWebMercator().GetGlobalPixelCoordinate(level);

		Width = lrX - RasterX;
		Height = lrY - RasterY;
		//Allow wrapping around 180/-180
		if (Width < 0)
			Width = (int)(Width + globalPixels);

		var pixelScale = WebCoordinate.Equator / globalPixels;
		var transform = new GeoTransform
		{
			UpperLeft_X = upperLeft.X,
			UpperLeft_Y = upperLeft.Y,
			PixelWidth = pixelScale,
			PixelHeight = -pixelScale
		};

		TempDataset = CreateEmptyDataset(cacheFile, transform);
	}

	protected override int GetTopGlobalPixel(ITile tile)
	{
		//Both Rows and Gdal datasets are top-to-bottom.
		var gpx_y = tile.Row * TILE_SZ;
		return gpx_y;
	}
}
