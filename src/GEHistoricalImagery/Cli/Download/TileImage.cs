using OSGeo.GDAL;

namespace GEHistoricalImagery.Cli.Download;

internal class TileImage
{
	private byte[] ImageBytes { get; }
	private int BandCount { get; }
	private int[] BandMap { get; }
	private int RasterX { get; }
	private int RasterY { get; }
	public TileImage(Dataset tile)
	{
		ArgumentNullException.ThrowIfNull(tile, nameof(tile));

		BandCount = tile.RasterCount;
		BandMap = Enumerable.Range(1, BandCount).ToArray();
		RasterX = tile.RasterXSize;
		RasterY = tile.RasterYSize;

		ImageBytes = GC.AllocateUninitializedArray<byte>(RasterX * RasterY * BandCount);
		tile.ReadRaster(0, 0, RasterX, RasterY, ImageBytes, RasterX, RasterY, BandCount, BandMap, BandCount, RasterY * BandCount, 1);
	}

	public TileImage(int width, int height, int bandCount)
	{
		BandCount = bandCount;
		BandMap = Enumerable.Range(1, BandCount).ToArray();
		RasterX = width;
		RasterY = height;
		ImageBytes = GC.AllocateUninitializedArray<byte>(RasterX * RasterY * BandCount);
	}

	public Dataset ToDataset()
	{
		using var tifDriver = Gdal.GetDriverByName("MEM");
		var memDataset = tifDriver.Create("", RasterX, RasterY, BandCount, DataType.GDT_Byte, null);
		memDataset.WriteRaster(0, 0, RasterX, RasterY, ImageBytes, RasterX, RasterY, BandCount, BandMap, BandCount, RasterY * BandCount, 1);
		return memDataset;
	}

	public byte[] GetPixel(int x, int y)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(x, nameof(x));
		ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, RasterX, nameof(x));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, RasterY, nameof(y));
		var index = (y * RasterY + x) * BandCount;
		var pixel = new byte[BandCount];
		for (int i = 0; i < BandCount; i++)
		{
			if (BandMap[i] != 0)
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

		var index = (y * RasterY + x) * BandCount;

		for (int i = 0; i < BandCount; i++)
		{
			if (BandMap[i] != 0)
				ImageBytes[index + i] = values[i];
		}
	}
}