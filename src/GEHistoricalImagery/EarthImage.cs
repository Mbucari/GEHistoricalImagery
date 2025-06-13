using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace GEHistoricalImagery;

internal class EarthImage<T> : IDisposable where T : IGeoCoordinate<T>
{
	public const int TILE_SZ = 256;
	protected int Width { get; init; }
	protected int Height { get; init; }

	protected Dataset? TempDataset { get; init; }

	/// <summary> The x-coordinate of the output dataset's top-left corner relative to global pixel space </summary>
	protected int RasterX { get; init; }

	/// <summary> The y-coordinate of the output dataset's top-left corner relative to global pixel space  </summary>
	protected int RasterY { get; init; }

	static EarthImage()
	{
#if LINUX
		Gdal.AllRegister();
#else
		GdalConfiguration.ConfigureGdal();
#endif
		Gdal.SetCacheMax(1024 * 1024 * 300);
	}

	public EarthImage(GeoRegion<T> region, int level, string? cacheFile = null)
	{
		long globalPixels = TILE_SZ * (1L << level);

		var pixels = region.ToPixelRegion(level);

		RasterX = pixels.LeftMostX.ToRoundedInt();
		RasterY = pixels.MinY.ToRoundedInt();

		Width = (pixels.RightMostX - pixels.LeftMostX).ToRoundedInt();
		Height = (pixels.MaxY - pixels.MinY).ToRoundedInt();
		//Allow wrapping around 180/-180
		if (Width < 0)
			Width = (int)(Width + globalPixels);

		using var sourceSr = new SpatialReference("");
		sourceSr.ImportFromEPSG(T.EpsgNumber);

		var geoTransform = new GeoTransform
		{
			UpperLeft_X = region.LeftMostX,
			UpperLeft_Y = region.MaxY,
			PixelWidth = T.Equator / globalPixels,
			PixelHeight = -T.Equator / globalPixels
		};

		TempDataset = CreateEmptyDataset(Width, Height, cacheFile);
		TempDataset.SetSpatialRef(sourceSr);
		TempDataset.SetGeoTransform(geoTransform);
	}

	public static Dataset CreateEmptyDataset(int width, int height, string? fileName)
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

	public void AddTile(ITile<T> tile, Dataset image)
	{
		//Tile's global pixel coordinates of the tile's top-left corner.
		var gpx = tile.GetTopLeftPixel();

		int gpx_x = (int)gpx.X;
		int gpx_y = (int)gpx.Y;

		//The tile is entirely to the left of the region, so wrap around the globe.
		if (gpx_x + TILE_SZ < RasterX)
			gpx_x += TILE_SZ << tile.Level;

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
		var rasterBuff = GC.AllocateUninitializedArray<byte>(size_x * size_y * bandCount);
		image.ReadRaster(read_x, read_y, size_x, size_y, rasterBuff, size_x, size_y, bandCount, bandMap, bandCount, size_x * bandCount, 1);
		TempDataset?.WriteRaster(write_x, write_y, size_x, size_y, rasterBuff, size_x, size_y, bandCount, bandMap, bandCount, size_x * bandCount, 1);
	}

	public void Save(string path, string? outSR, int cpuCount, double scale, double offsetX, double offsetY, bool scaleFirst)
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
				"-s_srs", $"EPSG:{T.EpsgNumber}",
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

			var worldFileExtension = Path.GetExtension(path) switch
			{
				".gif" or ".giff" => ".gfw",
				".jpg" or ".jpeg" => ".jgw",
				".tif" or ".tiff" => ".tfw",
				".png" => ".pgw",
				".jp2" => ".j2w",
				_ => ".worldfile"
			};

			var worldFile = Path.ChangeExtension(path, worldFileExtension);
			using var sw = new StreamWriter(worldFile);
			sw.WriteLine(geoTransform.PixelWidth);
			sw.WriteLine(geoTransform.ColumnRotation);
			sw.WriteLine(geoTransform.RowRotation);
			sw.WriteLine(geoTransform.PixelHeight);
			sw.WriteLine(geoTransform.UpperLeft_X);
			sw.WriteLine(geoTransform.UpperLeft_Y);
		}

		int reportProgress(double Complete, IntPtr Message, IntPtr Data)
		{
			var args = new ImageSaveEventArgs(Complete);
			Saving?.Invoke(this, args);
			return args.Continue ? 1 : 0;
		}
	}

	public event EventHandler<ImageSaveEventArgs>? Saving;

	public void Dispose()
	{
		TempDataset?.FlushCache();
		TempDataset?.Dispose();
	}
}

public class ImageSaveEventArgs : EventArgs
{
	public double Progress { get; }
	public bool Continue { get; } = true;

	internal ImageSaveEventArgs(double progress)
	{
		Progress = progress;
	}
}
