using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;

namespace LibEsri.Geometry;

public class DatedRegion(DateOnly date, Envelope envelope, OSGeo.OGR.Geometry region)
	: GeoRegion<WebMercator>(envelope.MinX, envelope.MaxX, envelope.MinY, envelope.MaxY, region)
{
	public DateOnly Date { get; } = date;
}
