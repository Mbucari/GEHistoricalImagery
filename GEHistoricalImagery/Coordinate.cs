using GoogleEarthImageDownload.Cli;
using System.ComponentModel;

namespace GoogleEarthImageDownload;

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
		Latitude = latitude;
		Longitude = longitude;
	}

	public override string ToString() => $"{Latitude:F7},{Longitude:F7}";
}
