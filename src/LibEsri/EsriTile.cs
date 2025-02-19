using LibMapCommon;

namespace LibEsri;

public class EsriTile : ITile<EsriTile>
{
	public int Level { get; }
	/// <summary> The number of <see cref="EsriTile"/> rows from the top-most (north-most) edge of the map. </summary>
	public int Row { get; }
	public int Column { get; }

	public EsriTile(int rowIndex, int colIndex, int level)
	{
		Level = level;
		Row = rowIndex;
		Column = colIndex;
	}

	private Coordinate ToCoordinate(double column, double row)
	{
		var n = Math.Pow(2, Level);

		var lon_deg = column / n * 360d - 180d;
		var lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * row / n)));
		var lat_deg = lat_rad * 180.0 / Math.PI;
		return new Coordinate(lat_deg, lon_deg);
	}

	public Coordinate LowerLeft => ToCoordinate(Column, Row + 1);
	public Coordinate LowerRight => ToCoordinate(Column + 1, Row + 1);
	public Coordinate UpperLeft => ToCoordinate(Column, Row);
	public Coordinate UpperRight => ToCoordinate(Column + 1, Row);
	public Coordinate Center => ToCoordinate(Column + 0.5, Row + 0.5);

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

	public (int gpx_x, int gpx_y) GetTopLeftGlobalPixel(int tileSize)
	{
		return (Column * tileSize, Row * tileSize);
	}

	public static EsriTile GetTile(Coordinate coordinate, int level)
	{
		var webCoord = coordinate.ToWebMercator();

		var size = 1 << level;

		var col = (int)double.Floor((0.5 + webCoord.X / WebCoordinate.Equator) * size);
		var row = (int)double.Floor((0.5 - webCoord.Y / WebCoordinate.Equator) * size);

		return new EsriTile(row, col, level);
	}

	public static EsriTile GetMinimumCorner(Coordinate c1, Coordinate c2, int level)
	{
		var topMost = Math.Max(c1.Latitude, c2.Latitude);
		var leftMost = Math.Min(c1.Longitude, c2.Longitude);
		return GetTile(new Coordinate(topMost, leftMost), level);
	}

	public static EsriTile Create(int row, int col, int level)
		=> new EsriTile(row, col, level);
}
