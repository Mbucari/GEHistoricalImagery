using LibMapCommon;
using LibMapCommon.Geometry;

namespace LibEsri;

public class EsriTile : ITile<EsriTile, WebMercator>
{
	
	public int Level { get; }
	/// <summary> The number of <see cref="EsriTile"/> rows from the top-most (north-most) edge of the map. </summary>
	public int Row { get; }
	public int Column { get; }

	public const int MaxLevel = 23;

	public EsriTile(int rowIndex, int colIndex, int level)
	{
		Level = level;
		Row = rowIndex;
		Column = colIndex;
	}

	private Wgs1984 ToCoordinate(double column, double row)
	{
		var n = Math.Pow(2, Level);

		var lon_deg = column / n * 360d - 180d;
		var lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * row / n)));
		var lat_deg = lat_rad * 180.0 / Math.PI;
		return new Wgs1984(lat_deg, lon_deg);
	}

	public Wgs1984 Wgs84Center => ToCoordinate(Column + 0.5, Row + 0.5);
	public WebMercator Center => Wgs84Center.ToWebMercator();
	public WebMercator LowerLeft => ToCoordinate(Column, Row + 1).ToWebMercator();
	public WebMercator LowerRight => ToCoordinate(Column + 1, Row + 1).ToWebMercator();
	public WebMercator UpperLeft => ToCoordinate(Column, Row).ToWebMercator();
	public WebMercator UpperRight => ToCoordinate(Column + 1, Row).ToWebMercator();
	public GeoPolygon<WebMercator> GetGeoPolygon() => new GeoPolygon<WebMercator>([LowerLeft, UpperLeft, UpperRight, LowerRight]);

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
