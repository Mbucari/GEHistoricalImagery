namespace GEHistoricalImagery;

internal static class QuadtreeExtensions
{
	public static int ToJpegCommentDate(this DateOnly date)
		=> ((date.Year & 0x7FF) << 9) | ((date.Month & 0xf) << 5) | (date.Day & 0x1f);
	public static DateOnly ToDate(this int datedTile)
		=> new(datedTile >> 9, (datedTile >> 5) & 0xf, datedTile & 0x1f);
}