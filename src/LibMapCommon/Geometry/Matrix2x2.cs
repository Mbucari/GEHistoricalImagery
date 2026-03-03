namespace LibMapCommon.Geometry;

public readonly record struct Matrix2x2(double M11, double M12, double M21, double M22)
{
	public static Vector2 Multiply(Matrix2x2 m, Vector2 v)
		=> new Vector2(m.M11 * v.X + m.M12 * v.Y, m.M21 * v.X + m.M22 * v.Y);

	public static Vector2 Multiply(Matrix2x2 m, Vector2 v, int roundingDigits)
		=> new Vector2(
			Math.Round(m.M11 * v.X + m.M12 * v.Y, roundingDigits),
			Math.Round(m.M21 * v.X + m.M22 * v.Y, roundingDigits));

	public bool Invert(out Matrix2x2 result)
	{
		var det = M11 * M22 - M21 * M12;

		if (Math.Abs(det) < double.Epsilon)
		{
			result = new Matrix2x2(double.NaN, double.NaN, double.NaN, double.NaN);
			return false;
		}

		var invDet = 1.0 / det;

		result = new Matrix2x2(
			  M22 * invDet, -M12 * invDet,
			 -M21 * invDet, M11 * invDet);

		return true;
	}
}
