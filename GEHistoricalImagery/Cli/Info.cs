using CommandLine;
using Keyhole;

namespace GoogleEarthImageDownload.Cli;

[Verb("info", HelpText = "Get imagery info at a specified location")]
internal class Info : OptionsBase
{
	[Option('l', "location", Required = true, HelpText = "Geographic location", MetaValue = "LAT,LONG")]
	public Coordinate? Coordinate { get; set; }

	[Option('z', "zoom", Default = null, HelpText = "Zoom level (Optional, [0-24])", MetaValue = "N", Required = false)]
	public int? ZoomLevel { get; set; }

	public override async Task Run()
	{
		if (Coordinate is null)
		{
			Console.Error.WriteLine("Invalid location coordinate.\r\n Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305");
			return;
		}
		if (ZoomLevel < 0 || ZoomLevel > 24)
		{
			Console.Error.WriteLine("Invalid zoom level");
			return;
		}

		Console.WriteLine($"Dated Imagery at {Coordinate}");

		var root = await DbRoot.CreateAsync();

		int startLevel = ZoomLevel ?? 0;
		int endLevel = ZoomLevel ?? 24;

		for (int i = startLevel; i <= endLevel; i++)
		{
			var tile = Coordinate.Value.GetTile(i);
			var node = await root.GetNodeAsync(tile.QtPath);

			Console.WriteLine($"  Level = {i}, Path = {tile.QtPath}");
			if (node?.Layer?.FirstOrDefault(l => l.Type is QuadtreeLayer.Types.LayerType.ImageryHistory) is QuadtreeLayer hLayer)
			{
				foreach (var dated in hLayer.DatesLayer.DatedTile)
				{
					var date = dated.Date.ToDate();

					if (date.Year == 1) continue;
					Console.WriteLine($"    date = {date:yyyy/MM/dd}, version = {dated.DatedTileEpoch}");
				}
			}
			else
			{
				Console.Error.WriteLine($"    NO AVAILABLE IMAGERY");
				break;
			}
		}
	}
}
