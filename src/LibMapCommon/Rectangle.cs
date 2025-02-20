namespace LibMapCommon;

/// <summary>
/// A region of space on earth defined by the lower-left and upper-right geographic coordinates.
/// </summary>
public readonly struct Rectangle
{
	/// <summary> The lower-left (southwest) corner of the <see cref="Rectangle"/> </summary>
	public readonly Wgs1984 LowerLeft;
	/// <summary> The upper-right (northeast) corner of the <see cref="Rectangle"/> </summary>
	public readonly Wgs1984 UpperRight;

	/// <summary> Gets the upper-left (northwest) corner of the <see cref="Rectangle"/> </summary>
	public TCoordinate GetUpperLeft<TCoordinate>() where TCoordinate : ICoordinate<TCoordinate>
		=> TCoordinate.FromWgs84(new Wgs1984(UpperRight.Latitude, LowerLeft.Longitude));

	/// <summary> Gets the lower-right (southeast) corner of the <see cref="Rectangle"/> </summary>
	public TCoordinate GetLowerRight<TCoordinate>() where TCoordinate : ICoordinate<TCoordinate>
		=> TCoordinate.FromWgs84(new Wgs1984(LowerLeft.Latitude, UpperRight.Longitude));

	/// <summary>
	/// Initializes a new instance of a <see cref="Rectangle"/> area on earth's surface by the lower-left and upper-right coordinates. 
	/// </summary>
	/// <param name="lowerLeft">The lower-left corner of the <see cref="Rectangle"/></param>
	/// <param name="upperRight">The lower-left corner of the <see cref="Rectangle"/></param>
	/// <exception cref="ArgumentException"></exception>
	public Rectangle(Wgs1984 lowerLeft, Wgs1984 upperRight)
	{
		if (!lowerLeft.IsValidGeographicCoordinate)
			throw new ArgumentException($"Invalid geographic coordinate {lowerLeft}", nameof(lowerLeft));
		if (!upperRight.IsValidGeographicCoordinate)
			throw new ArgumentException($"Invalid geographic coordinate {upperRight}", nameof(upperRight));
		if (lowerLeft.Latitude >= upperRight.Latitude)
			throw new ArgumentException($"{nameof(lowerLeft)} is not south of {nameof(upperRight)}");
		if (lowerLeft.Longitude == upperRight.Longitude)
			throw new ArgumentException($"{nameof(lowerLeft)} and {nameof(upperRight)} have the same longitude");

		LowerLeft = lowerLeft;
		UpperRight = upperRight;
	}
	/// <summary>
	/// Gets the number of rows and columns comprising this <see cref="Rectangle"/> at a specific zoom level
	/// </summary>
	/// <param name="level"></param>
	/// <param name="nRows">The number of rows from the lower (south) tile to the upper tile (inclusive)</param>
	/// <param name="nColumns">The number of columns from the left tile to the upper tile (inclusive)</param>
	public void GetNumRowsAndColumns<TTile>(int level, out int nRows, out int nColumns) where TTile : ITile<TTile>
	{
		var ll = LowerLeft.GetTile<TTile>(level);
		var ur = UpperRight.GetTile<TTile>(level);

		nColumns = Util.Mod(ur.Column - ll.Column, 1 << level) + 1;
		nRows = int.Abs(ur.Row - ll.Row) + 1;
	}

	/// <summary>
	/// Gets the number of tiles required to cover this <see cref="Rectangle"/>
	/// </summary>
	/// <param name="level">The zoom level of the tiles</param>
	/// <returns>The number of tiles required to tile the <see cref="Rectangle"></returns>
	public int GetTileCount<TTile>(int level) where TTile : ITile<TTile>
	{
		GetNumRowsAndColumns<TTile>(level, out var nRows, out var nColumns);
		return nRows * nColumns;
	}

	/// <summary>
	/// Enumerates the tiles covering this <see cref="Rectangle"/>
	/// 
	/// The enumeration starts at the lower-left corner, procedes left-to-right, then bottom-to-top.
	/// </summary>
	/// <param name="level">The zoom level of the tiles</param>
	/// <returns>The <see cref="KeyholeTile"/> enumeration</returns>
	public IEnumerable<TTile> GetTiles<TTile>(int level) where TTile : ITile<TTile>
	{
		var minCorner = TTile.GetMinimumCorner(LowerLeft, UpperRight, level);
		GetNumRowsAndColumns<TTile>(level, out var nRows, out var nColumns);

		int numTiles = 1 << level;

		for (int r = 0; r < nRows; r++)
		{
			for (int c = 0; c < nColumns; c++)
			{
				var row = (minCorner.Row + r) % numTiles;
				var col = (minCorner.Column + c) % numTiles;
				yield return TTile.Create(row, col, level);
			}
		}
	}
}
