using System.Diagnostics;

namespace LibMapCommon.Geometry;

[DebuggerDisplay("{X},{Y}")]
public readonly struct Vector2(double x, double y) : ICoordinate
{
	public readonly double X { get; } = x;
	public readonly double Y { get; } = y;

	public static Vector2 UnitY => new Vector2(0, 1);

	public static Vector2 operator -(Vector2 left, Vector2 right)
		=> new Vector2(left.X - right.X, left.Y - right.Y);

	public static Vector2 operator -(Vector2 vector)
		=> new Vector2(-vector.X, -vector.Y);

	public double Dot(Vector2 other) => X * other.X + Y * other.Y;
	public double Cross(Vector2 other) => X * other.Y - other.X * Y;
	public double Length => Math.Sqrt(X * X + Y * Y);
}
