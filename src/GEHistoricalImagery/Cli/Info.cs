using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;

namespace GEHistoricalImagery.Cli;

[Verb("info", HelpText = "Get imagery info at a specified location")]
internal class Info : OptionsBase
{
	[Option('l', "location", Required = true, HelpText = "Geographic location", MetaValue = "LAT,LONG")]
	public Coordinate? Coordinate { get; set; }

	[Option('z', "zoom", Default = null, HelpText = "Zoom level (Optional, [0-23])", MetaValue = "N", Required = false)]
	public int? ZoomLevel { get; set; }

	public override async Task RunAsync()
	{
		if (Coordinate is null)
		{
			Console.Error.WriteLine("Invalid location coordinate.\r\n Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305");
			return;
		}
		if (ZoomLevel < 1 || ZoomLevel > 23)
		{
			Console.Error.WriteLine("Invalid zoom level");
			return;
		}

		Console.WriteLine($"Dated Imagery at {Coordinate}");

		int startLevel = ZoomLevel ?? 1;
		int endLevel = ZoomLevel ?? 23;

		var task = Provider is Provider.Wayback ? Run_Esri(Coordinate.Value, startLevel, endLevel)
			: Run_Keyhole(Coordinate.Value, startLevel, endLevel);

		await task;
	}

	private async Task Run_Esri(Coordinate coordinate, int startLevel, int endLevel)
	{
		var wayBack = await WayBack.CreateAsync(CacheDir);

		for (int i = startLevel; i <= endLevel; i++)
		{
			var tile = coordinate.GetTile<EsriTile>(i);

			Console.WriteLine($"  Level = {i}");
			int count = 0;
			await foreach (var dated in wayBack.GetDatesAsync(tile))
			{
				Console.WriteLine($"    date = {DateString(dated.Date)}, version = {dated.Version}");
				count++;
			}

			if (count == 0)
			{
				Console.Error.WriteLine($"    NO AVAILABLE IMAGERY");
				break;
			}
		}
	}

	private async Task Run_Keyhole(Coordinate coordinate, int startLevel, int endLevel)
	{
		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);

		for (int i = startLevel; i <= endLevel; i++)
		{
			var tile = coordinate.GetTile<KeyholeTile>(i);
			var node = await root.GetNodeAsync(tile);

			Console.WriteLine($"  Level = {i}, Path = {tile.Path}");
			if (node == null)
			{
				Console.Error.WriteLine($"    NO AVAILABLE IMAGERY");
				break;
			}
			else
			{
				foreach (var dated in node.GetAllDatedTiles())
				{
					if (dated.Date.Year == 1)
						continue;
					Console.WriteLine($"    date = {DateString(dated.Date)}, version = {dated.Epoch}");
				}
			}
		}
	}
}
