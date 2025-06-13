using System.Diagnostics;

namespace LibMapCommon.Geometry;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Vector3
{
	public double X { get; }
	public double Y { get; }
	public double Z { get; }
	public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
	public Vector3(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public static double GetAngle(Vector3 A, Vector3 B, Vector3 C)
	{
		var u = A.Cross(B);
		var v = C.Cross(B);

		var ddd = u.Dot(v) / u.Length / v.Length;
		return Math.Acos(ddd);
	}

	public double Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;
	public Vector3 Cross(Vector3 other)
		=> new Vector3(
			Y * other.Z - Z * other.Y,
			Z * other.X - X * other.Z,
			X * other.Y - Y * other.X);

	public string DebuggerDisplay => $"{X},{Y},{Z}";
}
