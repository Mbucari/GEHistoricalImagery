namespace LibMapCommon.Geometry;


public interface IPolygon<T> where T : IPolygon<T>
{
	public abstract T CreateFromEdges(IEnumerable<Line2> edges);
}

public abstract class Polygon<TCoordinate> where TCoordinate : struct, ICoordinate
{
	public double MinX { get; }
	public double MinY { get; }
	public double MaxX { get; }
	public double MaxY { get; }
	public Line2[] Edges { get; }

	protected Polygon(params TCoordinate[] coords) : this(CreateEdges(coords)) { }
	protected Polygon(IEnumerable<Line2> edges)
	{
		MinX = edges.MinBy(v => v.Origin.X).Origin.X;
		MinY = edges.MinBy(v => v.Origin.Y).Origin.Y;
		MaxX = edges.MaxBy(v => v.Origin.X).Origin.X;
		MaxY = edges.MaxBy(v => v.Origin.Y).Origin.Y;
		Edges = edges.ToArray();
		if (Edges.Length < 3)
			throw new ArgumentException("Polygon must contain at least three edges");
	}

	protected abstract TCoordinate GetFromWgs1984(Wgs1984 point);

	/// <summary>
	/// Convert to the global pixel space for the current polygon's coordinate system.
	/// </summary>
	public abstract PixelPointPoly ToPixelPolygon(int level);

	private static Line2[] CreateEdges<T>(T[] coords) where T : ICoordinate
	{
		var edges = new Line2[coords.Length];
		for (int i = 0; i < coords.Length; i++)
		{
			var origin = coords[i];
			var next = coords[(i + 1) % coords.Length];
			edges[i] = LineFrom(origin.X, origin.Y, next.X, next.Y);
		}
		return edges;
	}

	protected static Line2 LineFrom(TCoordinate origin, TCoordinate destination)
		=> LineFrom(origin.X, origin.Y, destination.X, destination.Y);

	protected static Line2 LineFrom(double x1, double y1, double x2, double y2)
		=> new Line2(new Vector2(x1, y1), new Vector2(x2 - x1, y2 - y1));


	/// <summary>
	/// Determine if the point resides inside the polygon using ray casting
	/// </summary>
	public bool ContainsPoint(TCoordinate point) => ContainsPoint(point.X, point.Y);

	/// <summary>
	/// Determine if the x,y point resides inside the polygon using ray casting
	/// </summary>
	private bool ContainsPoint(double x, double y)
	{
		if (x < MinX || x > MaxX || y < MinY || y > MaxY) return false;

		var testEdge = new Line2(new Vector2(x, y), Vector2.UnitX);

		int hitCount = 0;
		foreach (var edge in Edges)
		{
			var v = edge.Intersect(testEdge);

			if (v.X > 0 && v.X < 1 && v.Y > 0)
				hitCount++;
		}

		return (hitCount & 1) == 1;
	}

	/// <summary>
	/// Indicates whether the tile intersects the polyline.
	/// </summary>
	public bool TileOnBroder(ITile tile)
	{
		var ul = GetFromWgs1984(tile.UpperLeft);
		var ur = GetFromWgs1984(tile.UpperRight);
		var ll = GetFromWgs1984(tile.LowerLeft);
		var lr = GetFromWgs1984(tile.LowerRight);

		Line2[] tileEdges = [LineFrom(ul, ur), LineFrom(ur, lr), LineFrom(lr, ll), LineFrom(ll, ul)];

		return Edges.Any(e => tileEdges.Any(t => LinesIntersect(e, t)));

		static bool LinesIntersect(Line2 l1, Line2 l2)
		{
			var v = l1.Intersect(l2);
			return v.X > 0 && v.X < 1 && v.Y > 0 && v.Y < 1;
		}
	}

	/// <summary>
	/// Clip this polygon 
	/// </summary>
	/// <returns>A collection of polygons which, combined, span the clipped polygon</returns>
	public TPoly[] Clip<TPoly>(TPoly clippingPolygon) where TPoly : Polygon<TCoordinate>, IPolygon<TPoly>
		=> TriangulatePolygon(clippingPolygon).Select(ClipToTriangle).OfType<TPoly>().ToArray();

	/// <summary>
	/// Convert a polygon to a collection of triangular polygons
	/// </summary>
	public static TPoly[] TriangulatePolygon<TPoly>(TPoly polygon) where TPoly : Polygon<TCoordinate>, IPolygon<TPoly>
	{
		//Ear clipping
		var triangles = new List<TPoly>(polygon.Edges.Length - 2);

		var poly = polygon.CreateFromEdges(polygon.Edges);
		var edges = polygon.Edges.ToList();

		for (int i = 0; edges.Count > 3; i = (i + 1) % edges.Count)
		{
			var e1 = edges[i];
			var e2 = edges[(i + 1) % edges.Count];
			var e3 = edges[(i + 2) % edges.Count];

			var v1 = e1.Direction;
			var v2 = e2.Direction;

			if (Math.Abs(v1.Dot(v2) / v1.Length / v2.Length) > 0.9999999999)
			{
				//e1 is colinear with e2 (within 0.00081 degrees)
				ClipEdges();
				continue;
			}

			var centroidX = (e1.Origin.X + e2.Origin.X + e3.Origin.X) / 3;
			var centroidY = (e1.Origin.Y + e2.Origin.Y + e3.Origin.Y) / 3;

			if (poly.ContainsPoint(centroidX, centroidY))
			{
				triangles.Add(polygon.CreateFromEdges([
					LineFrom(e1.Origin.X, e1.Origin.Y, e2.Origin.X, e2.Origin.Y),
					LineFrom(e2.Origin.X, e2.Origin.Y, e3.Origin.X, e3.Origin.Y),
					LineFrom(e3.Origin.X, e3.Origin.Y, e1.Origin.X, e1.Origin.Y)]));

				ClipEdges();
			}

			void ClipEdges()
			{
				edges.RemoveAt(i);
				edges[edges.IndexOf(e2)] = LineFrom(e1.Origin.X, e1.Origin.Y, e3.Origin.X, e3.Origin.Y);
				poly = polygon.CreateFromEdges(edges);
				i--;
			}
		}

		triangles.Add(poly);
		return triangles.ToArray();
	}

	/// <summary>
	/// Sutherland–Hodgman polygon clipping algorithm.
	/// Requires the clipping polygon to be convex, so only clip with triangles.
	/// </summary>
	private TPoly? ClipToTriangle<TPoly>(TPoly triangle) where TPoly : Polygon<TCoordinate>, IPolygon<TPoly>
	{
		if (triangle.Edges.Length != 3)
			throw new ArgumentException("Clipping polygon must be a triangle");

		//Determine triangle direction for easy Inside() checks.
		var clockwise = triangle.Edges[0].Direction.Cross(triangle.Edges[1].Direction) < 0;

		List<Vector2> outputList = Edges.Select(e => e.Origin).ToList();

		foreach (var clipEdge in triangle.Edges)
		{
			List<Vector2> inputList = outputList;
			outputList = [];

			for (int i = 0; i < inputList.Count; i++)
			{
				var prev_point = inputList[i];
				var current_point = inputList[(i + 1) % inputList.Count];

				if (Inside(clipEdge, clockwise, current_point))
				{
					if (!Inside(clipEdge, clockwise, prev_point))
					{
						outputList.Add(IntersectPoint(clipEdge, prev_point, current_point));
					}

					outputList.Add(current_point);
				}
				else if (Inside(clipEdge, clockwise, prev_point))
				{
					outputList.Add(IntersectPoint(clipEdge, prev_point, current_point));
				}
			}
		}

		return outputList.Count < 3 ? null : triangle.CreateFromEdges(CreateEdges(outputList.ToArray()));
	}

	private static Vector2 IntersectPoint(Line2 clipEdge, Vector2 prev_point, Vector2 current_point)
	{
		var targetEdge = LineFrom(prev_point.X, prev_point.Y, current_point.X, current_point.Y);
		var v = clipEdge.Intersect(targetEdge);
		var newX = targetEdge.Origin.X + targetEdge.Direction.X * v.Y;
		var newY = targetEdge.Origin.Y + targetEdge.Direction.Y * v.Y;
		return new Vector2(newX, newY);
	}

	private static bool Inside(Line2 testEdge, bool clockwise, Vector2 point)
		=> (testEdge.Direction.Cross(point - testEdge.Origin) > 0) ^ clockwise;
}
