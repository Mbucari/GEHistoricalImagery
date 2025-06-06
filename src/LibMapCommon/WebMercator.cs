﻿using System.Diagnostics.CodeAnalysis;

namespace LibMapCommon;

/// <summary>
/// A Web Mercator coordinate
/// </summary>
public readonly struct WebMercator : IEquatable<WebMercator>, ICoordinate<WebMercator>
{
	public static double Equator => 40075016.68557849;
	public static int EpsgNumber => 3857;

	private readonly double _Y;
	private readonly double _X;

	/// <summary> The X coordinate (meters)</summary>
	public double X => _X;
	/// <summary> The Y coordinate (meters)</summary>
	public double Y => _Y;

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

		_X = x;
		_Y = y;
	}

	/// <summary>
	/// converts the Web Mercator coordinate to a WGS 84 geographic coordinate.
	/// </summary>
	public Wgs1984 ToWgs1984()
	{
		var longitude = X * 360 / Equator;
		var latitude = double.Atan(double.Exp(Y * 2 * double.Pi / Equator)) * 360 / double.Pi - 90;

		return new Wgs1984(latitude, longitude);
	}

	public override string ToString() => $"{X:F2}, {Y:F2}";
	public bool Equals(WebMercator other)
		=> other.X == X && other.Y == Y;
	public override int GetHashCode()
		=> HashCode.Combine(_X, _Y);
	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is WebMercator other && Equals(other);

	public static WebMercator FromWgs84(Wgs1984 wgs1984) => wgs1984.ToWebMercator();
}
