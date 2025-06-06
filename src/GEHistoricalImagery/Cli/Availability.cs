﻿using CommandLine;
using Google.Protobuf.WellKnownTypes;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon.Geometry;
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
		if (ConcurrentDownload > 10)
		{
			ConcurrentDownload = 10;
			Console.Error.WriteLine($"Limiting to {ConcurrentDownload} concurrent scrapes of Esri metadata.");
		}

		var wayBack = await WayBack.CreateAsync(CacheDir);

		Console.Write("Loading World Atlas WayBack Layer Info: ");

		var all = await GetAllEsriRegions(wayBack, Region, ZoomLevel);
		ReplaceProgress("Done!\r\n");

		if (all.Sum(r => r.Availabilities.Length) == 0)
		{
			Console.Error.WriteLine($"No imagery available at zoom level {ZoomLevel}");
			return;
		}

		new OptionChooser<EsriRegion>().WaitForOptions(all);
	}

	private async Task<EsriRegion[]> GetAllEsriRegions(WayBack wayBack, Wgs1984Poly aoi, int zoomLevel)
	{
		int count = 0;
		int numTiles = wayBack.Layers.Count;
		ReportProgress(0);

		var mercAoi = aoi.ToWebMercator();
		var rect = aoi.GetBoundingRectangle();
		var ll = rect.LowerLeft.GetTile<EsriTile>(ZoomLevel);
		var ur = rect.UpperRight.GetTile<EsriTile>(ZoomLevel);
		rect.GetNumRowsAndColumns<EsriTile>(ZoomLevel, out int nRows, out int nColumns);

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
			var regions = await wayBack.GetDateRegionsAsync(layer, mercAoi, ZoomLevel);

			List<RegionAvailability> displays = new(regions.Length);

			for (int i = 0; i < regions.Length; i++)
			{
				var availability = new RegionAvailability(regions[i].Date, nRows, nColumns);

				foreach (var tile in Region.GetTiles<EsriTile>(ZoomLevel))
				{
					var cIndex = tile.Column - ll.Column;
					var rIndex = tile.Row - ur.Row;
					availability[rIndex, cIndex] = regions[i].ContainsTile(tile);
				}

				if (availability.HasAnyTiles())
					displays.Add(availability);
			}

			return new EsriRegion(layer, displays.OrderByDescending(d => d.Date).ToArray());
		}
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

				Availabilities[0].DrawMap();
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

	#endregion

	#region Keyhole
	private async Task Run_Keyhole()
	{
		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		Console.Write("Loading Quad Tree Packets: ");

		var all = await GetAllDatesAsync(root, Region, ZoomLevel);
		ReplaceProgress("Done!\r\n");

		if (all.Length == 0)
		{
			Console.Error.WriteLine($"No dated imagery available at zoom level {ZoomLevel}");
			return;
		}

		new OptionChooser<RegionAvailability>().WaitForOptions(all);
	}

	private async Task<RegionAvailability[]> GetAllDatesAsync(DbRoot root, Wgs1984Poly reg, int zoomLevel)
	{
		int count = 0;
		int numTiles = reg.GetTileCount<KeyholeTile>(zoomLevel);
		ReportProgress(0);

		ParallelProcessor<List<DatedTile>> processor = new(ConcurrentDownload);

		var aoi = reg.GetBoundingRectangle();

		aoi.GetNumRowsAndColumns<KeyholeTile>(zoomLevel, out int nRows, out int nColumns);
		var ll = aoi.LowerLeft.GetTile<KeyholeTile>(ZoomLevel);
		var ur = aoi.UpperRight.GetTile<KeyholeTile>(ZoomLevel);

		Dictionary<DateOnly, RegionAvailability> uniqueDates = new();
		HashSet<Tuple<int, int>> uniquePoints = new();

		await foreach (var dSet in processor.EnumerateResults(reg.GetTiles<KeyholeTile>(zoomLevel).Select(getDatedTiles)))
		{
			foreach (var d in dSet)
			{
				if (!uniqueDates.ContainsKey(d.Date))
				{
					uniqueDates.Add(d.Date, new RegionAvailability(d.Date, nRows, nColumns));
				}

				var region = uniqueDates[d.Date];

				var cIndex = d.Tile.Column - ll.Column;
				var rIndex = ur.Row - d.Tile.Row;

				uniquePoints.Add(new Tuple<int, int>(rIndex, cIndex));
				region[rIndex, cIndex] = await root.GetNodeAsync(d.Tile) is TileNode;
			}

			ReportProgress(++count / (double)numTiles);
		}

		//Go back and mark unavailable tiles within the region of interest
		foreach (var a in uniqueDates.Values)
		{
			for (int r = 0; r < a.Height; r++)
			{
				for (int c = 0; c < a.Width; c++)
				{
					if (uniquePoints.Contains(new Tuple<int, int>(r, c)) && a[r, c] is null)
						a[r, c] = false;
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

	private class RegionAvailability : IEquatable<RegionAvailability>, IDatedOption
	{
		public DateOnly Date { get; }
		private bool?[,] Availability { get; }

		public int Height => Availability.GetLength(0);
		public int Width => Availability.GetLength(1);
		public bool? this[int rIndex, int cIndex]
		{
			get => Availability[rIndex, cIndex];
			set => Availability[rIndex, cIndex] = value;
		}

		public RegionAvailability(DateOnly date, int height, int width)
		{
			Date = date;
			Availability = new bool?[height, width];
		}

		public bool HasAnyTiles() => Availability.OfType<bool>().Any(b => b);
		public bool Equals(RegionAvailability? other)
		{
			if (other == null || other.Date != Date || other.Height != Height || other.Width != Width)
				return false;

			for (int i = 0; i < Height; i++)
			{
				for (int j = 0; j < Width; j++)
				{
					if (other.Availability[i, j] != Availability[i, j])
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
			DrawMap();
		}

		public void DrawMap()
		{
			/*
			 _________________________
			 | Top       | TTTFFFNNN |
			 ------------|------------
			 | Bottom    | TFNTFNTFN |
			 ------------|------------
			 | Character | █▀▀▄:˙▄.  |
			 -------------------------
			 */

			for (int y = 0; y < Height; y += 2)
			{
				var has2Rows = y + 1 < Height;
				char[] row = new char[Width];
				for (int x = 0; x < Width; x++)
				{
					var top = Availability[y, x];
					if (has2Rows)
					{
						var bottom = Availability[y + 1, x];
						row[x] = top is true & bottom is true ? '█' :
							top is true ? '▀' :
							bottom is true ? '▄' :
							top is false & bottom is false ? ':' :
							top is false ? '˙' :
							bottom is false ? '.' : ' ';
					}
					else
					{
						row[x] = top is true ? '▀' :
							top is false ? '˙' : ' ';
					}
				}

				Console.WriteLine(new string(row));
			}
		}
	}
	#endregion
}
