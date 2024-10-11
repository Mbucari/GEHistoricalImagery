using CommandLine;
using LibGoogleEarth;
using System.Text;

namespace GEHistoricalImagery.Cli;

[Verb("availability", HelpText = "Get imagery date availability in a specified region")]
internal class Availability : AoiVerb
{
	[Option('p', "parallel", HelpText = "Number of concurrent downloads", MetaValue = "N", Default = 20)]
	public int ConcurrentDownload { get; set; }

	private static readonly string INDICES = "0123456789abcdefghijklmnopqrstuvwxyz";

	public override async Task RunAsync()
	{
		bool hasError = false;

		foreach (var errorMessage in GetAoiErrors())
		{
			Console.Error.WriteLine(errorMessage);
			hasError = true;
		}

		if (hasError) return;

		var root = await DbRoot.CreateAsync(Database.TimeMachine);
		Console.Write("Loading Quad Tree Packets: ");

		var all = await GetAllDatesAsync(root, Aoi, ZoomLevel);
		ReplaceProgress("Done!\r\n");

		if (all.Length == 0)
		{
			Console.Error.WriteLine($"No dated imagery available at zoom level {ZoomLevel}");
			return;
		}

		Console.OutputEncoding = Encoding.Unicode;

		if (all.Length <= INDICES.Length)
			await WaitForSingleCharSelection(root, all);
		else
			await WaitForMultiCharSelection(root, all);
	}

	private async Task WaitForSingleCharSelection(DbRoot root, DateOnly[] dates)
	{
		const string finalOption = "[Esc]  Exit";
		char[][] array = [];

		var dateDict = dates.Select((d, i) => new KeyValuePair<char, DateOnly>(INDICES[i], d)).ToDictionary();

		WriteDateOptions(dateDict, finalOption);
		while (Console.ReadKey(true) is ConsoleKeyInfo key && key.Key != ConsoleKey.Escape)
		{
			if (dateDict.TryGetValue(key.KeyChar, out var date))
			{
				var availabilityStr = $"Tile availability on {DateString(date)}";
				Console.WriteLine("\r\n" + availabilityStr);
				Console.WriteLine(new string('=', availabilityStr.Length) + "\r\n");

				array = await DrawAvailability(root, date);

				foreach (var ca in array)
					Console.WriteLine(new string(ca));

				Console.WriteLine();
				WriteDateOptions(dateDict, finalOption);
			}
		}
	}

	private async Task WaitForMultiCharSelection(DbRoot root, DateOnly[] dates)
	{
		const string finalOption = "[E]  Exit";
		char[][] array = [];

		int numPlaces = (int)Math.Ceiling(Math.Log10(dates.Length));
		var decFormat = "D" + numPlaces;
		var printableDict = dates.Select((d, i) => new KeyValuePair<string, DateOnly>(i.ToString(decFormat), d));

		WriteDateOptions(printableDict, finalOption);
		while (Console.ReadLine() is string key && !string.Equals(key, "E", StringComparison.OrdinalIgnoreCase))
		{
			if (key != null && int.TryParse(key, out var selectedInt) && selectedInt >= 0 && selectedInt < dates.Length)
			{
				var date = dates[selectedInt];
				var availabilityStr = $"Tile availability on {DateString(date)}";

				Console.WriteLine("\r\n" + availabilityStr);
				Console.WriteLine(new string('=', availabilityStr.Length) + "\r\n");

				array = await DrawAvailability(root, date);

				foreach (var ca in array)
					Console.WriteLine(new string(ca));

				Console.WriteLine();
				WriteDateOptions(printableDict, finalOption);
			}
		}
	}

	private static void WriteDateOptions<T>(IEnumerable<KeyValuePair<T, DateOnly>> dateDict, string finalOption) where T : notnull
	{
		const string spacer = "  ";

		foreach (var entry in dateDict.Select((kvp, i) => $"[{kvp.Key}]  {DateString(kvp.Value)}").Append(finalOption))
		{
			Console.Write(entry);

			var remainingSpace = Console.WindowWidth - Console.CursorLeft;

			if (remainingSpace < entry.Length + spacer.Length)
				Console.WriteLine();
			else
				Console.Write(spacer);
		}
		if (Console.CursorLeft > 0)
			Console.WriteLine();
	}

	private async Task<char[][]> DrawAvailability(DbRoot root, DateOnly date)
	{
		var ll = Aoi.LowerLeft.GetTile(ZoomLevel);
		var ur = Aoi.UpperRight.GetTile(ZoomLevel);

		var width = ur.Column - ll.Column + 1;
		var height = ur.Row - ll.Row + 1;
		height = height % 2 == 0 ? height / 2 : height / 2 + 1;

		//Each character row represents two node rows
		char[][] availability = new char[height][];

		for (int r = ur.Row, rIndex = 0; r >= ll.Row; r--, rIndex++)
		{
			availability[rIndex / 2] ??= new char[width];

			for (int c = ll.Column; c <= ur.Column; c++)
			{
				var tile = new Tile(r, c, ZoomLevel);

				var node = await root.GetNodeAsync(tile);

				var cIndex = c - ll.Column;

				if (node?.HasDate(date) is true)
				{
					if (rIndex % 2 == 0)
						availability[rIndex / 2][cIndex] = '▀';
					else
						availability[rIndex / 2][cIndex]
							= availability[rIndex / 2][cIndex] == '▀'
							? '█'
							: '▄';
				}
				else
				{
					availability[rIndex / 2][cIndex] = ':';
				}
			}
		}

		return availability;
	}

	private async Task<DateOnly[]> GetAllDatesAsync(DbRoot root, Rectangle aoi, int zoomLevel)
	{
		int count = 0;
		int numTiles = aoi.GetTileCount(zoomLevel);
		ReportProgress(0);

		SortedSet<DateOnly> dates = [];
		ParallelProcessor<SortedSet<DateOnly>> processor = new(ConcurrentDownload);

		await foreach (var dSet in processor.EnumerateResults(aoi.GetTiles(zoomLevel).Select(getDatedTiles)))
		{
			foreach (var d in dSet)
				dates.Add(d);

			ReportProgress(++count / (double)numTiles);
		}

		return dates.Reverse().ToArray();

		async Task<SortedSet<DateOnly>> getDatedTiles(Tile tile)
		{
			SortedSet<DateOnly> dates = [];

			if (await root.GetNodeAsync(tile) is not TileNode node)
				return dates;

			foreach (var date in node.GetAllDatedTiles().Select(dt => dt.Date))
			{
				if (date.Year == 1) continue;
				dates.Add(date);
			}
			return dates;
		}
	}
}
