using System.Diagnostics.CodeAnalysis;

namespace LibMapCommon;

public readonly struct PixelPoint : IEquatable<PixelPoint>, ICoordinate
{
	private readonly double _X, _Y;
	public double X => _X;

	public double Y => _Y;

	/// <summary>
	/// Initialize a new <see cref="PixelPoint"/> instance.
	/// </summary>
	/// <param name="x">The pixel's global X coordinate</param>
	/// <param name="y">The pixel's global Y coordinate</param>
	public PixelPoint(int level, double x, double y)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level, nameof(level));
		var equator = 256 << level;
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(double.Abs(x), equator, nameof(x));
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(double.Abs(y), equator, nameof(y));

		_X = x;
		_Y = y;
	}

	public override int GetHashCode()
		=> HashCode.Combine(_X, _Y);
	public bool Equals(PixelPoint other)
		=> other.X == X && other.Y == Y;
	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is PixelPoint other && Equals(other);
}
