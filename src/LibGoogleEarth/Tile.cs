using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace LibGoogleEarth;

/// <summary>
/// A square tile on earth's surface at a particular zoom level.
/// </summary>
public class Tile
{
	/// <summary> The quadtree path string </summary>
	public string Path { get; }
	/// <summary> The subindex order of this path in the index packet.  </summary>
	public int SubIndex { get; }
	/// <summary> Indicates if this instances represents the root quadtree node. </summary>
	public bool IsRoot => Path == Root.Path;
	/// <summary> Enumerates all quad tree indices after the root node. </summary>
	public IEnumerable<Tile> Indices => EnumerateIndices();

	/*
	   c0    c1
	|-----|-----|
r1	|  3  |  2  |
	|-----|-----|
r0	|  0  |  1  |
	|-----|-----|
	*/
	/// <summary> The <see cref="Tile"/>'s zoom level. </summary>
	public int Level { get; }
	/// <summary> The number of <see cref="Tile"/> rows from the bottom-most (south-most) edge of the map. </summary>
	public int Row { get; }
	/// <summary> The number of <see cref="Tile"/> columns from the left-most (west-most) edge of the map. </summary>
	public int Column { get; }
	/// <summary>  The roo quadtree node </summary>
	public static readonly Tile Root = new("0");
	public const int MaxLevel = 30;
	private const int SUBINDEX_MAX_SZ = 4;

	#region Constructors
	/// <summary>
	/// Initializes a new instance of a <see cref="Tile"/> from a quadtree path string
	/// </summary>
	/// <param name="quadTreePath">The rooted quadtree path string.</param>
	public Tile(string quadTreePath)
	{
		ValidateQuadTreePath(quadTreePath);

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
	/// Initializes a new instance of a <see cref="Tile"/> by row, column, and zoom level
	/// </summary>
	/// <param name="rowIndex">The row containing the <see cref="Tile"/></param>
	/// <param name="colIndex">The column containing the <see cref="Tile"/></param>
	/// <param name="level">The <see cref="Tile"/>'s zoom level</param>
	public Tile(int rowIndex, int colIndex, int level)
	{
		Util.ValidateLevel(level);
		ArgumentOutOfRangeException.ThrowIfNegative(rowIndex, nameof(rowIndex));
		ArgumentOutOfRangeException.ThrowIfNegative(colIndex, nameof(colIndex));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(rowIndex, (1 << level) - 1, nameof(rowIndex));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(colIndex, (1 << level) - 1, nameof(colIndex));

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
		ValidateQuadTreePath(Path);
		SubIndex = GetSubIndex(Path);
	}
	#endregion

	#region Coordinates
	private double RowColToLatLong(double rowCol)
	{
		int numTiles = 1 << Level;
		ArgumentOutOfRangeException.ThrowIfNegative(rowCol, nameof(rowCol));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(rowCol, numTiles, nameof(rowCol));
		return rowCol * 360d / numTiles - 180;
	}

	/// <summary> The lower-left (southwest) <see cref="Coordinate"/> of this <see cref="Tile"/> </summary>
	public Coordinate LowerLeft => new(RowColToLatLong(Row), RowColToLatLong(Column));
	/// <summary> The lower-right (southeast) <see cref="Coordinate"/> of this <see cref="Tile"/> </summary>
	public Coordinate LowerRight => new(RowColToLatLong(Row), RowColToLatLong(Column + 1));
	/// <summary> The upper-left (northwest) <see cref="Coordinate"/> of this <see cref="Tile"/> </summary>
	public Coordinate UpperLeft => new(RowColToLatLong(Row + 1), RowColToLatLong(Column));
	/// <summary> The upper-right (northeast) <see cref="Coordinate"/> of this <see cref="Tile"/> </summary>
	public Coordinate UpperRight => new(RowColToLatLong(Row + 1), RowColToLatLong(Column + 1));
	/// <summary> <see cref="Coordinate"/> of the center of this <see cref="Tile"/> </summary>
	public Coordinate Center => new(RowColToLatLong(Row + 0.5), RowColToLatLong(Column + 0.5));
	#endregion

	#region Helpers
	private IEnumerable<Tile> EnumerateIndices()
	{
		for (int end = SUBINDEX_MAX_SZ; end < Path.Length; end += SUBINDEX_MAX_SZ)
			yield return new Tile(Path[..end]);
	}
	public override string ToString() => Path;
	public override int GetHashCode() => Path.GetHashCode();
	public override bool Equals(object? obj) => obj is Tile other && other.Path == Path;

	/// <summary>
	/// Gets the number of columns between teo <see cref="Tile"/>s. May span 180/-180
	/// </summary>
	/// <param name="leftTile">The left (western) <see cref="Tile"/> of the region</param>
	/// <param name="rightTile">The right (eastern) <see cref="Tile"/> of the region</param>
	/// <returns>The column span</returns>
	/// <exception cref="ArgumentException">thrown if boh <see cref="Tile"/>s do not have the same <see cref="Tile.Level"/></exception>
	public static int ColumnSpan(Tile leftTile, Tile rightTile)
	{
		if (leftTile.Level != rightTile.Level)
			throw new ArgumentException("Tile levels do not match", nameof(rightTile));

		return Util.Mod(rightTile.Column - leftTile.Column, 1 << rightTile.Level);
	}
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
			? getRootSubIndex(quadTreePath)
			: getSubIndex(getSubindexPath());

		string getSubindexPath()
			=> quadTreePath.Substring((quadTreePath.Length - 1) / SUBINDEX_MAX_SZ * SUBINDEX_MAX_SZ);

		static int getSubIndex(string quadTreePath)
			=> getRootSubIndex(quadTreePath) + (quadTreePath[0] - 0x30) * 85 + 1;

		static int getRootSubIndex(string quadTreePath)
		{
			ValidatePathCharacters(quadTreePath);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(quadTreePath.Length, SUBINDEX_MAX_SZ, nameof(quadTreePath));

			int subIndex = 0;

			for (int i = 1; i < quadTreePath.Length; i++)
			{
				subIndex *= SUBINDEX_MAX_SZ;
				subIndex += quadTreePath[i] - 0x30 + 1;
			}
			return subIndex;
		}
	}
	#endregion

	#region Validation

	[StackTraceHidden]
	private static void ValidateQuadTreePath([NotNull] string? quadTreePath)
	{
		ValidatePathCharacters(quadTreePath);
		if (quadTreePath[0] != '0')
			throw new ArgumentException("All quadtree paths must begin with a '0'", nameof(quadTreePath));
	}

	[StackTraceHidden]
	private static void ValidatePathCharacters([NotNull] string? quadTreePath)
	{
		ArgumentException.ThrowIfNullOrEmpty(quadTreePath, nameof(quadTreePath));
		if (quadTreePath?.All(c => c is '0' or '1' or '2' or '3') is not true)
			throw new ArgumentException("Quad Tree Path can only contain the characters '0', '1', '2', and '3'", nameof(quadTreePath));
	}
	#endregion
}
