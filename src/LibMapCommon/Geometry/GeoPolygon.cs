namespace LibMapCommon.Geometry;

public class GeoPolygon<TCoordinate> : Polygon<GeoPolygon<TCoordinate>, TCoordinate>
	where TCoordinate : IGeoCoordinate<TCoordinate>
{
	public GeoPolygon(params TCoordinate[] coords) : base(-HalfEquator, HalfEquator, coords) { }
	protected GeoPolygon(IList<Line2> edges) : base(-HalfEquator, HalfEquator, edges) { }

	protected static double HalfEquator = TCoordinate.Equator / 2;

	public GeoPolygon<TOther> ConvertTo<TOther>(Func<TCoordinate, TOther> converter) where TOther : IGeoCoordinate<TOther>
		=> new GeoPolygon<TOther>(ConvertEdges(converter));

	protected override GeoPolygon<TCoordinate> CreateFromEdges(IList<Line2> edges) => new GeoPolygon<TCoordinate>(edges);

	/// <summary>
	/// Determines whether this polygon contains any portion of the tile's polygon.
	/// </summary>
	/// <param name="tile">A map tile to test</param>
	/// <returns>True if any part of the tile is within this polygon, otherwise false</returns>
	public bool ContainsTile<TTile>(TTile tile) where TTile : ITile<TCoordinate>
		=> ContainsPoint(tile.LowerLeft) ||
			ContainsPoint(tile.LowerRight) ||
			ContainsPoint(tile.UpperLeft) ||
			ContainsPoint(tile.UpperRight) ||
			PolygonIntersects(tile.GetGeoPolygon());

	/// <summary>
	/// Convert to the global pixel space for the current polygon's coordinate system.
	/// </summary>
	public PixelPolygon ToPixelPolygon(int level)
	{
		var edges = ConvertEdges(c => c.GetGlobalPixelCoordinate(level));
		return new PixelPolygon(level, edges);
	}

	private Line2[] ConvertEdges<TOther>(Func<TCoordinate, TOther> converter) where TOther : ICoordinate
	{
		var edges = new Line2[Edges.Count];
		for (int i = 0; i < Edges.Count; i++)
		{
			var current = Edges[i];
			var next = Edges[(i + 1) % Edges.Count];

			var currentVertex = converter(TCoordinate.Create(current.Origin.X, current.Origin.Y));
			var nextVertex = converter(TCoordinate.Create(next.Origin.X, next.Origin.Y));

			var edge = new Line2(new(currentVertex.X, currentVertex.Y), new(nextVertex.X - currentVertex.X, nextVertex.Y - currentVertex.Y));
			edges[i] = edge;
		}
		return edges;
	}

	/// <summary>
	/// Gets the tile statistics for the region defined by this polygon.
	/// </summary>
	/// <param name="level">The zoom level of interest</param>
	public TileStats GetPolygonalRegionStats<TTile>(int level) where TTile : ITile<TTile, TCoordinate>
	{
		var stats = GetRectangularRegionStats<TTile>(level);
		var tileCount = EnumerateTiles<TTile>(stats).Count();
		return stats with { TileCount = tileCount };
	}

	/// <summary>
	/// Gets the tile statistics for the region defined by this polygon's rectangular envelope
	/// </summary>
	/// <param name="level">The zoom level of interest</param>
	public TileStats GetRectangularRegionStats<TTile>(int level) where TTile : ITile<TTile, TCoordinate>
	{
		var llCoord = TCoordinate.Create(MinX, MinY);
		var urCoord = TCoordinate.Create(MaxX, MaxY);
		var ll = TTile.GetTile(llCoord, level);
		var ur = TTile.GetTile(urCoord, level);

		var (minColumn, maxColumn) = (ll.Column, ur.Column);
		var (minRow, maxRow) = ur.Row < ll.Row ? (ur.Row, ll.Row) : (ll.Row, ur.Row);

		var nColumns = Util.Mod(ur.Column - ll.Column, 1 << level) + 1;
		var nRows = maxRow - minRow + 1;

		return new TileStats(level, nColumns, nRows, minRow, maxRow, minColumn, maxColumn, nColumns * nRows);
	}

	/// <summary>
	/// Enumerates the tiles covering this <see cref="Rectangle"/>
	/// 
	/// The enumeration starts at the lower-left corner, procedes left-to-right, then bottom-to-top.
	/// </summary>
	/// <param name="level">The zoom level of the tiles</param>
	/// <returns>The <see cref="KeyholeTile"/> enumeration</returns>
	public IEnumerable<TTile> GetTiles<TTile>(int level) where TTile : ITile<TTile, TCoordinate>
	{
		var stats = GetRectangularRegionStats<TTile>(level);
		return EnumerateTiles<TTile>(stats);
	}

	internal IEnumerable<TTile> EnumerateTiles<TTile>(TileStats stats) where TTile : ITile<TTile, TCoordinate>
	{
		if (stats.TileCount == 1)
		{
			yield return TTile.Create(stats.MinRow, stats.MinColumn, stats.Zoom);
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

				if (ContainsTile(tile))
					yield return tile;
			}
		}
	}
}
