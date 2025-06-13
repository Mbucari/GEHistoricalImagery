using LibGoogleEarth;
using LibMapCommon;

namespace LibGoogleEarthTest;

[TestClass]
public class CoordinateTests
{
	[DataTestMethod]
	[DataRow(0, 0, 0, 0, 0)]
	[DataRow(0, 0, 1, 1, 1)]
	[DataRow(0, 180, 1, 1, 1)]
	[DataRow(0.00000001, 0, 1, 1, 1)]
	[DataRow(-0.00000001, 0, 1, 0, 1)]
	[DataRow(90, 0, 1, 1, 1)]
	[DataRow(0, 0.00000001, 1, 1, 1)]
	[DataRow(0, -0.00000001, 1, 1, 0)]
	public void GetTile(double lat, double lon, int zoom, int expectedRow, int expectedColumn)
	{
		var c = new Wgs1984(lat, lon);
		var tile = KeyholeTile.GetTile(c, zoom);

		Assert.AreEqual(zoom, tile.Level);
		Assert.AreEqual(expectedRow, tile.Row);
		Assert.AreEqual(expectedColumn, tile.Column);
	}

	[DataTestMethod]
	[DataRow(-1)]
	[DataRow(KeyholeTile.MaxLevel + 1)]
	public void GetTileFail(int zoom)
	{
		var c = new Wgs1984(0, 0);
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => KeyholeTile.GetTile(c, zoom));
	}

	[DataTestMethod]
	[DataRow(200, 0)]
	[DataRow(0, 400)]
	[DataRow(180.00000001, 0)]
	[DataRow(0, 360.00000001)]
	public void InvalidCoordinate(double lat, double lon)
	{
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Wgs1984(lat, lon));
	}

	[DataTestMethod]
	//Valid web mercater coordinates, but invalid geographic coordinates
	[DataRow(180, 0)]
	[DataRow(90.00000001, 0)]
	public void InvalidGeographicCoordinate(double lat, double lon)
	{
		Assert.IsFalse(new Wgs1984(lat, lon).IsValidGeographicCoordinate);
	}
}
