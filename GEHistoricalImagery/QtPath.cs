using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GEHistoricalImagery;

public class QtPath
{
	public string Path { get; }
	public int SubIndex { get; }
	public bool IsRoot => Path == Root.Path;

	public static readonly QtPath Root = new(["0"]);
	private readonly string[] Indices;
	private const int SUBINDEX_MAX_SZ = 4;

	private QtPath(string[] pathParts)
	{
		ArgumentNullException.ThrowIfNull(pathParts, nameof(pathParts));
		ArgumentOutOfRangeException.ThrowIfZero(pathParts.Length, nameof(pathParts));

		Path = string.Concat(pathParts);
		SubIndex
			= pathParts.Length == 1
			? GetRootSubIndex(pathParts[0])
			: GetSubIndex(pathParts[^1]);

		Array.Resize(ref pathParts, pathParts.Length - 1);
		Indices = pathParts;
	}

	public IEnumerable<QtPath> EnumerateIndices()
	{
		for (int i = 0; i < Indices.Length; i++)
			yield return new QtPath(Indices.Take(i + 1).ToArray());
	}

	public override string ToString() => Path;
	public override int GetHashCode() => Path.GetHashCode();
	public override bool Equals(object? obj)
		=> obj is QtPath other && other.Path == Path;

	public static bool TryParse(string? quadTreePath, [NotNullWhen(true)] out QtPath? qtPath)
	{
		if (quadTreePath is null || quadTreePath.Length == 0 || quadTreePath[0] != '0' || !IsPathValid(quadTreePath))
		{
			qtPath = null;
			return false;
		}

		var qtpParts = quadTreePath.Chunk(SUBINDEX_MAX_SZ).Select(c => new string(c)).ToArray();

		qtPath = new QtPath(qtpParts);
		Debug.Assert(qtPath.Path == quadTreePath);
		return true;
	}

	// Nodes have two numbering schemes:
	//
	// 1) "Subindex".  This numbering starts at the top of the tree
	// and goes left-to-right across each level, like this:
	//
	//                    0
	//                 /     \                           .
	//               1  86 171 256
	//            /     \                                .
	//          2  3  4  5 ...
	//        /   \                                      .
	//       6 7 8 9  ...
	//
	// Notice that the second row is weird in that it's not left-to-right
	// order.  HOWEVER, the root node in Keyhole is special in that it
	// doesn't have this weird ordering.  It looks like this:
	//
	//                    0
	//                 /     \                           .
	//               1  2  3  4
	//            /     \                                .
	//          5  6  7  8 ...
	//       /     \                                     .
	//     21 22 23 24  ...
	//
	// The mangling of the second row is controlled by a parameter to the
	// constructor.

	private static int GetSubIndex(string qtp)
		=> GetRootSubIndex(qtp) + (qtp[0] - 0x30) * 85 + 1;

	private static int GetRootSubIndex(string quadTreePath)
	{
		ArgumentException.ThrowIfNullOrEmpty(nameof(quadTreePath));
		ArgumentOutOfRangeException.ThrowIfLessThan(quadTreePath.Length, 1, nameof(quadTreePath));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(quadTreePath.Length, SUBINDEX_MAX_SZ, nameof(quadTreePath));

		if (!IsPathValid(quadTreePath))
			throw new ArgumentException("Quad Tree Path can only contain the characters '0', '1', '2', and '3'", nameof(quadTreePath));

		int subIndex = 0;

		for (int i = 1; i < quadTreePath.Length; i++)
		{
			subIndex *= 4;
			subIndex += quadTreePath[i] - 0x30 + 1;
		}
		return subIndex;
	}

	static bool IsPathValid(string? quadTreePath)
		=> quadTreePath?.All(c => c is '0' or '1' or '2' or '3') is true;
}
