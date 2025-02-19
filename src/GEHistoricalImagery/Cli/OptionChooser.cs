namespace GEHistoricalImagery.Cli
{
	public interface IDatedOption
	{
		public DateOnly Date { get; }
		public void DrawOption();
	}

	internal class OptionChooser<T> where T : IDatedOption
	{
		private static readonly string INDICES = "0123456789abcdefghijklmnopqrstuvwxyz";
		public OptionChooser() { }

		protected static string DateString(DateOnly date) => date.ToString("yyyy/MM/dd");

		public void WaitForOptions(T[] options)
		{
			if (options.Length <= INDICES.Length)
				WaitForSingleCharSelection(options);
			else
				WaitForMultiCharSelection(options);
		}

		private void WaitForSingleCharSelection(T[] options)
		{
			const string finalOption = "[Esc]  Exit";
			var dateDict = options.Select((d, i) => new KeyValuePair<char, T>(INDICES[i], d)).ToDictionary();

			WriteDateOptions(dateDict, finalOption);

			while (Console.ReadKey(true) is ConsoleKeyInfo key && key.Key != ConsoleKey.Escape)
			{
				if (dateDict.TryGetValue(key.KeyChar, out var option))
				{
					option.DrawOption();
					Console.WriteLine();
					WriteDateOptions(dateDict, finalOption);
				}
			}
		}

		private void WaitForMultiCharSelection(T[] options)
		{
			const string finalOption = "[E]  Exit";

			int numPlaces = (int)Math.Ceiling(Math.Log10(options.Length));
			var decFormat = "D" + numPlaces;
			var dateDict = options.Select((d, i) => new KeyValuePair<string, T>(i.ToString(decFormat), d)).ToDictionary();

			WriteDateOptions(dateDict, finalOption);
			while (Console.ReadLine() is string key && !string.Equals(key, "E", StringComparison.OrdinalIgnoreCase))
			{
				if (dateDict.TryGetValue(key, out var option))
				{
					option.DrawOption();
					Console.WriteLine();
					WriteDateOptions(dateDict, finalOption);
				}
			}
		}

		private static void WriteDateOptions<S>(IEnumerable<KeyValuePair<S, T>> dateDict, string finalOption) where S : notnull
		{
			const string spacer = "  ";

			foreach (var entry in dateDict.Select((kvp, i) => $"[{kvp.Key}]  {DateString(kvp.Value.Date)}").Append(finalOption))
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
}
