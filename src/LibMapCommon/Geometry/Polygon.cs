namespace LibMapCommon.Geometry;

public abstract class Polygon<TPoly, TCoordinate>
	where TPoly : Polygon<TPoly, TCoordinate>
	where TCoordinate : ICoordinate
{
	public double MinY { get; }
	public double MaxY { get; }
	public double MinX { get; }
	public double MaxX { get; }
	public IList<Line2> Edges { get; }

	protected Polygon(double smallestX, double largestX, params TCoordinate[] coords)
	{
		Edges = CreateEdges(coords);
		(MinX, MaxX, MinY, MaxY) = Validate(smallestX, largestX);
	}

	protected Polygon(double smallestX, double largestX, IList<Line2> edges)
	{
		Edges = edges;
		(MinX, MaxX, MinY, MaxY) = Validate(smallestX, largestX);
	}

	private (double minX, double maxX, double minY, double maxY) Validate(double smallestX, double largestX)
	{
		if (Edges.Count < 3)
			throw new ArgumentException("Polygon must contain at least three edges");

		double minY = Edges.Select(v => v.Origin.Y).Min();
		double maxY = Edges.Select(v => v.Origin.Y).Max();
		double minX = Edges.Select(v => v.Origin.X).Min();
		double maxX = Edges.Select(v => v.Origin.X).Max();

		if (minX < smallestX)
			throw new InvalidOperationException($"Smallest X value '{minX}' is smaller than the minimum allowed value of '{smallestX}'");

		if (maxX > largestX)
			throw new InvalidOperationException($"Largest X value '{maxX}' is larger than the maximum allowed value of '{largestX}'");

		if (minY == maxY)
			throw new InvalidOperationException("Polygon cannot have zero height");

		if (minX == maxX)
			throw new InvalidOperationException("Polygon cannot have zero width");

		return (minX, maxX, minY, maxY);
	}

	private IList<Line2> CreateEdges<T>(IList<T> coords) where T : ICoordinate
	{
		var edges = new List<Line2>(coords.Count);
		for (int i = 0; i < coords.Count; i++)
		{
			var origin = coords[i];
			var next = coords[(i + 1) % coords.Count];
			var newEdge = LineFrom(origin, next);
			edges.Add(newEdge);
		}
		return edges;
	}

	private Line2 LineFrom<T>(T origin, T destination) where T : ICoordinate
	{
		var dx = destination.X - origin.X;
		return new Line2(new Vector2(origin.X, origin.Y), new Vector2(dx, destination.Y - origin.Y));
	}

	protected abstract TPoly CreateFromEdges(IList<Line2> edges);

	/// <summary>
	/// Determine if the point resides inside the polygon using ray casting
	/// </summary>
	public bool ContainsPoint(TCoordinate point) => ContainsPoint(point.X, point.Y);

	/// <summary>
	/// Determine if the x,y point resides inside the polygon using ray casting
	/// </summary>
	private bool ContainsPoint(double x, double y)
	{
		if (y < MinY || y > MaxY) return false;

		var testEdge = new Line2(new Vector2(x, y), Vector2.UnitY);

		int hitCount = 0;
		foreach (var edge in Edges)
		{
			var v = edge.Intersect(testEdge, roundingDigits);

			if (v.X > 0 && v.X < 1 && v.Y > 0)
				hitCount++;
		}

		return (hitCount & 1) == 1;
	}

	private const int roundingDigits = 15;

	/// <summary>
	/// Indicates whether any edges of the supplied polygon intersect any edges of this polygon
	/// </summary>
	public bool PolygonIntersects(TPoly other)
	{
		return Edges.Any(e => other.Edges.Any(t => SegmentsIntersect(e, t)));

		static bool SegmentsIntersect(Line2 l1, Line2 l2)
		{
			var v = l1.Intersect(l2, roundingDigits);
			return v.X >= 0 && v.X < 1 && v.Y >= 0 && v.Y < 1;
		}
	}

	/// <summary>
	/// Clip this polygon 
	/// </summary>
	/// <returns>A collection of polygons which, combined, span the clipped polygon</returns>
	public TPoly[] Clip(TPoly clippingPolygon)
		=> clippingPolygon.TriangulatePolygon().Select(ClipToTriangle).OfType<TPoly>().ToArray();

	public string AutoCad => "pl " + string.Join("\r\n", Edges.Select(e => $"{e.Origin.X},{e.Origin.Y}")) + " c\r\n";

	/// <summary>
	/// Convert a polygon to a collection of triangular polygons
	/// </summary>
	public IList<TPoly> TriangulatePolygon()
	{
		//Ear clipping
		var triangles = new List<TPoly>(Edges.Count - 2);

		var poly = CreateFromEdges(Edges);
		var edges = Edges.ToList();

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
				triangles.Add(CreateFromEdges([
					LineFrom(e1.Origin, e2.Origin),
					LineFrom(e2.Origin, e3.Origin),
					LineFrom(e3.Origin, e1.Origin)]));

				ClipEdges();
			}

			#region Algorithm Function
			void ClipEdges()
			{
				edges.RemoveAt(i);
				edges[edges.IndexOf(e2)] = LineFrom(e1.Origin, e3.Origin);
				poly = CreateFromEdges(edges);
				i--;
			}
			#endregion
		}

		triangles.Add(poly);
		return triangles;
	}

	/// <summary>
	/// Sutherland–Hodgman polygon clipping algorithm.
	/// Requires the clipping polygon to be convex, so only clip with triangles.
	/// </summary>
	private TPoly? ClipToTriangle(TPoly triangle)
	{
		if (triangle.Edges.Count != 3)
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

				if (Inside(current_point))
				{
					if (!Inside(prev_point))
					{
						outputList.Add(ComputeIntersection(LineFrom(prev_point, current_point)));
					}

					outputList.Add(current_point);
				}
				else if (Inside(prev_point))
				{
					outputList.Add(ComputeIntersection(LineFrom(prev_point, current_point)));
				}
			}

			#region Algorithm Functions
			bool Inside(Vector2 point)
			=> (clipEdge.Direction.Cross(point - clipEdge.Origin) > 0) ^ clockwise;

			Vector2 ComputeIntersection(Line2 targetEdge)
			{
				var v = clipEdge.Intersect(targetEdge);
				var newX = targetEdge.Origin.X + targetEdge.Direction.X * v.Y;
				var newY = targetEdge.Origin.Y + targetEdge.Direction.Y * v.Y;
				return new Vector2(newX, newY);
			}
			#endregion
		}

		return outputList.Count < 3 ? null : triangle.CreateFromEdges(CreateEdges(outputList));
	}
}
