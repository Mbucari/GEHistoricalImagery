using LibEsri;
using LibMapCommon;
using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Cli.Download;

internal partial class DownloadCommand
{
	private async Task Run_Esri(FileInfo saveFile, IEnumerable<DateOnly> desiredDates)
	{
		var wayBack = await WayBack.CreateAsync(CacheDir);
		var mercAoi = Region.Transform<WebMercator>();
		var regionTiles = GetTiles(mercAoi);

		await Run_Common(saveFile, mercAoi, regionTiles.Length, generateWork());

		IEnumerable<Task<TileDataset<WebMercator>>> generateWork()
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
				return regionTiles.Select(t => Task.Run(() => DownloadTile(mercAoi, wayBack, t, datedLayer.DatedElement)));
			}
			else
			{
				ProgressWriter.Instance.BeginProgress($"Grabbing {regionTiles.Length:N0} Image Tiles {DateMatchPreposition} Specified Date{(desiredDates.Count() > 1 ? "s" : "")}: ");
				return regionTiles.Select(t => Task.Run(() => DownloadTile(mercAoi, wayBack, t, desiredDates)));
			}
		}
	}

	private async Task<TileDataset<WebMercator>> DownloadTile(GeoRegion<WebMercator> aoi, WayBack wayBack, EsriTile tile, IEnumerable<DateOnly> desiredDates)
	{
		try
		{
			EsriTile gotTile = tile;
			DateMatchResult<DatedEsriTile>? dmr;
			while ((dmr = await wayBack.GetDatesAsync(tile).GetClosestDatedElement(desiredDates, DateMatch)) is null &&
				tile.Level - gotTile.Level < 2 && gotTile.Level >= 2)
			{
				gotTile = EsriTile.Create(gotTile.Row / 2, gotTile.Column / 2, gotTile.Level - 1);
			}

			if (dmr is null)
				return EmptyDataset(tile);

			if (DateMatch is DateMatchType.Exact && !dmr.IsExactMatch)
				return EmptyDataset(tile, $"Could not find an exact date match for tile at {tile.Wgs84Center} Closest tile date found: {dmr.DatedElement.CaptureDate.ToDateString()}");

			var imageBts = await wayBack.DownloadTileAsync(dmr.DatedElement.Layer, dmr.DatedElement.Tile);
			var dataset = OpenDataset(imageBts);
			var message = dmr.IsExactMatch ? null : $"Substituting imagery from {dmr.DatedElement.CaptureDate.ToDateString()} for tile at {tile.Wgs84Center}";

			if (gotTile.Level != tile.Level)
			{
				dataset = ResizeTile(gotTile, dataset, tile);
				message = $"Substituting level {gotTile.Level} imagery from {dmr.DatedElement.CaptureDate.ToDateString()} for tile at {tile.Wgs84Center}";
			}

			dataset = TrimDataset(dataset, aoi, tile);

			return new(tile)
			{
				Message = message,
				Dataset = dataset
			};
		}
		catch (HttpRequestException)
		{ /* Failed to get a dated tile image. This wile will be black in the final image. */ }

		return EmptyDataset(tile);
	}

	private static async Task<TileDataset<WebMercator>> DownloadTile(GeoRegion<WebMercator> aoi, WayBack wayBack, EsriTile tile, LibEsri.Layer layer)
	{
		EsriTile gotTile = tile;

		while (tile.Level - gotTile.Level < 2 && gotTile.Level >= 2)
		{
			try
			{
				var imageBts = await wayBack.DownloadTileAsync(layer, gotTile);
				var dataset = OpenDataset(imageBts);
				string? message = null;

				if (gotTile.Level != tile.Level)
				{
					dataset = ResizeTile(gotTile, dataset, tile);
					message = $"Substituting level {gotTile.Level} imagery from {layer.Title} for tile at {tile.Wgs84Center}";
				}

				dataset = TrimDataset(dataset, aoi, tile);

				return new(tile)
				{
					Message = message,
					Dataset = dataset
				};
			}
			catch (HttpRequestException)
			{ /* Failed to get a dated tile image. Try to find next level up. */ }

			gotTile = EsriTile.Create(gotTile.Row / 2, gotTile.Column / 2, gotTile.Level - 1);
		}

		return EmptyDataset(tile);
	}
}
