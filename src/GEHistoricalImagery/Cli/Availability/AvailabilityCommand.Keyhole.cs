using LibGoogleEarth;
using LibGoogleEarth.Geometry;
using System.Reflection;

namespace GEHistoricalImagery.Cli.Availability;

internal partial class AvailabilityCommand
{
	private async Task Run_Keyhole()
	{
		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);

		var regionTiles = GetTiles(Region);
		var stats = Region.GetRectangularRegionStats<KeyholeTile>(ZoomLevel) with { TileCount = regionTiles.Length };
		var datedRegions = await GetAllKeyholeDatedRegionsAsync(root, regionTiles);

		if (!Quiet)
		{
			var availabilities = await GetRegionAvailabilities(stats, regionTiles, datedRegions);
			PresentRegions(availabilities);
		}
	}

	private async Task<DatedRegion[]> GetAllKeyholeDatedRegionsAsync(DbRoot root, KeyholeTile[] regionTiles)
	{
		int count = 0;
		ProgressWriter.Instance.BeginProgress("Building dated tile regions: ");
		ParallelProcessor<List<DatedTile>> processor = new(ConcurrentDownload);
		var datedRegions = await root.GetDateRegionsAsync(processor.EnumerateResults(regionTiles.Select(getDatedTiles)));
		for (int i = 0; i < datedRegions.Length; i++)
		{
			if (datedRegions[i].TileCount == regionTiles.Length)
				datedRegions[i].MarkComplete();
		}
		if (CompleteOnly)
			datedRegions = datedRegions.Where(d => d.IsComplete).ToArray();
		ProgressWriter.Instance.EndProgress();

		ProgressWriter.Instance.BeginProgress("Flattening tile regions: ");
		await Parallel.ForAsync(0, datedRegions.Length, async (i, _) =>
		{
			using var datedRegion = datedRegions[i];
			datedRegions[i] = datedRegion.MergePolygons();
			ProgressWriter.Instance.ReportProgress(i / (double)datedRegions.Length);
		});
		ProgressWriter.Instance.EndProgress();
		return datedRegions;

		async Task<List<DatedTile>> getDatedTiles(KeyholeTile tile)
		{
			List<DatedTile> dates = new();

			if (await root.GetNodeAsync(tile) is not TileNode node)
				return dates;

			foreach (var datedTile in node.GetAllDatedTiles().Where(d => d.Date.Year != 1 && d.Date >= MinDate && d.Date <= MaxDate))
			{
				if (!dates.Any(d => d.Date == datedTile.Date))
					dates.Add(datedTile);
			}

			ProgressWriter.Instance.ReportProgress(++count / (double)regionTiles.Length);
			return dates;
		}
	}
}
