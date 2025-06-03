namespace LibMapCommon.Geometry;

public readonly struct Line2(Vector2 origin, Vector2 direction)
{
	public readonly Vector2 Origin = origin;
	public readonly Vector2 Direction = direction;

	/// <returns>a vector containing the t and u multiples of the two lines at their intersection</returns>
	public Vector2 Intersect(Line2 other)
	{
		var A = new Matrix2x2(
			Direction.X, -other.Direction.X,
			Direction.Y, -other.Direction.Y);

		return
			A.Invert(out var A_1)
			? A_1 * (other.Origin - Origin)
			: new Vector2(float.NaN, float.NaN);
	}
}
