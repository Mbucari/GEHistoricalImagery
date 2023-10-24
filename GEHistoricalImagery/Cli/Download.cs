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

	[Option('p', "parallel", HelpText = "Number of concurrent downloads", MetaValue = "N", Default = 10)]
	public int ConcurrentDownload { get; set; }

	[Option("target-sr", HelpText = "Warp image to Spatial Reference", MetaValue = "https://epsg.io/1234.wkt", Default = null)]
	public string? TargetSpatialReference { get; set; }

	[Option('o', "output", HelpText = "Output GeoTiff save location", MetaValue = "out.tif", Required = true)]
	public string? SavePath { get; set; }

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

		if (hasError) return;

		//Try to create the output file so any problems will cause early failure
		var saveFile = new FileInfo(SavePath!);
		saveFile.Create().Dispose();

		Console.Write("Grabbing Image Tiles: ");
		ReportProgress(0);

		var aoi = new Rectangle(LowerLeft!.Value, UpperRight!.Value);

		var root = await DbRoot.CreateAsync();
		var desiredDate = Date!.Value.ToJpegCommentDate();
		using var image = new EarthImage(aoi, ZoomLevel);

		int count = 0, numDl = 0;
		int numTiles = aoi.GetTileCount(ZoomLevel);
		ParallelProcessor<TileDataset> processor = new(ConcurrentDownload);

		await foreach (var tds in processor.EnumerateWork(aoi.GetTiles(ZoomLevel).Select(downloadTile))) 
			using (tds)
			{
				if (tds.Dataset is not null)
				{
					image.AddTile(tds.Tile, tds.Dataset);
					numDl++;
				}

				if (tds.Message is not null)
					Console.Error.WriteLine($"\r\n{tds.Message}");

				ReportProgress(++count / (double)numTiles);
			}

		ReplaceProgress("Done!\r\n");
		Console.WriteLine($"{numDl} out of {numTiles} downloaded");

		if (numDl == 0)
		{
			if (saveFile.Exists)
				saveFile.Delete();
			return;
		}

		Console.Write("Saving Image: ");
		Progress = 0;

		image.Save(saveFile.FullName, TargetSpatialReference, ReportProgress);
		ReplaceProgress("Done!\r\n");

		async Task<TileDataset> downloadTile(Tile tile)
		{
			const string ROOT_URL = "https://khmdb.google.com/flatfile?db=tm&f1-{0}-i.{1}-{2}";
			var node = await root.GetNodeAsync(tile.QtPath);

			var tempFilename = Path.GetTempFileName();

			int tries = 0;
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
						Dataset = Gdal.Open(tempFilename, Access.GA_ReadOnly),
						FileName = tempFilename,
						Message = dd.Date == desiredDate ? null : $"Substituting imagery from {dd.Date.ToDate()} for tile at {tile.LowerLeft}"
					};
				}
				catch (HttpRequestException)
				{
					tries++;
				}
			}
			return new()
			{
				Tile = tile,
				Message = $"No imagery available for tile at {tile.LowerLeft}"
			};
		}
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
