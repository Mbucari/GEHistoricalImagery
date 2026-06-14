using LibEsri;
using LibEsri.Geometry;
using LibMapCommon;
using LibMapCommon.Geometry;
using System.Collections.Concurrent;

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

		var mercAoi = Region.Transform<WebMercator>();
		var wayBack = await WayBack.CreateAsync(CacheDir);

		var regionTiles = GetTiles(mercAoi);
		var stats = mercAoi.GetRectangularRegionStats<EsriTile>(ZoomLevel) with { TileCount = regionTiles.Length };
		var datedRegions = await GetAllEsriDatedRegionsAsync(wayBack, mercAoi);
		HandleDatedRegions(datedRegions);

		if (!Quiet)
		{
			var availabilities = await GetAllEsriRegions(stats, regionTiles, datedRegions);
			PresentRegions(availabilities);
		}
	}

	private static async Task<EsriRegion[]> GetAllEsriRegions(TileStats stats, EsriTile[] regionTiles, DatedRegion[] datedRegions)
	{
		ProgressWriter.Instance.BeginProgress("Collating Dated Regions: ");
		ConcurrentDictionary<int, EsriRegion> regionDict = new();

		int count = 0;
		await Parallel.ForEachAsync(datedRegions.OrderBy(d => d.Layer.Date).ThenBy(d => d.Date).GroupBy(dr => dr.Layer), async (layerRegionsGrouping, _) =>
		{
			var layerRegions = layerRegionsGrouping.ToArray();
			var availabilities = await GetRegionAvailabilities(stats, regionTiles, layerRegions);
			if (availabilities.Length > 0)
			{
				//Check if a layer with the same availabilities already exists, and if so, don't add this layer.
				var esriRegion = new EsriRegion(layerRegionsGrouping.Key, availabilities);
				HashCode codeHash = new();
				foreach (var availability in availabilities)
				{
					codeHash.Add(availability);
				}
				var code = codeHash.ToHashCode();
				regionDict.TryAdd(code, esriRegion);
			}

			ProgressWriter.Instance.ReportProgress(Interlocked.Add(ref count, layerRegions.Length) / (double)datedRegions.Length);
		});

		ProgressWriter.Instance.EndProgress();
		return regionDict.Values.OrderByDescending(l => l.Date).ToArray();
	}

	private async Task<DatedRegion[]> GetAllEsriDatedRegionsAsync(WayBack wayBack, GeoRegion<WebMercator> mercAoi)
	{
		//A layer date < MinDate will not have imagery captured after MinDate, but
		//a layer date > MaxDate may still have imagery captured before MaxDate.
		//Truncate the Layers whose layer date is older than MinDate, then search layers from
		//oldest to newest, stopping when a layer contains no imagery captured before MaxDate
		var layers = wayBack.Layers.Where(l => l.Date >= MinDate).OrderBy(l => l.Date).ToArray();
		ParallelProcessor<LayerDatedRegion?> processor = new(ConcurrentDownload);

		//Set with the first layer found that has no imagery captured before MaxDate
		Layer? cancelIfAfter = null;

		List<DatedRegion> allRegions = new(layers.Length);
		int count = 0;
		ProgressWriter.Instance.BeginProgress("Building dated tile regions: ");

		await foreach (var layerRegions in processor.EnumerateResults(layers.Select(getDatedRegions)).OfType<LayerDatedRegion>())
		{
			var layersWithDateMatches = layerRegions.Regions.Where(d => d.Date >= MinDate && d.Date <= MaxDate).ToArray();
			if (layersWithDateMatches.Length == 0)
			{
				if (layerRegions.Regions.Any(r => r.Date > MaxDate))
				{
					//This layer has no imagery captured before MaxDate, therefore no further layers
					//will have imagery captured before MaxDate. Set cancelIfAfter so that no more
					//calls to GetDateRegionsAsync will be made for layers after this layer.
					if (cancelIfAfter is null || layerRegions.Layer.Date < cancelIfAfter.Date)
						cancelIfAfter = layerRegions.Layer;
				}
			}
			else if (!CompleteOnly)
			{
				allRegions.AddRange(layersWithDateMatches);
			}
			else if (layersWithDateMatches.Length == 1 && layersWithDateMatches[0].IsComplete)
			{
				allRegions.Add(layersWithDateMatches[0]);
			}

			ProgressWriter.Instance.ReportProgress(++count / (double)layers.Length);
		}
		ProgressWriter.Instance.EndProgress();
		return allRegions.ToArray();

		async Task<LayerDatedRegion?> getDatedRegions(Layer layer)
		{
			if (cancelIfAfter is { Date: var cancelDate } && layer.Date > cancelDate)
				return null;

			var datedRegions = await wayBack.GetDateRegionsAsync(layer, mercAoi, ZoomLevel);
			return new LayerDatedRegion(layer, datedRegions);
		}
	}
	private record LayerDatedRegion(Layer Layer, DatedRegion[] Regions);
}
