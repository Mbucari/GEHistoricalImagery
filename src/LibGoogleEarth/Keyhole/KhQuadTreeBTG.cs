using System.Runtime.InteropServices;

namespace Keyhole;

[StructLayout(LayoutKind.Sequential, Size = 2, Pack = 2)]
internal readonly record struct KhQuadTreeBTG
{
	private static readonly byte[] bytemaskBTG = {
	  0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80
	};

	private readonly byte children;

	public bool GetBit(int bit) { return (children & bytemaskBTG[bit]) != 0; }

	public bool Child0 => GetBit(0);
	public bool Child1 => GetBit(1);
	public bool Child2 => GetBit(2);
	public bool Child3 => GetBit(3);

	// CacheNodeBit indicates a node on last level.
	// client does not process children info for these,
	// since we don't actually have info for the children.
	// As a result, no need to set any of the children bits for
	// cache nodes, since client will simply disregard them.
	public bool HasCacheNode => GetBit(4);

	// Set if this node contains vector data.
	public bool HasDrawable => GetBit(5);

	// Set if this node contains image data.
	public bool HasImage => GetBit(6);

	// Set if this node contains terrain data.
	public bool HasTerrain => GetBit(7);
}
