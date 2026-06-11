using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Cli.Download;

internal partial class DownloadCommand
{
	private async Task Run_Keyhole(FileInfo saveFile, IEnumerable<DateOnly> desiredDates)
	{
		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		var regionTiles = GetTiles(Region);

		await Run_Common(saveFile, Region, regionTiles.Length, generateWork());

		IEnumerable<Task<TileDataset<Wgs1984>>> generateWork()
		{
			ProgressWriter.Instance.BeginProgress($"Grabbing {regionTiles.Length:N0} Image Tiles {DateMatchPreposition} Specified Date{(desiredDates.Count() > 1 ? "s" : "")}: ");

			return regionTiles.Select(t => Task.Run(() => DownloadTile(Region, root, t, desiredDates)));
		}
	}

	private async Task<TileDataset<Wgs1984>> DownloadTile(GeoRegion<Wgs1984> aoi, DbRoot root, KeyholeTile tile, IEnumerable<DateOnly> desiredDates)
	{
		KeyholeTile gotTile = tile;
		TileNode? node;

		while ((node = await root.GetNodeAsync(gotTile)) is null &&
				tile.Level - gotTile.Level < 2 && gotTile.Level >= 2)
		{
			gotTile = KeyholeTile.Create(gotTile.Row / 2, gotTile.Column / 2, gotTile.Level - 1);
		}

		if (node is null)
			return EmptyDataset(tile);

		foreach (var dt in node.GetAllDatedTiles().SortByNearestDates(desiredDates, DateMatch))
		{
			try
			{
				if (DateMatch is DateMatchType.Exact && !dt.IsExactMatch)
					return EmptyDataset(tile, $"Exact date match not found for tile at {tile.Wgs84Center}. Closest layer date found: {dt.DatedElement.Date.ToDateString()}");

				if (await root.GetEarthAssetAsync(dt.DatedElement) is not byte[] imageBts)
					continue;

				var dataset = OpenDataset(imageBts);
				string? message = null;

				if (gotTile.Level != tile.Level)
				{
					dataset = ResizeTile(gotTile, dataset, tile);
					message = dt.DatedElement.Date == default
						? $"Substituting level {gotTile.Level} default imagery of unknown date for tile at {tile.Wgs84Center}"
						: $"Substituting level {gotTile.Level} imagery from {dt.DatedElement.Date.ToDateString()} for tile at {tile.Wgs84Center}";
				}
				else if (!dt.IsExactMatch)
					message = dt.DatedElement.Date == default
						? $"Substituting default imagery of unknown date for tile at {tile.Wgs84Center}"
						: $"Substituting imagery from {dt.DatedElement.Date.ToDateString()} for tile at {tile.Wgs84Center}";

				dataset = TrimDataset(dataset, aoi, tile);

				return new(tile)
				{
					Dataset = dataset,
					Message = message
				};
			}
			catch (HttpRequestException)
			{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }
		}

		return EmptyDataset(tile);
	}
}
