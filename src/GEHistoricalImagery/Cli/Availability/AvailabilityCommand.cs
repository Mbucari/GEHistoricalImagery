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

	[Option('o', "output", HelpText = "Filename to save the image availability regions to GeoJSON (dash (-) for console output)", MetaValue = "<out.json>", Required = false)]
	public string? SavePath { get; set; }

	protected override IEnumerable<string> GetValidationErrors()
	{
		foreach (var error in base.GetValidationErrors())
		{
			yield return error;
		}
		if (Provider is Provider.Wayback && ZoomLevel < 10)
		{
			yield return "Esri provides no data for zoom levels below 10.";
		}
		if (MaxDate.DayNumber == 0)
		{
			MaxDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
		}
		if (MinDate > MaxDate)
		{
			yield return "Min date must be less than or equal to max date.";
		}
	}

	public override async Task RunAsync()
	{
		if (AnyValidationErrors())
			return;

		await (Provider is Provider.Wayback ? Run_Esri() : Run_Keyhole());
	}

	private void HandleDatedRegions<TCoordinate>(DatedRegion<TCoordinate>[] regions)
		where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		if (SavePath is not null)
		{
			string savefile = SavePath is "-" ? "/vsistdout/out.json" : SavePath.ReplaceUnixHomeDir();
			regions.SaveInfoData(savefile, ZoomLevel, Provider);
		}
	}

	private static void PresentRegions(IConsoleOption[] options)
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
		HashSet<(int row, int col)> tilesWithData = new(stats.NumRows * stats.NumColumns);
		RegionAvailability[] displays = new RegionAvailability[regions.Length];

		for (int i = 0; i < regions.Length; i++)
		{
			var availability = new RegionAvailability(regions[i].Date, stats.NumRows, stats.NumColumns);
			foreach (var tile in regionTiles)
			{
				var cIndex = Util.Mod(tile.Column - stats.MinColumn, 1 << tile.Level);
				var rIndex = tile.RowsIncreaseToSouth ? tile.Row - stats.MinRow : stats.MaxRow - tile.Row;
				if (regions[i].ContainsTile(tile))
				{
					availability[rIndex, cIndex] = Availability.Available;
					tilesWithData.Add((rIndex, cIndex));
				}
				else
				{
					availability[rIndex, cIndex] = Availability.Unavailable;
				}
			}
			ProgressWriter.Instance.ReportProgress(i / (double)regions.Length);
			displays[i] = availability;
		}
		RegionAvailability[] availabilities = displays.OrderByDescending(d => d.Date).ToArray();

		//Mark tiles that are not available in any region as unavailable in all regions,
		//to avoid confusion when viewing the results
		(int row, int col) checkPoint = new();
		foreach (var region in availabilities)
		{
			for (checkPoint.row = 0; checkPoint.row < region.Height; checkPoint.row++)
			{
				for (checkPoint.col = 0; checkPoint.col < region.Width; checkPoint.col++)
				{
					if (!tilesWithData.Contains(checkPoint))
						region[checkPoint.row, checkPoint.col] = Availability.None;
				}
			}
		}
		return availabilities;
	}
}
