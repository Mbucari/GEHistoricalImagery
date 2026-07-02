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
		var mercAoi = Region.Transform<WebMercator>();
		var wayBack = await WayBack.CreateAsync(CacheDir);

		var regionTiles = GetTiles(mercAoi);
		var stats = mercAoi.GetRectangularRegionStats<EsriTile>(ZoomLevel) with { TileCount = regionTiles.Length };
		var datesOnLayers = await GetAllEsriLayerDatesAsync(wayBack, mercAoi);
		if (datesOnLayers.Length == 0)
		{
			Console.Error.WriteLine($"No dated imagery available within specified constraints");
			return;
		}
		var datedRegions = await GetAllEsriDatedRegionsAsync(wayBack, mercAoi, datesOnLayers);
		if (datedRegions.Length == 0)
		{
			Console.Error.WriteLine($"No dated imagery available within specified constraints");
			return;
		}

		if (SavePath is not null)
		{
			SaveDatedRegions(SavePath, datedRegions);
		}

		if (!Quiet)
		{
			var availabilities = await GetAllEsriRegions(stats, datedRegions, regionTiles);
			PresentRegions(availabilities);
		}
	}

	private static async Task<EsriRegion[]> GetAllEsriRegions(TileStats stats, DatedRegion[] datedRegions, EsriTile[] regionTiles)
	{
		ProgressWriter.Instance.BeginProgress("Collating Dated Regions: ");
		ConcurrentDictionary<int, EsriRegion> regionDict = new();

		//Create a BoolMap of the tiles inside the region. Unlike Keyhole,
		//we do not map regions were there is no imagery inside the region
		//because it's too computationally expensive for a marginal benifit.
		BoolMap insideRegion = new BoolMap(stats.NumColumns, stats.NumRows);
		foreach (var tile in regionTiles)
		{
			var cIndex = Util.Mod(tile.Column - stats.MinColumn, 1 << tile.Level);
			var rIndex = tile.Row - stats.MinRow;
			insideRegion[rIndex, cIndex] = true;
		}

		int count = 0;
		await Parallel.ForEachAsync(datedRegions.OrderBy(d => d.Layer.Date).ThenByDescending(d => d.Date).GroupBy(dr => dr.Layer), async (layerRegionsGrouping, _) =>
		{
			var layerRegions = layerRegionsGrouping.ToArray();			
			var availabilities = layerRegions.Select(dr => new RegionAvailability(dr, insideRegion)).ToArray();
			if (availabilities.Length > 0)
			{
				//Check if a layer with the same availabilities already exists, and if so, don't add this layer.
				//Comparing actual geometries is too expensive, so assume that if two layers have the same sets
				//of dates then those regions are the same. This is not always the case, but in practice it is a
				//good heuristic that allows us to significantly reduce processing time without significantly
				//impacting the accuracy of the results. Note that the output GeoJSON will still have all regions.
				var esriRegion = new EsriRegion(layerRegionsGrouping.Key, availabilities);
				HashCode codeHash = new();
				foreach (var availability in availabilities)
				{
					codeHash.Add(availability.Date);
				}
				var code = codeHash.ToHashCode();
				regionDict.TryAdd(code, esriRegion);
			}

			ProgressWriter.Instance.ReportProgress(Interlocked.Add(ref count, layerRegions.Length) / (double)datedRegions.Length);
		});

		ProgressWriter.Instance.EndProgress();
		return regionDict.Values.OrderByDescending(l => l.Date).ToArray();
	}

	private async Task<DatedRegion[]> GetAllEsriDatedRegionsAsync(WayBack wayBack, GeoRegion<WebMercator> mercAoi, DateOnLayer[] datesOnLayers)
	{
		ProgressWriter.Instance.BeginProgress("Getting geometry for dated regions: ");
		int count = 0;
		var options = new ParallelOptions() { MaxDegreeOfParallelism = ConcurrentDownload };
		DatedRegion?[] datedRegions = new DatedRegion?[datesOnLayers.Length];
		await Parallel.ForAsync(0, datesOnLayers.Length, options, async (i, _) =>
		{
			var datesOnLayer = datesOnLayers[i];
			datedRegions[i] = await wayBack.GetDatedRegionAsync(datesOnLayer, mercAoi);
			double c = Interlocked.Add(ref count, 1);
			ProgressWriter.Instance.ReportProgress(c / datesOnLayers.Length);
		});
		ProgressWriter.Instance.EndProgress();

		ProgressWriter.Instance.BeginProgress("Combining geometries with same dates: ");
		count = 0;
		var layerGroups = datedRegions.OfType<DatedRegion>().OrderBy(d => d.Layer.Date).GroupBy(d => d.Layer).ToArray();
		DatedRegion[][] combinedByLayer = new DatedRegion[layerGroups.Length][];
		Parallel.For(0, layerGroups.Length, (i, _) =>
		{
			var dict = new Dictionary<DateOnly, DatedRegion>();
			foreach (var region in layerGroups[i])
			{
				if (dict.TryGetValue(region.Date, out var existingRegion))
				{
					//If there are multiple regions for the same date and layer, combine them into a single region.
					existingRegion.Add(region);
				}
				else
				{
					dict.Add(region.Date, region);
				}
			}
			combinedByLayer[i] = dict.Values.ToArray();
			foreach (var region in combinedByLayer[i])
			{
				region.Flatten();
			}
			ProgressWriter.Instance.ReportProgress(Interlocked.Add(ref count, 1) / (double)layerGroups.Length);
		});

		ProgressWriter.Instance.EndProgress();
		return combinedByLayer.SelectMany(x => x).OrderBy(d => d.Layer.Date).ThenBy(d => d.Date).ToArray();
	}

	public async Task<DateOnLayer[]> GetAllEsriLayerDatesAsync(WayBack wayBack, GeoRegion<WebMercator> mercAoi)
	{
		//A layer date < MinDate will not have imagery captured after MinDate, but
		//a layer date > MaxDate may still have imagery captured before MaxDate.
		//Truncate the Layers whose layer date is older than MinDate, then search layers from
		//oldest to newest, stopping when a layer contains no imagery captured before MaxDate
		var layers = wayBack.Layers.Where(l => l.Date >= MinDate).OrderBy(l => l.Date).ToArray();
		ParallelProcessor<LayerDatesOnLayer?> processor = new(ConcurrentDownload);

		//Set with the first layer found that has no imagery captured before MaxDate
		Layer? cancelIfAfter = null;
		int count = 0;
		List<DateOnLayer> allRegions = new(layers.Length);
		ProgressWriter.Instance.BeginProgress("Querying wayback layers for dated regions: ");
		await foreach (var layerRegions in processor.EnumerateResults(layers.Select(getDatesOnLayer)).OfType<LayerDatesOnLayer>())
		{
			var layersWithDateMatches = layerRegions.DatesOnLayer.Where(d => d.SourceDate >= MinDate && d.SourceDate <= MaxDate).ToArray();
			if (layersWithDateMatches.Length == 0)
			{
				if (layerRegions.DatesOnLayer.Any(r => r.SourceDate > MaxDate))
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
			ProgressWriter.Instance.ReportProgress(Interlocked.Add(ref count, 1) / (double)layers.Length);
		}
		ProgressWriter.Instance.EndProgress();
		return allRegions.ToArray();

		async Task<LayerDatesOnLayer?> getDatesOnLayer(Layer layer)
		{
			if (cancelIfAfter is { Date: var cancelDate } && layer.Date > cancelDate)
				return null;

			var datesOnLayer = await wayBack.GetDatesOnLayerAsync(layer, mercAoi, ZoomLevel);
			return new LayerDatesOnLayer(layer, datesOnLayer);
		}
	}
	private record LayerDatesOnLayer(Layer Layer, DateOnLayer[] DatesOnLayer);
}
