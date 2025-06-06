﻿using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;

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

		int tileCount = Region.GetTileCount<EsriTile>(ZoomLevel);
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var filenameFormatter = new FilenameFormatter<EsriTile>(Formatter!, Region.GetBoundingRectangle(), ZoomLevel);
		var processor = new ParallelProcessor<TileDataset>(ConcurrentDownload);

		await foreach (var tds in processor.EnumerateResults(generateWork()))
		{
			if (tds.Message is not null)
				Console.Error.WriteLine($"\r\n{tds.Message}");

			if (tds.Dataset is null)
				Console.Error.WriteLine($"\r\nDataset for tile {tds.Tile} is empty");
			else
			{
				var saveFile = filenameFormatter.GetString(tds);
				var savePath = Path.Combine(saveFolder.FullName, saveFile);
				File.WriteAllBytes(savePath, tds.Dataset);
				numTilesDownload++;
			}

			ReportProgress(++numTilesProcessed / (double)tileCount);
		}

		ReplaceProgress("Done!\r\n");
		Console.WriteLine($"{numTilesDownload} out of {tileCount} downloaded");

		IEnumerable<Task<TileDataset>> generateWork()
		{
			if (LayerDate)
			{
				var layer = wayBack.Layers.OrderBy(l => int.Abs(l.Date.DayNumber - desiredDate.DayNumber)).First();

				Console.Write($"Grabbing Image Tiles From {layer.Title}: ");
				ReportProgress(0);
				return Region.GetTiles<EsriTile>(ZoomLevel).Select(t => Task.Run(() => DownloadEsriTile(wayBack, t, layer, filenameFormatter.HasTileDate)));
			}
			else
			{
				Console.Write($"Grabbing Image Tiles Nearest To {DateString(desiredDate)}: ");
				ReportProgress(0);
				return Region.GetTiles<EsriTile>(ZoomLevel).Select(t => Task.Run(() => DownloadEsriTile(wayBack, t, desiredDate)));
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

			return new()
			{
				Tile = tile,
				Dataset = imageBts,
				Message = dt.CaptureDate == desiredDate ? null : $"Substituting imagery from {DateString(dt.CaptureDate)} for tile at {tile.Center}",
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

			return new()
			{
				Tile = tile,
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
		int tileCount = Region.GetTileCount<KeyholeTile>(ZoomLevel);
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var filenameFormatter = new FilenameFormatter<KeyholeTile>(Formatter!, Region.GetBoundingRectangle(), ZoomLevel);
		var processor = new ParallelProcessor<TileDataset>(ConcurrentDownload);

		await foreach (var tds in processor.EnumerateResults(generateWork()))
		{
			if (tds.Message is not null)
				Console.Error.WriteLine($"\r\n{tds.Message}");

			if (tds.Dataset is null)
				Console.Error.WriteLine($"\r\nDataset for tile {tds.Tile} is empty");
			else
			{
				var saveFile = filenameFormatter.GetString(tds);
				var savePath = Path.Combine(saveFolder.FullName, saveFile);
				File.WriteAllBytes(savePath, tds.Dataset);
				numTilesDownload++;
			}

			ReportProgress(++numTilesProcessed / (double)tileCount);
		}

		ReplaceProgress("Done!\r\n");
		Console.WriteLine($"{numTilesDownload} out of {tileCount} downloaded");

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
					return new()
					{
						Tile = tile,
						Dataset = imageBts,
						Message = dt.Date == desiredDate ? null
						: $"Substituting imagery from {DateString(dt.Date)} for tile at {tile.Center}",
						TileDate = dt.Date
					};
				}
			}
			catch (HttpRequestException)
			{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }
		}

		return EmptyDataset(tile);

	}

	private static TileDataset EmptyDataset(ITile tile) => new()
	{
		Tile = tile,
		Message = $"No imagery available for tile at {tile.Center}"
	};

	private class TileDataset
	{
		public DateOnly? LayerDate { get; init; }
		public DateOnly TileDate { get; init; }
		public required ITile Tile { get; init; }
		public byte[]? Dataset { get; init; }
		public required string? Message { get; init; }
	}

	#endregion

	#region Common

	private class FilenameFormatter<T> where T : ITile<T>
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
		public FilenameFormatter(string formatter, Rectangle aoi, int zoom)
		{
			var lowerLeft = aoi.LowerLeft.GetTile<T>(zoom);

			LowerLeftRow = lowerLeft.Row;
			LowerLeftColumn = lowerLeft.Column;
			NumTilesAtLevel = 1 << zoom;
			GetLocalFormatters(aoi, zoom, out LocalColumnFormat, out LocalRowFormat);
			GetGlobalFormatters(aoi, zoom, out GlobalColumnFormat, out GlobalRowFormat);

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

		private static void GetGlobalFormatters(Rectangle aoi, int zoom, out string colFormatter, out string rowFormatter)
		{
			var lowerLeft = aoi.LowerLeft.GetTile<T>(zoom);
			var upperRight = aoi.UpperRight.GetTile<T>(zoom);

			var maxRow = Math.Max(lowerLeft.Row, upperRight.Row);
			var maxCol = Math.Max(lowerLeft.Column, upperRight.Column);

			rowFormatter = DigitFormatter(maxRow);
			colFormatter = DigitFormatter(maxCol);
		}

		private static void GetLocalFormatters(Rectangle aoi, int zoom, out string colFormatter, out string rowFormatter)
		{
			aoi.GetNumRowsAndColumns<T>(zoom, out int numRows, out int numColumns);

			rowFormatter = DigitFormatter(numRows);
			colFormatter = DigitFormatter(numColumns);
		}

		private static string DigitFormatter(int maxNumber)
		{
			var maxNumDigits = (int)Math.Ceiling(Math.Log10(maxNumber));
			return "D" + maxNumDigits;
		}
	}
	#endregion
}
