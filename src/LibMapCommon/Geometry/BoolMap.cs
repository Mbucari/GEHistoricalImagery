using System.Collections;

namespace LibMapCommon.Geometry;

public class BoolMap
{
	public int Width { get; }
	public int Height { get; }
	private readonly BitArray _bitArray;
	public BoolMap(int width, int height)
	{
		Width = width;
		Height = height;
		_bitArray = new BitArray(width * height);
	}
	public BoolMap(int width, int height, bool[] array)
	{
		if (array.Length != width * height)
			throw new ArgumentException("Array length must match width * height.");
		Width = width;
		Height = height;
		_bitArray = new BitArray(array);
	}
	private BoolMap(int width, int height, BitArray bitArray)
	{
		Width = width;
		Height = height;
		_bitArray = bitArray;
	}
	public bool this[int row, int col]
	{
		get => _bitArray.Get(row * Width + col);
		set => _bitArray.Set(row * Width + col, value);
	}
	public BoolMap Or(BoolMap other)
		=> Width != other.Width || Height != other.Height
			? throw new ArgumentException("BoolMaps must have the same dimensions to perform OR operation.")
			: new(Width, Height, _bitArray.Or(other._bitArray));
}
