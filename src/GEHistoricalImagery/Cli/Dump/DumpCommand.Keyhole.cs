using LibGoogleEarth;
using LibMapCommon;

namespace GEHistoricalImagery.Cli.Dump;

internal partial class DumpCommand
{
	private async Task Run_Keyhole(DirectoryInfo saveFolder, IEnumerable<DateOnly> desiredDates)
	{
		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		var regionTiles = GetTiles(Region);

		var stats = Region.GetRectangularRegionStats<KeyholeTile>(ZoomLevel) with { TileCount = regionTiles.LongLength };
		var formatter = new FilenameFormatter(Formatter!, stats);

		ProgressWriter.Instance.BeginProgress($"Grabbing {regionTiles.Length:N0} Image Tiles {DateMatchPreposition} Specified Date{(desiredDates.Count() > 1 ? "s" : "")}: ");
		await Run_Common(saveFolder, stats.TileCount, formatter, generateWork());

		IEnumerable<Task<ITileDataset>> generateWork()
			=> regionTiles.Select(t => Task.Run(() => DownloadTile(root, t, desiredDates)));
	}

	private async Task<ITileDataset> DownloadTile(DbRoot root, KeyholeTile tile, IEnumerable<DateOnly> desiredDates)
	{
		if (await root.GetNodeAsync(tile) is not TileNode node)
			return EmptyDataset(tile);

		foreach (var dtr in node.GetAllDatedTiles().SortByNearestDates(desiredDates, DateMatch))
		{
			try
			{
				if (DateMatch is DateMatchType.Exact && !dtr.IsExactMatch)
					return EmptyDataset(tile, $"Exact date match not found for tile at {tile.Wgs84Center}. Closest tile date found: {dtr.DatedElement.Date.ToDateString()}");

				if (await root.GetEarthAssetAsync(dtr.DatedElement) is byte[] imageBts)
				{
					return new TileDataset<Wgs1984>(tile)
					{
						TileBytes = imageBts,
						Message = dtr.IsExactMatch ? null
						: $"Substituting imagery from {dtr.DatedElement.Date.ToDateString()} for tile at {tile.Wgs84Center}",
						TileDate = dtr.DatedElement.Date
					};
				}
			}
			catch (HttpRequestException)
			{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }
		}

		return EmptyDataset(tile);
	}
}
