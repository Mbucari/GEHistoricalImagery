using System.Diagnostics.CodeAnalysis;

namespace LibMapCommon;

/// <summary>
/// A Web Mercator coordinate
/// </summary>
public readonly struct WebCoordinate : IEquatable<WebCoordinate>
{
	public const double Equator = 40075016.68557849;
	internal const double HalfEquator = Equator / 2;
	internal const double MetersPerDegree = Equator / 360;

	/// <summary> The X coordinate (meters)</summary>
	public readonly double X;
	/// <summary> The Y coordinate (meters)</summary>
	public readonly double Y;

	/// <summary>
	/// Initialize a new <see cref="Coordinate"/> instance.
	/// </summary>
	/// <param name="x">The Web Mercator's X coordinate</param>
	/// <param name="y">The Web Mercator's Y coordinate</param>
	/// <exception cref="ArgumentOutOfRangeException">The abs(<paramref name="x"/>) > <see cref="HalfEquator"/> or abs(<paramref name="y"/>) > <see cref="HalfEquator"/></exception>
	public WebCoordinate(double x, double y)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(double.Abs(x), HalfEquator, nameof(x));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(double.Abs(y), HalfEquator, nameof(y));

		X = x;
		Y = y;
	}

	/// <summary>
	/// converts the Web Mercator coordinate to a WGS 84 geographic coordinate.
	/// </summary>
	public Coordinate ToWgs1984()
	{
		var longitude = X / MetersPerDegree;
		var latitude = double.Atan(double.Exp(Y * double.Pi / HalfEquator)) * 360 / double.Pi - 90;

		return new Coordinate(latitude, longitude);
	}

	public override string ToString() => $"{X:F2}, {Y:F2}";
	public bool Equals(WebCoordinate other)
		=> other.X == X && other.Y == Y;
	public override int GetHashCode()
		=> X.GetHashCode() ^ Y.GetHashCode();
	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is WebCoordinate other && Equals(other);
}
