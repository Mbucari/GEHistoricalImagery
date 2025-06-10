using System.Diagnostics.CodeAnalysis;

namespace LibMapCommon;

/// <summary>
/// A Web Mercator coordinate
/// </summary>
public readonly struct WebMercator : IEquatable<WebMercator>, IGeoCoordinate<WebMercator>
{
	public static double Equator => 40075016.68557849;
	public static int EpsgNumber => 3857;

	/// <summary> The X coordinate (meters)</summary>
	public double X { get; }
	/// <summary> The Y coordinate (meters)</summary>
	public double Y { get; }
	public static WebMercator Create(double x, double y) => new WebMercator(x, y);

	/// <summary>
	/// Initialize a new <see cref="WebMercator"/> instance.
	/// </summary>
	/// <param name="x">The Web Mercator's X coordinate</param>
	/// <param name="y">The Web Mercator's Y coordinate</param>
	/// <exception cref="ArgumentOutOfRangeException">The abs(<paramref name="x"/>) > <see cref="Equator"/>/2 or abs(<paramref name="y"/>) > <see cref="Equator"/>/2</exception>
	public WebMercator(double x, double y)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(double.Abs(x), Equator / 2, nameof(x));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(double.Abs(y), Equator / 2, nameof(y));

		X = x;
		Y = y;
	}

	/// <summary>
	/// converts the Web Mercator coordinate to a WGS 84 geographic coordinate.
	/// </summary>
	public Wgs1984 ToWgs1984()
	{
		var longitude = X * 360 / Equator;
		var latitude = Math.Atan(Math.Exp(Y * 2 * Math.PI / Equator)) * 360 / Math.PI - 90;

		return new Wgs1984(latitude, longitude);
	}

	public override string ToString() => $"{X:F2}, {Y:F2}";
	public bool Equals(WebMercator other)
		=> other.X == X && other.Y == Y;
	public override int GetHashCode()
		=> HashCode.Combine(X, Y);
	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is WebMercator other && Equals(other);
	public static bool operator ==(WebMercator left, WebMercator right) => left.Equals(right);
	public static bool operator !=(WebMercator left, WebMercator right) => !left.Equals(right);
}
