namespace LibMapCommon.Geometry;

public readonly record struct TileStats(
	int Zoom,
	int NumColumns,
	int NumRows,
	int MinRow,
	int MaxRow,
	int MinColumn,
	int MaxColumn,
	int TileCount)
{
	public static TileStats operator +(TileStats s1, TileStats s2)
		=> new TileStats(
			s1.Zoom + s2.Zoom,
			s1.NumColumns + s2.NumColumns,
			s1.NumRows + s2.NumRows,
			s1.MinRow + s2.MinRow,
			s1.MaxRow + s2.MaxRow,
			s1.MinColumn + s2.MinColumn,
			s1.MaxColumn + s2.MaxColumn,
			s1.TileCount + s2.TileCount);
}
