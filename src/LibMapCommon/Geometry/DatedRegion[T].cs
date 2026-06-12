namespace LibMapCommon.Geometry;

public abstract class DatedRegion<TCoordinate> : GeoRegion<TCoordinate>
	where TCoordinate : IGeoCoordinate<TCoordinate>
{
	public DateOnly Date { get; }
	public bool IsComplete { get; protected set; }
	protected DatedRegion(DateOnly date, double leftmostX, double rightmostX, double minY, double maxY, OSGeo.OGR.Geometry region)
		: base(leftmostX, rightmostX, minY, maxY, region)
	{
		Date = date;
	}
}
