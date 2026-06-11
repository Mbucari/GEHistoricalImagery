using LibEsri;
using LibMapCommon;
using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Cli.Availability;

internal partial class AvailabilityCommand
{
	private async Task Run_Esri()
	{
		if (ConcurrentDownload > 10)
		{
			ConcurrentDownload = 10;
			Console.Error.WriteLine($"Limiting to {ConcurrentDownload} concurrent scrapes of Esri metadata.");
		}

		var wayBack = await WayBack.CreateAsync(CacheDir);

		var all = await GetAllEsriRegions(wayBack, Region.Transform<WebMercator>());
		ProgressWriter.Instance.EndProgress();

		if (all.Sum(r => r.Availabilities.Length) == 0)
		{
			Console.Error.WriteLine($"No imagery available at zoom level {ZoomLevel}");
			return;
		}

		OptionChooser<EsriRegion>.WaitForOptions(all);
	}

	private async Task<EsriRegion[]> GetAllEsriRegions(WayBack wayBack, GeoRegion<WebMercator> aoi)
	{
		//A layer date < MinDate will not have imagery captured after MinDate, but
		//a layer date > MaxDate may still have imagery captured before MaxDate.
		//Truncate the Layers whose layer date is older than MinDate, then search layers from
		//oldest to newest, stopping when a layer contains no imagery captured before MaxDate
		var layers = wayBack.Layers.Where(l => l.Date >= MinDate).OrderBy(l => l.Date).ToArray();

		int count = 0;
		int numTiles = layers.Length;

		var mercAoi = aoi.Transform<WebMercator>();
		var regionTiles = GetTiles(mercAoi);
		ProgressWriter.Instance.BeginProgress("Loading World Atlas WayBack Layer Info: ");
		var stats = mercAoi.GetRectangularRegionStats<EsriTile>(ZoomLevel) with { TileCount = regionTiles.LongLength };

		ParallelProcessor<EsriRegion> processor = new(ConcurrentDownload);
		List<EsriRegion> allLayers = new();

		await foreach (var region in processor.EnumerateResults(layers.Select(getLayerDates)))
		{
			if (region.Availabilities.Any(a => a.Date <= MaxDate))
			{
				allLayers.Add(region);
				ProgressWriter.Instance.ReportProgress(++count / (double)numTiles);
			}
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
				var availability = new RegionAvailability(regions[i].Date, stats.NumRows, stats.NumColumns);

				foreach (var tile in regionTiles)
				{
					var cIndex = LibMapCommon.Util.Mod(tile.Column - stats.MinColumn, 1 << tile.Level);
					var rIndex = tile.Row - stats.MinRow;

					availability[rIndex, cIndex] = regions[i].ContainsTile<EsriTile>(tile);
				}

				if (availability.HasAnyTiles() && (availability.HasAllTiles() || !CompleteOnly))
					displays.Add(availability);
			}

			return new EsriRegion(layer, displays.OrderByDescending(d => d.Date).ToArray());
		}
	}
}
