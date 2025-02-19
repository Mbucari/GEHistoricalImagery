namespace LibEsri.Geometry;

internal struct Matrix2x2
{
	public double M11; public double M12;
	public double M21; public double M22;

	public Matrix2x2(double m11, double m12, double m21, double m22)
	{
		M11 = m11; M12 = m12; M21 = m21; M22 = m22;
	}

	public static Vector2 operator *(Matrix2x2 m, Vector2 v)
		=> new Vector2(m.M11 * v.X + m.M12 * v.Y, m.M21 * v.X + m.M22 * v.Y);

	public static bool Invert(Matrix2x2 matrix, out Matrix2x2 result)
	{
		var det = matrix.M11 * matrix.M22 - matrix.M21 * matrix.M12;

		if (double.Abs(det) < double.Epsilon)
		{
			result = new Matrix2x2(double.NaN, double.NaN, double.NaN, double.NaN);
			return false;
		}

		var invDet = 1.0 / det;

		result.M11 = matrix.M22 * invDet;
		result.M12 = -matrix.M12 * invDet;

		result.M21 = -matrix.M21 * invDet;
		result.M22 = matrix.M11 * invDet;

		return true;
	}
}
