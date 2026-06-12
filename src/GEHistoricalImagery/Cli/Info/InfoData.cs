using LibMapCommon;

namespace GEHistoricalImagery.Cli.Info;

internal class InfoData(Wgs1984 location, Provider provider)
{
	public Wgs1984 Wgs1984 { get; } = location;
	public Provider Provider { get; } = provider;
	public List<LevelInfo> LevelInfos { get; } = [];
}

internal class LevelInfo(ITile tile)
{
	public int Level { get; } = tile.Level;
	public int Column { get; } = tile.Column;
	public int Row { get; } = tile.Row;
	public string? QuadtreePath { get; init; }
	public List<TileInfo> TileInfos { get; } = [];
}

internal class TileInfo
{
	public string? Provider { get; init; }
	public int? Epoch { get; init; }
	public string? LayerName { get; init; }
	public int? LayerId { get; init; }
	public required DateOnly ImageryDate { get; init; }
	public DateOnly? LayerDate { get; init; }

	public IEnumerable<string> GetProperties()
	{
		if (LayerDate.HasValue) yield return $"layer_date = {LayerDate.ToDateString()}";
		yield return $"imagery_date = {ImageryDate.ToDateString()}";
		if (!string.IsNullOrEmpty(Provider)) yield return $"provider = {Provider}";
	}
}
