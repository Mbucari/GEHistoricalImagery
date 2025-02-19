using LibGoogleEarth;
using LibMapCommon;

namespace GEHistoricalImageryTest;

[TestClass]
public class RectangleTests
{
	[TestMethod]
	public void WrapAroundRectangle()
	{
		var ll = new Coordinate(-90, 0);
		var ur = new Coordinate(90, -0.0000001);

		var rec = new Rectangle(ll, ur);
		for (int i = 1; i <= KeyholeTile.MaxLevel; i++)
		{
			var numTiles = 1 << i;
			rec.GetNumRowsAndColumns<KeyholeTile>(i, out var nRows, out var nColumns);
			Assert.AreEqual(numTiles, nColumns);
			Assert.AreEqual(numTiles / 2 + 1, nRows);
		}
	}

	[DataTestMethod]
	//Valid web mercater coordinates, but invalid geographic coordinates
	[DataRow(0, 0, 180, 0)]
	[DataRow(180, 0, 0, 0)]
	[DataRow(0, 0, 90 + double.Epsilon, 0)]
	[DataRow(90 + double.Epsilon, 0, 0, 0)]
	//Invalid regions
	[DataRow(0, 0, 0, 0)] // zero area
	[DataRow(0, -10, 0, 10)] //zero height
	[DataRow(-10, 0, 10, 0)] //zero width
	[DataRow(1, -10, 0, 10)] //negative height (negative width is allowed for wrapping around 180/-180)
	public void InvalidRectangles(double ll_lat, double ll_long, double ur_lat, double ur_long)
	{
		var ll = new Coordinate(ll_lat, ll_long);
		var ur = new Coordinate(ur_lat, ur_long);
		Assert.ThrowsException<ArgumentException>(() => new Rectangle(ll, ur));
	}
}
