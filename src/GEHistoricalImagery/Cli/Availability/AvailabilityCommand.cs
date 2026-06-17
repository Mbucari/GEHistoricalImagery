using CommandLine;
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

	private void SaveDatedRegions(string savePath, IDatedRegion[] regions)
	{
		string savefile = savePath is "-" ? "/vsistdout/out.json" : savePath.ReplaceUnixHomeDir();
		regions.SaveAvailabilityData(savefile, Provider);
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
}
