using CommandLine;
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
}
