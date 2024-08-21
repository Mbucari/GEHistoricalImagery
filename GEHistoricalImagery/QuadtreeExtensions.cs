using Keyhole;

namespace GEHistoricalImagery;

internal static class QuadtreeExtensions
{
	public static int ToJpegCommentDate(this DateOnly date)
		=> ((date.Year & 0x7FF) << 9) | ((date.Month & 0xf) << 5) | (date.Day & 0x1f);
	public static DateOnly GetDate(this QuadtreeImageryDatedTile datedTile)
		=> new(datedTile.Date >> 9, (datedTile.Date >> 5) & 0xf, datedTile.Date & 0x1f);
}