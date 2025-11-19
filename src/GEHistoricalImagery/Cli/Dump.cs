using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.GDAL;

namespace GEHistoricalImagery.Cli;

[Verb("dump", HelpText = "Dump historical image tiles into a folder")]
internal partial class Dump : AoiVerb
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

	[Option('d', "date", HelpText = "Imagery Date", MetaValue = "yyyy/MM/dd", Required = true)]
	public DateOnly? Date { get; set; }

	[Option("layer-date", HelpText = "(Wayback only) The date specifies a layer instead of an image capture date")]
	public bool LayerDate { get; set; }

	[Option('o', "output", HelpText = "Output image tile save directory", MetaValue = "[Directory]", Required = true)]
	public string? SavePath { get; set; }

	[Option('f', "format", HelpText = formatHelpText, Default = "z={Z}-Col={c}-Row={r}.jpg", MetaValue = "[FilenameFormat]")]
	public string? Formatter { get; set; }

	[Option('p', "parallel", HelpText = $"(Default: ALL_CPUS) Number of concurrent downloads", MetaValue = "N")]
	public int ConcurrentDownload { get; set; }

	[Option("target-sr", HelpText = "Warp image to Spatial Reference. Either EPSG:#### or path to projection file (file system or web)", MetaValue = "[SPATIAL REFERENCE]", Default = null)]
	public string? TargetSpatialReference { get; set; }

	[Option('w', "world", HelpText = "Write a world file for each tile")]
	public bool WriteWorldFile { get; set; }

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

		if (string.IsNullOrEmpty(Formatter))
		{
			Console.Error.WriteLine($"Invalid filename formatter");
			hasError = true;
		}
		else if (Formatter.FirstOrDefault(c => Path.GetInvalidFileNameChars().Any(i => i == c)) is char fileChar && fileChar != default)
		{
			Console.Error.WriteLine($"Invalid filename character: {fileChar}");
			hasError = true;
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
			hasError = true;
		}

		if (hasError) return;

		DirectoryInfo saveFolder;
		try
		{
			//Try to create the output file so any problems will cause early failure
			saveFolder = new DirectoryInfo(SavePath!);
			saveFolder.Create();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error saving file {SavePath}");
			Console.Error.WriteLine($"\t{ex.Message}");
			return;
		}

		if (ConcurrentDownload <= 0)
			ConcurrentDownload = Environment.ProcessorCount;

		var desiredDate = Date!.Value;

		var task = Provider is Provider.Wayback ? Run_Esri(saveFolder, desiredDate)
		: Run_Keyhole(saveFolder, desiredDate);

		await task;
	}

	#region Esri

	private async Task Run_Esri(DirectoryInfo saveFolder, DateOnly desiredDate)
	{
		var wayBack = await WayBack.CreateAsync(CacheDir);

		var webMerc = Region.ToWebMercator();
		var stats = webMerc.GetPolygonalRegionStats<EsriTile>(ZoomLevel);
		var formatter = new FilenameFormatter(Formatter!, stats);
		await Run_Common(saveFolder, desiredDate, stats.TileCount, formatter, generateWork());

		IEnumerable<Task<TileDataset>> generateWork()
		{
			if (LayerDate)
			{
				var layer = wayBack.Layers.OrderBy(l => int.Abs(l.Date.DayNumber - desiredDate.DayNumber)).First();

				Console.Write($"Grabbing Image Tiles From {layer.Title}: ");
				ReportProgress(0);
				return webMerc.GetTiles<EsriTile>(ZoomLevel).Select(t => Task.Run(() => DownloadEsriTile(wayBack, t, layer, formatter.HasTileDate)));
			}
			else
			{
				Console.Write($"Grabbing Image Tiles Nearest To {DateString(desiredDate)}: ");
				ReportProgress(0);
				return webMerc.GetTiles<EsriTile>(ZoomLevel).Select(t => Task.Run(() => DownloadEsriTile(wayBack, t, desiredDate)));
			}
		}
	}

	private static async Task<TileDataset> DownloadEsriTile(WayBack wayBack, EsriTile tile, DateOnly desiredDate)
	{
		try
		{
			var dt = await wayBack.GetNearestDatedTileAsync(tile, desiredDate);
			if (dt is null)
				return EmptyDataset(tile);

			var imageBts = await wayBack.DownloadTileAsync(dt.Layer, dt.Tile);

			return new TileDataset<WebMercator>(tile)
			{
				Dataset = imageBts,
				Message = dt.CaptureDate == desiredDate ? null : $"Substituting imagery from {DateString(dt.CaptureDate)} for tile at {tile.Wgs84Center}",
				TileDate = dt.CaptureDate,
				LayerDate = dt.LayerDate
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
				Dataset = imageBts,
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

	private async Task Run_Keyhole(DirectoryInfo saveFolder, DateOnly desiredDate)
	{
		Console.Write("Grabbing Image Tiles: ");
		ReportProgress(0);

		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		var stats = Region.GetPolygonalRegionStats<KeyholeTile>(ZoomLevel);
		var formatter = new FilenameFormatter(Formatter!, stats);
		await Run_Common(saveFolder, desiredDate, stats.TileCount, formatter, generateWork());

		IEnumerable<Task<TileDataset>> generateWork()
			=> Region
			.GetTiles<KeyholeTile>(ZoomLevel)
			.Select(t => Task.Run(() => DownloadTile(root, t, desiredDate)));
	}

	private static async Task<TileDataset> DownloadTile(DbRoot root, KeyholeTile tile, DateOnly desiredDate)
	{
		if (await root.GetNodeAsync(tile) is not TileNode node)
			return EmptyDataset(tile);

		foreach (var dt in node.GetAllDatedTiles().OrderBy(d => int.Abs(desiredDate.DayNumber - d.Date.DayNumber)))
		{
			try
			{
				if (await root.GetEarthAssetAsync(dt) is byte[] imageBts)
				{
					return new TileDataset<Wgs1984>(tile)
					{
						Dataset = imageBts,
						Message = dt.Date == desiredDate ? null
						: $"Substituting imagery from {DateString(dt.Date)} for tile at {tile.Wgs84Center}",
						TileDate = dt.Date
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

	private async Task Run_Common(DirectoryInfo saveFolder, DateOnly desiredDate, double tileCount, FilenameFormatter formatter, IEnumerable<Task<TileDataset>> generator)
	{
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var processor = new ParallelProcessor<TileDataset>(ConcurrentDownload);

		await foreach (var tds in processor.EnumerateResults(generator))
		{
			if (tds.Message is not null)
				Console.Error.WriteLine($"\r\n{tds.Message}");

			if (tds.Dataset is null)
				Console.Error.WriteLine($"\r\nDataset for tile {tds.Tile} is empty");
			else
			{
				var saveFile = formatter.GetString(tds);
				var savePath = Path.Combine(saveFolder.FullName, saveFile);
				SaveDataset(savePath, tds);
				numTilesDownload++;
			}

			ReportProgress(++numTilesProcessed / tileCount);
		}

		ReplaceProgress("Done!\r\n");
		Console.WriteLine($"{numTilesDownload} out of {tileCount} downloaded");
	}

	private void SaveDataset(string filePath, TileDataset tds)
	{
		if (TargetSpatialReference is null)
		{
			if (tds.Dataset is null)
			{
				Console.Error.WriteLine($"\r\nDataset for tile {tds.Tile} is empty");
				return;
			}
			File.WriteAllBytes(filePath, tds.Dataset);
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
			Gdal.FileFromMemBuffer(srcFile, tds.Dataset);
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

	private abstract class TileDataset
	{
		public DateOnly? LayerDate { get; init; }
		public DateOnly TileDate { get; init; }
		public abstract ITile Tile { get; }
		public byte[]? Dataset { get; init; }
		public required string? Message { get; init; }
		public abstract GeoTransform GetGeoTransform();
		public abstract GDALWarpAppOptions GetWarpOptions(string targetSr);

		static TileDataset()
		{
			GdalLib.Register();
		}
	}

	private class TileDataset<TCoordinate> : TileDataset
		where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		public override ITile<TCoordinate> Tile { get; }
		public TileDataset(ITile<TCoordinate> tile)
		{
			Tile = tile;
		}
		public override GeoTransform GetGeoTransform() => Tile.GetGeoTransform();
		public override GDALWarpAppOptions GetWarpOptions(string targetSr)
			=> EarthImage<TCoordinate>.GetWarpOptions(targetSr);
	}

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
