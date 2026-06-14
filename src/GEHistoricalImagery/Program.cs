using CommandLine;
using GEHistoricalImagery.Cli;
using GEHistoricalImagery.Cli.Availability;
using GEHistoricalImagery.Cli.Download;
using GEHistoricalImagery.Cli.Dump;
using GEHistoricalImagery.Cli.Info;
using OSGeo.GDAL;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace GEHistoricalImagery;

public enum ExitCode
{
	ProcessCompletedSuccessfully = 0,
	NonRunNonError = 1,
	ParseError = 2,
	RunTimeError = 3
}

internal class Program
{
	private static void ConfigureParser(ParserSettings settings)
	{
		settings.AutoVersion = true;
		settings.AutoHelp = true;
		settings.HelpWriter = Console.Error;
		settings.AllowMultiInstance = true;
		settings.CaseInsensitiveEnumValues = true;
	}

	[STAThread]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(InfoCommand))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AvailabilityCommand))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DownloadCommand))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DumpCommand))]
	private static async Task Main(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;

		Parser parser = new(ConfigureParser);
		ParserResult<object> result = parser.ParseArguments<InfoCommand, AvailabilityCommand, DownloadCommand, DumpCommand>(args);
		if (result.Value is OptionsBase { Quiet: true })
		{
			Console.SetError(TextWriter.Null);
		}
		else
		{
			ProgressWriter.Instance.SetErrorStream(Console.OpenStandardError());
			Console.SetError(ProgressWriter.Instance);
		}

		try
		{
#if DEBUG
			Gdal.SetConfigOption("CPL_DEBUG", "ON");
#endif
			Gdal.SetErrorHandler(GdalMessageHandler, 0);
			Gdal.SetConfigOption("GDAL_DISABLE_READDIR_ON_OPEN", "TRUE");
			await result.WithParsedAsync<OptionsBase>(opt => opt.RunAsync());
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("An error occurred:" + Environment.NewLine + Environment.NewLine + ex.ToString());
		}
		finally
		{
			Gdal.SetErrorHandler(null, 0);
		}
	}
	private static void GdalMessageHandler(int eclass, int code, nint msg)
	{
		CPLErr level = (CPLErr)eclass;
		if (level > CPLErr.CE_Log) unsafe
		{
			var msgBts = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)msg);
			var message = Encoding.UTF8.GetString(msgBts);
			string levelStr = level.ToString()[3..];
			Console.Error.WriteLine($"GDAL {levelStr}: {message}");
		}
	}
}