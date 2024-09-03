namespace GEHistoricalImagery;

/// <summary>
/// A region of space on earth defined by the lower-left and upper-right geographic coordinates.
/// </summary>
/// <param name="lowerLeft"></param>
/// <param name="upperRight"></param>
internal readonly struct Rectangle(Coordinate lowerLeft, Coordinate upperRight)
{
	public readonly Coordinate LowerLeft = lowerLeft, UpperRight = upperRight;

	public int GetTileCount(int level)
	{
		var ll = LowerLeft.GetTile(level);
		var ur = UpperRight.GetTile(level);

		return (ur.Row - ll.Row + 1) * (ur.Column - ll.Column + 1);
	}

	public IEnumerable<Tile> GetTiles(int level)
	{
		var ll = LowerLeft.GetTile(level);
		var ur = UpperRight.GetTile(level);

		for (int r = ll.Row; r <= ur.Row; r++)
		{
			for (int c = ll.Column; c <= ur.Column; c++)
			{
				yield return new Tile(r, c, level);
			}
		}
	}
}
