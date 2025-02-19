using CommandLine;
using Google.Protobuf.WellKnownTypes;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using System.Text;

namespace GEHistoricalImagery.Cli;

[Verb("availability", HelpText = "Get imagery date availability in a specified region")]
internal class Availability : AoiVerb
{
	[Option('p', "parallel", HelpText = "Number of concurrent downloads", MetaValue = "N", Default = 20)]
	public int ConcurrentDownload { get; set; }

	public override async Task RunAsync()
	{
		bool hasError = false;

		foreach (var errorMessage in GetAoiErrors())
		{
			Console.Error.WriteLine(errorMessage);
			hasError = true;
		}

		if (hasError) return;
		Console.OutputEncoding = Encoding.Unicode;

		await (Provider is Provider.Wayback ? Run_Esri() : Run_Keyhole());
	}

	#region Esri
	private async Task Run_Esri()
	{
		var wayBack = await WayBack.CreateAsync(CacheDir);

		Console.Write("Loading World Atlas WayBack Layer Info: ");

		var all = await GetAllEsriRegions(wayBack, Aoi, ZoomLevel);
		ReplaceProgress("Done!\r\n");

		if (all.Sum(r => r.Availabilities.Length) == 0)
		{
			Console.Error.WriteLine($"No imagery available at zoom level {ZoomLevel}");
			return;
		}

		new OptionChooser<EsriRegion>().WaitForOptions(all);
	}

	private async Task<EsriRegion[]> GetAllEsriRegions(WayBack wayBack, Rectangle aoi, int zoomLevel)
	{
		int count = 0;
		int numTiles = wayBack.Layers.Count;
		ReportProgress(0);

		ParallelProcessor<EsriRegion> processor = new(ConcurrentDownload);
		List<EsriRegion> allLayers = new();

		await foreach (var region in processor.EnumerateResults(wayBack.Layers.Select(getLayerDates)))
		{
			allLayers.Add(region);
			ReportProgress(++count / (double)numTiles);
		}

		//De-duplicate list
		allLayers.Sort((a, b) => a.Layer.Date.CompareTo(b.Layer.Date));

		for (int i = 1; i < allLayers.Count; i++)
		{
			for (int k = i - 1; k >= 0; k--)
			{
				if (allLayers[i].Availabilities.SequenceEqual(allLayers[k].Availabilities))
				{
					allLayers.RemoveAt(i--);
					break;
				}
			}
		}

		return allLayers.OrderByDescending(l => l.Date).ToArray();

		async Task<EsriRegion> getLayerDates(Layer layer)
		{
			var regions = await wayBack.GetDateRegionsAsync(layer, aoi, ZoomLevel);

			List<RegionAvailability> displays = new();

			for (int i = 0; i < regions.Length; i++)
			{
				var availability = DrawAvailability(regions[i]);

				if (hasAnyTiles(availability))
					displays.Add(new RegionAvailability(regions[i].Date, availability));
			}

			return new EsriRegion(layer, displays.ToArray());
		}

		static bool hasAnyTiles(char[][] map)
			=> map.Any(x => x.Any(c => c != ':' && c != '˙'));
	}

	private class EsriRegion(Layer layer, RegionAvailability[] regions) : IDatedOption
	{
		public Layer Layer { get; } = layer;
		public RegionAvailability[] Availabilities { get; } = regions;

		public DateOnly Date => Layer.Date;

		public void DrawOption()
		{
			if (Availabilities.Length == 1)
			{
				var availabilityStr = $"Tile availability on {DateString(Layer.Date)} (captured on {DateString(Availabilities[0].Date)})";
				Console.WriteLine("\r\n" + availabilityStr);
				Console.WriteLine(new string('=', availabilityStr.Length) + "\r\n");

				foreach (var ca in Availabilities[0].Availability)
					Console.WriteLine(new string(ca));
			}
			else if (Availabilities.Length > 1)
			{
				var availabilityStr = $"Layer {Layer.Title} has imagery from {Availabilities.Length} different dates";
				Console.WriteLine("\r\n" + availabilityStr);
				Console.WriteLine(new string('=', availabilityStr.Length) + "\r\n");

				new OptionChooser<RegionAvailability>().WaitForOptions(Availabilities);
			}
		}
	}

	private char[][] DrawAvailability(LibEsri.Geometry.DatedRegion region)
	{
		var ll = Aoi.LowerLeft.GetTile<EsriTile>(ZoomLevel);
		var ur = Aoi.UpperRight.GetTile<EsriTile>(ZoomLevel);

		var width = ur.Column - ll.Column + 1;
		var height = ll.Row - ur.Row + 1;
		height = height % 2 == 0 ? height / 2 : height / 2 + 1;

		//Each character row represents two node rows
		char[][] availability = new char[height][];
		for (int i = 0; i < height; i++)
			availability[i] = Enumerable.Repeat(':', width).ToArray();

		for (int r = ll.Row, rIndex = 0; r >= ur.Row; r--, rIndex++)
		{
			availability[rIndex / 2] ??= new char[width];

			for (int c = ll.Column; c <= ur.Column; c++)
			{
				var tile = new EsriTile(r, c, ZoomLevel);

				if (region.Contains(tile.Center.ToWebMercator()))
					MarkAvailable(availability, rIndex, c - ll.Column);
			}
		}

		if ((ll.Row - ur.Row) % 2 == 0)
		{
			for (int c = 0; c < availability[^1].Length; c++)
			{
				if (availability[^1][c] == ':')
					availability[^1][c] = '˙';
			}
		}

		return availability;
	}

	#endregion

	#region Keyhole
	private async Task Run_Keyhole()
	{
		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		Console.Write("Loading Quad Tree Packets: ");

		var all = await GetAllDatesAsync(root, Aoi, ZoomLevel);
		ReplaceProgress("Done!\r\n");

		if (all.Length == 0)
		{
			Console.Error.WriteLine($"No dated imagery available at zoom level {ZoomLevel}");
			return;
		}

		new OptionChooser<RegionAvailability>().WaitForOptions(all);
	}

	private async Task<RegionAvailability[]> GetAllDatesAsync(DbRoot root, Rectangle aoi, int zoomLevel)
	{
		int count = 0;
		int numTiles = aoi.GetTileCount<KeyholeTile>(zoomLevel);
		ReportProgress(0);

		ParallelProcessor<List<DatedTile>> processor = new(ConcurrentDownload);

		var ll = Aoi.LowerLeft.GetTile<KeyholeTile>(ZoomLevel);
		var ur = Aoi.UpperRight.GetTile<KeyholeTile>(ZoomLevel);

		var width = ur.Column - ll.Column + 1;
		var height = ur.Row - ll.Row + 1;
		height = height % 2 == 0 ? height / 2 : height / 2 + 1;

		Dictionary<DateOnly, RegionAvailability> uniqueDates = new();

		await foreach (var dSet in processor.EnumerateResults(aoi.GetTiles<KeyholeTile>(zoomLevel).Select(getDatedTiles)))
		{
			foreach (var d in dSet)
			{
				if (!uniqueDates.ContainsKey(d.Date))
				{
					var map = new char[height][];
					for (int i = 0; i < height; i++)
						map[i] = Enumerable.Repeat(':', width).ToArray();
					uniqueDates.Add(d.Date, new RegionAvailability(d.Date, map));
				}

				var region = uniqueDates[d.Date];

				var cIndex = d.Tile.Column - ll.Column;
				var rIndex = ur.Row - d.Tile.Row;

				if (await root.GetNodeAsync(d.Tile) is TileNode node)
					MarkAvailable(region.Availability, rIndex, cIndex);
			}

			ReportProgress(++count / (double)numTiles);
		}


		if ((ur.Row - ll.Row) % 2 == 0)
		{
			foreach (var region in uniqueDates.Values)
			{
				for (int c = 0; c < region.Availability[^1].Length; c++)
				{
					if (region.Availability[^1][c] == ':')
						region.Availability[^1][c] = '˙';
				}
			}
		}

		return uniqueDates.Values.OrderByDescending(r => r.Date).ToArray();

		async Task<List<DatedTile>> getDatedTiles(KeyholeTile tile)
		{
			List<DatedTile> dates = new();

			if (await root.GetNodeAsync(tile) is not TileNode node)
				return dates;

			foreach (var datedTile in node.GetAllDatedTiles())
			{
				if (datedTile.Date.Year == 1) continue;

				if (!dates.Any(d => d.Date == datedTile.Date))
					dates.Add(datedTile);
			}
			return dates;
		}
	}

	#endregion

	#region Common

	private class RegionAvailability(DateOnly date, char[][] availability) : IEquatable<RegionAvailability>, IDatedOption
	{
		public DateOnly Date { get; } = date;
		public char[][] Availability { get; } = availability;

		public bool Equals(RegionAvailability? other)
		{
			return other != null && other.Date == Date && arraysEqual(other.Availability, Availability);
		}

		private static bool arraysEqual(char[][] m1, char[][] m2)
		{
			if (m1.Length != m2.Length)
				return false;

			for (int i = 0; i < m1.Length; i++)
			{
				if (m1[i].Length != m2[i].Length)
					return false;

				for (int j = 0; j < m1[i].Length; j++)
				{
					if (m1[i][j] != m2[i][j])
						return false;
				}
			}
			return true;
		}

		public void DrawOption()
		{
			var availabilityStr = $"Tile availability on {DateString(Date)}";
			Console.WriteLine("\r\n" + availabilityStr);
			Console.WriteLine(new string('=', availabilityStr.Length) + "\r\n");

			foreach (var ca in Availability)
				Console.WriteLine(new string(ca));
		}
	}
	private static void MarkAvailable(char[][] availability, int rIndex, int cIndex)
	{
		var existing = availability[rIndex / 2][cIndex];
		if (rIndex % 2 == 0)
		{
			availability[rIndex / 2][cIndex]
				= existing == ':' ? '▀'
				: existing == '▄' ? '█'
				: existing;
		}
		else
		{
			availability[rIndex / 2][cIndex]
				= existing == ':' ? '▄'
				: existing == '▀' ? '█'
				: existing;
		}
	}

	#endregion
}
