using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;

namespace LibEsri.Geometry;

public class DatedRegion : GeoRegion<WebMercator>
{
	internal DatedRegion(DateOnly date, Envelope envelope, OSGeo.OGR.Geometry region)
		: base(envelope.MinX, envelope.MaxX, envelope.MinY, envelope.MaxY, region)
	{
		Date = date;
	}
	public DateOnly Date { get; }
}
