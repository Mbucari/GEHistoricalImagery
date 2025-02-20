namespace LibMapCommon;

public interface ITile<T> : ITile
{
	static abstract T GetTile(Wgs1984 coordinate, int level);
	static abstract T GetMinimumCorner(Wgs1984 c1, Wgs1984 c2, int level);
	static abstract T Create(int row, int col, int level);
}

public interface ITile
{
	int Row { get; }
	/// <summary> The number of <see cref="ITile"/> columns from the left-most (west-most) edge of the map. </summary>
	int Column { get; }
	/// <summary> The <see cref="ITile"/>'s zoom level. </summary>
	int Level { get; }

	/// <summary> The lower-left (southwest) <see cref="Wgs1984"/> of this <see cref="ITile"/> </summary>
	Wgs1984 LowerLeft { get; }
	/// <summary> The lower-right (southeast) <see cref="Wgs1984"/> of this <see cref="v"/> </summary>
	Wgs1984 LowerRight { get; }
	/// <summary> The upper-left (northwest) <see cref="Wgs1984"/> of this <see cref="ITile"/> </summary>
	Wgs1984 UpperLeft { get; }
	/// <summary> The upper-right (northeast) <see cref="Wgs1984"/> of this <see cref="ITile"/> </summary>
	Wgs1984 UpperRight { get; }
	/// <summary> <see cref="Wgs1984"/> of the center of this <see cref="ITile"/> </summary>
	Wgs1984 Center { get; }
}
