using CommandLine;
using LibMapCommon;
using OSGeo.GDAL;

namespace GEHistoricalImagery.Cli.Dump;

[Verb("dump", HelpText = "Dump historical image tiles into a folder")]
internal partial class DumpCommand : FileDownloadVerb
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

	[Option('o', "output", HelpText = "Output image tile save directory", MetaValue = "<Directory>", Required = true)]
	public override string? SavePath { get; set; }

	[Option('f', "format", HelpText = formatHelpText, Default = "z={Z}-Col={c}-Row={r}.jpg", MetaValue = "<FilenameFormat>")]
	public string? Formatter { get; set; }

	[Option('w', "world", HelpText = "Write a world file for each tile")]
	public bool WriteWorldFile { get; set; }
	[Option("dump-db", HelpText = "Path to SQLite database to save dump operations' tile data", MetaValue = "<out.sqlite3>")]
	public string? DumpDatabaseFilename { get; set; }

	private DumpDatabase? DumpDatabase { get; set; }

	public override async Task RunAsync()
	{
		if (AnyValidationErrors())
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
			saveFolder = new DirectoryInfo(SavePath!.ReplaceUnixHomeDir());
			saveFolder.Create();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error saving file {SavePath}");
			Console.Error.WriteLine($"\t{ex.Message}");
			return;
		}

		if (!string.IsNullOrWhiteSpace(DumpDatabaseFilename))
		{
			DumpDatabase = new DumpDatabase(DumpDatabaseFilename, Region, Provider, ZoomLevel, saveFolder.FullName);
		}

		var desiredDates = Dates!;
		var task = Provider is Provider.Wayback ? Run_Esri(saveFolder, desiredDates)
		: Run_Keyhole(saveFolder, desiredDates);

		using (DumpDatabase)
		{
			await task;
		}
	}

	private async Task Run_Common(DirectoryInfo saveFolder, double tileCount, FilenameFormatter formatter, IEnumerable<Task<ITileDataset>> generator)
	{
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var processor = new ParallelProcessor<ITileDataset>(ConcurrentDownload);

		await foreach (var tds in processor.EnumerateResults(generator))
		{
			if (tds.Message is not null)
				Console.Error.WriteLine(tds.Message);

			if (tds.TileBytes is not null)
			{
				var saveFile = formatter.GetString(tds);
				var savePath = Path.Combine(saveFolder.FullName, saveFile);
				SaveDataset(savePath, tds);
				DumpDatabase?.AddDumpedTile(tds, savePath);
				numTilesDownload++;
			}

			ProgressWriter.Instance.ReportProgress(++numTilesProcessed / tileCount);
		}

		ProgressWriter.Instance.EndProgress();
		Console.Error.WriteLine($"{numTilesDownload} out of {tileCount} downloaded");
	}

	private void SaveDataset(string filePath, ITileDataset tds)
	{
		if (TargetSpatialReference is null)
		{
			if (tds.TileBytes is null)
			{
				Console.Error.WriteLine($"Dataset for tile {tds.Tile} is empty");
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

			using var options = tds.GetWarpOptions(RasterOptions.Jpeg, TargetSpatialReference);
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

	private static ITileDataset EmptyDataset<TCoordinate>(IGeoTile<TCoordinate> tile, string? messageOverride = null)
		where TCoordinate : IGeoCoordinate<TCoordinate> => new TileDataset<TCoordinate>(tile)
	{
		Message = messageOverride ?? $"No imagery available for tile at {tile.Wgs84Center}"
	};
}
