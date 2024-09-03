using GEHistoricalImagery.Cli;
using System.ComponentModel;

namespace GEHistoricalImagery;

/// <summary>
/// A geographic coordinate
/// </summary>
[TypeConverter(typeof(CoordinateTypeConverter))]
internal readonly struct Coordinate
{
	public readonly double Latitude, Longitude;
	public Tile GetTile(int level)
		=> new(LatLongToRowCol(Latitude, level), LatLongToRowCol(Longitude, level), level);

	private static int LatLongToRowCol(double latLong, int level)
		=> (int)Math.Floor((latLong + 180) / 360 * (1 << level));

	public Coordinate(double latitude, double longitude)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90, nameof(latitude));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90, nameof(latitude));
		ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180, nameof(longitude));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180, nameof(longitude));
		Latitude = latitude;
		Longitude = longitude;
	}

	public override string ToString() => $"{Latitude:F7},{Longitude:F7}";
}
