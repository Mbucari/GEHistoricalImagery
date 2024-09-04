using Keyhole;

namespace LibGoogleEarth;

/// <summary>
/// Relates a <see cref="Keyhole.QuadtreeNode"/> to a <see cref="LibGoogleEarth.Tile"/>
/// </summary>
public class TileNode
{
	private const int MIN_JPEG_DATE = 545;
	/// <summary> The Google Earth quadtree node </summary>
	public QuadtreeNode QuadtreeNode { get; }
	/// <summary> The <see cref="LibGoogleEarth.Tile"/> associated with the <see cref="QuadtreeNode"/> </summary>
	public Tile Tile { get; }

	internal TileNode(Tile tile, QuadtreeNode quadtreeNode)
	{
		QuadtreeNode = quadtreeNode;
		Tile = tile;
	}

	/// <summary>
	/// Determines whether this quadtree node has imagery available from a specific date.
	/// </summary>
	/// <param name="dateOnly">A specific date</param> 
	/// <returns><see langword="true"/> if the quadtree node has imagery available from the date; otherwise, <see langword="false"/>.</returns>
	public bool HasDate(DateOnly dateOnly)
	=> GetAllDatedTiles().Any(dt => dt.Date == dateOnly);

	/// <summary>
	/// Returns an enumerable collection of all <see cref="DatedTile"/>s present in the <see cref="QuadtreeNode"/>
	/// </summary>
	public IEnumerable<DatedTile> GetAllDatedTiles()
	{
		var datesLayer
			= QuadtreeNode
			?.Layer
			?.FirstOrDefault(l => l.Type is QuadtreeLayer.Types.LayerType.ImageryHistory)
			?.DatesLayer
			.DatedTile;

		if (datesLayer == null)
			yield break;

		foreach (var dt in datesLayer)
		{
			if (dt.Date <= MIN_JPEG_DATE)
				continue;
			else if (dt.Provider != 0)
				yield return new DatedTile(Tile, dt);
			//When Provider is zero, that tile's imagery is being used as the default and is in the Imagery layer.
			else if (QuadtreeNode?.Layer?.FirstOrDefault(l => l.Type is QuadtreeLayer.Types.LayerType.Imagery) is QuadtreeLayer regImagery)
				yield return new DatedTile(Tile, dt.DateOnly, regImagery);
		}
	}
}
