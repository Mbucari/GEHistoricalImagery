using OSGeo.GDAL;
using System.Buffers;

namespace GEHistoricalImagery.Cli.Download;

internal class TileImage : IDisposable
{
	private byte[] ImageBytes { get; }
	private int BandCount { get; }
	private int RasterX { get; }
	private int RasterY { get; }
	private byte[]? MaskBand;
	private bool m_Disposed;
	public TileImage(Dataset tile)
	{
		ArgumentNullException.ThrowIfNull(tile, nameof(tile));
		BandCount = tile.RasterCount;
		RasterX = tile.RasterXSize;
		RasterY = tile.RasterYSize;		
		ImageBytes = ArrayPool<byte>.Shared.Rent(RasterX * RasterY * BandCount);
		tile.ReadRaster(0, 0, RasterX, RasterY, ImageBytes, RasterX, RasterY, BandCount, null, BandCount, RasterX * BandCount, 1);
	}

	public TileImage(int width, int height, int bandCount)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width, nameof(width));
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bandCount, nameof(bandCount));
		BandCount = bandCount;
		RasterX = width;
		RasterY = height;
		ImageBytes = ArrayPool<byte>.Shared.Rent(RasterX * RasterY * BandCount);
	}

	public Dataset ToDataset()
	{
		using var tifDriver = Gdal.GetDriverByName("MEM");
		var memDataset = tifDriver.Create("", RasterX, RasterY, BandCount, DataType.GDT_Byte, null);
		memDataset.WriteRaster(0, 0, RasterX, RasterY, ImageBytes, RasterX, RasterY, BandCount, null, BandCount, RasterX * BandCount, 1);
		if (MaskBand is not null)
		{
			memDataset.CreateMaskBand(GdalConst.GMF_PER_DATASET);
			for (int i = 1; i <= BandCount; i++)
			{
				using var band = memDataset.GetRasterBand(i);
				using var mask = band.GetMaskBand();
				mask.WriteRaster(0, 0, RasterX, RasterY, MaskBand, RasterX, RasterY, 1, RasterX);
			}
		}
		return memDataset;
	}

	public byte[] GetPixel(int x, int y)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(x, nameof(x));
		ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, RasterX, nameof(x));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, RasterY, nameof(y));
		var index = (y * RasterX + x) * BandCount;
		var pixel = new byte[BandCount];
		for (int i = 0; i < BandCount; i++)
		{
			pixel[i] = ImageBytes[index + i];
		}
		return pixel;
	}

	public void SetPixel(int x, int y, byte[] values)
	{
		ArgumentNullException.ThrowIfNull(values, nameof(values));
		ArgumentOutOfRangeException.ThrowIfNegative(x, nameof(x));
		ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, RasterX, nameof(x));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, RasterY, nameof(y));
		if (values.Length != BandCount)
			throw new ArgumentOutOfRangeException(nameof(values));

		var index = (y * RasterX + x) * BandCount;

		for (int i = 0; i < BandCount; i++)
		{
			ImageBytes[index + i] = values[i];
		}
	}

	public void SetNoData(int x, int y)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(x, nameof(x));
		ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, RasterX, nameof(x));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, RasterY, nameof(y));

		if (MaskBand is null)
		{
			MaskBand = ArrayPool<byte>.Shared.Rent(RasterX * RasterY);
			new Span<byte>(MaskBand).Fill(255);
		}
		
		var index = (y * RasterX + x) * BandCount;
		for (int i = 0; i < BandCount; i++)
		{
			ImageBytes[index + i] = 0;
		}
		MaskBand[y * RasterX + x] = 0;
	}

	~TileImage() => Dispose();
	public void Dispose()
	{
		if (!Interlocked.CompareExchange(ref m_Disposed, true, false))
		{
			ArrayPool<byte>.Shared.Return(ImageBytes);
			if (MaskBand is not null)
				ArrayPool<byte>.Shared.Return(MaskBand);
			GC.SuppressFinalize(this);
		}
	}
}