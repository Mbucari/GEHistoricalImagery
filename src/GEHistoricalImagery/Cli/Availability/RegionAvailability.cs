namespace GEHistoricalImagery.Cli.Availability;

internal class RegionAvailability : IEquatable<RegionAvailability>, IConsoleOption
{
	public DateOnly Date { get; }
	public string DisplayValue => Date.ToDateString();
	private bool?[,] Availability { get; }

	public int Height => Availability.GetLength(0);
	public int Width => Availability.GetLength(1);
	public bool? this[int rIndex, int cIndex]
	{
		get => Availability[rIndex, cIndex];
		set => Availability[rIndex, cIndex] = value;
	}

	public RegionAvailability(DateOnly date, int height, int width)
	{
		Date = date;
		Availability = new bool?[height, width];
	}

	public bool HasAnyTiles() => Availability.OfType<bool>().Any(b => b);
	public bool HasAllTiles() => Availability.OfType<bool>().All(b => b);

	public static bool operator ==(RegionAvailability a, RegionAvailability b) => a.Equals(b);
	public static bool operator !=(RegionAvailability a, RegionAvailability b) => !a.Equals(b);
	public override int GetHashCode() => HashCode.Combine(Date, Availability);
	public override bool Equals(object? obj) => Equals(obj as RegionAvailability);
	public bool Equals(RegionAvailability? other)
	{
		if (other is null || other.Date != Date || other.Height != Height || other.Width != Width)
			return false;

		for (int i = 0; i < Height; i++)
		{
			for (int j = 0; j < Width; j++)
			{
				if (other.Availability[i, j] != Availability[i, j])
					return false;
			}
		}
		return true;
	}

	public bool DrawOption()
	{
		var availabilityStr = $"Tile availability on {Date.ToDateString()}";
		Console.Error.WriteLine(Environment.NewLine + availabilityStr);
		Console.Error.WriteLine(new string('=', availabilityStr.Length) + Environment.NewLine);
		DrawMap();
		return false;
	}

	public void DrawMap()
	{
		/*
		 _________________________
		 | Top       | TTTFFFNNN |
		 ------------|------------
		 | Bottom    | TFNTFNTFN |
		 ------------|------------
		 | Character | █▀▀▄:˙▄.  |
		 -------------------------
		 */

		for (int y = 0; y < Height; y += 2)
		{
			var has2Rows = y + 1 < Height;
			char[] row = new char[Width];
			for (int x = 0; x < Width; x++)
			{
				var top = Availability[y, x];
				if (has2Rows)
				{
					var bottom = Availability[y + 1, x];
					row[x] = top is true & bottom is true ? '█' :
						top is true ? '▀' :
						bottom is true ? '▄' :
						top is false & bottom is false ? ':' :
						top is false ? '˙' :
						bottom is false ? '.' : ' ';
				}
				else
				{
					row[x] = top is true ? '▀' :
						top is false ? '˙' : ' ';
				}
			}

			Console.Error.WriteLine(new string(row));
		}
	}
}