using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace LibMapCommon;

/// <summary>
/// A WGS 1984 geographic coordinate
/// </summary>
[TypeConverter(typeof(Wgs1984TypeConverter))]
public readonly struct Wgs1984 : IEquatable<Wgs1984>, IGeoCoordinate<Wgs1984>
{
	public static double Equator => 360d;
	public static int EpsgNumber => 4326;

	public double X { get; }
	public double Y { get; }

	/// <summary> The <see cref="Wgs1984"/>'s longitude </summary>
	public double Longitude => X;
	/// <summary> The <see cref="Wgs1984"/>'s latitude </summary>
	public double Latitude => Y;
	/// <summary> Indicates whether this instance is a valid geographic coordinate </summary>
	public readonly bool IsValidGeographicCoordinate => Math.Abs(Latitude) <= 90 && Math.Abs(Longitude) <= 180;
	public static Wgs1984 Create(double x, double y) => new Wgs1984(y, x);

	/// <summary>
	/// Initialize a new <see cref="Wgs1984"/> instance.
	/// </summary>
	/// <param name="latitude">The geographic coordinate's longitude</param>
	/// <param name="longitude">The geographic coordinate's latitude</param>
	/// <exception cref="ArgumentOutOfRangeException">The abs(<paramref name="latitude"/>) > 180 or abs(<paramref name="longitude"/>) > 180</exception>
	public Wgs1984(double latitude, double longitude)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(latitude), 180, nameof(latitude));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(longitude), 360, nameof(longitude));
		Y = latitude;
		X = longitude;
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
	public WebMercator ToWebMercator()
	{
		var longitude = NormalizedLongitude();
		ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(Latitude), 85.05, nameof(Latitude));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(longitude), 180, nameof(Longitude));

		//https://gis.stackexchange.com/questions/17336/transforming-epsg3857-to-epsg4326
		//https://gis.stackexchange.com/questions/153839/how-to-transform-epsg3857-to-tile-pixel-coordinates-at-zoom-factor-0

		var x = longitude * WebMercator.Equator / 360;
		var y = Math.Log(Math.Tan((90 + Latitude) * Math.PI / 360)) * WebMercator.Equator / (Math.PI * 2);

		return new WebMercator(x, y);
	}

	public double NormalizedLongitude() => Longitude < -180 ? Longitude + 360 : Longitude > 180 ? Longitude - 360 : Longitude;

	public bool Equals(Wgs1984 other)
		=> Latitude == other.Latitude && Longitude == other.Longitude;
	public override int GetHashCode()
		=> HashCode.Combine(X, Y);
	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is Wgs1984 other && Equals(other);
	public static bool operator ==(Wgs1984 left, Wgs1984 right) => left.Equals(right);
	public static bool operator !=(Wgs1984 left, Wgs1984 right) => !left.Equals(right);
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
