namespace LibEsri.Geometry;

internal struct Line2
{
	public Vector2 Origin;
	public Vector2 Direction;

	/// <returns>a vector containing the t and u multiples of the two lines at their intersection</returns>
	public Vector2 Intersect(Line2 other)
	{
		var A = new Matrix2x2(
			Direction.X, -other.Direction.X,
			Direction.Y, -other.Direction.Y);

		var B = new Vector2(
			other.Origin.X - Origin.X,
			other.Origin.Y - Origin.Y);

		if (Matrix2x2.Invert(A, out var A_1))
		{
			var r = A_1 * B;
			return r;
		}
		else return new Vector2(float.NaN, float.NaN);
	}
}
