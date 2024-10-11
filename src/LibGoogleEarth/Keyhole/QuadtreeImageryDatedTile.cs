namespace Keyhole;

public partial class QuadtreeImageryDatedTile
{
	private static int ToJpegCommentDate(DateOnly date)
		=> ((date.Year & 0x7FF) << 9) | ((date.Month & 0xf) << 5) | (date.Day & 0x1f);
	private static DateOnly GetDate(QuadtreeImageryDatedTile datedTile)
		=> new(datedTile.Date >> 9, (datedTile.Date >> 5) & 0xf, datedTile.Date & 0x1f);
	public DateOnly DateOnly => GetDate(this);
}