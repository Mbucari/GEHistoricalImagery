using LibMapCommon;
using LibMapCommon.Geometry;

namespace LibEsri;

public class EsriTile : ITile<EsriTile, WebMercator>
{
	public int Level { get; }
	/// <summary> The number of <see cref="EsriTile"/> rows from the top-most (north-most) edge of the map. </summary>
	public int Row { get; }
	public int Column { get; }
	public bool RowsIncreaseToSouth => true;

	public const int MaxLevel = 23;

	public EsriTile(int rowIndex, int colIndex, int level)
	{
		Util.ValidateLevel(level, MaxLevel);
		Level = level;
		Row = rowIndex;
		Column = colIndex;
	}

	private WebMercator ToCoordinate(double column, double row)
	{
		var n = 1 << Level;
		var x = (column / n - 0.5) * WebMercator.Equator;
		var y = (0.5 - row / n) * WebMercator.Equator;

		return new WebMercator(x, y);
	}

	public Wgs1984 Wgs84Center => Center.ToWgs1984();
	public WebMercator Center => ToCoordinate(Column + 0.5, Row + 0.5);
	public WebMercator LowerLeft => ToCoordinate(Column, Row + 1);
	public WebMercator LowerRight => ToCoordinate(Column + 1, Row + 1);
	public WebMercator UpperLeft => ToCoordinate(Column, Row);
	public WebMercator UpperRight => ToCoordinate(Column + 1, Row);

	/// <summary>
	/// Gets the number of columns between teo <see cref="EsriTile"/>s. May span 180/-180
	/// </summary>
	/// <param name="leftTile">The left (western) <see cref="EsriTile"/> of the region</param>
	/// <param name="rightTile">The right (eastern) <see cref="EsriTile"/> of the region</param>
	/// <returns>The column span</returns>
	/// <exception cref="ArgumentException">thrown if boh <see cref="EsriTile"/>s do not have the same <see cref="Level"/></exception>
	public static int ColumnSpan(EsriTile leftTile, EsriTile rightTile)
	{
		if (leftTile.Level != rightTile.Level)
			throw new ArgumentException("Tile levels do not match", nameof(rightTile));

		return Util.Mod(rightTile.Column - leftTile.Column, 1 << rightTile.Level);
	}

	public static EsriTile GetTile(WebMercator webCoord, int level)
	{
		var size = Util.ValidateLevel(level, MaxLevel);

		var column = (0.5 + webCoord.X / WebMercator.Equator) * size;
		var row = (0.5 - webCoord.Y / WebMercator.Equator) * size;

		return new EsriTile((int)row, (int)column, level);
	}

	public static EsriTile Create(int row, int col, int level)
		=> new EsriTile(row, col, level);
}
