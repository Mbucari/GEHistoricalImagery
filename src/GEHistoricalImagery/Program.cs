using CommandLine;
using GEHistoricalImagery.Cli;
using System.Diagnostics.CodeAnalysis;

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
		settings.CaseInsensitiveEnumValues = true;
	}

	[STAThread]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Info))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Availability))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Download))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Dump))]
	private static async Task Main(string[] args)
	{
		Console.OutputEncoding = System.Text.Encoding.UTF8;

		Parser parser = new(ConfigureParser);
		ParserResult<object> result = parser.ParseArguments(args, typeof(Info), typeof(Availability), typeof(Download), typeof(Dump));
		if (result.Value is IQuietCommand { Quiet: true })
		{
			Console.SetError(new StreamWriter(Stream.Null));
			OSGeo.GDAL.Gdal.SetErrorHandler((_, _, _) => { }, 0);
		}

		try
		{
			await result.WithParsedAsync<OptionsBase>(opt => opt.RunAsync());
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("An error occurred:" + Environment.NewLine + Environment.NewLine + ex.ToString());
		}
	}
}
