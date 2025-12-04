using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.GDAL;

namespace GEHistoricalImagery.Cli;

[Verb("dump", HelpText = "Dump historical image tiles into a folder")]
internal partial class Dump : FileDownloadVerb
{
	private const string formatHelpText = """
				
				Filename formatter:
				  "{Z}" = tile's zoom level
				  "{C}" = tile's global column number
				  "{R}" = tile's global row number
				  "{c}" = tile's column number within the rectangle
				  "{r}" = tile's row number within the rectangle
				  "{D}" = tile's image capture date
				  "{LD}" = tile's layer date (wayback only)
				""";

	[Option('o', "output", HelpText = "Output image tile save directory", MetaValue = "[Directory]", Required = true)]
	public override string? SavePath { get; set; }

	[Option('f', "format", HelpText = formatHelpText, Default = "z={Z}-Col={c}-Row={r}.jpg", MetaValue = "[FilenameFormat]")]
	public string? Formatter { get; set; }

	[Option('w', "world", HelpText = "Write a world file for each tile")]
	public bool WriteWorldFile { get; set; }

	public override async Task RunAsync()
	{
		if (AnyFileDownloadErrors())
			return;

		if (string.IsNullOrEmpty(Formatter))
		{
			Console.Error.WriteLine($"Invalid filename formatter");
			return;
		}
		else if (Formatter.FirstOrDefault(c => Path.GetInvalidFileNameChars().Any(i => i == c)) is char fileChar && fileChar != default)
		{
			Console.Error.WriteLine($"Invalid filename character: {fileChar}");
			return;
		}
		else if (!(Formatter.Contains("{C}") || Formatter.Contains("{c}")) ||
			!(Formatter.Contains("{R}") || Formatter.Contains("{r}")))
		{
			Console.Error.WriteLine(
				"""
				Filename formatter must contain:
				 a "{C}" tag for the tile's global column number
				  or a "{c}" tag for the tile's column number within the rectangle
				 a "{R}" tag for the tile's global row number
				  or a "{r}" tag for the tile's row number within the rectangle
				 (optional) a "{Z}" tag for the tile's zoom level
				""");
			return;
		}

		DirectoryInfo saveFolder;
		try
		{
			//Try to create the output file so any problems will cause early failure
			saveFolder = new DirectoryInfo(PathHelper.ReplaceUnixHomeDir(SavePath!));
			saveFolder.Create();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error saving file {SavePath}");
			Console.Error.WriteLine($"\t{ex.Message}");
			return;
		}

		var desiredDates = Dates!;
		var task = Provider is Provider.Wayback ? Run_Esri(saveFolder, desiredDates)
		: Run_Keyhole(saveFolder, desiredDates);

		await task;
	}

	#region Esri

	private async Task Run_Esri(DirectoryInfo saveFolder, IEnumerable<DateOnly> desiredDates)
	{
		var wayBack = await WayBack.CreateAsync(CacheDir);

		var webMerc = Region.ToWebMercator();
		var stats = webMerc.GetPolygonalRegionStats<EsriTile>(ZoomLevel);
		var formatter = new FilenameFormatter(Formatter!, stats);
		await Run_Common(saveFolder, stats.TileCount, formatter, generateWork());

		IEnumerable<Task<TileDataset>> generateWork()
		{
			if (LayerDate)
			{
				var datedLayer = wayBack.Layers.SortByNearestDates(d => d.Date, desiredDates).FirstOrDefault();

				if (datedLayer is null)
				{
					Console.Error.WriteLine($"ERROR: No layers found");
					return [];
				}
				else if (ExactMatch && !datedLayer.IsExactMatch)
				{
					Console.Error.WriteLine($"ERROR: Exact layer date match not found. Closest layer date found: {DateString(datedLayer.DatedElement.Date)}");
					return [];
				}

				Console.Write($"Grabbing Image Tiles From {datedLayer.DatedElement.Title}: ");
				ReportProgress(0);
				return webMerc.GetTiles<EsriTile>(ZoomLevel).Select(t => Task.Run(() => DownloadEsriTile(wayBack, t, datedLayer.DatedElement, formatter.HasTileDate)));
			}
			else
			{
				var message = ExactMatch ? "On" : "Nearest To";
				Console.Write($"Grabbing Image Tiles {message} Spefidied Date{(desiredDates.Count() > 1 ? "s":"")}: ");
				ReportProgress(0);
				return webMerc.GetTiles<EsriTile>(ZoomLevel).Select(t => Task.Run(() => DownloadEsriTile(wayBack, t, desiredDates)));
			}
		}
	}

	private async Task<TileDataset> DownloadEsriTile(WayBack wayBack, EsriTile tile, IEnumerable<DateOnly> desiredDates)
	{
		try
		{
			//Only try for the first, closest match when using Wayback capture dates
			//because enumerating all capture dates for each tiles is too slow.
			var dt = await wayBack.GetDatesAsync(tile).GetCloseteDatedElement(d => d.CaptureDate, desiredDates);
			if (dt is null)
				return EmptyDataset(tile);

			if (ExactMatch && !dt.IsExactMatch)
				return EmptyDataset(tile, $"Could not find an exact date match for tile at {tile.Wgs84Center} Closest tile date found: {DateString(dt.DatedElement.CaptureDate)}");

			var imageBts = await wayBack.DownloadTileAsync(dt.DatedElement.Layer, dt.DatedElement.Tile);

			return new TileDataset<WebMercator>(tile)
			{
				TileBytes = imageBts,
				Message = dt.IsExactMatch ? null : $"Substituting imagery from {DateString(dt.DatedElement.CaptureDate)} for tile at {tile.Wgs84Center}",
				TileDate = dt.DatedElement.CaptureDate,
				LayerDate = dt.DatedElement.LayerDate
			};
		}
		catch (HttpRequestException)
		{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }

		return EmptyDataset(tile);
	}

	private static async Task<TileDataset> DownloadEsriTile(WayBack wayBack, EsriTile tile, Layer layer, bool getTileDate)
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

	#endregion

	#region Keyhole

	private async Task Run_Keyhole(DirectoryInfo saveFolder, IEnumerable<DateOnly> desiredDates)
	{
		Console.Write("Grabbing Image Tiles: ");
		ReportProgress(0);

		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		var stats = Region.GetPolygonalRegionStats<KeyholeTile>(ZoomLevel);
		var formatter = new FilenameFormatter(Formatter!, stats);
		await Run_Common(saveFolder, stats.TileCount, formatter, generateWork());

		IEnumerable<Task<TileDataset>> generateWork()
			=> Region
			.GetTiles<KeyholeTile>(ZoomLevel)
			.Select(t => Task.Run(() => DownloadTile(root, t, desiredDates)));
	}

	private async Task<TileDataset> DownloadTile(DbRoot root, KeyholeTile tile, IEnumerable<DateOnly> desiredDates)
	{
		if (await root.GetNodeAsync(tile) is not TileNode node)
			return EmptyDataset(tile);

		foreach (var dtr in node.GetAllDatedTiles().SortByNearestDates(t => t.Date, desiredDates))
		{
			try
			{
				if (ExactMatch && !dtr.IsExactMatch)
					return EmptyDataset(tile, $"Exact date match not found for tile at {tile.Wgs84Center}. Closest tile date found: {DateString(dtr.DatedElement.Date)}");

				if (await root.GetEarthAssetAsync(dtr.DatedElement) is byte[] imageBts)
				{
					return new TileDataset<Wgs1984>(tile)
					{
						TileBytes = imageBts,
						Message = dtr.IsExactMatch ? null
						: $"Substituting imagery from {DateString(dtr.DatedElement.Date)} for tile at {tile.Wgs84Center}",
						TileDate = dtr.DatedElement.Date
					};
				}
			}
			catch (HttpRequestException)
			{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }
		}

		return EmptyDataset(tile);
	}

	#endregion

	#region Common

	private async Task Run_Common(DirectoryInfo saveFolder, double tileCount, FilenameFormatter formatter, IEnumerable<Task<TileDataset>> generator)
	{
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var processor = new ParallelProcessor<TileDataset>(ConcurrentDownload);

		await foreach (var tds in processor.EnumerateResults(generator))
		{
			if (tds.Message is not null)
				Console.Error.WriteLine($"{Environment.NewLine}{tds.Message}");

			if (tds.TileBytes is not null)
			{
				var saveFile = formatter.GetString(tds);
				var savePath = Path.Combine(saveFolder.FullName, saveFile);
				SaveDataset(savePath, tds);
				numTilesDownload++;
			}

			ReportProgress(++numTilesProcessed / tileCount);
		}

		ReplaceProgress($"Done!{Environment.NewLine}");
		Console.WriteLine($"{numTilesDownload} out of {tileCount} downloaded");
	}

	private void SaveDataset(string filePath, TileDataset tds)
	{
		if (TargetSpatialReference is null)
		{
			if (tds.TileBytes is null)
			{
				Console.Error.WriteLine($"{Environment.NewLine}Dataset for tile {tds.Tile} is empty");
				return;
			}
			File.WriteAllBytes(filePath, tds.TileBytes);
			if (WriteWorldFile)
			{
				tds.GetGeoTransform().WriteWorldFile(filePath);
			}
			return;
		}

		const GDAL_OF openOptions = GDAL_OF.RASTER | GDAL_OF.INTERNAL | GDAL_OF.READONLY;
		string srcFile = $"/vsimem/{Guid.NewGuid()}.jpeg";

		try
		{
			Gdal.FileFromMemBuffer(srcFile, tds.TileBytes);
			using var sourceDs = Gdal.OpenEx(srcFile, (uint)openOptions, ["JPEG"], null, []);

			var geoTransform = tds.GetGeoTransform();
			sourceDs.SetGeoTransform(geoTransform);

			using var options = tds.GetWarpOptions(TargetSpatialReference);
			using var destDs = Gdal.Warp(filePath, [sourceDs], options, null, null);
			if (WriteWorldFile)
			{
				destDs.GetGeoTransform().WriteWorldFile(filePath);
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Failed to open GDAL dataset for tile at {tds.Tile.Wgs84Center}: {ex.Message}");
		}
		finally
		{
			Gdal.Unlink(srcFile);
		}
	}

	private static TileDataset EmptyDataset<TCoordinate>(ITile<TCoordinate> tile, string? messageOverride = null)
		where TCoordinate : IGeoCoordinate<TCoordinate> => new TileDataset<TCoordinate>(tile)
	{
		Message = messageOverride ?? $"No imagery available for tile at {tile.Wgs84Center}"
	};


	private class FilenameFormatter
	{
		public bool HasTileDate { get; }

		private readonly string LocalRowFormat;
		private readonly string LocalColumnFormat;
		private readonly string GlobalRowFormat;
		private readonly string GlobalColumnFormat;
		private readonly string FormatString;
		private readonly int LowerLeftRow;
		private readonly int LowerLeftColumn;
		private readonly int NumTilesAtLevel;
		public FilenameFormatter(string formatter, TileStats stats)
		{
			LowerLeftRow = stats.MinRow;
			LowerLeftColumn = stats.MinColumn;
			NumTilesAtLevel = 1 << stats.Zoom;
			GetLocalFormatters(stats, out LocalColumnFormat, out LocalRowFormat);
			GetGlobalFormatters(stats, out GlobalColumnFormat, out GlobalRowFormat);

			HasTileDate = formatter.Contains("{D}");
			FormatString
				= formatter
				.Replace("{Z}", "{0}")
				.Replace("{C}", "{1}")
				.Replace("{c}", "{2}")
				.Replace("{R}", "{3}")
				.Replace("{r}", "{4}")
				.Replace("{D}", "{5}")
				.Replace("{LD}", "{6}");
		}

		public string GetString(TileDataset dataset)
		{
			int localCol = dataset.Tile.Column - LowerLeftColumn;
			if (localCol < 0)
				localCol += NumTilesAtLevel;

			int localRow = int.Abs(dataset.Tile.Row - LowerLeftRow);
			if (localRow < 0)
				localRow += NumTilesAtLevel;

			return string.Format(
				FormatString,
				dataset.Tile.Level,
				dataset.Tile.Column.ToString(GlobalColumnFormat),
				localCol.ToString(LocalColumnFormat),
				dataset.Tile.Row.ToString(GlobalRowFormat),
				localRow.ToString(LocalRowFormat),
				dataset.TileDate.ToString("yyyy-MM-dd"),
				dataset.LayerDate?.ToString("yyyy-MM-dd"));
		}

		private static void GetGlobalFormatters(TileStats stats, out string colFormatter, out string rowFormatter)
		{
			rowFormatter = DigitFormatter(stats.MaxRow);
			colFormatter = DigitFormatter(stats.MaxColumn);
		}

		private static void GetLocalFormatters(TileStats stats, out string colFormatter, out string rowFormatter)
		{
			rowFormatter = DigitFormatter(stats.NumRows);
			colFormatter = DigitFormatter(stats.NumColumns);
		}

		private static string DigitFormatter(int maxNumber)
		{
			var maxNumDigits = (int)Math.Ceiling(Math.Log10(maxNumber));
			return "D" + maxNumDigits;
		}
	}
	#endregion
}
