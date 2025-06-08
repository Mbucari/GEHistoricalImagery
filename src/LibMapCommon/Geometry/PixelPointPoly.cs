namespace LibMapCommon.Geometry;

public class PixelPointPoly : Polygon<PixelPointPoly, PixelPoint>
{
	public int ZoomLevel { get; }

	internal PixelPointPoly(int zoomLevel, IList<Line2> edges)
		: base(0, 256 << zoomLevel, edges)
	{
		ZoomLevel = zoomLevel;
	}

	protected override PixelPointPoly CreateFromEdges(IList<Line2> edges) => new PixelPointPoly(ZoomLevel, edges);
}
