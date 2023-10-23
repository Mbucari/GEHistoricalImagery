namespace GoogleEarthImageDownload;

internal readonly struct Rectangle
{
	public readonly Coordinate LowerLeft, UpperRight;
	public Rectangle(Coordinate lowerLeft, Coordinate upperRight)
	{
		LowerLeft = lowerLeft;
		UpperRight = upperRight;
	}

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
