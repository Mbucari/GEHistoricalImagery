using CommandLine;

namespace GEHistoricalImagery.Cli;

public enum Provider
{
	TM,
	Wayback
}

internal abstract class OptionsBase
{
	[Option("provider", MetaValue = "<Provider>", Default = Provider.TM, HelpText = "Aerial imagery provider\n [TM]      Google Earth Time Machine\n [Wayback] ESRI World Imagery Wayback")]
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

	public double Progress { get; private set; }

	private int lastProgLen;
	protected void ReportProgress(double progress)
	{
		lock (this)
		{
			if (progress >= Progress)
			{
				var p = progress.ToString("P");
				var message = lastProgLen == 0 ? $"\e[K{p}" : $"\e[{lastProgLen}D\e[K{p}";
				Console.Error.Write(message);
				lastProgLen = p.Length;
				Progress = progress;
			}
		}
	}
	private DateTime startTime;
	private string? taskMessage;
	protected void BeginProgress(string text)
	{
		if (text[^1] != ' ')
			text += ' ';
		taskMessage = text;
		Console.Error.Write(text);
		startTime = DateTime.UtcNow;
		lastProgLen = 0;
		Progress = 0;
		ReportProgress(0);
	}

	protected void EndProgress()
	{
		var elapsed = DateTime.UtcNow - startTime;
		Console.Error.WriteLine($"\e[G\e[K{taskMessage}Done! ({elapsed:h\\:mm\\:ss\\.FF})");
		Progress = 0;
		lastProgLen = 0;
	}

	protected static string DateString(DateOnly? date) => date?.ToString("yyyy/MM/dd") ?? "N/A";
}
