using LibGoogleEarth;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace GEHistoricalImagery;

internal class EarthImage : IDisposable
{
	private const int TILE_SZ = 256;
	private const string WGS_1984_WKT = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";
	public int Width { get; }
	public int Height { get; }

	private readonly Dataset TempDataset;

	/// <summary> The x-coordinate of the output dataset's top-left corner relative to global pixel space </summary>
	private readonly int RasterX;

	/// <summary> The y-coordinate of the output dataset's top-left corner relative to global pixel space  </summary>
	private readonly int RasterY;

	static EarthImage()
	{
		GdalConfiguration.ConfigureGdal();
		Gdal.SetCacheMax(1024 * 1024 * 300);
	}

	public EarthImage(Rectangle rectangle, int level, string? cacheFile = null)
	{
		var pixelScale = 360d / (1 << level) / TILE_SZ;

		RasterX = (int)double.Round((rectangle.LowerLeft.Longitude + 180) / pixelScale);
		//Web Mercater is a square of 360d x 360d, but only the middle 180d height is used.
		RasterY = (int)double.Round((180 - rectangle.UpperRight.Latitude) / pixelScale);

		var heightDeg = rectangle.UpperRight.Latitude - rectangle.LowerLeft.Latitude;
		var widthDeg = rectangle.UpperRight.Longitude - rectangle.LowerLeft.Longitude;
		//Allow wrapping around 180/-180
		if (widthDeg < 0)
			widthDeg += 360;

		Width = (int)double.Round(widthDeg / pixelScale);
		Height = (int)double.Round(heightDeg / pixelScale);

		using var wgs = new SpatialReference(WGS_1984_WKT);

		TempDataset = CreateEmptyDataset(Width, Height, cacheFile);
		TempDataset.SetSpatialRef(wgs);
		TempDataset.SetGeoTransform(new GeoTransform
		{
			UpperLeft_X = rectangle.LowerLeft.Longitude,
			UpperLeft_Y = rectangle.UpperRight.Latitude,
			PixelWidth = pixelScale,
			PixelHeight = -pixelScale
		});
	}

	private static Dataset CreateEmptyDataset(int width, int height, string? fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			using var tifDriver = Gdal.GetDriverByName("MEM");
			return tifDriver.Create("", width, height, 3, DataType.GDT_Byte, null);
		}
		else
		{
			using var tifDriver = Gdal.GetDriverByName("GTiff");
			return tifDriver.Create(fileName, width, height, 3, DataType.GDT_Byte, null);
		}
	}

	public void AddTile(Tile tile, Dataset image)
	{
		//Tile's global pixel coordinates of the tile's top-left corner.
		var gpx_x = tile.Column * TILE_SZ;
		//Rows are from bottom-to-top, but Gdal datasets are top-to-bottom.
		var gpx_y = ((1 << tile.Level) - tile.Row - 1) * TILE_SZ;

		//The tile is entirely to the left of the region, so wrap around the globe.
		if (gpx_x + TILE_SZ < RasterX)
			gpx_x += (1 << tile.Level) * TILE_SZ;

		//Pixel coordinate to read the tile's data, relative to the tile's top-left corner.
		int read_x = int.Max(0, RasterX - gpx_x);
		int read_y = int.Max(0, RasterY - gpx_y);

		//Pixel coordinate to write the data, relative to output dataset's top-left corner.
		int write_x = gpx_x + read_x - RasterX;
		int write_y = gpx_y + read_y - RasterY;

		//Raster dimensions to read/write
		int size_x = int.Min(TILE_SZ - read_x, Width - write_x);
		int size_y = int.Min(TILE_SZ - read_y, Height - write_y);

		if (size_x <= 0 || size_y <= 0)
			return;

		int bandCount = image.RasterCount;
		var bandMap = Enumerable.Range(1, bandCount).ToArray();

		var buff2 = GC.AllocateUninitializedArray<byte>(size_x * size_y * bandCount);
		image.ReadRaster(read_x, read_y, size_x, size_y, buff2, size_x, size_y, bandCount, bandMap, bandCount, size_x * bandCount, 1);
		TempDataset.WriteRaster(write_x, write_y, size_x, size_y, buff2, size_x, size_y, bandCount, bandMap, bandCount, size_x * bandCount, 1);
	}

	public void Save(string path, string? outSR, Action<double> progress, int cpuCount, double scale, double offsetX, double offsetY, bool scaleFirst)
	{
		TempDataset?.FlushCache();

		Dataset saved;

		if (outSR != null)
		{
			string[] parameters =
			[
				"-multi",
				"-wo", $"NUM_THREADS={cpuCount}",
				"-of", "GTiff",
				"-ot", "Byte",
				"-wo", "OPTIMIZE_SIZE=TRUE",
				"-co", "COMPRESS=JPEG",
				"-co", "PHOTOMETRIC=YCBCR",
				"-co", "TILED=TRUE",
				"-r", "bilinear",
				"-s_srs", WGS_1984_WKT,
				"-t_srs", outSR
			];
			using var options = new GDALWarpAppOptions(parameters);
			saved = Gdal.Warp(path, [TempDataset], options, reportProgress, null);
		}
		else
		{
			string[] parameters =
			[
				"COMPRESS=JPEG",
				"PHOTOMETRIC=YCBCR",
				"TILED=TRUE",
				$"NUM_THREADS={cpuCount}"
			];
			using var tifDriver = Gdal.GetDriverByName("GTiff");
			saved = tifDriver.CreateCopy(path, TempDataset, 1, parameters, reportProgress, null);
		}

		using (saved)
		{
			var geoTransform = saved.GetGeoTransform();

			if (scaleFirst)
				geoTransform.Scale(scale);

			geoTransform.Translate(offsetX, offsetY);

			if (!scaleFirst)
				geoTransform.Scale(scale);

			saved.SetGeoTransform(geoTransform);
			saved.FlushCache();
		}

		int reportProgress(double Complete, IntPtr Message, IntPtr Data)
		{
			progress(Complete);
			return 1;
		}
	}

	public void Dispose()
	{
		TempDataset?.FlushCache();
		TempDataset?.Dispose();
	}
}
