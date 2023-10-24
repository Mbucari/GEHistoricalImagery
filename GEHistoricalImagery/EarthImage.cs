using OSGeo.GDAL;
using OSGeo.OSR;

namespace GoogleEarthImageDownload;

internal class EarthImage : IDisposable
{
	private const int TILE_SZ = 256;
	const string WGS_1984_WKT = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";
	public int Width { get; }
	public int Height { get; }


	private readonly Dataset dataset;

	public Tile UpperLeft { get; }

	static EarthImage()
	{
		GdalConfiguration2.ConfigureGdal();
		Gdal.SetCacheMax(1024 * 1024 * 100);
	}

	public EarthImage(Rectangle rectangle, int level)
	{
		var ll = rectangle.LowerLeft.GetTile(level);
		var ur = rectangle.UpperRight.GetTile(level);

		var nRows = ur.Row - ll.Row + 1;
		var nCols = ur.Column - ll.Column + 1;

		Width = TILE_SZ * nCols;
		Height = TILE_SZ * nRows;
		UpperLeft = new Tile(ur.Row, ll.Column, level);

		using var wgs = new SpatialReference(WGS_1984_WKT);
		using var tifDriver = Gdal.GetDriverByName("MEM");

		dataset = tifDriver.Create("", Width, Height, 3, DataType.GDT_Byte, null);
		dataset.SetSpatialRef(wgs);
		dataset.SetGeoTransform(new double[]
		{
			UpperLeft.UpperLeft.Longitude,
			360d / (1 << level) / TILE_SZ,
			0,
			UpperLeft.UpperLeft.Latitude,
			0,
			-360d / (1 << level) / TILE_SZ
		});
	}

	public void AddTile(Tile tile, Dataset image)
	{
		var x = (tile.Column - UpperLeft.Column) * TILE_SZ;
		var y = (UpperLeft.Row - tile.Row) * TILE_SZ;

		var bandCount = image.RasterCount;

		var bandMap = Enumerable.Range(1, bandCount).ToArray();

		var buff2 = new byte[TILE_SZ * TILE_SZ * bandCount];
		image.ReadRaster(0, 0, TILE_SZ, TILE_SZ, buff2, TILE_SZ, TILE_SZ, bandCount, bandMap, bandCount, TILE_SZ * bandCount, 1);
		dataset.WriteRaster(x, y, TILE_SZ, TILE_SZ, buff2, TILE_SZ, TILE_SZ, bandCount, bandMap, bandCount, TILE_SZ * bandCount, 1);
	}

	public void Save(string path, string? outSR, Action<double> progress, int cpuCount)
	{
		dataset?.FlushCache();
		if (outSR != null)
		{
			using var options = new GDALWarpAppOptions(
				new string[]
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
				});
			using var _ = Gdal.Warp(path, new[] { dataset }, options, reportProgress, null);
		}
		else
		{
			using var tifDriver = Gdal.GetDriverByName("GTiff");
			using var _ = tifDriver.CreateCopy(path, dataset, 1,
				new string[]
				{
					"COMPRESS=JPEG",
					"PHOTOMETRIC=YCBCR",
					$"NUM_THREADS={cpuCount}"
				},
				reportProgress, null);
		}

		int reportProgress(double Complete, IntPtr Message, IntPtr Data)
		{
			progress(Complete);
			return 1;
		}
	}

	public void Dispose()
	{
		dataset?.FlushCache();
		dataset?.Dispose();
	}
}

[Flags]
public enum GDAL_OF_ : uint
{
	GDAL_OF_READONLY = 0,
	GDAL_OF_ALL = 0,
	GDAL_OF_UPDATE = 1,
	GDAL_OF_RASTER = 2,
	GDAL_OF_VECTOR = 4,
	GDAL_OF_GNM = 8,
	GDAL_OF_MULTIDIM_RASTER = 0x10,
	GDAL_OF_SHARED = 0x20,
	GDAL_OF_VERBOSE_ERROR = 0x40,
	GDAL_OF_INTERNAL = 0x80,
	GDAL_OF_ARRAY_BLOCK_ACCESS = 0x100,
	GDAL_OF_HASHSET_BLOCK_ACCESS = 0x100
}