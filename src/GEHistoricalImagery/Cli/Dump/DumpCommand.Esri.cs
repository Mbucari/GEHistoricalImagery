using LibEsri;
using LibMapCommon;

namespace GEHistoricalImagery.Cli.Dump;

internal partial class DumpCommand
{
	private async Task Run_Esri(DirectoryInfo saveFolder, IEnumerable<DateOnly> desiredDates)
	{
		var wayBack = await WayBack.CreateAsync(CacheDir);

		var mercAoi = Region.Transform<WebMercator>();
		var regionTiles = GetTiles(mercAoi);
		var stats = mercAoi.GetRectangularRegionStats<EsriTile>(ZoomLevel) with { TileCount = regionTiles.Length };
		var formatter = new FilenameFormatter(Formatter!, stats);
		await Run_Common(saveFolder, stats.TileCount, formatter, generateWork());

		IEnumerable<Task<ITileDataset>> generateWork()
		{
			if (LayerDate)
			{
				var datedLayer = wayBack.Layers.SortByNearestDates(desiredDates, DateMatch).FirstOrDefault();
				if (datedLayer is null)
				{
					Console.Error.WriteLine($"ERROR: No layers found");
					return [];
				}
				else if (DateMatch is DateMatchType.Exact && !datedLayer.IsExactMatch)
				{
					Console.Error.WriteLine($"ERROR: Exact layer date match not found. Closest layer date found: {datedLayer.DatedElement.Date.ToDateString()}");
					return [];
				}
				ProgressWriter.Instance.BeginProgress($"Grabbing {regionTiles.Length:N0} Image Tiles From {datedLayer.DatedElement.Title}: ");
				return regionTiles.Select(t => Task.Run(() => DownloadEsriTile(wayBack, t, datedLayer.DatedElement, formatter.HasTileDate)));
			}
			else
			{
				ProgressWriter.Instance.BeginProgress($"Grabbing {regionTiles.Length:N0} Image Tiles {DateMatchPreposition} Specified Date{(desiredDates.Count() > 1 ? "s" : "")}: ");
				return regionTiles.Select(t => Task.Run(() => DownloadEsriTile(wayBack, t, desiredDates)));
			}
		}
	}

	private async Task<ITileDataset> DownloadEsriTile(WayBack wayBack, EsriTile tile, IEnumerable<DateOnly> desiredDates)
	{
		try
		{
			//Only try for the first, closest match when using Wayback capture dates
			//because enumerating all capture dates for each tiles is too slow.
			var dt = await wayBack.GetDatesAsync(tile).GetClosestDatedElement(desiredDates, DateMatch);
			if (dt is null)
				return EmptyDataset(tile);

			if (DateMatch is DateMatchType.Exact && !dt.IsExactMatch)
				return EmptyDataset(tile, $"Could not find an exact date match for tile at {tile.Wgs84Center} Closest tile date found: {dt.DatedElement.CaptureDate.ToDateString()}");

			var imageBts = await wayBack.DownloadTileAsync(dt.DatedElement.Layer, dt.DatedElement.Tile);

			return new TileDataset<WebMercator>(tile)
			{
				TileBytes = imageBts,
				Message = dt.IsExactMatch ? null : $"Substituting imagery from {dt.DatedElement.CaptureDate.ToDateString()} for tile at {tile.Wgs84Center}",
				TileDate = dt.DatedElement.CaptureDate,
				LayerDate = dt.DatedElement.LayerDate
			};
		}
		catch (HttpRequestException)
		{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }

		return EmptyDataset(tile);
	}

	private static async Task<ITileDataset> DownloadEsriTile(WayBack wayBack, EsriTile tile, Layer layer, bool getTileDate)
	{
		try
		{
			var imageBts = await wayBack.DownloadTileAsync(layer, tile);

			return new TileDataset<WebMercator>(tile)
			{
				TileBytes = imageBts,
				Message = null,
				TileDate = getTileDate ? await wayBack.GetDateAsync(layer, tile) : default,
				LayerDate = layer.Date
			};
		}
		catch (HttpRequestException)
		{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }

		return EmptyDataset(tile);
	}
}
