using LibMapCommon;
using LibMapCommon.Geometry;

namespace LibGoogleEarth.Geometry;

public class DatedRegion : DatedRegion<Wgs1984>
{
	public int TileCount { get; }
	public void MarkComplete() => IsComplete = true;
	internal DatedRegion(int tileCount, DateOnly date, double leftmostX, double rightmostX, double minY, double maxY, OSGeo.OGR.Geometry region)
		: base(date, leftmostX, rightmostX, minY, maxY, region)
	{
		TileCount = tileCount;
	}

	// To avoid returning true for tiles which are outside the GeoRegion but still intersect (touch) the region,
	// we check if the center of the tile is contained in the region. This works because DatedRegions for keyhole
	// are built from tile rectangles, so if a tile's center is in the region then the whole tile is in the region.
	public override bool ContainsTile<TTile>(TTile tile)
	{
		var center = tile.Center;
		using var pointGeo = new OSGeo.OGR.Geometry(OSGeo.OGR.wkbGeometryType.wkbPoint);
		pointGeo.AddPoint_2D(center.X, center.Y);
		return pointGeo.Within(Region);
	}

	public DatedRegion MergePolygons()
		=> new DatedRegion(TileCount, Date, LeftMostX, RightMostX, MinY, MaxY, Region.UnionCascaded())
		{
			IsComplete = IsComplete
		};
}
