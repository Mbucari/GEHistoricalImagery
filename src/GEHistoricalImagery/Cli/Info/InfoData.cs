using LibMapCommon;
using System.Text.Json.Serialization;

namespace GEHistoricalImagery.Cli.Info;

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
		if (LayerDate.HasValue) yield return $"layer_date = {LayerDate.ToDateString()}";
		yield return $"imagery_date = {ImageryDate.ToDateString()}";
		if (!string.IsNullOrEmpty(Provider)) yield return $"provider = {Provider}";
	}
}
