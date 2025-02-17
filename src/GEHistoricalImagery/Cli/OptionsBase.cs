using CommandLine;

namespace GEHistoricalImagery.Cli;

internal abstract class OptionsBase
{
	[Option("no-cache", HelpText = "Disable local caching", Default = false)]
	public bool DisableCache { get; set; }
	public abstract Task RunAsync();

	protected string? CacheDir => DisableCache ? null : "./cache";

	public double Progress { get; set; }

	private int lastProgLen;
	protected void ReportProgress(double progress)
	{
		lock (this)
		{
			if (progress >= Progress)
			{
				var p = progress.ToString("P");
				Console.Write(new string('\b', lastProgLen) + p);
				lastProgLen = p.Length;
				Progress = progress;
			}
		}
	}

	protected void ReplaceProgress(string text)
	{
		var newText = new string('\b', lastProgLen);

		newText = newText + new string(' ', lastProgLen) + newText + text;

		Console.Write(newText);
		Progress = 0;
		lastProgLen = 0;
	}

	protected static string DateString(DateOnly date) => date.ToString("yyyy/MM/dd");
}
