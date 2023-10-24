using Keyhole;

namespace GoogleEarthImageDownload;

internal static class QuadtreeExtensions
{
	private const int MIN_JPEG_DATE = 545;
	public static int ToJpegCommentDate(this DateOnly date)
		=> ((date.Year & 0x7FF) << 9) | ((date.Month & 0xf) << 5) | (date.Day & 0x1f);
	public static DateOnly ToDate(this QuadtreeImageryDatedTile datedTile)
		=> new(datedTile.Date >> 9, (datedTile.Date >> 5) & 0xf, datedTile.Date & 0x1f);
	public static DateOnly ToDate(this int datedTile)
		=> new(datedTile >> 9, (datedTile >> 5) & 0xf, datedTile & 0x1f);

	public static bool HasDate(this QuadtreeNode? node, DateOnly dateOnly)
		=> node
		?.GetAllDatedTiles()
		.Any(dt => dt.ToDate() == dateOnly) ?? false;

	public static IEnumerable<DateOnly> GetAllDates(this QuadtreeNode? node)
		=> node
		?.GetAllDatedTiles()
		.Select(dt => dt.ToDate()) ?? Enumerable.Empty<DateOnly>();

	// Imagery APPEARS to be unavailable when DatedTile.Provider is 0 or the date is MIN_JPEG_DATE
	public static IEnumerable<QuadtreeImageryDatedTile> GetAllDatedTiles(this QuadtreeNode? node)
		=> node
		?.Layer
		?.FirstOrDefault(l => l.Type is QuadtreeLayer.Types.LayerType.ImageryHistory)
		?.DatesLayer
		.DatedTile.Where(dt => dt.Provider != 0 && dt.Date > MIN_JPEG_DATE) ?? Enumerable.Empty<QuadtreeImageryDatedTile>();
}