using System.Diagnostics;

namespace LibMapCommon;

public static class Util
{
	public static int Mod(int value, int modulus)
	{
		var result = value % modulus;
		return result >= 0 ? result : result + modulus;
	}

	private static (double pixelX, double pixelY) CoordinateToPixel(double x, double y, int level, double equator)
	{
		//https://github.com/Leaflet/Leaflet/discussions/8100
		const int TILE_SZ = 256;
		const int HALF_TILE = TILE_SZ / 2;
		var size = 1 << level;

		var tileX = (HALF_TILE + x * TILE_SZ / equator) * size;
		var tileY = (HALF_TILE - y * TILE_SZ / equator) * size;
		return (tileX, tileY);
	}

	public static int ToRoundedInt(this double value) => (int)Math.Round(value, 0);

	public static (double pixelX, double pixelY) GetGlobalPixelCoordinate<T>(this T coordinate, int level)
		where T : ICoordinate
		=> CoordinateToPixel(coordinate.X, coordinate.Y, level, T.Equator);

	/// <summary>
	/// Get the global pixel coordinates of the <see cref="ITile.UpperLeft"/> corner of this tile.
	/// </summary>
	/// <returns>X and Y coordinates of the pixel in global pixel space</returns>
	public static (int pixelX, int pixelY) GetTopLeftPixel<T>(this ITile tile)
		where T : ICoordinate<T>
	{
		var topLeft = T.FromWgs84(tile.UpperLeft);

		(double pixelX, double pixelY) = CoordinateToPixel(topLeft.X, topLeft.Y, tile.Level, T.Equator);
		//A tile corner coordinate should always be on an integer pixel.
		//Due to floating point errors, use rounding instead of floor/casting to int.
		return (pixelX.ToRoundedInt(), pixelY.ToRoundedInt());
	}

	[StackTraceHidden]
	public static int ValidateLevel(int level, int maxLevel)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(level, nameof(level));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(level, maxLevel, nameof(level));
		return 1 << level;
	}
}
