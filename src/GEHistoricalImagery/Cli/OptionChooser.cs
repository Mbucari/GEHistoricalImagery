namespace GEHistoricalImagery.Cli
{
	public interface IConsoleOption
	{
		public string DisplayValue { get; }

		/// <returns>True if option choice is final, otherwise false to continue.</returns>
		bool DrawOption();
	}

	internal class OptionChooser<T> where T : class, IConsoleOption
	{
		private static readonly string INDICES = "0123456789abcdefghijklmnopqrstuvwxyz";
		public OptionChooser() { }

		protected static string DateString(DateOnly date) => date.ToString("yyyy/MM/dd");

		public T? WaitForOptions(T[] options)		
			=> options.Length <= INDICES.Length
			? WaitForSingleCharSelection(options)
			: WaitForMultiCharSelection(options);


		private T? WaitForSingleCharSelection(T[] options)
		{
			const string finalOption = "[Esc]  Exit";
			var dateDict = options.Select((d, i) => new KeyValuePair<char, T>(INDICES[i], d)).ToDictionary();

			WriteDateOptions(dateDict, finalOption);

			while (Console.ReadKey(true) is ConsoleKeyInfo key && key.Key != ConsoleKey.Escape)
			{
				if (dateDict.TryGetValue(key.KeyChar, out var option))
				{
					if (option.DrawOption())
						return option;
					Console.WriteLine();
					WriteDateOptions(dateDict, finalOption);
				}
			}
			return null;
		}

		private T? WaitForMultiCharSelection(T[] options)
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
					if (option.DrawOption())
						return option;
					Console.WriteLine();
					WriteDateOptions(dateDict, finalOption);
				}
			}
			return null;
		}

		private static void WriteDateOptions<S>(IEnumerable<KeyValuePair<S, T>> dateDict, string finalOption) where S : notnull
		{
			const string spacer = "  ";

			foreach (var entry in dateDict.Select((kvp, i) => $"[{kvp.Key}]  {kvp.Value.DisplayValue}").Append(finalOption))
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
