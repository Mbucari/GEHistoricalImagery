using CommandLine;
using Google.Protobuf.WellKnownTypes;
using OSGeo.GDAL;

namespace GoogleEarthImageDownload.Cli;

[Verb("download", HelpText = "Download historical imagery")]
internal class Download : OptionsBase
{
	[Option("lower-left", Required = true, HelpText = "Geographic location", MetaValue = "LAT,LONG")]
	public Coordinate? LowerLeft { get; set; }

	[Option("upper-right", Required = true, HelpText = "Geographic location", MetaValue = "LAT,LONG")]
	public Coordinate? UpperRight { get; set; }

	[Option('z', "zoom", HelpText = "Zoom level [0-24]", MetaValue = "N", Required = true)]
	public int ZoomLevel { get; set; }

	[Option('d', "date", HelpText = "Imagery Date", MetaValue = "10/23/2023", Required = true)]
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

		//Try to create the output file so any problems will cause early failure
		var saveFile = new FileInfo(SavePath!);
		saveFile.Create().Dispose();

		Console.Write("Grabbing Image Tiles: ");
		ReportProgress(0);

		var aoi = new Rectangle(LowerLeft!.Value, UpperRight!.Value);
		var root = await DbRoot.CreateAsync();
		var desiredDate = Date!.Value.ToJpegCommentDate();
		var tempFile = Path.GetTempFileName();
		int tileProcessedCount = 0;
		int tileDownloadCount = 0;
		int tileCount = aoi.GetTileCount(ZoomLevel);
		var processor = new ParallelProcessor<TileDataset>(ConcurrentDownload);

		try
		{
			using EarthImage image = new(aoi, ZoomLevel, tempFile);

			await foreach (var tds in processor.EnumerateWorkAsync(generateWork()))
				using (tds)
				{
					if (tds.Dataset is not null)
					{
						image.AddTile(tds.Tile, tds.Dataset);
						tileDownloadCount++;
					}

					if (tds.Message is not null)
						Console.Error.WriteLine($"\r\n{tds.Message}");

					ReportProgress(++tileProcessedCount / (double)tileCount);
				}

			ReplaceProgress("Done!\r\n");
			Console.WriteLine($"{tileDownloadCount} out of {tileCount} downloaded");

			if (tileDownloadCount == 0)
			{
				if (saveFile.Exists)
					saveFile.Delete();
				return;
			}

			Console.Write("Saving Image: ");
			Progress = 0;

			image.Save(saveFile.FullName, TargetSpatialReference, ReportProgress, ConcurrentDownload, ScaleFactor, OffsetX, OffsetY);
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
			.Select(t => Task.Run(() => downloadTile(root, t, desiredDate)));
	}

	private async Task<TileDataset> downloadTile(DbRoot root, Tile tile, int desiredDate)
	{
		const GDAL_OF_ openOptions = GDAL_OF_.GDAL_OF_RASTER | GDAL_OF_.GDAL_OF_INTERNAL | GDAL_OF_.GDAL_OF_READONLY;
		const string ROOT_URL = "https://khmdb.google.com/flatfile?db=tm&f1-{0}-i.{1}-{2}";
		var node = await root.GetNodeAsync(tile.QtPath);

		var tempFilename = Path.GetTempFileName();

		foreach (var dd in node.GetAllDatedTiles().OrderBy(d => int.Abs(desiredDate - d.Date)))
		{
			string url = string.Format(ROOT_URL, tile.QtPath, dd.DatedTileEpoch, dd.Date.ToString("x"));

			try
			{
				var imageBts = await root.DownloadBytesAsync(url);
				await File.WriteAllBytesAsync(tempFilename, imageBts);

				return new()
				{
					Tile = tile,
					Dataset = Gdal.OpenEx(tempFilename, (uint)openOptions, null, null, new string[] { "" }),
					FileName = tempFilename,
					Message = dd.Date == desiredDate ? null : $"Substituting imagery from {dd.Date.ToDate()} for tile at {tile.LowerLeft}"
				};
			}
			catch (HttpRequestException) { }
		}
		return new()
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
