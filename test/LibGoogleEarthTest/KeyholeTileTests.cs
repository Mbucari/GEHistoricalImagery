using LibGoogleEarth;

namespace LibGoogleEarthTest;

[TestClass]
public class KeyholeTileTests
{
	private const int MAX_ROX_COL_SZ = (1 << KeyholeTile.MaxLevel) - 1;

	[DataTestMethod]
	[DataRow(0, 0, 0, "0")]
	[DataRow(0, 0, 1, "00")]
	[DataRow(0, 1, 1, "01")]
	[DataRow(1, 1, 1, "02")]
	[DataRow(1, 0, 1, "03")]
	[DataRow((1 << 10) - 1, 0, 10, "03333333333")]
	[DataRow(0, (1 << 10) - 1, 10, "01111111111")]
	[DataRow((1 << 10) - 1, (1 << 10) - 1, 10, "02222222222")]
	[DataRow(MAX_ROX_COL_SZ, 0, KeyholeTile.MaxLevel, "0333333333333333333333333333333")]
	[DataRow(0, MAX_ROX_COL_SZ, KeyholeTile.MaxLevel, "0111111111111111111111111111111")]
	[DataRow(MAX_ROX_COL_SZ, MAX_ROX_COL_SZ, KeyholeTile.MaxLevel, "0222222222222222222222222222222")]

	/*
	   c0    c1
	|-----|-----|
r1	|  3  |  2  |
	|-----|-----|
r0	|  0  |  1  |
	|-----|-----|
	*/
	[DataRow(0b0111011011, 0b1101101101, 10, "01232132132")]

	public void ValidTiles(int row, int col, int zoom, string qtp)
	{
		var tile = new KeyholeTile(row, col, zoom);
		Assert.AreEqual(qtp, tile.Path);
		Assert.AreEqual(zoom, tile.Level);
	}

	[DataTestMethod]
	[DataRow(0, 0, -1)]
	[DataRow(0, -1, 0)]
	[DataRow(-1, 0, 0)]
	[DataRow(1 << 10, 0, 10)]
	[DataRow(0, 1 << 10, 10)]
	[DataRow(0, 0, 31)]

	public void TilesOutOfRange(int row, int col, int zoom)
	{
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => new KeyholeTile(row, col, zoom));
	}
}
