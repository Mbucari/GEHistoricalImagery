namespace GEHistoricalImagery.Cli
{
	public interface IConsoleOption
	{
		string DisplayValue { get; }

		/// <returns>True if option choice is final, otherwise false to continue.</returns>
		bool DrawOption();
	}

	internal class OptionChooser
	{
		private static readonly string INDICES = "0123456789abcdefghijklmnopqrstuvwxyz";
		public OptionChooser() { }

		public static T? WaitForOptions<T>(T[] options) where T : class, IConsoleOption
			=> options.Length <= INDICES.Length
			? WaitForSingleCharSelection(options)
			: WaitForMultiCharSelection(options);

		private static T? WaitForSingleCharSelection<T>(T[] options) where T : class, IConsoleOption
		{
			const string finalOption = "[Esc]  Exit";
			var dateDict = options.Select((d, i) => new KeyValuePair<char, T>(INDICES[i], d)).ToDictionary();

			WriteOptions(dateDict, finalOption);

			while (Console.ReadKey(true) is ConsoleKeyInfo key && key.Key != ConsoleKey.Escape)
			{
				if (dateDict.TryGetValue(key.KeyChar, out var option))
				{
					if (option.DrawOption())
						return option;
					Console.Error.WriteLine();
					WriteOptions(dateDict, finalOption);
				}
			}
			return null;
		}

		private static T? WaitForMultiCharSelection<T>(T[] options) where T : class, IConsoleOption
		{
			const string finalOption = "[E]  Exit";

			int numPlaces = (int)Math.Ceiling(Math.Log10(options.Length));
			var decFormat = "D" + numPlaces;
			var dateDict = options.Select((d, i) => new KeyValuePair<string, T>(i.ToString(decFormat), d)).ToDictionary();

			WriteOptions(dateDict, finalOption);
			while (Console.ReadLine() is string key && !string.Equals(key, "E", StringComparison.OrdinalIgnoreCase))
			{
				if (dateDict.TryGetValue(key, out var option))
				{
					if (option.DrawOption())
						return option;
					Console.Error.WriteLine();
					WriteOptions(dateDict, finalOption);
				}
			}
			return null;
		}

		private static void WriteOptions<S,T>(IEnumerable<KeyValuePair<S, T>> dateDict, string finalOption) where S : notnull where T : class, IConsoleOption
		{
			const string spacer = "  ";
			var entries = dateDict.Select((kvp, i) => $"[{kvp.Key}]  {kvp.Value.DisplayValue}").Append(finalOption).ToArray();

			for (int i = 0; i < entries.Length - 1; i++)
			{
				Console.Error.Write(entries[i]);

				var remainingSpace = Console.WindowWidth - Console.CursorLeft;
				if (remainingSpace < entries[i + 1].Length + spacer.Length)
					Console.Error.WriteLine();
				else
					Console.Error.Write(spacer);
			}

			Console.Error.Write(entries[^1]);

			if (Console.CursorLeft > 0)
				Console.Error.WriteLine();
		}
	}
}
