using System.Diagnostics.CodeAnalysis;

namespace LibMapCommon;

public readonly struct PixelPoint : IEquatable<PixelPoint>, ICoordinate
{
	public double X { get; }
	public double Y { get; }

	/// <summary>
	/// Initialize a new <see cref="PixelPoint"/> instance.
	/// </summary>
	/// <param name="x">The pixel's global X coordinate</param>
	/// <param name="y">The pixel's global Y coordinate</param>
	public PixelPoint(int level, double x, double y)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level, nameof(level));
		var equator = 256 << level;
		ArgumentOutOfRangeException.ThrowIfGreaterThan(double.Abs(x), equator, nameof(x));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(double.Abs(y), equator, nameof(y));

		X = x;
		Y = y;
	}

	public override int GetHashCode()
		=> HashCode.Combine(X, Y);
	public bool Equals(PixelPoint other)
		=> other.X == X && other.Y == Y;
	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is PixelPoint other && Equals(other);
	public static bool operator ==(PixelPoint left, PixelPoint right) => left.Equals(right);
	public static bool operator !=(PixelPoint left, PixelPoint right) => !left.Equals(right);
}
