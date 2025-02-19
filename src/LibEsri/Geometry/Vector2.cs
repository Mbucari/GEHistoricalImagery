using LibMapCommon;

namespace LibEsri.Geometry;

internal struct Vector2
{
	public double X;
	public double Y;
	public Vector2(double x, double y)
	{
		X = x;
		Y = y;
	}
	public static Vector2 UnitX => new Vector2(1, 0);

	public static Vector2 operator -(Vector2 left, Vector2 right)
		=> new Vector2(left.X - right.X, left.Y - right.Y);

	public static explicit operator Vector2(WebCoordinate webCoordinate)
		=> new Vector2(webCoordinate.X, webCoordinate.Y);
}
