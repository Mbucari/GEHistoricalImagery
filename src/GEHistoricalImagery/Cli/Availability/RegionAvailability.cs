using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Cli.Availability;

public enum Availability
{
	None,
	Unavailable,
	Available
}

internal class RegionAvailability : IConsoleOption
{
	public DateOnly Date { get; }
	public string DisplayValue => Date.ToDateString();

	public int Height { get; }
	public int Width { get; }
	public Availability this[int rIndex, int cIndex]
		=> DatedRegion.HasDataMap[rIndex, cIndex] ? Availability.Available
		: AllRegionsAvailability[rIndex, cIndex] ? Availability.Unavailable
		: Availability.None;

	private IDatedRegion DatedRegion { get; }
	private BoolMap AllRegionsAvailability { get; }

	public RegionAvailability(IDatedRegion datedRegion, BoolMap allRegionsAvailability)
	{
		Width = datedRegion.Stats.NumColumns;
		Height = datedRegion.Stats.NumRows;
		Date = datedRegion.Date;
		AllRegionsAvailability = allRegionsAvailability;
		DatedRegion = datedRegion;
	}

	public bool DrawOption()
	{
		var availabilityStr = $"Tile availability on {Date.ToDateString()}";
		Console.Error.WriteLine(Environment.NewLine + availabilityStr);
		Console.Error.WriteLine(new string('=', availabilityStr.Length) + Environment.NewLine);
		DrawMap();
		return false;
	}

	public void DrawMap()
	{
		/*
		 _________________________
		 | Top       | TTTFFFNNN |
		 ------------|------------
		 | Bottom    | TFNTFNTFN |
		 ------------|------------
		 | Character | █▀▀▄:˙▄.  |
		 -------------------------
		 */

		for (int y = 0; y < Height; y += 2)
		{
			var has2Rows = y + 1 < Height;
			char[] row = new char[Width];
			for (int x = 0; x < Width; x++)
			{
				var top = this[y, x];
				if (has2Rows)
				{
					var bottom = this[y + 1, x];
					row[x] = top == Availability.Available & bottom == Availability.Available ? '█' :
						top == Availability.Available ? '▀' :
						bottom == Availability.Available ? '▄' :
						top == Availability.Unavailable & bottom == Availability.Unavailable ? ':' :
						top == Availability.Unavailable ? '˙' :
						bottom == Availability.Unavailable ? '.' : ' ';
				}
				else
				{
					row[x] = top == Availability.Available ? '▀' :
						top == Availability.Unavailable ? '˙' : ' ';
				}
			}

			Console.Error.WriteLine(row);
		}
	}
}