namespace LibMapCommon.Geometry;

public class Region<TPoly, TCoordinate>
	where TPoly : Polygon<TPoly, TCoordinate>
	where TCoordinate : ICoordinate
{
	public double MinY { get; }
	public double MaxY { get; }
	public double LeftMostX { get; }
	public double RightMostX { get; }
	public TPoly[] Polygons { get; }
	protected Region(double leftmostX, double rightmostX, TPoly[] polygons)
	{
		if (leftmostX == rightmostX)
			throw new InvalidOperationException("Left-most X and right-most X cannot be equal");
		LeftMostX = leftmostX;
		RightMostX = rightmostX;
		Polygons = polygons;
		MinY = Polygons.Select(p => p.MinY).Min();
		MaxY = Polygons.Select(p => p.MaxY).Max();
	}

	/// <summary>
	/// Determine if the point resides inside the region using ray casting
	/// </summary>
	public bool ContainsPoint(TCoordinate point) => Polygons.Any(polygon => polygon.ContainsPoint(point));

	/// <summary>
	/// Indicates whether any edges of the supplied polygon intersect any edges of this region
	/// </summary>
	public bool PolygonIntersects(TPoly other) => Polygons.Any(polygon => polygon.PolygonIntersects(other));

	/// <summary>
	/// Convert a region to a collection of triangular polygons
	/// </summary>
	public IList<TPoly> TriangulatePolygon() => Polygons.SelectMany(polygon => polygon.TriangulatePolygon()).ToArray();
}
