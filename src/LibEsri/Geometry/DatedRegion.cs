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
	public bool IsComplete { get; private set; }
	public Layer Layer { get; }
	public int ZoomLevel => Stats.Zoom;
	public TileStats Stats { get; }
	internal void MarkComplete() => IsComplete = true;

	private readonly Lazy<BoolMap> lazyHasData;
	public BoolMap HasDataMap => lazyHasData.Value;

	internal DatedRegion(Layer layer, TileStats stats, DateOnly date, OSGeo.OGR.Geometry multiPolygon)
	{
		Stats = stats;
		Layer = layer;
		Date = date;
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

	public OSGeo.OGR.Geometry GetMultiPolygon() => MultiPolygon.Clone();

	BoolMap BuildBoolMap()
	{
		var hasData = new BoolMap(Stats.NumColumns, Stats.NumRows);
		foreach (var tile in Stats.EnumerateTiles<EsriTile>())
		{
			using var polygon = tile.GetPolygon();
			if (polygon.Intersects(MultiPolygon))
			{
				var cIndex = Util.Mod(tile.Column - Stats.MinColumn, 1 << tile.Level);
				var rIndex = tile.Row - Stats.MinRow;
				hasData[rIndex, cIndex] = true;
			}
		}
		return hasData;
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
