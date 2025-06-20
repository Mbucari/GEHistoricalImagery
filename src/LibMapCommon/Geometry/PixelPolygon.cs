namespace LibMapCommon.Geometry;

public class PixelPolygon : Polygon<PixelPolygon, PixelPoint>
{
	public int ZoomLevel { get; }
	private const int TILE_SZ = 256;

	internal PixelPolygon(int zoomLevel, IList<Line2> edges)
		: base(0, TILE_SZ << zoomLevel, edges)
	{
		ZoomLevel = zoomLevel;
	}

	protected override PixelPolygon CreateFromEdges(IList<Line2> edges) => new PixelPolygon(ZoomLevel, edges);
}
