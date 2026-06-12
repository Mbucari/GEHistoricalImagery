using CommandLine;
using LibMapCommon;

namespace GEHistoricalImagery.Cli;

internal abstract class FileDownloadVerb : AoiVerb
{
	[Option('d', "date", HelpText = "Imagery Date(s). One or more dates, either separated by a comma (,) or supplied with multiple --date options.", MetaValue = "<yyyy/MM/dd>", Required = true, Separator = ',')]
	public IEnumerable<DateOnly>? Dates { get; set; }

	[Option("date-match", MetaValue = "<MatchType>", Default = DateMatchType.Closest, HelpText = "Type of date match to use for tiles\n [Closest]       Use the closest date\n [Exact]         Require an exact date match\n [ClosestBefore] Use the closest date before the specified date\n [ClosestAfter]  Use the closest date after the specified date")]
	public DateMatchType DateMatch { get; set; }

	[Option("exact-date", HelpText = "Require an exact date match for tiles to be downloaded (overrides --date-match)")]
	public bool ExactMatch { get; set; }

	[Option("layer-date", HelpText = "(Wayback only) The date specifies a layer instead of an image capture date")]
	public bool LayerDate { get; set; }

	[Option("target-sr", HelpText = "Warp image to Spatial Reference. Either EPSG:#### or path to projection file (file system or web)", MetaValue = "<SpatialReference>", Default = null)]
	public string? TargetSpatialReference { get; set; }

	public abstract string? SavePath { get; set; }

	protected override IEnumerable<string> GetValidationErrors()
	{
		foreach (var errorMessage in base.GetValidationErrors())
			yield return errorMessage;

		if (Dates?.Any() is not true)
		{
			yield return "At least one date must be specified.";
		}

		if (string.IsNullOrWhiteSpace(SavePath))
		{
			yield return "Invalid output file path";
		}

		if (ExactMatch)
		{
			DateMatch = DateMatchType.Exact;
		}
	}
	protected string DateMatchPreposition => DateMatch switch
	{
		DateMatchType.Exact => "On",
		DateMatchType.ClosestBefore => "Closest Before",
		DateMatchType.ClosestAfter => "Closest After",
		_ => "Nearest To"
	};
}
