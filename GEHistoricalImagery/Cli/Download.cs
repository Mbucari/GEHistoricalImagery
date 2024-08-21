using CommandLine;
using Google.Protobuf.WellKnownTypes;
using OSGeo.GDAL;

namespace GEHistoricalImagery.Cli;

[Verb("download", HelpText = "Download historical imagery")]
internal class Download : OptionsBase
{
	[Option("lower-left", Required = true, HelpText = "Geographic location", MetaValue = "LAT,LONG")]
	public Coordinate? LowerLeft { get; set; }

	[Option("upper-right", Required = true, HelpText = "Geographic location", MetaValue = "LAT,LONG")]
	public Coordinate? UpperRight { get; set; }

	[Option('z', "zoom", HelpText = "Zoom level [0-24]", MetaValue = "N", Required = true)]
	public int ZoomLevel { get; set; }

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

	[Option("offset-x", HelpText = "Geo transform X offset (post-scaling)", MetaValue = "X", Default = 0d)]
	public double OffsetX { get; set; }

	[Option("offset-y", HelpText = "Geo transform Y offset (post-scaling)", MetaValue = "Y", Default = 0d)]
	public double OffsetY { get; set; }

	[Option("scale-first", HelpText = "Performe scaling before translation", Default = false)]
	public bool ScaleFirst { get; set; }


	public override async Task Run()
	{
		bool hasError = false;
		if (LowerLeft is null || UpperRight is null)
		{
			Console.Error.WriteLine("Invalid coordinate(s).\r\n Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305");
			hasError = true;
		}

		if (ZoomLevel > 24)
		{
			Console.Error.WriteLine($"Zoom level: {ZoomLevel} is too large. Max zoom is 24");
			hasError = true;
		}
		else if (ZoomLevel < 0)
		{
			Console.Error.WriteLine($"Zoom level: {ZoomLevel} is too small. Min zoom is 0");
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

		Console.Write("Grabbing Image Tiles: ");
		ReportProgress(0);

		var aoi = new Rectangle(LowerLeft!.Value, UpperRight!.Value);
		var root = await DbRoot.CreateAsync();
		var desiredDate = Date!.Value;
		var tempFile = Path.GetTempFileName();
		int tileCount = aoi.GetTileCount(ZoomLevel);
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var processor = new ParallelProcessor<TileDataset>(ConcurrentDownload);

		try
		{
			using EarthImage image = new(aoi, ZoomLevel, tempFile);

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
			=> aoi
			.GetTiles(ZoomLevel)
			.Select(t => Task.Run(() => DownloadTile(root, t, desiredDate)));
	}

	private static async Task<TileDataset> DownloadTile(DbRoot root, Tile tile, DateOnly desiredDate)
	{
		const GDAL_OF openOptions = GDAL_OF.RASTER | GDAL_OF.INTERNAL | GDAL_OF.READONLY;

		if (await root.GetNodeAsync(tile) is not Node node)
			return emptyDataset();

		var tempFilename = Path.GetTempFileName();

		foreach (var dd in node.GetAllDatedTiles().OrderBy(d => int.Abs(desiredDate.DayNumber - d.Date.DayNumber)))
		{
			try
			{
				var imageBts = await root.DownloadBytesAsync(dd.TileUrl);
				await File.WriteAllBytesAsync(tempFilename, imageBts);

				return new()
				{
					Tile = tile,
					Dataset = Gdal.OpenEx(tempFilename, (uint)openOptions, null, null, []),
					FileName = tempFilename,
					Message = dd.Date == desiredDate ? null : $"Substituting imagery from {dd.Date} for tile at {tile.GetCenter()}"
				};
			}
			catch (HttpRequestException) { }
		}

		return emptyDataset();

		TileDataset emptyDataset() => new()
		{
			Tile = tile,
			Message = $"No imagery available for tile at {tile.LowerLeft}"
		};
	}

	private class TileDataset : IDisposable
	{
		public required Tile Tile { get; init; }
		public Dataset? Dataset { get; init; }
		public string? FileName { get; init; }
		public required string? Message { get; init; }

		public void Dispose()
		{
			Dataset?.Dispose();
			if (File.Exists(FileName))
				File.Delete(FileName);
		}
	}
}
