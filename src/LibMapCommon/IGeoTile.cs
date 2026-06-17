using LibMapCommon.Geometry;

namespace LibMapCommon;

public interface IGeoTile<TTile, TCoordinate> : IGeoTile<TCoordinate>, ITile<TTile> where TTile : ITile<TTile> where TCoordinate : IGeoCoordinate<TCoordinate>
{
	static abstract TTile GetTile(TCoordinate coordinate, int level);
}

public interface ITile<TTile> where TTile : ITile<TTile>
{
	static abstract TTile Create(int row, int col, int level);
}

public interface IGeoTile<TCoordinate> : ITile where TCoordinate : IGeoCoordinate<TCoordinate>
{
	/// <summary> The lower-left (southwest) coordinate of this <see cref="IGeoTile{TCoordinate}"/> </summary>
	TCoordinate LowerLeft { get; }
	/// <summary> The lower-right (southeast) coordinate of this <see cref="IGeoTile{TCoordinate}"/> </summary>
	TCoordinate LowerRight { get; }
	/// <summary> The upper-left (northwest) coordinate of this <see cref="IGeoTile{TCoordinate}"/> </summary>
	TCoordinate UpperLeft { get; }
	/// <summary> The upper-right (northeast) coordinate of this <see cref="IGeoTile{TCoordinate}"/> </summary>
	TCoordinate UpperRight { get; }
	/// <summary> coordinate of the center of this <see cref="IGeoTile{TCoordinate}"/> </summary>
	TCoordinate Center { get; }
}

public interface ITile
{
	/// <summary>
	/// If <see cref="RowsIncreaseToSouth"/>, the number of rows from the top-most (north-most) edge of the map.
	/// Otherwise the number of rows from the bottom-most (south-most) edge of the map.
	/// </summary>
	int Row { get; }
	/// <summary> The number of <see cref="ITile"/> columns from the left-most (west-most) edge of the map. </summary>
	int Column { get; }
	/// <summary> The <see cref="ITile"/>'s zoom level. </summary>
	int Level { get; }
	/// <summary> coordinate of the center of this <see cref="IGeoTile{TCoordinate}"/> </summary>
	Wgs1984 Wgs84Center { get; }
	/// <summary>
	/// True if the row numbers increase from north to south, false if they increase from south to north. 
	/// </summary>
	bool RowsIncreaseToSouth { get; }
}
