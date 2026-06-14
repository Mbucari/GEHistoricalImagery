using CommandLine;
using LibMapCommon;
using LibMapCommon.Geometry;

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

	private static async Task<RegionAvailability[]> GetRegionAvailabilities<TTile, TCoordinate>(TileStats stats, TTile[] regionTiles, DatedRegion<TCoordinate>[] regions, bool parallel = false)
		where TTile : ITile<TCoordinate>
		where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		if (parallel)
			ProgressWriter.Instance.BeginProgress("Collating Dated Regions: ");

		byte[,] tilesWithData = new byte[stats.NumRows, stats.NumColumns];
		RegionAvailability[] availabilities = new RegionAvailability[regions.Length];		
		Action<int> processRegion = i =>
		{
			var availability = availabilities[i] = new(regions[i].Date, stats.NumRows, stats.NumColumns);
			foreach (var tile in regionTiles)
			{
				var cIndex = Util.Mod(tile.Column - stats.MinColumn, 1 << tile.Level);
				var rIndex = tile.RowsIncreaseToSouth ? tile.Row - stats.MinRow : stats.MaxRow - tile.Row;
				if (regions[i].ContainsTile(tile))
				{
					availability[rIndex, cIndex] = Availability.Available;
					tilesWithData[rIndex, cIndex] = 1;
				}
				else
				{
					availability[rIndex, cIndex] = Availability.Unavailable;
				}
			}

			if (parallel)
				ProgressWriter.Instance.ReportProgress((i + 1) / (double)regions.Length);
		};
		if (parallel)
		{
			Parallel.For(0, regions.Length, processRegion);
		}
		else
		{
			for (int i = 0; i < regions.Length; i++)
				processRegion(i);
		}

		//Mark tiles that are not available in any region as unavailable in all regions,
		//to avoid confusion when viewing the results
		foreach (var region in availabilities)
		{
			for (int row = 0; row < region.Height; row++)
			{
				for (int col = 0; col < region.Width; col++)
				{
					if (tilesWithData[row, col] == 0)
						region[row, col] = Availability.None;
				}
			}
		}

		Array.Sort(availabilities, (a, b) => b.Date.CompareTo(a.Date));
		if (parallel)
			ProgressWriter.Instance.EndProgress();
		return availabilities;
	}
}
