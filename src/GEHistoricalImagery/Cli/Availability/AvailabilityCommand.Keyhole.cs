using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Cli.Availability;

internal partial class AvailabilityCommand
{
	private async Task Run_Keyhole()
	{
		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		var all = await GetAllDatesAsync(root, Region);
		ProgressWriter.Instance.EndProgress();

		if (all.Length == 0)
		{
			Console.Error.WriteLine($"No dated imagery available within specified constraints");
			return;
		}

		OptionChooser<RegionAvailability>.WaitForOptions(all);
	}

	private async Task<RegionAvailability[]> GetAllDatesAsync(DbRoot root, GeoRegion<Wgs1984> reg)
	{
		int count = 0;

		var regionTiles = GetTiles(reg);
		var stats = reg.GetRectangularRegionStats<KeyholeTile>(ZoomLevel) with { TileCount = regionTiles.Length };
		ProgressWriter.Instance.BeginProgress("Loading Quad Tree Packets: ");
		ParallelProcessor<List<DatedTile>> processor = new(ConcurrentDownload);

		Dictionary<DateOnly, RegionAvailability> uniqueDates = new();
		HashSet<Tuple<int, int>> uniquePoints = new();

		await foreach (var dSet in processor.EnumerateResults(regionTiles.Select(getDatedTiles)))
		{
			foreach (var d in dSet)
			{
				if (!uniqueDates.TryGetValue(d.Date, out RegionAvailability? region))
				{
					region = new RegionAvailability(d.Date, stats.NumRows, stats.NumColumns);
					uniqueDates.Add(d.Date, region);
				}

				var cIndex = LibMapCommon.Util.Mod(d.Tile.Column - stats.MinColumn, 1 << d.Tile.Level);
				var rIndex = stats.MaxRow - d.Tile.Row;

				uniquePoints.Add(new Tuple<int, int>(rIndex, cIndex));
				region[rIndex, cIndex] = await root.GetNodeAsync(d.Tile) is not null;
			}

			ProgressWriter.Instance.ReportProgress(++count / (double)stats.TileCount);
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

		return uniqueDates.Values.Where(r => !CompleteOnly || !r.HasAllTiles()).OrderByDescending(r => r.Date).ToArray();

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
			return dates;
		}
	}
}
