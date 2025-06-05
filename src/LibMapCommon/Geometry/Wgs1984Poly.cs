namespace LibMapCommon.Geometry;

public sealed class Wgs1984Poly : Polygon<Wgs1984Poly, Wgs1984>
{
	public Wgs1984Poly(params Wgs1984[] coords)
		: base(coords) { }
	private Wgs1984Poly(IList<Line2> edges)
		: base(edges) { }

	public WebMercatorPoly ToWebMercator()
		=> new WebMercatorPoly(Edges.Select(e => new Wgs1984(e.Origin.Y, e.Origin.X).ToWebMercator()));

	protected override Wgs1984Poly CreateFromEdges(IList<Line2> edges) => new Wgs1984Poly(edges);

	public Rectangle GetBoundingRectangle()
		=> new Rectangle(new Wgs1984(MinY, MinX), new Wgs1984(MaxY, MaxX));

	public override PixelPointPoly ToPixelPolygon(int level)
	{
		var pixelCoords = new PixelPoint[Edges.Count];
		for (int i = 0; i < Edges.Count; i++)
		{
			var origin = Edges[i].Origin;
			var vertex = new Wgs1984(origin.Y, origin.X);
			pixelCoords[i] = vertex.GetGlobalPixelCoordinate(level);
		}
		return new PixelPointPoly((p, z) => p.GetGlobalPixelCoordinate(z), level, pixelCoords);
	}

	/// <summary>
	/// Gets the number of tiles required to cover this <see cref="Rectangle"/>
	/// </summary>
	/// <param name="level">The zoom level of the tiles</param>
	/// <returns>The number of tiles required to tile the <see cref="Rectangle"></returns>
	public int GetTileCount<TTile>(int level) where TTile : ITile<TTile>
		=> GetTiles<TTile>(level).Count();

	/// <summary>
	/// Enumerates the tiles covering this <see cref="Rectangle"/>
	/// 
	/// The enumeration starts at the lower-left corner, proceeds left-to-right, then bottom-to-top.
	/// </summary>
	/// <param name="level">The zoom level of the tiles</param>
	/// <returns>The <see cref="KeyholeTile"/> enumeration</returns>
	public IEnumerable<TTile> GetTiles<TTile>(int level) where TTile : ITile<TTile>
		=> GetBoundingRectangle()
		.GetTiles<TTile>(level)
		.Where(t => ContainsPoint(t.LowerLeft) ||
				ContainsPoint(t.LowerRight) ||
				ContainsPoint(t.UpperLeft) ||
				ContainsPoint(t.UpperRight) ||
				TileOnBroder(t));

	protected override Wgs1984 GetFromWgs1984(Wgs1984 point) => point;
}