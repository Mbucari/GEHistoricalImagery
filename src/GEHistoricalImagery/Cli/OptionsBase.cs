using CommandLine;
using System.Diagnostics;

namespace GEHistoricalImagery.Cli;

public enum Provider
{
	TM,
	Wayback
}

internal abstract class OptionsBase
{
	[Option("provider", MetaValue = "TM", Default = Provider.TM, HelpText = "Aerial imagery provider\n [TM]      Google Earth Time Machine\n [Wayback] ESRI World Imagery Wayback")]
	public Provider Provider { get; set; }

	[Option("no-cache", HelpText = "Disable local caching", Default = false)]
	public bool DisableCache { get; set; }
	public abstract Task RunAsync();

	string? _cacheDir = null;
	protected string? CacheDir
	{
		get
		{
			if (DisableCache)
			{
				return null;
			}
			else if (_cacheDir is not null)
			{
				return _cacheDir;
			}
			else if (Environment.GetEnvironmentVariable("GEHistoricalImagery_Cache") is string cd)
			{
				_cacheDir = PathHelper.ReplaceUnixHomeDir(cd);
			}
			else
			{
				try
				{
					_cacheDir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "GEHI_cache")).FullName;
				}
				catch
				{
					_cacheDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "GEHI_cache")).FullName;
				}
			}
			return _cacheDir;
		}
	}


	public double Progress { get; set; }

	private int lastProgLen;
	protected void ReportProgress(double progress)
	{
		lock (this)
		{
			if (progress >= Progress)
			{
				var p = progress.ToString("P");
				Console.Error.Write(new string('\b', lastProgLen) + p);
				lastProgLen = p.Length;
				Progress = progress;
			}
		}
	}
	private Stopwatch progressTimer = new Stopwatch();
	protected void BeginProgress(string text)
	{
		Console.Error.Write(text);
		progressTimer.Restart();
		ReportProgress(0);
	}
	protected void ReplaceProgress()
	{
		progressTimer.Stop();
		var newText = new string('\b', lastProgLen);

		newText = newText + new string(' ', lastProgLen) + newText + $"Done! ({progressTimer.Elapsed:g})";

		Console.Error.WriteLine(newText);
		Progress = 0;
		lastProgLen = 0;
	}

	protected static string DateString(DateOnly? date) => date?.ToString("yyyy/MM/dd") ?? "N/A";
}
