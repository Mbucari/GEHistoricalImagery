using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OSR;

namespace OSGeo.OGR;

public static class OgrExtensions
{
	public static Geometry GetPolygon<TCoordinate>(this IGeoTile<TCoordinate> tile)
		where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		return MakePolygon(tile.UpperLeft, tile.LowerLeft, tile.LowerRight, tile.UpperRight);
	}

	public static Geometry MakeRectangle<TCoordinate>(TCoordinate lowerLeft, double width, double height) where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		var upperLeft = TCoordinate.Create(lowerLeft.X, lowerLeft.Y + height);
		var upperRight = TCoordinate.Create(lowerLeft.X + width, lowerLeft.Y + height);
		var lowerRight = TCoordinate.Create(lowerLeft.X + width, lowerLeft.Y);
		return MakePolygon(lowerLeft, upperLeft, upperRight, lowerRight);
	}

	public static Geometry MakePolygon<TCoordinate>(params TCoordinate[] coordinates) where TCoordinate : IGeoCoordinate
	{
		if (coordinates.Length < 3)
			throw new ArgumentException("At least 3 coordinates are required to create a polygon.", nameof(coordinates));
		using Geometry ring = new Geometry(wkbGeometryType.wkbLinearRing);
		foreach (TCoordinate c in coordinates)
		{
			ring.AddPoint_2D(c.X, c.Y);
		}
		ring.CloseRings();
		Geometry polygon = new Geometry(wkbGeometryType.wkbPolygon);
		polygon.AddGeometryDirectly(ring);
		if (!polygon.IsValid())
		{
			var gValid = polygon.MakeValid(["MODE=STRUCTURE"]);
			if (gValid is null || !gValid.IsValid())
			{
				gValid?.Dispose();
				throw new InvalidOperationException("The provided coordinates do not form a valid polygon.");
			}
			polygon.Dispose();
			polygon = gValid;
		}

		using SpatialReference sr = new SpatialReference(null);
		sr.Import<TCoordinate>();
		polygon.AssignSpatialReference(sr);
		return polygon;
	}
	public static void Import<TCoordinate>(this SpatialReference sr) where TCoordinate : IGeoCoordinate
	{
		sr.ImportFromEPSG(TCoordinate.EpsgNumber);
		sr.ApplyAxisMap();
	}
	public static void ApplyAxisMap(this SpatialReference sr)
	{
		string target = sr.IsProjected() == 0 ? "GEOGCS" : "PROJCS";
		int[] axisMap = new int[sr.GetAxesCount()];
		for (int i = 0; i < axisMap.Length; i++)
			axisMap[i] = sr.GetAxisOrientation(target, i).GetAxisIndex();
		sr.SetDataAxisToSRSAxisMapping(axisMap.Length, axisMap);
	}
	private static int GetAxisIndex(this AxisOrientation axis) => axis switch
	{
		AxisOrientation.OAO_East or AxisOrientation.OAO_West => 1,
		AxisOrientation.OAO_North or AxisOrientation.OAO_South => 2,
		AxisOrientation.OAO_Up or AxisOrientation.OAO_Down => 3,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
	};

	public static void AddPoygons(this Geometry to, Geometry polygons)
	{
		var gtype = polygons.GetGeometryType();
		if (gtype is wkbGeometryType.wkbPolygon)
			to.AddGeometry(polygons);
		var gcount = polygons.GetGeometryCount();
		for (int i = 0; i < gcount; i++)
		{
			AddPoygons(to, polygons.GetGeometryRef(i));
		}
	}

	public static void Translate(this Geometry geom, double x, double y)
	{
		var gcount = geom.GetGeometryCount();
		for (int i = 0; i < gcount; i++)
		{
			var g = geom.GetGeometryRef(i);
			var pcount = g.GetPointCount();
			if (pcount != 0)
			{
				double[] point = new double[2];
				for (int j = 0; j < pcount; j++)
				{
					g.GetPoint(j, point);
					g.SetPoint_2D(j, point[0] + x, point[1] + y);
				}
			}
			else if (!g.IsEmpty())
				Translate(g, x, y);
		}
	}

	public static TileStats GetRectangularRegionStats<TTile, TCoordinate>(this Geometry geometry, int level) where TTile : IGeoTile<TTile, TCoordinate>
		where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		using var envelope = new Envelope();
		geometry.GetEnvelope(envelope);
		var llCoord = TCoordinate.Create(envelope.MinX, envelope.MinY);
		var urCoord = TCoordinate.Create(envelope.MaxX, envelope.MaxY);
		var ll = TTile.GetTile(llCoord, level);
		var ur = TTile.GetTile(urCoord, level);

		var (minColumn, maxColumn) = (ll.Column, ur.Column);
		var (minRow, maxRow) = ur.Row < ll.Row ? (ur.Row, ll.Row) : (ll.Row, ur.Row);

		var nColumns = Util.Mod(ur.Column - ll.Column, 1 << level) + 1;
		var nRows = maxRow - minRow + 1;

		return new TileStats(level, nColumns, nRows, minRow, maxRow, minColumn, maxColumn, nColumns * nRows);
	}

	public static void SetField(this Feature? feature, string fieldName, DateOnly date)
		=> feature?.SetField(fieldName, date.Year, date.Month, date.Day, 0, 0, 0, 100);
}
