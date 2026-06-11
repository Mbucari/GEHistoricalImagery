using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Cli.Dump;

internal class FilenameFormatter
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

	public string GetString(ITileDataset dataset)
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
