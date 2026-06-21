using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;

namespace LibEsri.Geometry;

public class DatedRegion : IDatedRegion
{
	public int TileCount { get; private set; }
	private OSGeo.OGR.Geometry? m_MultiPolygon;
	private OSGeo.OGR.Geometry MultiPolygon => m_MultiPolygon ?? throw new ObjectDisposedException(nameof(DatedRegion));

	public DateOnly Date { get; }
	public bool IsComplete { get; }
	public Layer Layer { get; }
	public int ZoomLevel => Stats.Zoom;
	public TileStats Stats { get; }

	private readonly Lazy<BoolMap> lazyHasData;
	public BoolMap HasDataMap => lazyHasData.Value;

	internal DatedRegion(Layer layer, bool complete, TileStats stats, DateOnly date, OSGeo.OGR.Geometry multiPolygon)
	{
		Stats = stats;
		Layer = layer;
		Date = date;
		IsComplete = complete;
		switch(multiPolygon.GetGeometryType())
		{
			case wkbGeometryType.wkbPolygon:
				var mp = new OSGeo.OGR.Geometry(wkbGeometryType.wkbMultiPolygon);
				mp.AddGeometryDirectly(multiPolygon);
				using (var sr = multiPolygon.GetSpatialReference())
					mp.AssignSpatialReference(sr);
					m_MultiPolygon = mp;
				break;
			case wkbGeometryType.wkbMultiPolygon:
				m_MultiPolygon = multiPolygon;
				break;
			default: throw new InvalidOperationException();
		}
		lazyHasData = new Lazy<BoolMap>(BuildBoolMap);
	}

	public void Add(DatedRegion other)
	{
		if (other.MultiPolygon.GetGeometryType() != wkbGeometryType.wkbMultiPolygon)
			throw new InvalidOperationException();

		for (int i = other.MultiPolygon.GetGeometryCount() - 1; i >= 0; i--)
		{
			var part = other.MultiPolygon.GetGeometryRef(i);
			MultiPolygon.AddGeometry(part);
		}
	}

	public void Flatten()
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

	public OSGeo.OSR.SpatialReference GetSpatialReference() => MultiPolygon.GetSpatialReference();
	public OSGeo.OGR.Geometry GetMultiPolygon() => MultiPolygon.Clone();

	private BoolMap BuildBoolMap()
	{
		var hasData = new BoolMap(Stats.NumColumns, Stats.NumRows);
		var bt = new bool[Stats.NumColumns * Stats.NumRows];

		Parallel.ForEach(Stats.EnumerateTiles<EsriTile>(), tile =>
		{
			using var polygon = tile.GetPolygon();
			if (polygon.Intersects(MultiPolygon))
			{
				var cIndex = Util.Mod(tile.Column - Stats.MinColumn, 1 << tile.Level);
				var rIndex = tile.Row - Stats.MinRow;
				bt[rIndex *  Stats.NumColumns + cIndex] = true;
			}
		});
		return new BoolMap(Stats.NumColumns, Stats.NumRows, bt);
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
