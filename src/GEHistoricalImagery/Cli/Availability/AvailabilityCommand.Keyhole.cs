using LibGoogleEarth;
using LibGoogleEarth.Geometry;
using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Cli.Availability;

internal partial class AvailabilityCommand
{
	private async Task Run_Keyhole()
	{
		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);

		var regionTiles = GetTiles(Region);
		var stats = Region.GetRectangularRegionStats<KeyholeTile>(ZoomLevel) with { TileCount = regionTiles.Length };
		var datedRegions = await GetAllKeyholeDatedRegionsAsync(root, stats, regionTiles);
		if (datedRegions.Length == 0)
		{
			Console.Error.WriteLine($"No dated imagery available within specified constraints");
			return;
		}

		if (SavePath is not null)
		{
			ProgressWriter.Instance.BeginProgress("Flattening dated regions: ");
			int count = 0;
			Parallel.For(0, datedRegions.Length, i =>
			{
				datedRegions[i].Flatten();
				ProgressWriter.Instance.ReportProgress(Interlocked.Add(ref count, 1) / (double)datedRegions.Length);
			});
			ProgressWriter.Instance.EndProgress();
			SaveDatedRegions(SavePath, datedRegions);
		}

		if (!Quiet)
		{
			var availabilities = GetKeyholeRegionAvailabilities(stats, datedRegions);
			PresentRegions(availabilities);
		}
	}

	private static RegionAvailability[] GetKeyholeRegionAvailabilities(TileStats stats, DatedRegion[] datedRegions)
	{
		//Build a map of tiles which hav data on any date. This map is used to
		//draw blanks for tiles which have no data in the entire set.
		BoolMap allData = new(stats.NumColumns, stats.NumRows);
		for (int i = 0; i < datedRegions.Length; i++)
		{
			allData = allData.Or(datedRegions[i].HasDataMap);
		}
		return datedRegions.Select(dr => new RegionAvailability(dr, allData)).ToArray();
	}

	private async Task<DatedRegion[]> GetAllKeyholeDatedRegionsAsync(DbRoot root, TileStats stats, KeyholeTile[] regionTiles)
	{
		ProgressWriter.Instance.BeginProgress("Building dated tile regions: ");
		var progress = new Progress<double>(ProgressWriter.Instance.ReportProgress);
		var datedRegions = await root.GetDateRegionsAsync(regionTiles, ConcurrentDownload, stats, MinDate, MaxDate, progress);
		root.ClearCache();

		if (CompleteOnly)
		{
			foreach (var unused in datedRegions.Where(r => !r.IsComplete))
			{
				unused.Dispose();
			}
			datedRegions = datedRegions.Where(r => r.IsComplete).ToArray();
		}
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive);
		ProgressWriter.Instance.EndProgress();

		return datedRegions;
	}
}
