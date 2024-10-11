using LibGoogleEarth.TypeConverters;
using System.ComponentModel;

namespace LibGoogleEarth;

/// <summary>
/// A geographic coordinate
/// </summary>
[TypeConverter(typeof(CoordinateTypeConverter))]
public readonly struct Coordinate
{
	/// <summary> The <see cref="Coordinate"/>'s longitude </summary>
	public readonly double Latitude;
	/// <summary> The <see cref="Coordinate"/>'s latitude </summary>
	public readonly double Longitude;
	/// <summary> Indicates whether this instance is a valid geographic coordinate </summary>
	public readonly bool IsValidGeographicCoordinate => Math.Abs(Latitude) <= 90 && Math.Abs(Longitude) <= 180;

	/// <summary>
	/// Gets the <see cref="Tile"/> containing this <see cref="Coordinate"/> at a specified zoom level.
	/// </summary>
	/// <param name="level">The <see cref="Tile"/>'s zoom level</param>
	public Tile GetTile(int level)
	{
		return new(Util.LatLongToRowCol(Latitude, level), Util.LatLongToRowCol(Longitude, level), level);
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
