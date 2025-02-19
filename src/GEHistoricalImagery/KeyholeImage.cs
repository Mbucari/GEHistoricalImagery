using LibMapCommon;
using OSGeo.GDAL;

namespace GEHistoricalImagery;

internal class KeyholeImage : EarthImage
{
	protected override int EpsgNumber => 4326;

	public KeyholeImage(Rectangle rectangle, int level, string? cacheFile = null)
	{
		var pixelScale = 360d / (1 << level) / TILE_SZ;

		RasterX = (int)double.Round((rectangle.LowerLeft.Longitude + 180) / pixelScale);
		//Web Mercator is a square of 360d x 360d, but only the middle 180d height is used.
		RasterY = (int)double.Round((180 - rectangle.UpperRight.Latitude) / pixelScale);

		var heightDeg = rectangle.UpperRight.Latitude - rectangle.LowerLeft.Latitude;
		var widthDeg = rectangle.UpperRight.Longitude - rectangle.LowerLeft.Longitude;
		//Allow wrapping around 180/-180
		if (widthDeg < 0)
			widthDeg += 360;

		Width = (int)double.Round(widthDeg / pixelScale);
		Height = (int)double.Round(heightDeg / pixelScale);

		var transform = new GeoTransform
		{
			UpperLeft_X = rectangle.LowerLeft.Longitude,
			UpperLeft_Y = rectangle.UpperRight.Latitude,
			PixelWidth = pixelScale,
			PixelHeight = -pixelScale
		};

		TempDataset = CreateEmptyDataset(cacheFile, transform);
	}

	protected override int GetTopGlobalPixel(ITile tile)
	{
		//Rows are from bottom-to-top, but Gdal datasets are top-to-bottom.
		var gpx_y = ((1 << tile.Level) - tile.Row - 1) * TILE_SZ;
		return gpx_y;
	}
}
