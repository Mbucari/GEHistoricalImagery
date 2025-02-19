using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using OSGeo.GDAL;

namespace GEHistoricalImagery.Cli;

[Verb("download", HelpText = "Download historical imagery")]
internal class Download : AoiVerb
{
	[Option('d', "date", HelpText = "Imagery Date", MetaValue = "yyyy/MM/dd", Required = true)]
	public DateOnly? Date { get; set; }

	[Option('o', "output", HelpText = "Output GeoTiff save location", MetaValue = "out.tif", Required = true)]
	public string? SavePath { get; set; }

	[Option('p', "parallel", HelpText = $"(Default: ALL_CPUS) Number of concurrent downloads", MetaValue = "N")]
	public int ConcurrentDownload { get; set; }

	[Option("target-sr", HelpText = "Warp image to Spatial Reference", MetaValue = "https://epsg.io/1234.wkt", Default = null)]
	public string? TargetSpatialReference { get; set; }

	[Option("scale", HelpText = "Geo transform scale factor", MetaValue = "S", Default = 1d)]
	public double ScaleFactor { get; set; }

	[Option("offset-x", HelpText = "Geo transform X offset", MetaValue = "X", Default = 0d)]
	public double OffsetX { get; set; }

	[Option("offset-y", HelpText = "Geo transform Y offset", MetaValue = "Y", Default = 0d)]
	public double OffsetY { get; set; }

	[Option("scale-first", HelpText = "Perform scaling before offsetting X and Y", Default = false)]
	public bool ScaleFirst { get; set; }

	public override async Task RunAsync()
	{
		bool hasError = false;

		foreach (var errorMessage in GetAoiErrors())
		{
			Console.Error.WriteLine(errorMessage);
			hasError = true;
		}

		if (Date is null)
		{
			Console.Error.WriteLine("Invalid imagery date");
			hasError = true;
		}

		if (string.IsNullOrWhiteSpace(SavePath))
		{
			Console.Error.WriteLine("Invalid output file");
			hasError = true;
		}

		if (ConcurrentDownload <= 0)
			ConcurrentDownload = Environment.ProcessorCount;

		if (hasError) return;

		FileInfo saveFile;
		try
		{
			//Try to create the output file so any problems will cause early failure
			saveFile = new FileInfo(SavePath!);
			saveFile.Create().Dispose();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error saving file {SavePath}");
			Console.Error.WriteLine($"\t{ex.Message}");
			return;
		}

		var desiredDate = Date!.Value;

		var task = Provider is Provider.Wayback ? Run_Esri(saveFile, desiredDate)
			: Run_Keyhole(saveFile, desiredDate);

		await task;
	}

	#region Esri

	private async Task Run_Esri(FileInfo saveFile, DateOnly desiredDate)
	{
		var wayBack = await WayBack.CreateAsync(CacheDir);
		var layer = wayBack.Layers.OrderBy(l => int.Abs(l.Date.DayNumber - desiredDate.DayNumber)).First();

		Console.Write($"Grabbing Image Tiles From {layer.Title}: ");
		ReportProgress(0);

		var tempFile = Path.GetTempFileName();
		int tileCount = Aoi.GetTileCount<EsriTile>(ZoomLevel);
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var processor = new ParallelProcessor<TileDataset>(ConcurrentDownload);

		try
		{
			using var image = new EsriImage(Aoi, ZoomLevel, tempFile);

			await foreach (var tds in processor.EnumerateResults(generateWork()))
				using (tds)
				{
					if (tds.Dataset is not null)
					{
						image.AddTile((EsriTile)tds.Tile, tds.Dataset);
						numTilesDownload++;
					}

					if (tds.Message is not null)
						Console.Error.WriteLine($"\r\n{tds.Message}");

					ReportProgress(++numTilesProcessed / (double)tileCount);
				}

			ReplaceProgress("Done!\r\n");
			Console.WriteLine($"{numTilesDownload} out of {tileCount} downloaded");

			if (numTilesDownload == 0)
			{
				if (saveFile.Exists)
					saveFile.Delete();
				return;
			}

			Console.Write("Saving Image: ");
			Progress = 0;

			image.Save(saveFile.FullName, TargetSpatialReference, ReportProgress, ConcurrentDownload, ScaleFactor, OffsetX, OffsetY, ScaleFirst);
			ReplaceProgress("Done!\r\n");
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}


		IEnumerable<Task<TileDataset>> generateWork()
			=> Aoi
			.GetTiles<EsriTile>(ZoomLevel)
			.Select(t => Task.Run(() => DownloadTile(wayBack, t, layer)));
	}

	private static async Task<TileDataset> DownloadTile(WayBack wayBack, EsriTile tile, Layer layer)
	{
		const GDAL_OF openOptions = GDAL_OF.RASTER | GDAL_OF.INTERNAL | GDAL_OF.READONLY;

		try
		{
			var bytes = await wayBack.DownloadTileAsync(layer, tile);

			string memFile = $"/vsimem/{Guid.NewGuid()}.jpeg";
			try
			{
				Gdal.FileFromMemBuffer(memFile, bytes);

				return new()
				{
					Tile = tile,
					Message = null,
					Dataset = Gdal.OpenEx(memFile, (uint)openOptions, ["JPEG"], null, [])
				};
			}
			finally
			{
				Gdal.Unlink(memFile);
			}
		}
		catch (HttpRequestException)
		{ /* Failed to get a dated tile image. This wile will be black in the final image. */ }

		return EmptyDataset(tile);
	}
	#endregion

	#region Keyhole

	private async Task Run_Keyhole(FileInfo saveFile, DateOnly desiredDate)
	{
		Console.Write("Grabbing Image Tiles: ");
		ReportProgress(0);

		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		var tempFile = Path.GetTempFileName();
		int tileCount = Aoi.GetTileCount<KeyholeTile>(ZoomLevel);
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var processor = new ParallelProcessor<TileDataset>(ConcurrentDownload);

		try
		{
			using var image = new KeyholeImage(Aoi, ZoomLevel, tempFile);

			await foreach (var tds in processor.EnumerateResults(generateWork()))
				using (tds)
				{
					if (tds.Dataset is not null)
					{
						image.AddTile(tds.Tile, tds.Dataset);
						numTilesDownload++;
					}

					if (tds.Message is not null)
						Console.Error.WriteLine($"\r\n{tds.Message}");

					ReportProgress(++numTilesProcessed / (double)tileCount);
				}

			ReplaceProgress("Done!\r\n");
			Console.WriteLine($"{numTilesDownload} out of {tileCount} downloaded");

			if (numTilesDownload == 0)
			{
				if (saveFile.Exists)
					saveFile.Delete();
				return;
			}

			Console.Write("Saving Image: ");
			Progress = 0;

			image.Save(saveFile.FullName, TargetSpatialReference, ReportProgress, ConcurrentDownload, ScaleFactor, OffsetX, OffsetY, ScaleFirst);
			ReplaceProgress("Done!\r\n");
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}

		IEnumerable<Task<TileDataset>> generateWork()
			=> Aoi
			.GetTiles<KeyholeTile>(ZoomLevel)
			.Select(t => Task.Run(() => DownloadTile(root, t, desiredDate)));
	}

	private static async Task<TileDataset> DownloadTile(DbRoot root, KeyholeTile tile, DateOnly desiredDate)
	{
		const GDAL_OF openOptions = GDAL_OF.RASTER | GDAL_OF.INTERNAL | GDAL_OF.READONLY;

		if (await root.GetNodeAsync(tile) is not TileNode node)
			return EmptyDataset(tile);

		foreach (var dt in node.GetAllDatedTiles().OrderBy(d => int.Abs(desiredDate.DayNumber - d.Date.DayNumber)))
		{
			try
			{
				byte[]? imageBts = await root.GetEarthAssetAsync(dt);
				if (imageBts == null)
					continue;

				string memFile = $"/vsimem/{Guid.NewGuid()}.jpeg";
				try
				{
					Gdal.FileFromMemBuffer(memFile, imageBts);

					return new()
					{
						Tile = tile,
						Dataset = Gdal.OpenEx(memFile, (uint)openOptions, ["JPEG"], null, []),
						Message = dt.Date == desiredDate ? null : $"Substituting imagery from {DateString(dt.Date)} for tile at {tile.Center}"
					};
				}
				finally
				{
					Gdal.Unlink(memFile);
				}
			}
			catch (HttpRequestException)
			{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }
		}

		return EmptyDataset(tile);
	}

	#endregion

	#region Common

	private static TileDataset EmptyDataset(ITile tile) => new()
	{
		Tile = tile,
		Message = $"No imagery available for tile at {tile.Center}"
	};

	private class TileDataset : IDisposable
	{
		public required ITile Tile { get; init; }
		public Dataset? Dataset { get; init; }
		public required string? Message { get; init; }

		public void Dispose()
		{
			Dataset?.Dispose();
		}
	}
	#endregion
}
