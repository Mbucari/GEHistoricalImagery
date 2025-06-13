namespace LibMapCommon.Geometry;

public readonly record struct TileStats(
	int Zoom,
	int NumColumns,
	int NumRows,
	int MinRow,
	int MaxRow,
	int MinColumn,
	int MaxColumn,
	int TileCount);
