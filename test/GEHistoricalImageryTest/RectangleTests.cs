using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;

namespace GEHistoricalImageryTest;

[TestClass]
public class RectangleTests
{
	[TestMethod]
	public void WrapAroundRectangle()
	{
		var ll = new Wgs1984(-90, 0);
		var ur = new Wgs1984(90, -0.0000001);

		var rec = GeoRegion<Wgs1984>.Create(new Wgs1984(-90, 0), new Wgs1984(89.99999999, 0), new Wgs1984(89.99999999, 359.99999999), new Wgs1984(-90, 359.99999999));
		for (int i = 2; i <= KeyholeTile.MaxLevel; i++)
		{
			var numTiles = 1 << i;
			var stats = rec.GetRectangularRegionStats<KeyholeTile>(i);
			Assert.AreEqual(numTiles, stats.NumColumns);
			Assert.AreEqual(numTiles, stats.NumRows);
		}

		rec = GeoRegion<Wgs1984>.Create(new Wgs1984(-90, -0.000000001), new Wgs1984(89.99999999, -0.000000001), new Wgs1984(89.99999999, -360), new Wgs1984(-90, -360));
		for (int i = 2; i <= KeyholeTile.MaxLevel; i++)
		{
			var numTiles = 1 << i;
			var stats = rec.GetRectangularRegionStats<KeyholeTile>(i);
			Assert.AreEqual(numTiles, stats.NumColumns);
			Assert.AreEqual(numTiles, stats.NumRows);
		}
	}

	[DataTestMethod]
	//Valid web mercater coordinates, but invalid geographic coordinates
	[DataRow(0, 0, 180, 1)]
	[DataRow(180, 0, 0, 1)]
	[DataRow(0, 0, 90 + 0.000000001, 1)]
	[DataRow(90 + 0.000000001, 0, 0, 1)]
	public void InvalidCoordinates(double ll_lat, double ll_long, double ur_lat, double ur_long)
	{
		var ll = new Wgs1984(ll_lat, ll_long);
		var ul = new Wgs1984(ur_lat, ll_long);
		var ur = new Wgs1984(ur_lat, ur_long);
		var lr = new Wgs1984(ll_lat, ur_long);
		Assert.IsFalse(ll.IsValidGeographicCoordinate && ul.IsValidGeographicCoordinate && ur.IsValidGeographicCoordinate && lr.IsValidGeographicCoordinate);
	}

	[DataTestMethod]
	//Invalid regions
	[DataRow(0, 0, 0, 0)] // zero area
	[DataRow(0, -10, 0, 9)] //zero height
	[DataRow(-10, 0, 9, 0)] //zero width
	public void InvalidRectangles(double ll_lat, double ll_long, double ur_lat, double ur_long)
	{
		var ll = new Wgs1984(ll_lat, ll_long);
		var ul = new Wgs1984(ur_lat, ll_long);
		var ur = new Wgs1984(ur_lat, ur_long);
		var lr = new Wgs1984(ll_lat, ur_long);
		Assert.ThrowsException<InvalidOperationException>(() => new GeoPolygon<Wgs1984>(ll, ul, ur, lr));
	}
}
