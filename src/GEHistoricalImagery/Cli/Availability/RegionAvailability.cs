using System.Runtime.InteropServices;

namespace GEHistoricalImagery.Cli.Availability;

public enum Availability : byte
{
	None,
	Available,
	Unavailable,
}

internal class RegionAvailability : IEquatable<RegionAvailability>, IConsoleOption
{
	public DateOnly Date { get; }
	public string DisplayValue => Date.ToDateString();
	private Availability[,] Availabilities { get; }

	public int Height { get; }
	public int Width { get; }
	public Availability this[int rIndex, int cIndex]
	{
		get => Availabilities[rIndex, cIndex];
		set => Availabilities[rIndex, cIndex] = value;
	}

	public RegionAvailability(DateOnly date, int height, int width)
	{
		Date = date;
		Height = height;
		Width = width;
		Availabilities = new Availability[height, width];
	}

	public static bool operator ==(RegionAvailability a, RegionAvailability b) => a.Equals(b);
	public static bool operator !=(RegionAvailability a, RegionAvailability b) => !a.Equals(b);
	public override int GetHashCode()
	{
		HashCode hashCode = new();
		hashCode.Add(Date.GetHashCode());
		Span<byte> flatSpan = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(Availabilities), Height * Width);
		hashCode.AddBytes(flatSpan);
		return hashCode.ToHashCode();
	}
	public override bool Equals(object? obj) => Equals(obj as RegionAvailability);
	public bool Equals(RegionAvailability? other)
	{
		if (other is null || other.Date != Date || other.Height != Height || other.Width != Width)
			return false;

		for (int i = 0; i < Height; i++)
		{
			for (int j = 0; j < Width; j++)
			{
				if (other.Availabilities[i, j] != Availabilities[i, j])
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
				var top = Availabilities[y, x];
				if (has2Rows)
				{
					var bottom = Availabilities[y + 1, x];
					row[x] = top == Availability.Available & bottom == Availability.Available ? '█' :
						top == Availability.Available ? '▀' :
						bottom == Availability.Available ? '▄' :
						top == Availability.Unavailable & bottom == Availability.Unavailable ? ':' :
						top == Availability.Unavailable ? '˙' :
						bottom == Availability.Unavailable ? '.' : ' ';
				}
				else
				{
					row[x] = top == Availability.Available ? '▀' :
						top == Availability.Unavailable ? '˙' : ' ';
				}
			}

			Console.Error.WriteLine(row);
		}
	}
}