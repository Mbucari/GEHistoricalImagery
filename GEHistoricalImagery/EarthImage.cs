using OSGeo.GDAL;
using OSGeo.OSR;

namespace GoogleEarthImageDownload;

internal class EarthImage : IDisposable
{
	private const int TILE_SZ = 256;
	const string WGS_1984_WKT = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";
	public int Width { get; }
	public int Height { get; }


	private readonly Dataset TempDataset;

	public Tile UpperLeft { get; }

	private int X_Left;
	private int Y_Top;

	static EarthImage()
	{
		GdalConfiguration2.ConfigureGdal();
		Gdal.SetCacheMax(1024 * 1024 * 100);
	}

	public EarthImage(Rectangle rectangle, int level, string? cacheFile = null)
	{
		var ll = rectangle.LowerLeft.GetTile(level);
		var ur = rectangle.UpperRight.GetTile(level);

		var pixelScale = 360d / (1 << level) / TILE_SZ;

		X_Left = (int)double.Round((rectangle.LowerLeft.Longitude - ll.LowerLeft.Longitude) / pixelScale);
		var y_Bottom = (int)double.Round((rectangle.LowerLeft.Latitude - ll.LowerLeft.Latitude) / pixelScale);

		var x_Right = (int)double.Round((ur.UpperRight.Longitude - rectangle.UpperRight.Longitude) / pixelScale);
		Y_Top = (int)double.Round((ur.UpperRight.Latitude - rectangle.UpperRight.Latitude) / pixelScale);

		var nRows = ur.Row - ll.Row + 1;
		var nCols = ur.Column - ll.Column + 1;

		Width = TILE_SZ * nCols - X_Left - x_Right;
		Height = TILE_SZ * nRows - Y_Top - y_Bottom;
		UpperLeft = new Tile(ur.Row, ll.Column, level);

		using var wgs = new SpatialReference(WGS_1984_WKT);

		TempDataset = CreateEmptyDataset(Width, Height, cacheFile);
		TempDataset.SetSpatialRef(wgs);
		TempDataset.SetGeoTransform(
		[
			rectangle.LowerLeft.Longitude,
			pixelScale,
			0,
			rectangle.UpperRight.Latitude,
			0,
			-pixelScale
		]);
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
		var x = (tile.Column - UpperLeft.Column) * TILE_SZ - X_Left;
		var y = (UpperLeft.Row - tile.Row) * TILE_SZ - Y_Top;

		int read_x = -int.Min(0, x);
		int write_x = int.Max(0, x);
		int size_x = int.Min(TILE_SZ - read_x, Width - x);

		int read_y = -int.Min(0, y);
		int write_y = int.Max(0, y);
		int size_y = int.Min(TILE_SZ - read_y, Height - y);

		int bandCount = image.RasterCount;
		var bandMap = Enumerable.Range(1, bandCount).ToArray();

		var buff2 = new byte[size_x * size_y * bandCount];
		image.ReadRaster(read_x, read_y, size_x, size_y, buff2, size_x, size_y, bandCount, bandMap, bandCount, size_x * bandCount, 1);
		TempDataset.WriteRaster(write_x, write_y, size_x, size_y, buff2, size_x, size_y, bandCount, bandMap, bandCount, size_x * bandCount, 1);
	}

	public void Save(string path, string? outSR, Action<double> progress, int cpuCount, double scale, double offsetX, double offsetY)
	{
		TempDataset?.FlushCache();

		Dataset saved;

		if (outSR != null)
		{
			var parameters = new string[]
			{
				"-multi",
				"-wo", $"NUM_THREADS={cpuCount}",
				"-of", "GTiff",
				"-ot", "Byte",
				"-wo", "OPTIMIZE_SIZE=TRUE",
				"-co", "COMPRESS=JPEG",
				"-co", "PHOTOMETRIC=YCBCR",
				"-r", "bilinear",
				"-s_srs", WGS_1984_WKT,
				"-t_srs", outSR
			};
			using var options = new GDALWarpAppOptions(parameters);
			saved = Gdal.Warp(path, new[] { TempDataset }, options, reportProgress, null);
		}
		else
		{
			var parameters = new string[]
			{
				"COMPRESS=JPEG",
				"PHOTOMETRIC=YCBCR",
				$"NUM_THREADS={cpuCount}"
			};
			using var tifDriver = Gdal.GetDriverByName("GTiff");
			saved = tifDriver.CreateCopy(path, TempDataset, 1, parameters, reportProgress, null);
		}

		using (saved)
		{
			var geoTransform = new double[6];
			saved.GetGeoTransform(geoTransform);

			//offset
			geoTransform[0] += offsetX;
			geoTransform[3] += offsetY;

			//scale
			geoTransform[0] *= scale;
			geoTransform[1] *= scale;
			geoTransform[3] *= scale;
			geoTransform[5] *= scale;

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

[Flags]
public enum GDAL_OF : uint
{
	READONLY = 0,
	ALL = 0,
	UPDATE = 1,
	RASTER = 2,
	VECTOR = 4,
	GNM = 8,
	MULTIDIM_RASTER = 0x10,
	SHARED = 0x20,
	VERBOSE_ERROR = 0x40,
	INTERNAL = 0x80,
	ARRAY_BLOCK_ACCESS = 0x100,
	HASHSET_BLOCK_ACCESS = 0x100
}