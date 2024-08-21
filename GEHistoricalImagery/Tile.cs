using System.Diagnostics.CodeAnalysis;

namespace GEHistoricalImagery;

internal class Tile
{
	public int Level { get; }
	public int Row { get; }
	public int Column { get; }
	public QtPath QtPath { get; }

	private double RowColToLatLong(double rowCol)
		=> rowCol * 360d / (1 << Level) - 180;

	public Coordinate LowerLeft => new(RowColToLatLong(Row), RowColToLatLong(Column));
	public Coordinate LowerRight => new(RowColToLatLong(Row), RowColToLatLong(Column + 1));
	public Coordinate UpperLeft => new(RowColToLatLong(Row + 1), RowColToLatLong(Column));
	public Coordinate UpperRight => new(RowColToLatLong(Row + 1), RowColToLatLong(Column + 1));

	public Coordinate GetCenter()
	{
		var ll = LowerLeft;
		var ur = UpperRight;

		var midLat = (ll.Latitude + ur.Latitude) / 2;
		var midLong = (ll.Longitude + ur.Longitude) / 2;
		return new Coordinate(midLat, midLong);
	}


	/*
		   c0    c1
		|-----|-----|
	r1	|  3  |  2  |
		|-----|-----|
	r0	|  0  |  1  |
		|-----|-----|
	*/


	public Tile(int rowIndex, int colIndex, int level)
	{
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
		if (!QtPath.TryParse(new string(chars), out var p))
			throw new InvalidDataException($"Unable to convert tole to quad tree path. R = {Row}, C = {Column}, Z = {Level}");
		QtPath = p;
	}

	public static bool TryParse(string rowColId, [NotNullWhen(true)] out Tile? quadtreePath)
	{
		int rowIndex = 0;
		int colIndex = 0;

		for (int i = 0; i < rowColId.Length; i++)
		{
			if (rowColId[i] < '0' || rowColId[i] > '3')
			{
				quadtreePath = null;
				return false;
			}

			var cell = rowColId[i] & 3;
			int row = cell >> 1;
			int col = row ^ (cell & 1);

			rowIndex = (rowIndex << 1) | row;
			colIndex = (colIndex << 1) | col;
		}

		quadtreePath = new Tile(rowIndex, colIndex, rowColId.Length - 1);
		return true;
	}

	public override string ToString() => $"{Level}: {Column}, {Row}";
}
