using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;
using OSGeo.OSR;

namespace LibGoogleEarth.Geometry;

public class DatedRegion : IDatedRegion
{
	public BoolMap HasDataMap { get; }
	public TileStats Stats { get; }
	public DateOnly Date { get; }
	public bool IsComplete => Stats.TileCount == TileCount;
	public int ZoomLevel => Stats.Zoom;
	private int TileCount { get; set; }
	private OSGeo.OGR.Geometry MultiPolygon => m_MultiPolygon ?? throw new ObjectDisposedException(nameof(DatedRegion));
	private OSGeo.OGR.Geometry? m_MultiPolygon;

	internal DatedRegion(DateOnly date, TileStats stats)
	{
		Date = date;
		m_MultiPolygon = new OSGeo.OGR.Geometry(wkbGeometryType.wkbMultiPolygon);
		using var sr = new SpatialReference(null);
		sr.Import<Wgs1984>();
		m_MultiPolygon.AssignSpatialReference(sr);
		HasDataMap = new BoolMap(stats.NumColumns, stats.NumRows);
		Stats = stats;
	}
	public OSGeo.OGR.Geometry GetMultiPolygon() => MultiPolygon.Clone();
	private readonly Lock locker = new();
	internal void AddTile(KeyholeTile tile)
	{
		using var tileRegion = tile.GetPolygon();
		lock (locker)
		{
			MultiPolygon.AddGeometryDirectly(tileRegion);
		}
		TileCount++;
		var cIndex = LibMapCommon.Util.Mod(tile.Column - Stats.MinColumn, 1 << tile.Level);
		var rIndex = Stats.MaxRow - tile.Row;
		HasDataMap[rIndex, cIndex] = true;
	}

	public void Flatten()
	{
		lock (locker)
		{
			var toReturn = MultiPolygon.UnionCascaded();
			if (toReturn.GetGeometryType() == wkbGeometryType.wkbPolygon)
			{
				var multiPoly = new OSGeo.OGR.Geometry(wkbGeometryType.wkbMultiPolygon);
				multiPoly.AddGeometryDirectly(toReturn);
				using var sr = toReturn.GetSpatialReference();
				multiPoly.AssignSpatialReference(sr);
				toReturn.Dispose();
				toReturn = multiPoly;
			}
			Interlocked.Exchange(ref m_MultiPolygon, toReturn)?.Dispose();
		}
	}

	public void Dispose()
	{
		var polygon = Interlocked.Exchange(ref m_MultiPolygon, null);
		if (polygon != null)
		{
			polygon.Dispose();
			GC.SuppressFinalize(this);
		}
	}
	~DatedRegion() => Dispose();
}
