using CommandLine;

namespace GEHistoricalImagery.Cli;

internal abstract class FileDownloadVerb : AoiVerb
{
	[Option('d', "date", HelpText = "Imagery Date(s). Multiple dates separated by a comma (,)", MetaValue = "yyyy/MM/dd", Required = true, Separator = ',')]
	public IEnumerable<DateOnly>? Dates { get; set; }

	[Option("exact-date", HelpText = "Require an exact date match for tiles to be download")]
	public bool ExactMatch { get; set; }

	[Option("layer-date", HelpText = "(Wayback only) The date specifies a layer instead of an image capture date")]
	public bool LayerDate { get; set; }

	[Option("target-sr", HelpText = "Warp image to Spatial Reference. Either EPSG:#### or path to projection file (file system or web)", MetaValue = "[SPATIAL REFERENCE]", Default = null)]
	public string? TargetSpatialReference { get; set; }

	public abstract string? SavePath { get; set; }

	protected bool AnyFileDownloadErrors()
	{
		var errors = GetFileDownloadErrors().ToList();
		errors.ForEach(Console.Error.WriteLine);
		return errors.Count > 0;
	}

	private IEnumerable<string> GetFileDownloadErrors()
	{
		foreach (var errorMessage in GetAoiErrors())
			yield return errorMessage;

		if (Dates?.Any() is not true)
		{
			yield return "At least one date must be specified.";
		}

		if (string.IsNullOrWhiteSpace(SavePath))
		{
			yield return "Invalid output file path";
		}
	}
}
