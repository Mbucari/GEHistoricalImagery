using LibMapCommon;

namespace LibGoogleEarth;

/// <summary>
/// A square tile on earth's surface at a particular zoom level.
/// </summary>
public class KeyholeTile : ITile<KeyholeTile, Wgs1984>
{
	/// <summary> The quadtree path string </summary>
	public string Path { get; }
	/// <summary> The subindex order of this path in the index packet.  </summary>
	public int SubIndex { get; }
	/// <summary> Indicates if this instances represents the root quadtree node. </summary>
	public bool IsRoot => Path == Root.Path;
	/// <summary> Enumerates all quad tree indices after the root node. </summary>
	public IEnumerable<KeyholeTile> Indices => EnumerateIndices();

	/*
	   c0    c1
	|-----|-----|
r1	|  3  |  2  |
	|-----|-----|
r0	|  0  |  1  |
	|-----|-----|
	*/
	public int Level { get; }
	/// <summary> The number of <see cref="KeyholeTile"/> rows from the bottom-most (south-most) edge of the map. </summary>
	public int Row { get; }
	public int Column { get; }
	public bool RowsIncreaseToSouth => false;
	/// <summary>  The roo quadtree node </summary>
	public static readonly KeyholeTile Root = new("0");
	public const int MaxLevel = 30;
	private const int SUBINDEX_MAX_SZ = 4;

	#region Constructors
	/// <summary>
	/// Initializes a new instance of a <see cref="KeyholeTile"/> from a quadtree path string
	/// </summary>
	/// <param name="quadTreePath">The rooted quadtree path string.</param>
	public KeyholeTile(string quadTreePath)
	{
		Util.ValidateQuadTreePath(quadTreePath);

		Path = quadTreePath;
		SubIndex = GetSubIndex(Path);

		for (int i = 0; i < quadTreePath.Length; i++)
		{
			var cell = quadTreePath[i] & 3;
			int row = cell >> 1;
			int col = row ^ (cell & 1);

			Row = (Row << 1) | row;
			Column = (Column << 1) | col;
		}
		Level = quadTreePath.Length - 1;
	}

	/// <summary>
	/// Initializes a new instance of a <see cref="KeyholeTile"/> by row, column, and zoom level
	/// </summary>
	/// <param name="rowIndex">The row containing the <see cref="KeyholeTile"/></param>
	/// <param name="colIndex">The column containing the <see cref="KeyholeTile"/></param>
	/// <param name="level">The <see cref="KeyholeTile"/>'s zoom level</param>
	public KeyholeTile(int rowIndex, int colIndex, int level)
	{
		var numTiles = LibMapCommon.Util.ValidateLevel(level, MaxLevel);
		ArgumentOutOfRangeException.ThrowIfNegative(rowIndex, nameof(rowIndex));
		ArgumentOutOfRangeException.ThrowIfNegative(colIndex, nameof(colIndex));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(rowIndex, numTiles - 1, nameof(rowIndex));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(colIndex, numTiles - 1, nameof(colIndex));

		Row = rowIndex;
		Column = colIndex;
		Level = level;

		var chars = new char[level + 1];
		for (int i = level; i >= 0; i--)
		{
			var row = rowIndex & 1;
			var col = colIndex & 1;
			rowIndex >>= 1;
			colIndex >>= 1;

			chars[i] = (char)(row << 1 | (row ^ col) | 0x30);
		}

		Path = new string(chars);
		Util.ValidateQuadTreePath(Path);
		SubIndex = GetSubIndex(Path);
	}
	#endregion

	public static KeyholeTile GetTile(Wgs1984 coordinate, int level)
	{
		return new(Util.LatLongToRowCol(coordinate.Latitude, level), Util.LatLongToRowCol(coordinate.Longitude, level), level);
	}

	public static KeyholeTile Create(int row, int col, int level)
		=> new KeyholeTile(row, col, level);

	#region Coordinates
	private double RowColToLatLong(double rowCol)
		=> Util.RowColToLatLong(Level, rowCol);

	public Wgs1984 Wgs84Center => new(RowColToLatLong(Row + 0.5), RowColToLatLong(Column + 0.5));
	public Wgs1984 Center => Wgs84Center;
	public Wgs1984 LowerLeft => new(RowColToLatLong(Row), RowColToLatLong(Column));
	public Wgs1984 LowerRight => new(RowColToLatLong(Row), RowColToLatLong(Column + 1));
	public Wgs1984 UpperLeft => new(RowColToLatLong(Row + 1), RowColToLatLong(Column));
	public Wgs1984 UpperRight => new(RowColToLatLong(Row + 1), RowColToLatLong(Column + 1));
	#endregion

	#region Helpers
	private IEnumerable<KeyholeTile> EnumerateIndices()
	{
		for (int end = SUBINDEX_MAX_SZ; end < Path.Length; end += SUBINDEX_MAX_SZ)
			yield return new KeyholeTile(Path[..end]);
	}
	public override string ToString() => Path;
	public override int GetHashCode() => Path.GetHashCode();
	public override bool Equals(object? obj) => obj is KeyholeTile other && other.Path == Path;

	#endregion

	#region Subindex Calculation

	// Nodes have two numbering schemes:
	//
	// 1) "Subindex".  This numbering starts at the top of the tree
	// and goes left-to-right across each level, like this:
	//
	//                    0
	//                 /     \                           .
	//               1  86 171 256
	//            /     \                                .
	//          2  3  4  5 ...
	//        /   \                                      .
	//       6 7 8 9  ...
	//
	// Notice that the second row is weird in that it's not left-to-right
	// order.  HOWEVER, the root node in Keyhole is special in that it
	// doesn't have this weird ordering.  It looks like this:
	//
	//                    0
	//                 /     \                           .
	//               1  2  3  4
	//            /     \                                .
	//          5  6  7  8 ...
	//       /     \                                     .
	//     21 22 23 24  ...
	//
	// The mangling of the second row is controlled by a parameter to the
	// constructor.

	private static int GetSubIndex(string quadTreePath)
	{
		return quadTreePath.Length <= SUBINDEX_MAX_SZ
			? Util.GetRootSubIndex(quadTreePath)
			: Util.GetTreeSubIndex(getSubindexPath());

		string getSubindexPath()
			=> quadTreePath.Substring((quadTreePath.Length - 1) / SUBINDEX_MAX_SZ * SUBINDEX_MAX_SZ);
	}

	#endregion

}
