using LibMapCommon.Geometry;
using System.Diagnostics;

namespace LibMapCommon;

public static class Util
{
	public static GeoRegion<WebMercator> ToWebMercator(this GeoRegion<Wgs1984> geoPolygon)
		=> geoPolygon.ConvertTo(c => c.ToWebMercator());
	public static GeoRegion<Wgs1984> ToWgs1984(this GeoRegion<WebMercator> geoPolygon)
		=> geoPolygon.ConvertTo(c => c.ToWgs1984());
	public static GeoPolygon<WebMercator> ToWebMercator(this GeoPolygon<Wgs1984> geoPolygon)
		=> geoPolygon.ConvertTo(c => c.ToWebMercator());
	public static GeoPolygon<Wgs1984> ToWgs1984(this GeoPolygon<WebMercator> geoPolygon)
		=> geoPolygon.ConvertTo(c => c.ToWgs1984());

	public static Vector3 ToRectangular(this Wgs1984 wgs84)
	{
		var lat = wgs84.Latitude * Math.PI / 180;
		var lon = wgs84.Longitude * Math.PI / 180;
		var cosLat = Math.Cos(lat);
		return new Vector3(cosLat * Math.Cos(lon), cosLat * Math.Sin(lon), Math.Sin(lat));
	}

	public static int Mod(int value, int modulus)
	{
		var result = value % modulus;
		return result >= 0 ? result : result + modulus;
	}

	private static PixelPoint CoordinateToPixel(double x, double y, int level, double equator)
	{
		//https://github.com/Leaflet/Leaflet/discussions/8100
		const int TILE_SZ = 256;
		const int HALF_TILE = TILE_SZ / 2;
		var size = 1 << level;

		var tileX = (HALF_TILE + x * TILE_SZ / equator) * size;
		var tileY = (HALF_TILE - y * TILE_SZ / equator) * size;
		return new PixelPoint(level, tileX, tileY);
	}

	public static int ToRoundedInt(this double value) => (int)Math.Round(value, 0);

	public static PixelPoint GetGlobalPixelCoordinate<T>(this T coordinate, int level)
		where T : IGeoCoordinate<T>
		=> CoordinateToPixel(coordinate.X, coordinate.Y, level, T.Equator);

	/// <summary>
	/// Get the global pixel coordinates of the <see cref="ITile{T}.UpperLeft"/> corner of this tile.
	/// </summary>
	/// <returns>X and Y coordinates of the pixel in global pixel space</returns>
	public static PixelPoint GetTopLeftPixel<T>(this ITile<T> tile)
		where T : IGeoCoordinate<T>
	{
		var topLeft = tile.UpperLeft;

		var pixel = topLeft.GetGlobalPixelCoordinate(tile.Level);
		//A tile corner coordinate should always be on an integer pixel.
		//Due to floating point errors, use rounding instead of floor/casting to int.
		return new PixelPoint(tile.Level, pixel.X.ToRoundedInt(), pixel.Y.ToRoundedInt());
	}

	[StackTraceHidden]
	public static int ValidateLevel(int level, int maxLevel)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(level, nameof(level));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(level, maxLevel, nameof(level));
		return 1 << level;
	}
}
