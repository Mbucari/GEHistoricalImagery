namespace LibMapCommon;

public interface ITile<T> : ITile
{
	public static abstract T GetTile(Coordinate coordinate, int level);
	public static abstract T GetMinimumCorner(Coordinate c1, Coordinate c2, int level);
	public static abstract T Create(int row, int col, int level);
}

public interface ITile
{
	public int Row { get; }
	/// <summary> The number of <see cref="ITile"/> columns from the left-most (west-most) edge of the map. </summary>
	public int Column { get; }
	/// <summary> The <see cref="ITile"/>'s zoom level. </summary>
	public int Level { get; }

	/// <summary> The lower-left (southwest) <see cref="Coordinate"/> of this <see cref="ITile"/> </summary>
	public Coordinate LowerLeft { get; }
	/// <summary> The lower-right (southeast) <see cref="Coordinate"/> of this <see cref="v"/> </summary>
	public Coordinate LowerRight { get; }
	/// <summary> The upper-left (northwest) <see cref="Coordinate"/> of this <see cref="ITile"/> </summary>
	public Coordinate UpperLeft { get; }
	/// <summary> The upper-right (northeast) <see cref="Coordinate"/> of this <see cref="ITile"/> </summary>
	public Coordinate UpperRight { get; }
	/// <summary> <see cref="Coordinate"/> of the center of this <see cref="ITile"/> </summary>
	public Coordinate Center { get; }
}
