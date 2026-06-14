using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System.Buffers;

namespace GEHistoricalImagery;

internal class EarthImage<TSource> : IDisposable where TSource : IGeoCoordinate<TSource>
{
	public const int TILE_SZ = 256;
	protected int Width { get; init; }
	protected int Height { get; init; }

	protected Dataset TempDataset { get; }

	/// <summary> The x-coordinate of the output dataset's top-left corner relative to global pixel space </summary>
	protected int RasterX { get; init; }

	/// <summary> The y-coordinate of the output dataset's top-left corner relative to global pixel space  </summary>
	protected int RasterY { get; init; }

	public EarthImage(GeoRegion<TSource> region, int level, string? cacheFile = null)
	{
		long globalPixels = TILE_SZ * (1L << level);

		var ll = TSource.Create(region.LeftMostX, region.MinY).GetGlobalPixelCoordinate(level);
		var ur = TSource.Create(region.RightMostX, region.MaxY).GetGlobalPixelCoordinate(level);

		RasterX = ll.X.ToRoundedInt();
		RasterY = ur.Y.ToRoundedInt();

		Width = (ur.X - ll.X).ToRoundedInt();
		Height = (ll.Y - ur.Y).ToRoundedInt();
		//Allow wrapping around 180/-180
		if (Width < 0)
			Width = (int)(Width + globalPixels);

		using var sourceSr = new SpatialReference(null);
		sourceSr.Import<TSource>();

		var geoTransform = new GeoTransform
		{
			UpperLeft_X = region.LeftMostX,
			UpperLeft_Y = region.MaxY,
			PixelWidth = TSource.Equator / globalPixels,
			PixelHeight = -TSource.Equator / globalPixels
		};

		TempDataset = CreateEmptyDataset(Width, Height, cacheFile);
		TempDataset.CreateMaskBand(GdalConst.GMF_PER_DATASET);
		TempDataset.SetSpatialRef(sourceSr);
		TempDataset.SetGeoTransform(geoTransform);
	}
	
	public static Dataset CreateEmptyDataset(int width, int height, string? fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			using var memDriver = Gdal.GetDriverByName("MEM");
			return memDriver.Create("", width, height, 3, DataType.GDT_Byte, null);
		}
		else
		{
			using var tifDriver = Gdal.GetDriverByName("GTiff");
			return tifDriver.Create(fileName, width, height, 3, DataType.GDT_Byte, ["BIGTIFF=YES"]);
		}
	}

	public void AddTile(ITile<TSource> tile, Dataset image)
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
		using var rasterBuff = MemoryPool<byte>.Shared.Rent(size_x * size_y);
		var bytes = rasterBuff.Memory.Span;
		unsafe
		{
			fixed (byte* p = bytes)
			{
				nint ptr = (nint)p;
				for (int i = 1; i <= bandCount; i++)
				{
					using var srcBand = image.GetRasterBand(i);
					using var destBand = TempDataset.GetRasterBand(i);
					srcBand.ReadRaster(read_x, read_y, size_x, size_y, ptr, size_x, size_y, DataType.GDT_Byte, 1, size_x);
					destBand.WriteRaster(write_x, write_y, size_x, size_y, ptr, size_x, size_y, DataType.GDT_Byte, 1, size_x);

					using var srcMask = srcBand.GetMaskBand();
					using var destMask = destBand.GetMaskBand();
					srcMask.ReadRaster(read_x, read_y, size_x, size_y, ptr, size_x, size_y, DataType.GDT_Byte, 1, size_x);
					destMask.WriteRaster(write_x, write_y, size_x, size_y, ptr, size_x, size_y, DataType.GDT_Byte, 1, size_x);
				}
			}
		}
	}

	public void Save(string path, RasterOptions rasterOptions, string? outSR, int cpuCount, double scale, double offsetX, double offsetY, bool scaleFirst)
	{
		TempDataset.FlushCache();

		Dataset saved;
		if (outSR is null)
		{
			using OSGeo.GDAL.Driver driver = Gdal.GetDriverByName(rasterOptions.DriverName);
			saved = driver.CreateCopy(path, TempDataset, 1, rasterOptions.GetCreationOptions(cpuCount), ReportProgress, null);
		}
		else
		{
			using GDALWarpAppOptions options = rasterOptions.GetWarpOptions<TSource>(outSR, cpuCount);
			saved = Gdal.Warp(path, [TempDataset], options, ReportProgress, null);
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
			geoTransform.WriteWorldFile(path);
		}
	}

	public event EventHandler<ImageSaveEventArgs>? Saving;

	private int ReportProgress(double Complete, IntPtr Message, IntPtr Data)
	{
		var args = new ImageSaveEventArgs(Complete);
		Saving?.Invoke(this, args);
		return args.Continue ? 1 : 0;
	}

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
