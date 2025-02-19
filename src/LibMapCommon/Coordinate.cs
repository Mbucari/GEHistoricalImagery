using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace LibMapCommon;


/// <summary>
/// A WGS 1984 geographic coordinate
/// </summary>
[TypeConverter(typeof(CoordinateTypeConverter))]
public readonly struct Coordinate : IEquatable<Coordinate>
{
	/// <summary> The <see cref="Coordinate"/>'s longitude </summary>
	public readonly double Latitude;
	/// <summary> The <see cref="Coordinate"/>'s latitude </summary>
	public readonly double Longitude;
	/// <summary> Indicates whether this instance is a valid geographic coordinate </summary>
	public readonly bool IsValidGeographicCoordinate => Math.Abs(Latitude) <= 90 && Math.Abs(Longitude) <= 180;

	/// <summary>
	/// Gets the <see cref="KeyholeTile"/> containing this <see cref="Coordinate"/> at a specified zoom level.
	/// </summary>
	/// <param name="level">The <see cref="KeyholeTile"/>'s zoom level</param>
	public T GetTile<T>(int level) where T : ITile<T>
	{
		return T.GetTile(this, level);
	}

	/// <summary>
	/// Initialize a new <see cref="Coordinate"/> instance.
	/// </summary>
	/// <param name="latitude">The geographic coordinate's longitude</param>
	/// <param name="longitude">The geographic coordinate's latitude</param>
	/// <exception cref="ArgumentOutOfRangeException">The abs(<paramref name="latitude"/>) > 180 or abs(<paramref name="longitude"/>) > 180</exception>
	public Coordinate(double latitude, double longitude)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(latitude), 180, nameof(latitude));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(longitude), 180, nameof(longitude));
		Latitude = latitude;
		Longitude = longitude;
	}

	public override string ToString() => ToString(CoordinateFormat.DecimalDegrees);

	public string ToString(CoordinateFormat numberFormat)
	{
		if (!Enum.IsDefined(numberFormat))
			throw new ArgumentOutOfRangeException(nameof(numberFormat), $"Enum value ({numberFormat}) is not defined");

		return GetCoordinate(Latitude, ['S', 'N'], numberFormat)
			+ ", "
			+ GetCoordinate(Longitude, ['W', 'E'], numberFormat);
	}

	private static string GetCoordinate(double coordinate, char[]? negPos, CoordinateFormat numberFormat)
	{
		if (numberFormat is CoordinateFormat.D_DecimalMins or CoordinateFormat.DM_DecimalSecs && negPos != null)
		{
			int sign = Math.Sign(coordinate);
			char direction = negPos[sign == -1 ? 0 : 1];
			coordinate *= sign;
			double degrees = (int)coordinate;
			double minutes = (coordinate - degrees) * 60;

			if (numberFormat is CoordinateFormat.D_DecimalMins)
				return $"{degrees}°{minutes:F3}'{direction}";

			double seconds = (minutes - (int)minutes) * 60;
			return $"{degrees}°{(int)minutes}'{seconds:F2}\"{direction}";

		}
		else
			return $"{coordinate:F6}°";
	}

	/// <summary>
	/// converts the WGS 84 geographic coordinate to a Web Mercator coordinate.
	/// </summary>
	public WebCoordinate ToWebMercator()
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(double.Abs(Latitude), 85.05, nameof(Latitude));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(double.Abs(Longitude), 180, nameof(Longitude));

		//https://gis.stackexchange.com/questions/17336/transforming-epsg3857-to-epsg4326
		//https://gis.stackexchange.com/questions/153839/how-to-transform-epsg3857-to-tile-pixel-coordinates-at-zoom-factor-0

		var x = Longitude * WebCoordinate.MetersPerDegree;
		var y = double.Log(double.Tan((90 + Latitude) * double.Pi / 360)) / (double.Pi / 180) * WebCoordinate.MetersPerDegree;
		return new WebCoordinate(x, y);
	}

	public bool Equals(Coordinate other)
		=> Latitude == other.Latitude && Longitude == other.Longitude;
	public override int GetHashCode()
		=> Latitude.GetHashCode() ^ Longitude.GetHashCode();
	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is Coordinate other && Equals(other);
}

public enum CoordinateFormat
{
	//Decimal degrees
	DecimalDegrees,
	//Degrees, decimal minutes
	D_DecimalMins,
	//Degrees, minutes, seconds
	DM_DecimalSecs,
}
