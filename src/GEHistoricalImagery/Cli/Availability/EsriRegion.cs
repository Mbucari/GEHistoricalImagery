using LibEsri;

namespace GEHistoricalImagery.Cli.Availability;

internal class EsriRegion(Layer layer, RegionAvailability[] regions) : IConsoleOption
{
	public Layer Layer { get; } = layer;
	public RegionAvailability[] Availabilities { get; } = regions;

	public DateOnly Date => Availabilities.Length > 1 ? Layer.Date : Availabilities[0].Date;
	public string DisplayValue => Date.ToDateString();

	public bool DrawOption()
	{
		if (Availabilities.Length == 1)
		{
			var availabilityStr = $"Tile availability on {Layer.Date.ToDateString()} (captured on {Availabilities[0].Date.ToDateString()})";
			Console.Error.WriteLine(Environment.NewLine + availabilityStr);
			Console.Error.WriteLine(new string('=', availabilityStr.Length) + Environment.NewLine);

			Availabilities[0].DrawMap();
		}
		else if (Availabilities.Length > 1)
		{
			var availabilityStr = $"Layer '{Layer.Title}' has imagery from {Availabilities.Length} different dates";
			Console.Error.WriteLine(Environment.NewLine + availabilityStr);
			Console.Error.WriteLine(new string('=', availabilityStr.Length) + Environment.NewLine);

			OptionChooser.WaitForOptions(Availabilities);
		}
		return false;
	}
}