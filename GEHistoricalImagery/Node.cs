using Keyhole;

namespace GEHistoricalImagery;

internal class Node
{
	private const int MIN_JPEG_DATE = 545;
	public QuadtreeNode QuadtreeNode { get; }
	public Tile Tile { get; }

	public Node(Tile tile, QuadtreeNode quadtreeNode)
	{
		QuadtreeNode = quadtreeNode;
		Tile = tile;
	}

	public bool HasDate(DateOnly dateOnly)
	=> GetAllDatedTiles().Any(dt => dt.Date == dateOnly);

	public IEnumerable<DateOnly> GetAllDates()
	=> GetAllDatedTiles().Select(dt => dt.Date) ?? Enumerable.Empty<DateOnly>();

	public IEnumerable<DatedTile> GetAllDatedTiles()
	{
		var datesLayer = QuadtreeNode?.Layer?.FirstOrDefault(l => l.Type is QuadtreeLayer.Types.LayerType.ImageryHistory)?.DatesLayer.DatedTile;

		if (datesLayer == null)
			yield break;

		foreach (var dt in datesLayer)
		{
			if (dt.Date < MIN_JPEG_DATE)
				continue;
			else if (dt.Provider != 0)
				yield return new DatedTile(Tile, dt);
			//When Provider is zero, that tile's imagery is being used as the default and is in the Imagery layer.
			else if (QuadtreeNode?.Layer?.FirstOrDefault(l => l.Type is QuadtreeLayer.Types.LayerType.Imagery) is QuadtreeLayer regImagery)
				yield return new DatedTile(Tile, dt.Date.ToDate(), regImagery);
		}
	}
}
