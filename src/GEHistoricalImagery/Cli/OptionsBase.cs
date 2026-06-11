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
				_cacheDir = cd.ReplaceUnixHomeDir();
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
	protected abstract IEnumerable<string> GetValidationErrors();

	protected bool AnyValidationErrors()
	{
		List<string> errors = GetValidationErrors().ToList();
		errors.ForEach(Console.Error.WriteLine);
		return errors.Count > 0;
	}
}
