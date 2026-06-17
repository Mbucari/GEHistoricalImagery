namespace LibMapCommon.Geometry;

public readonly record struct TileStats(
	int Zoom,
	int NumColumns,
	int NumRows,
	int MinRow,
	int MaxRow,
	int MinColumn,
	int MaxColumn,
	long TileCount)
{
	public IEnumerable<TTile> EnumerateTiles<TTile>(IProgress<int>? progress = null) where TTile : ITile<TTile>
	{
		var numTiles = 1 << Zoom;
		for (int r = 0; r < NumRows; r++)
		{
			for (int c = 0; c < NumColumns; c++)
			{
				var row = (MinRow + r) % numTiles;
				var col = (MinColumn + c) % numTiles;
				yield return TTile.Create(row, col, Zoom);
			}
			progress?.Report(NumColumns);
		}
	}
}
