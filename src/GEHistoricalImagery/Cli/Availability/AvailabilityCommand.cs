using CommandLine;
using LibMapCommon;
using LibMapCommon.Geometry;
using System.Text;

namespace GEHistoricalImagery.Cli.Availability;

[Verb("availability", HelpText = "Get imagery date availability in a specified region")]
internal partial class AvailabilityCommand : AoiVerb
{
	[Option('c', "complete", HelpText = "Only display dates with complete coverage of the region")]
	public bool CompleteOnly { get; set; }

	[Option("min-date", HelpText = "Oldest image tiles to consider", MetaValue = "yyyy/MM/dd")]
	public DateOnly MinDate { get; set; }

	[Option("max-date", HelpText = "Youngest (most recent) image tiles to consider", MetaValue = "yyyy/MM/dd")]
	public DateOnly MaxDate { get; set; }

	public override async Task RunAsync()
	{
		if (AnyValidationErrors())
			return;

		if (MaxDate.DayNumber == 0)
			MaxDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

		Console.OutputEncoding = Encoding.Unicode;

		await (Provider is Provider.Wayback ? Run_Esri() : Run_Keyhole());
	}

	private void PresentRegions(IConsoleOption[] options)
	{
		if (options.Length == 0)
		{
			Console.Error.WriteLine($"No dated imagery available within specified constraints");
			return;
		}

		OptionChooser.WaitForOptions(options);
	}

	private async Task<RegionAvailability[]> GetRegionAvailabilities<TTile, TCoordinate>(TileStats stats, TTile[] regionTiles, DatedRegion<TCoordinate>[] regions)
		where TTile : ITile<TCoordinate>
		where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		HashSet<ValueTuple<int, int>> tilesWithData = new(stats.NumRows * stats.NumColumns);
		RegionAvailability?[] displays = new RegionAvailability[regions.Length];

		for (int i = 0; i < regions.Length; i++)
		{
			var availability = new RegionAvailability(regions[i].Date, stats.NumRows, stats.NumColumns);
			foreach (var tile in regionTiles)
			{
				var cIndex = Util.Mod(tile.Column - stats.MinColumn, 1 << tile.Level);
				var rIndex = tile.RowsIncreaseToSouth ? tile.Row - stats.MinRow : stats.MaxRow - tile.Row;
				availability[rIndex, cIndex] = regions[i].ContainsTile(tile);

				if (availability[rIndex, cIndex]!.Value)
				{
					tilesWithData.Add(new ValueTuple<int, int>(rIndex, cIndex));
				}
			}
			ProgressWriter.Instance.ReportProgress(i / (double)regions.Length);
			if (availability.HasAnyTiles() && (!CompleteOnly || availability.HasAllTiles()))
				displays[i] = availability;
		}
		RegionAvailability[] availabilities = displays.OfType<RegionAvailability>().OrderByDescending(d => d.Date).ToArray();

		//Mark tiles that are not available in any region as unavailable in all regions,
		//to avoid confusion when viewing the results
		ValueTuple<int, int> checkPoint = new();
		foreach (var region in availabilities)
		{
			for (int r = 0; r < region.Height; r++)
			{
				checkPoint.Item1 = r;
				for (int c = 0; c < region.Width; c++)
				{
					checkPoint.Item2 = c;
					if (!tilesWithData.Contains(checkPoint))
						region[r, c] = null;
				}
			}
		}
		return availabilities;
	}
}
