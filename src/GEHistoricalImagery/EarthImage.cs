using LibMapCommon;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace GEHistoricalImagery;

internal abstract class EarthImage : IDisposable
{
	public const int TILE_SZ = 256;
	protected int Width { get; init; }
	protected int Height { get; init; }

	protected Dataset? TempDataset { get; init; }

	/// <summary> The x-coordinate of the output dataset's top-left corner relative to global pixel space </summary>
	protected int RasterX { get; init; }

	/// <summary> The y-coordinate of the output dataset's top-left corner relative to global pixel space  </summary>
	protected int RasterY { get; init; }
	protected abstract int EpsgNumber { get; }

	static EarthImage()
	{
		GdalConfiguration.ConfigureGdal();
		Gdal.SetCacheMax(1024 * 1024 * 300);
	}

	protected Dataset CreateEmptyDataset(string? fileName, GeoTransform geoTransform)
	{
		Dataset dataset;
		if (string.IsNullOrWhiteSpace(fileName))
		{
			using var tifDriver = Gdal.GetDriverByName("MEM");
			dataset = tifDriver.Create("", Width, Height, 3, DataType.GDT_Byte, null);
		}
		else
		{
			using var tifDriver = Gdal.GetDriverByName("GTiff");
			dataset = tifDriver.Create(fileName, Width, Height, 3, DataType.GDT_Byte, null);
		}

		using var sourceSr = new SpatialReference("");
		sourceSr.ImportFromEPSG(EpsgNumber);
		dataset.SetSpatialRef(sourceSr);
		dataset.SetGeoTransform(geoTransform);
		return dataset;
	}

	protected abstract int GetTopGlobalPixel(ITile tile);

	public void AddTile(ITile tile, Dataset image)
	{
		//Tile's global pixel coordinates of the tile's top-left corner.
		var gpx_x = tile.Column * TILE_SZ;
		var gpx_y = GetTopGlobalPixel(tile);

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
		TempDataset?.WriteRaster(write_x, write_y, size_x, size_y, buff2, size_x, size_y, bandCount, bandMap, bandCount, size_x * bandCount, 1);
	}

	public void Save(string path, string? outSR, Action<double> progress, int cpuCount, double scale, double offsetX, double offsetY, bool scaleFirst)
	{
		if (TempDataset == null) return;
		TempDataset.FlushCache();

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
				"-s_srs", $"EPSG:{EpsgNumber}",
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
