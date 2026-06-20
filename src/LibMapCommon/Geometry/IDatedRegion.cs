namespace LibMapCommon.Geometry;

public interface IDatedRegion : IDisposable
{
	DateOnly Date { get; }
	bool IsComplete { get; }
	int ZoomLevel { get; }
	BoolMap HasDataMap { get; }
	TileStats Stats { get; }
	OSGeo.OGR.Geometry GetMultiPolygon();
	OSGeo.OSR.SpatialReference GetSpatialReference();
}
