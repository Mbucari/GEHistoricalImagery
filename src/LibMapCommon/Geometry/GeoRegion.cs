using System.Drawing;

namespace LibMapCommon.Geometry;

public class GeoRegion<TCoordinate> : Region<GeoPolygon<TCoordinate>, TCoordinate>
	where TCoordinate : IGeoCoordinate<TCoordinate>
{
	public string AutoCad => string.Join("\r\n", Polygons.Select(p => p.AutoCad));
	protected GeoRegion(double leftmostX, double rightmostX, params GeoPolygon<TCoordinate>[] polygons)
		: base(leftmostX, rightmostX, polygons) { }

	public GeoRegion<TOther> ConvertTo<TOther>(Func<TCoordinate, TOther> converter) where TOther : IGeoCoordinate<TOther>
	{
		var ll = converter(TCoordinate.Create(LeftMostX, MinY));
		var ur = converter(TCoordinate.Create(RightMostX, MaxY));
		return new GeoRegion<TOther>(ll.X, ur.X, Polygons.Select(p => p.ConvertTo(converter)).ToArray());
	}

	public PixelRegion ToPixelRegion(int level)
	{
		var ll = TCoordinate.Create(LeftMostX, MinY).GetGlobalPixelCoordinate(level);
		var ur = TCoordinate.Create(RightMostX, MaxY).GetGlobalPixelCoordinate(level);
		return new PixelRegion(level, ll.X, ur.X, Polygons.Select(p => p.ToPixelPolygon(level)).ToArray());
	}


	/// <summary>
	/// Gets the tile statistics for the region defined by this polygon.
	/// </summary>
	/// <param name="level">The zoom level of interest</param>
	public TileStats GetPolygonalRegionStats<TTile>(int level) where TTile : ITile<TTile, TCoordinate>
	{
		var stats = GetRectangularRegionStats<TTile>(level);
		var tileCount = Polygons.Sum(p => p.EnumerateTiles<TTile>(stats).Count());
		return stats with { TileCount = tileCount };
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
		=> Polygons.SelectMany(p => p.GetTiles<TTile>(level));

	/// <summary>
	/// Determines whether this polygon contains any portion of the tile's polygon.
	/// </summary>
	/// <param name="tile">A map tile to test</param>
	/// <returns>True if any part of the tile is within this polygon, otherwise false</returns>
	public bool ContainsTile<TTile>(TTile tile) where TTile : ITile<TCoordinate>
		=> Polygons.Any(p => p.ContainsTile(tile));

	public static GeoRegion<TCoordinate> Create(params TCoordinate[] coords)
	{
		var minX = coords.Select(x => x.X).Min();
		var maxX = coords.Select(x => x.X).Max();

		if (maxX > TCoordinate.Equator)
			throw new ArgumentException($"The largest X coordinate ({maxX}) exceeds the equatorial distance ({TCoordinate.Equator})");

		if (minX < -TCoordinate.Equator)
			throw new ArgumentException($"The smallest X coordinate ({minX}) is smaller than the negative equatorial distance ({TCoordinate.Equator})");

		if (maxX - minX > TCoordinate.Equator)
			throw new ArgumentException($"The X span ({maxX - minX}) is larger than the equatorial distance ({TCoordinate.Equator})");

		var halfEquator = TCoordinate.Equator / 2;
		if (minX < -halfEquator)
		{
			var crossover = -halfEquator;
			var rings = SplitRingAtCrossing(coords, crossover);

			var east = rings
				.Where(r => r.Any(r => r.X < crossover))
				.Select(r => new GeoPolygon<TCoordinate>(r.Select(c => TCoordinate.Create(c.X + TCoordinate.Equator, c.Y)).ToArray()))
				.ToArray();

			var west = rings.Where(r => r.Any(r => r.X > crossover)).Select(r => new GeoPolygon<TCoordinate>(r.ToArray())).ToArray();
			var leftMostX = east.Select(p => p.MinX).Min();
			var rightMostX = west.Select(p => p.MaxX).Max();

			return new(leftMostX, rightMostX, east.Concat(west).ToArray());
		}
		else if (maxX > halfEquator)
		{
			var crossover = halfEquator;
			var rings = SplitRingAtCrossing(coords, crossover);

			var east = rings.Where(r => r.Any(r => r.X < crossover)).Select(r => new GeoPolygon<TCoordinate>(r.ToArray())).ToArray();

			var west = rings
				.Where(r => r.Any(r => r.X > crossover))
				.Select(r => new GeoPolygon<TCoordinate>(r.Select(c => TCoordinate.Create(c.X - TCoordinate.Equator, c.Y)).ToArray()))
				.ToArray();

			var leftMostX = east.Select(p => p.MinX).Min();
			var rightMostX = west.Select(p => p.MaxX).Max();

			return new(leftMostX, rightMostX, east.Concat(west).ToArray());
		}
		else
		{
			var polygon = new GeoPolygon<TCoordinate>(coords);
			return new(polygon.MinX, polygon.MaxX, polygon);
		}
	}

	private static TCoordinate[][] SplitRingAtCrossing(TCoordinate[] coords, double crossover)
	{
		for (int i = 1; i < coords.Length; i++)
		{
			if (PointsStraddle(coords[i - 1], coords[i]))
			{
				for (int j = i + 1; j <= coords.Length; j++)
				{
					if (PointsStraddle(coords[j - 1], coords[j % coords.Length]))
					{
						var soloRing = SplitRing(i, j, out var remainingRing);

						var subRings = SplitRingAtCrossing(remainingRing, crossover);
						Array.Resize(ref subRings, subRings.Length + 1);
						subRings[^1] = soloRing;
						return subRings;
					}
				}
			}
		}
		return [coords];

		#region Algorithm Functions

		TCoordinate[] SplitRing(int start, int end, out TCoordinate[] remainingRing)
		{
			var firstSplit = SplitPoints(coords[start - 1], coords[start]);
			var secondSplit = SplitPoints(coords[end - 1], coords[end % coords.Length]);

			var soloRing = new TCoordinate[end - start + 2];
			soloRing[0] = firstSplit;
			soloRing[^1] = secondSplit;
			Array.Copy(coords, start, soloRing, 1, end - start);

			remainingRing = new TCoordinate[coords.Length - end + start + 2];
			Array.Copy(coords, 0, remainingRing, 0, start);
			remainingRing[start] = firstSplit;
			remainingRing[start + 1] = secondSplit;
			Array.Copy(coords, end, remainingRing, start + 2, coords.Length - end);

			return soloRing;
		}

		bool PointsStraddle(TCoordinate p1, TCoordinate p2)
			=> p1.X < crossover && p2.X > crossover || p1.X > crossover && p2.X < crossover;

		TCoordinate SplitPoints(TCoordinate p1, TCoordinate p2)
			=> TCoordinate.Create(crossover, p1.Y + (p2.Y - p1.Y) / (p2.X - p1.X) * (crossover - p1.X));

		#endregion
	}
}

