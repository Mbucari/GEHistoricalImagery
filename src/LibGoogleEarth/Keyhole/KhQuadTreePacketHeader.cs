using System.Runtime.InteropServices;

namespace Keyhole;

[StructLayout(LayoutKind.Sequential, Size = 32, Pack = 4)]
public readonly record struct KhQuadTreePacketHeader
{
	public const int HEADER_SIZE = 32;
	private const uint kKeyholeMagicId = 32301;
	public readonly uint MagicId;
	public readonly uint DataTypeId;
	public readonly int Version;
	public readonly int NumInstances;
	public readonly int DataInstanceSize;
	public readonly int DataBufferOffset;
	public readonly int DataBufferSize;
	public readonly int MetaBufferSize;

	public static KhQuadTreePacketHeader ParseFrom(Span<byte> bytes)
	{
		if (bytes.Length < sizeof(int) * 8)
			throw new ArgumentException("buffer is too small", nameof(bytes));

		var h = MemoryMarshal.Cast<byte, KhQuadTreePacketHeader>(bytes)[0];

		if (h.MagicId != kKeyholeMagicId)
			throw new InvalidDataException($"invalid magic_id: {h.MagicId}");

		if (h.NumInstances != 0 && h.DataBufferOffset != 32 + h.NumInstances * h.DataInstanceSize)
			throw new InvalidDataException("invalid data_buffer_offset");

		return h;

	}
}
