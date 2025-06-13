namespace LibMapCommon.Geometry;

public class PixelRegion : Region<PixelPolygon, PixelPoint>
{
	public int ZoomLevel { get; }
	internal PixelRegion(int zoomLevel, double leftmostX, double rightmostX, PixelPolygon[] polygons)
		: base(leftmostX, rightmostX, polygons)
	{
		ZoomLevel = zoomLevel;
	}
}

