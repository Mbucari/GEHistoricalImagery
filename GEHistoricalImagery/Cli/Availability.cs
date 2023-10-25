using CommandLine;
using Google.Protobuf.WellKnownTypes;
using System.Text;

namespace GoogleEarthImageDownload.Cli;

[Verb("availability", HelpText = "Get imagery date availability in a specified region")]
internal class Availability : OptionsBase
{

	[Option("lower-left", Required = true, HelpText = "Geographic location", MetaValue = "LAT,LONG")]
	public Coordinate? LowerLeft { get; set; }

	[Option("upper-right", Required = true, HelpText = "Geographic location", MetaValue = "LAT,LONG")]
	public Coordinate? UpperRight { get; set; }

	[Option('z', "zoom", HelpText = "Zoom level (Optional, [0-24])", MetaValue = "N", Required = true)]
	public int ZoomLevel { get; set; }

	[Option('p', "parallel", HelpText = "Number of concurrent downloads", MetaValue = "N", Default = 20)]
	public int ConcurrentDownload { get; set; }

	static readonly string INDICES = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

	public override async Task Run()
	{
		bool hasError = false;
		if (LowerLeft is null || UpperRight is null)
		{
			Console.Error.WriteLine("Invalid coordinate(s).\r\n Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305");
			hasError = true;
		}

		if (ZoomLevel > 24)
		{
			Console.Error.WriteLine($"Zoom level: {ZoomLevel} is too large. Max zoom is 24");
			hasError = true;
		}
		else if (ZoomLevel < 0)
		{
			Console.Error.WriteLine($"Zoom level: {ZoomLevel} is too small. Min zoom is 0");
			hasError = true;
		}

		if (hasError) return;

		var aoi = new Rectangle(LowerLeft!.Value, UpperRight!.Value);
		var root = await DbRoot.CreateAsync();
		Console.Write("Loading Quad Tree Packets: ");

		var all = await GetAllDatesAsync(root, aoi, ZoomLevel);
		ReplaceProgress("Done!\r\n");

		if (all.Length == 0)
		{
			Console.Error.WriteLine($"No dataed imagery available at zoom level {ZoomLevel}");
			return;
		}

		int counter = 0;
		var dateDict = all.ToDictionary(d => INDICES[counter++]);

		writeDateOptions();

		Console.OutputEncoding = Encoding.Unicode;

		char[][] array = Array.Empty<char[]>();

		while (true)
		{
			var key = Console.ReadKey(true);

			if (key.Key == ConsoleKey.Escape)
				return;
			else if (dateDict.ContainsKey(key.KeyChar))
			{
				var date = dateDict[key.KeyChar];

				var availabilityStr = $"Tile availability on {date:yyyy/MM/dd}";
				Console.WriteLine("\r\n" + availabilityStr);
				Console.WriteLine(new string('=', availabilityStr.Length) + "\r\n");

				array = await DrawAvailability(root, aoi, date);

				foreach (var ca in array)
					Console.WriteLine(new string(ca));

				Console.WriteLine();
				writeDateOptions();
			}
		}

		void writeDateOptions()
		{
			const string spacer = "  ";
			foreach (var entry in dateDict.Select((kvp, i) => $"[{kvp.Key}]  {kvp.Value:yyyy/MM/dd}"))
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
	}

	private async Task<char[][]> DrawAvailability(DbRoot root, Rectangle aoi, DateOnly date)
	{
		var ll = aoi.LowerLeft.GetTile(ZoomLevel);
		var ur = aoi.UpperRight.GetTile(ZoomLevel);

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

				var node = await root.GetNodeAsync(tile.QtPath);

				var cIndex = c - ll.Column;

				if (node.HasDate(date))
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

		SortedSet<DateOnly> dates = new();
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
			SortedSet<DateOnly> dates = new();

			var node = await root.GetNodeAsync(tile.QtPath);
			foreach (var date in node.GetAllDates())
			{
				if (date.Year == 1) continue;
				dates.Add(date);
			}
			return dates;
		}
	}
}
