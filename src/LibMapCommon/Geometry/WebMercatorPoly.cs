namespace LibMapCommon.Geometry;

public sealed class WebMercatorPoly : Polygon<WebMercator>, IPolygon<WebMercatorPoly>
{
	public WebMercatorPoly(IEnumerable<WebMercator> coordinates)
		: base(coordinates.ToArray()) { }
	private WebMercatorPoly(IEnumerable<Line2> edges)
		: base(edges) { }

	public bool ContainsTile(ITile tile)
		=> ContainsPoint(tile.Center.ToWebMercator()) || TileOnBroder(tile);

	public WebMercatorPoly CreateFromEdges(IEnumerable<Line2> edges) => new WebMercatorPoly(edges);

	public override PixelPointPoly ToPixelPolygon(int level)
	{
		var pixelCoords = new PixelPoint[Edges.Length];
		for (int i = 0; i < Edges.Length; i++)
		{
			var origin = Edges[i].Origin;
			var vertex = new WebMercator(origin.X, origin.Y);
			pixelCoords[i] = vertex.GetGlobalPixelCoordinate(level);
		}
		return new PixelPointPoly((p,z) => p.ToWebMercator().GetGlobalPixelCoordinate(z), level, pixelCoords);
	}

	protected override WebMercator GetFromWgs1984(Wgs1984 point) => point.ToWebMercator();
}
