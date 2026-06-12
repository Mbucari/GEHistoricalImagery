using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;

namespace LibEsri.Geometry;

public class DatedRegion : DatedRegion<WebMercator>
{
	internal void MarkComplete() => IsComplete = true;
	public Layer Layer { get; }
	internal DatedRegion(Layer layer, DateOnly date, Envelope envelope, OSGeo.OGR.Geometry region)
		: base(date, envelope.MinX, envelope.MaxX, envelope.MinY, envelope.MaxY, region)
	{
		Layer = layer;
	}
}
