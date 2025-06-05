namespace LibMapCommon.Geometry;

public sealed class PixelPointPoly : Polygon<PixelPointPoly, PixelPoint>
{
	public int ZoomLevel { get; }
	private Func<Wgs1984, int, PixelPoint> PointConverter { get; }

	public PixelPointPoly(Func<Wgs1984, int, PixelPoint> pointConverter, int zoomLevel, params PixelPoint[] points)
		: base(points)
	{
		PointConverter = pointConverter;
		ZoomLevel = zoomLevel;
	}
	private PixelPointPoly(Func<Wgs1984, int, PixelPoint> pointConverter, int zoomLevel, IList<Line2> edges)
		: base(edges)
	{
		PointConverter = pointConverter;
		ZoomLevel = zoomLevel;
	}

	public override PixelPointPoly ToPixelPolygon(int level)
	{
		var pixelCoords = new PixelPoint[Edges.Count];

		var pixelScale = Math.Pow(2, level - ZoomLevel);
		for (int i = 0; i < Edges.Count; i++)
		{
			var origin = Edges[i].Origin;
			pixelCoords[i] = new PixelPoint(level, origin.X * pixelScale, origin.Y * pixelScale);
		}
		return new PixelPointPoly(PointConverter, level, pixelCoords);
	}

	protected override PixelPointPoly CreateFromEdges(IList<Line2> edges) => new PixelPointPoly(PointConverter, ZoomLevel, edges);
	protected override PixelPoint GetFromWgs1984(Wgs1984 point) => PointConverter(point, ZoomLevel);
}
