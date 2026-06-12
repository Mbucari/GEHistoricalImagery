using OSGeo.OGR;
using OSGeo.OSR;

namespace LibMapCommon.Geometry;

public class GeoRegion<TCoordinate> : IDisposable where TCoordinate : IGeoCoordinate<TCoordinate>
{
	public double MinY { get; }
	public double MaxY { get; }
	/// <summary>
	/// The left-most X coordinate of the region. Note that this may be greater than RightMostX if the region crosses the 180/-180 longitude line.
	/// </summary>
	public double LeftMostX { get; }
	/// <summary>
	/// The right-most X coordinate of the region. Note that this may be less than LeftMostX if the region crosses the 180/-180 longitude line.
	/// </summary>
	public double RightMostX { get; }
	/// <summary>
	/// The polygon or multipolygon geometry representing the region. The geometry is guaranteed to be in the same spatial reference as the input coordinates used to create the region, and to have a bounding box defined by LeftMostX, RightMostX, MinY, and MaxY.
	/// </summary>
	private OSGeo.OGR.Geometry? m_Region;
	protected OSGeo.OGR.Geometry Region => m_Region ?? throw new ObjectDisposedException(nameof(GeoRegion<>));
	protected GeoRegion(double leftmostX, double rightmostX, double minY, double maxY, OSGeo.OGR.Geometry region)
	{
		if (leftmostX == rightmostX)
			throw new InvalidOperationException("Left-most X and right-most X cannot be equal");
		LeftMostX = leftmostX;
		RightMostX = rightmostX;
		MinY = minY;
		MaxY = maxY;
		m_Region = region;
	}
	public OSGeo.OGR.Geometry GetMultiPolygon()
	{
		var type = Region.GetGeometryType();
		if (type is wkbGeometryType.wkbMultiPolygon or wkbGeometryType.wkbMultiPolygon25D or wkbGeometryType.wkbMultiPolygonM or wkbGeometryType.wkbMultiPolygonZM)
		{
			var multiPoly = Region.Clone();
			multiPoly.FlattenTo2D();
			return multiPoly;
		}
		else if (type is wkbGeometryType.wkbPolygon or wkbGeometryType.wkbPolygon25D or wkbGeometryType.wkbPolygonM or wkbGeometryType.wkbPolygonZM)
		{
			using var polygon = Region.Clone();
			polygon.FlattenTo2D();
			var multiPoly = new OSGeo.OGR.Geometry(wkbGeometryType.wkbMultiPolygon);
			multiPoly.AddGeometryDirectly(polygon);
			using var sr = Region.GetSpatialReference();
			multiPoly.AssignSpatialReference(sr);
			return multiPoly;
		}
		else
			throw new InvalidOperationException("Invalid geometry type. Only polygon and multipolygon geometries are supported.");
	}

	public IEnumerable<OSGeo.OGR.Geometry> GetPolygons() => GetPolygons(Region);
	private static IEnumerable<OSGeo.OGR.Geometry> GetPolygons(OSGeo.OGR.Geometry geometry)
	{
		var type = geometry.GetGeometryType();
		if (type is wkbGeometryType.wkbPolygon)
			yield return geometry;

		var geoCount = geometry.GetGeometryCount();
		for (int i = 0; i < geoCount; i++)
		{
			var g = geometry.GetGeometryRef(i);
			foreach (var p in GetPolygons(g))
				yield return p;
		}
	}
	public string WKT => Region.ExportToWkt(out var wkt) == 0 && wkt is not null? wkt : "POLYGON EMPTY";
	public OSGeo.OGR.Geometry Intersect(OSGeo.OGR.Geometry other) => Region.Intersection(other);
	public bool Overlaps(OSGeo.OGR.Geometry other) => Region.Overlaps(other);
	public double GeodesicArea => Region.GeodesicArea();
	public double Area => Region.Area();

	public GeoRegion<TOther> Transform<TOther>() where TOther : IGeoCoordinate<TOther>
	{
		if (typeof(TOther) == typeof(TCoordinate))
			return new GeoRegion<TOther>(LeftMostX, RightMostX, MinY, MaxY, Region.Clone());
		using var src = Region.GetSpatialReference();
		using var dest = new SpatialReference(null);
		dest.Import<TOther>();
		using var transform = new CoordinateTransformation(src, dest);

		double[] ll = [LeftMostX, MinY, 0];
		double[] ur = [RightMostX, MaxY, 0];
		transform.TransformPoint(ll);
		transform.TransformPoint(ur);

		var xformedRegion = Region.Clone();
		xformedRegion.Transform(transform);
		return new GeoRegion<TOther>(ll[0], ur[0], ll[1], ur[1], xformedRegion);
	}

	/// <summary>
	/// Creates a GeoRegion from the given coordinates. The coordinates should define a valid polygon
	/// (i.e. at least 3 points, and not self-intersecting). The region may cross the 180/-180
	/// longitude line, either by going more negative than -180 or more positive than 180.
	/// </summary>
	public static GeoRegion<TCoordinate> Create(params TCoordinate[] coordinates)
	{
		using var aoi = OgrExtensions.MakePolygon(coordinates);
		return Create(aoi);
	}

	/// <summary>
	/// Creates a GeoRegion from the given KML Placemark.
	/// </summary>
	public static GeoRegion<Wgs1984> Create(Placemark placemark)
		=> typeof(TCoordinate) == typeof(Wgs1984) ? GeoRegion<Wgs1984>.Create(placemark.Geometry)
		 : throw new InvalidOperationException($"This method can only be used to create GeoRegion<{typeof(Wgs1984).Name}>");

	public static GeoRegion<TCoordinate> Create(OSGeo.OGR.Geometry aoi)
	{
		var geoType = aoi.GetGeometryType();
		if (geoType is not (wkbGeometryType.wkbPolygon or wkbGeometryType.wkbMultiPolygon))
			throw new InvalidOperationException("Invalid geometry type. Only polygon and multipolygon geometries are supported.");

		using var env = new Envelope();
		aoi.GetEnvelope(env);
		var halfEquator = TCoordinate.Equator / 2;

		if (env.MinY >= env.MaxY)
			throw new InvalidOperationException("Region must have a positive height.");
		if (env.MinX >= env.MaxX)
			throw new InvalidOperationException("Region must have a positive width.");
		if (env.MaxX - env.MinX > TCoordinate.Equator)
			throw new InvalidOperationException("Invalid region. The longitude span of the region cannot exceed the equator distance.");
		if (env.MaxX > TCoordinate.Equator || env.MinX < -TCoordinate.Equator)
			throw new InvalidOperationException($"Invalid region. Valid longitudes are [-{TCoordinate.Equator}, {TCoordinate.Equator}].");

		if (env.MinX >= -halfEquator && env.MaxX <= halfEquator)
		{
			return new GeoRegion<TCoordinate>(env.MinX, env.MaxX, env.MinY, env.MaxY, aoi.Clone());
		}

		using OSGeo.OGR.Geometry globeMask = OgrExtensions.MakeRectangle(TCoordinate.Create(-halfEquator, -halfEquator), TCoordinate.Equator, TCoordinate.Equator);

		using OSGeo.OGR.Geometry intersect_main = aoi.Intersection(globeMask);
		OSGeo.OGR.Geometry multi = new OSGeo.OGR.Geometry(wkbGeometryType.wkbMultiPolygon);
		using var sr = aoi.GetSpatialReference();
		multi.AssignSpatialReference(sr);
		multi.AddGeometry(intersect_main);
		double leftmostX, rightmostX, minY = env.MinY, maxY = env.MaxY;
		if (env.MinX < -halfEquator)
		{
			using OSGeo.OGR.Geometry p_neg180 = OgrExtensions.MakeRectangle(TCoordinate.Create(-TCoordinate.Equator, -halfEquator), halfEquator, TCoordinate.Equator);
			using OSGeo.OGR.Geometry intersect_neg = aoi.Intersection(p_neg180);
			intersect_neg.Translate(360, 0);
			multi.AddPoygons(intersect_neg);
			leftmostX = env.MinX + 360;
			rightmostX = env.MaxX;
		}
		else
		{
			using OSGeo.OGR.Geometry p_plus180 = OgrExtensions.MakeRectangle(TCoordinate.Create(halfEquator, -halfEquator), halfEquator, TCoordinate.Equator);
			using OSGeo.OGR.Geometry intersect_plus = aoi.Intersection(p_plus180);
			intersect_plus.Translate(-360, 0);
			multi.AddPoygons(intersect_plus);
			leftmostX = env.MinX;
			rightmostX = env.MaxX - 360;
		}
		return new GeoRegion<TCoordinate>(leftmostX, rightmostX, minY, maxY, multi);
	}

	/// <summary>
	/// Gets the tile statistics for the region defined by this polygon's rectangular envelope
	/// </summary>
	/// <param name="level">The zoom level of interest</param>
	public TileStats GetRectangularRegionStats<TTile>(int level) where TTile : ITile<TTile, TCoordinate>
	{
		var llCoord = TCoordinate.Create(LeftMostX, MinY);
		var urCoord = TCoordinate.Create(RightMostX, MaxY);
		var ll = TTile.GetTile(llCoord, level);
		var ur = TTile.GetTile(urCoord, level);

		var (minColumn, maxColumn) = (ll.Column, ur.Column);
		var (minRow, maxRow) = ur.Row < ll.Row ? (ur.Row, ll.Row) : (ll.Row, ur.Row);

		var nColumns = Util.Mod(ur.Column - ll.Column, 1 << level) + 1;
		var nRows = maxRow - minRow + 1;

		return new TileStats(level, nColumns, nRows, minRow, maxRow, minColumn, maxColumn, (long)nColumns * nRows);
	}

	/// <summary>
	/// Determines whether this polygon contains any portion of the tile's polygon.
	/// </summary>
	/// <param name="tile">A map tile to test</param>
	/// <returns>True if any part of the tile is within this polygon, otherwise false</returns>
	public virtual bool ContainsTile<TTile>(TTile tile) where TTile : ITile<TCoordinate>
	{
		using var tilePolygon = tile.GetPolygon();
		return Region.Intersects(tilePolygon);
	}

	/// <summary>
	/// Enumerate the tiles that intersect with the AOI region.  Progress is reported as the tiles are being enumerated.
	/// </summary>
	public IEnumerable<TTile> EnumerateTiles<TTile>(int zoomLevel, Action<double>? reportProgress = null)
		where TTile : ITile<TTile, TCoordinate>
	{

		var polygons = GetPolygons().ToArray();
		var allRectStats = polygons.Select(p => p.GetRectangularRegionStats<TTile, TCoordinate>(zoomLevel)).ToArray();
		double totalTileCount = allRectStats.Sum(s => s.TileCount);
		reportProgress?.Invoke(0);
		long numTilesChecked = 0;

		for (int i = 0; i < allRectStats.Length; i++)
		{
			var polygon = polygons[i];
			var stats = allRectStats[i];
			var polygonTiles = EnumerateTiles<TTile>(polygon, stats, () =>
			{
				var progress = ++numTilesChecked / totalTileCount;
				reportProgress?.Invoke(progress);
			});
			foreach (var tile in polygonTiles)
				yield return tile;
		}
	}

	private static IEnumerable<TTile> EnumerateTiles<TTile>(OSGeo.OGR.Geometry polygon, TileStats stats, Action? progress = null)
		where TTile : ITile<TTile, TCoordinate>
	{
		if (stats.TileCount == 1)
		{
			yield return TTile.Create(stats.MinRow, stats.MinColumn, stats.Zoom);
			progress?.Invoke();
			yield break;
		}

		int numTiles = 1 << stats.Zoom;

		for (int r = 0; r < stats.NumRows; r++)
		{
			for (int c = 0; c < stats.NumColumns; c++)
			{
				var row = (stats.MinRow + r) % numTiles;
				var col = (stats.MinColumn + c) % numTiles;
				var tile = TTile.Create(row, col, stats.Zoom);
				using OSGeo.OGR.Geometry tilePoly = tile.GetPolygon();

				if (tilePoly.Intersects(polygon))
					yield return tile;
				progress?.Invoke();
			}
		}
	}
	~GeoRegion() => Dispose();
	public void Dispose()
	{
		var region = Interlocked.Exchange(ref m_Region, null);
		if (region != null)
		{
			region.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
