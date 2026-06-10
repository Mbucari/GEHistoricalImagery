using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace GEHistoricalImagery.Cli;

[Verb("info", HelpText = "Get imagery info at a specified location")]
internal partial class Info : OptionsBase, IQuietCommand
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
	protected IEnumerable<string> GetInfoErrors()
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

	private bool AnyInfoErrors()
	{
		List<string> errors = GetInfoErrors().ToList();
		errors.ForEach(Console.Error.WriteLine);
		return errors.Count > 0;
	}
	public override async Task RunAsync()
	{
		if (AnyInfoErrors())
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
			using Stream output = SavePath == "-" ? Console.OpenStandardOutput() : File.OpenWrite(PathHelper.ReplaceUnixHomeDir(SavePath));
			await JsonSerializer.SerializeAsync(output, infoData, SourceGenerationContext.Default.InfoData);
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

	private async IAsyncEnumerable<TileInfo> EnumerateTileInfos_Esri(EsriTile tile)
	{
		WayBack wayBack = await WayBack.CreateAsync(CacheDir);

		await foreach (DatedEsriTile dated in wayBack.GetDatesAsync(tile))
		{
			yield return new TileInfo
			{
				LayerDate = dated.LayerDate,
				ImageryDate = dated.CaptureDate,
				LayerName = dated.Layer.Title,
				LayerId = dated.Layer.ID
			};
		}
	}

	private async IAsyncEnumerable<TileInfo> EnumerateTileInfos_Keyhole(KeyholeTile tile)
	{
		DbRoot root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		if (await root.GetNodeAsync(tile) is not { } node)
			yield break;

		foreach (DatedTile? dated in node.GetAllDatedTiles().Where(d => d.Date.Year != 1).OrderByDescending(d => d.Date))
		{
			yield return new TileInfo
			{
				ImageryDate = dated.Date,
				Epoch = dated.Epoch,
				Provider = root.GetProviderCopyright(dated)
			};
		}
	}

	internal class TileInfo
	{
		[JsonPropertyName("provider")]
		public string? Provider { get; init; }
		[JsonPropertyName("epoch")]
		public int? Epoch { get; init; }
		[JsonPropertyName("layer_name")]
		public string? LayerName { get; init; }
		[JsonPropertyName("layer_id")]
		public int? LayerId { get; init; }
		[JsonPropertyName("imagery_date")]
		public required DateOnly ImageryDate { get; init; }
		[JsonPropertyName("layer_date")]
		public DateOnly? LayerDate { get; init; }

		public IEnumerable<string> GetProperties()
		{
			if (LayerDate.HasValue) yield return $"layer_date = {DateString(LayerDate)}";
			yield return $"imagery_date = {DateString(ImageryDate)}";
			if (!string.IsNullOrEmpty(Provider)) yield return $"provider = {Provider}";
		}
	}

	internal class LevelInfo(ITile tile)
	{
		[JsonPropertyName("zoom_level")]
		public int Level { get; } = tile.Level;
		[JsonPropertyName("column")]
		public int Column { get; } = tile.Column;
		[JsonPropertyName("row")]
		public int Row { get; } = tile.Row;
		[JsonPropertyName("quadtree_path")]
		public string? QuadtreePath { get; init; }
		[JsonPropertyName("tile_infos")]
		public List<TileInfo> TileInfos { get; } = [];
	}

	internal class Wgs1984Info(Wgs1984 location)
	{
		[JsonPropertyName("latitude")]
		public double Latitude { get; } = Math.Round(location.Latitude, 6);
		[JsonPropertyName("longitude")]
		public double Longitude { get; } = Math.Round(location.Longitude, 6);
	}

	internal class WebMercatorInfo(WebMercator location)
	{
		[JsonPropertyName("x")]
		public double X { get; } = Math.Round(location.X, 2);
		[JsonPropertyName("y")]
		public double Y { get; } = Math.Round(location.Y, 2);
	}

	internal class InfoData(Wgs1984 location, Provider provider)
	{
		[JsonPropertyName("wgs_1984")]
		public Wgs1984Info Wgs1984 { get; } = new Wgs1984Info(location);
		[JsonPropertyName("web_mercator")]
		public WebMercatorInfo WebMercator { get; } = new WebMercatorInfo(location.ToWebMercator());
		[JsonPropertyName("provider")]
		public Provider Provider { get; } = provider;
		[JsonPropertyName("level_infos")]
		public List<LevelInfo> LevelInfos { get; } = [];
	}

	[JsonSourceGenerationOptions(
		WriteIndented = true,
		UseStringEnumConverter = true,
		AllowDuplicateProperties = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonSerializable(typeof(InfoData))]
	internal partial class SourceGenerationContext : JsonSerializerContext
	{
		static SourceGenerationContext()
		{
			Default = new SourceGenerationContext(new JsonSerializerOptions(Default.Options)
			{
				Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
			});
		}
	}
}
