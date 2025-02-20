using System.Runtime.InteropServices;

namespace Keyhole;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct KhQuadTreeQuantum16
{
	private const int kImageNeighborCount = 8;
	private const int kSerialSize = 32; // size when serialized
	public readonly KhQuadTreeBTG children;

	public readonly short cnode_version;  // cachenode version
	public readonly short image_version;
	public readonly short terrain_version;

	public readonly short num_channels;
	private readonly ushort junk16;
	internal readonly int type_offset;
	internal readonly int version_offset;


	internal readonly long image_neighbors;


	// Data provider info.
	// Terrain data provider does not seem to be used.
	public readonly byte image_data_provider;
	public readonly byte terrain_data_provider;
	private readonly ushort junk16_2;
}
