using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace LibGoogleEarth;

public static class Util
{
	public static int GetTreeSubIndex(string quadTreePath)
			=> GetRootSubIndex(quadTreePath) + (quadTreePath[0] - 0x30) * 85 + 1;

	public static int GetRootSubIndex(string quadTreePath)
	{
		const int SUBINDEX_MAX_SZ = 4;
		ValidatePathCharacters(quadTreePath);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(quadTreePath.Length, SUBINDEX_MAX_SZ, nameof(quadTreePath));

		int subIndex = 0;

		for (int i = 1; i < quadTreePath.Length; i++)
		{
			subIndex *= SUBINDEX_MAX_SZ;
			subIndex += quadTreePath[i] - 0x30 + 1;
		}
		return subIndex;
	}
	public static int LatLongToRowCol(double latLong, int level)
	{
		int numTiles = LibMapCommon.Util.ValidateLevel(level, KeyholeTile.MaxLevel);
		int rowCol = (int)Math.Floor((latLong + 180) / 360 * numTiles);
		return Math.Min(rowCol, numTiles - 1);
	}

	public static double RowColToLatLong(int level, double rowCol)
	{
		int numTiles = LibMapCommon.Util.ValidateLevel(level, KeyholeTile.MaxLevel);
		ArgumentOutOfRangeException.ThrowIfNegative(rowCol, nameof(rowCol));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(rowCol, numTiles, nameof(rowCol));
		return rowCol * 360d / numTiles - 180;
	}

	[StackTraceHidden]
	public static void ValidateQuadTreePath([NotNull] string? quadTreePath)
	{
		ValidatePathCharacters(quadTreePath);
		if (quadTreePath[0] != '0')
			throw new ArgumentException("All quadtree paths must begin with a '0'", nameof(quadTreePath));
	}

	[StackTraceHidden]
	public static void ValidatePathCharacters([NotNull] string? quadTreePath)
	{
		ArgumentException.ThrowIfNullOrEmpty(quadTreePath, nameof(quadTreePath));
		if (quadTreePath?.All(c => c is '0' or '1' or '2' or '3') is not true)
			throw new ArgumentException("Quad Tree Path can only contain the characters '0', '1', '2', and '3'", nameof(quadTreePath));
	}
}
