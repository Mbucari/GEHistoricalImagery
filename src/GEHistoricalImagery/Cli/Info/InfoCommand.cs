using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using System.Text.Json;

namespace GEHistoricalImagery.Cli.Info;

[Verb("info", HelpText = "Get imagery info at a specified location")]
internal partial class InfoCommand : OptionsBase, IQuietCommand
{
	[Option('l', "location", Required = true, HelpText = "Geographic location", MetaValue = "<LAT>,<LONG>")]
	public Wgs1984? Coordinate { get; set; }

	[Option('z', "zoom", Default = null, HelpText = "Zoom level (Optional, [1-23])", MetaValue = "<N>", Required = false)]
	public int? ZoomLevel { get; set; }

	[Option("min-zoom", Default = null, HelpText = "Minimum zoom level (Optional, [1-23])", MetaValue = "<N>", Required = false)]
	public int? MinZoomLevel { get; set; }

	[Option("max-zoom", Default = null, HelpText = "Maximum zoom level (Optional, [1-23])", MetaValue = "<N>", Required = false)]
	public int? MaxZoomLevel { get; set; }

	[Option('o', "output", HelpText = "Output image info JSON save location (dash (-) for console output)", MetaValue = "<info.json>", Required = false)]
	public string? SavePath { get; set; }
	public bool Quiet { get; set; }
	protected override IEnumerable<string> GetValidationErrors()
	{
		if (Coordinate is null)
		{
			yield return $"Invalid location coordinate.{Environment.NewLine} Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305";
		}
		if (ZoomLevel.HasValue)
		{
			if (MinZoomLevel.HasValue || MaxZoomLevel.HasValue)
				yield return "Cannot specify single zoom level with min-zoom or max-zoom";

			if (ZoomLevel < 1)
				yield return $"zoom level ({ZoomLevel}) is too small. Minimum zoom is 1";
			if (ZoomLevel > 23)
				yield return $"zoom level ({ZoomLevel}) is too large. Maximum zoom is 23";
			MinZoomLevel = ZoomLevel.Value;
			MaxZoomLevel = ZoomLevel.Value;
		}
		else
		{
			MinZoomLevel ??= 1;
			MaxZoomLevel ??= 23;
			if (MinZoomLevel < 1)
				yield return $"min-zoom level ({MinZoomLevel}) is too small. Minimum zoom is 1";
			if (MinZoomLevel > 23)
				yield return $"min-zoom level ({MinZoomLevel}) is too large. Maximum zoom is 23";
			if (MaxZoomLevel < 1)
				yield return $"max-zoom level ({MaxZoomLevel}) is too small. Minimum zoom is 1";
			if (MaxZoomLevel > 23)
				yield return $"max-zoom level ({MaxZoomLevel}) is too large. Maximum zoom is 23";
			if (MaxZoomLevel < MinZoomLevel)
				yield return $"max-zoom level ({MaxZoomLevel}) must be >= min-zoom level ({MinZoomLevel})";
		}

		if (Provider is not (Provider.TM or Provider.Wayback))
			yield return "Invalid provider";
	}

	public override async Task RunAsync()
	{
		if (AnyValidationErrors())
			return;

		Console.Error.WriteLine($"Dated Imagery at {Coordinate}");

		InfoData infoData = new(Coordinate!.Value, Provider);

		for (int level = MinZoomLevel!.Value; level <= MaxZoomLevel!.Value; level++)
		{
			LevelInfo levelInfo = await GetLevelInfo(Coordinate.Value, level);
			if (levelInfo.TileInfos.Count > 0)
			{
				infoData.LevelInfos.Add(levelInfo);
			}
			else
			{
				WriteWithIndent(4, "NO AVAILABLE IMAGERY");
				break;
			}
		}

		if (SavePath != null)
		{
			using Stream output = SavePath == "-" ? Console.OpenStandardOutput() : File.OpenWrite(SavePath.ReplaceUnixHomeDir());
			await JsonSerializer.SerializeAsync(output, infoData, InfoDataSerilizer.Default.InfoData);
		}
	}

	private async Task<LevelInfo> GetLevelInfo(Wgs1984 coordinate, int level)
	{
		LevelInfo levelInfo;
		IAsyncEnumerable<TileInfo> tileInfos;

		if (Provider is Provider.TM)
		{
			KeyholeTile tile = KeyholeTile.GetTile(coordinate, level);
			levelInfo = new LevelInfo(tile) { QuadtreePath = tile.Path };
			tileInfos = EnumerateTileInfos_Keyhole(tile);
		}
		else
		{
			EsriTile tile = EsriTile.GetTile(coordinate.ToWebMercator(), level);
			levelInfo = new LevelInfo(tile);
			tileInfos = EnumerateTileInfos_Esri(tile);
		}

		WriteWithIndent(2, $"Level = {levelInfo.Level}, Column = {levelInfo.Column}, Row = {levelInfo.Row}");
		await foreach (TileInfo tileInfo in tileInfos)
		{
			levelInfo.TileInfos.Add(tileInfo);
			WriteWithIndent(4, string.Join(", ", tileInfo.GetProperties()));
		}
		return levelInfo;
	}

	private static void WriteWithIndent(int spaces, string message)
		=> Console.Error.WriteLine(new string(' ', spaces) + message);
}
