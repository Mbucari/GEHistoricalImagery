using System.Diagnostics;

namespace LibGoogleEarth;

internal static class Util
{
	public static int Mod(int value, int modulus)
	{
		var result = value % modulus;
		return result >= 0 ? result : result + modulus;
	}

	[StackTraceHidden]
	public static void ValidateLevel(int level)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(level, nameof(level));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(level, Tile.MaxLevel, nameof(level));
	}
}
