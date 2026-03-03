using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;

namespace LibGoogleEarthTest;

[TestClass]
public class CoordinateTests
{
	[TestMethod]
	public void TriangulateEarClip()
	{
		var p = new GeoPolygon<Wgs1984>([
			new(3,1),
			new(5,2),
			new(2,5),
			new(2,4),
			new(-1,3.5),
			new(2,3),
			new(3,2),
			]);

		//Test the tile enumeration
		Assert.HasCount(9, p.GetTiles<KeyholeTile>(8));
		//Test the triangulation
		var polygon = p.TriangulatePolygon();
		Assert.IsNotNull(polygon);
		Assert.HasCount(p.Edges.Count - 2, polygon);
	}

	[TestMethod]
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

	[TestMethod]
	[DataRow(-1)]
	[DataRow(KeyholeTile.MaxLevel + 1)]
	public void GetTileFail(int zoom)
	{
		var c = new Wgs1984(0, 0);
		Assert.Throws<ArgumentOutOfRangeException>(() => KeyholeTile.GetTile(c, zoom));
	}

	[TestMethod]
	[DataRow(200, 0)]
	[DataRow(0, 400)]
	[DataRow(180.00000001, 0)]
	[DataRow(0, 360.00000001)]
	public void InvalidCoordinate(double lat, double lon)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new Wgs1984(lat, lon));
	}

	[TestMethod]
	//Valid web mercater coordinates, but invalid geographic coordinates
	[DataRow(180, 0)]
	[DataRow(90.00000001, 0)]
	public void InvalidGeographicCoordinate(double lat, double lon)
	{
		Assert.IsFalse(new Wgs1984(lat, lon).IsValidGeographicCoordinate);
	}
}
