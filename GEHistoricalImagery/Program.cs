using CommandLine;
using GoogleEarthImageDownload.Cli;
using System.Diagnostics.CodeAnalysis;

namespace GoogleEarthImageDownload;

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
		settings.AutoVersion = false;
		settings.AutoHelp = true;
		settings.HelpWriter = Console.Error;
	}

	[STAThread]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Info))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Availability))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Download))]
	static async Task Main(string[] args)
	{
		var parser = new Parser(ConfigureParser);

		var result = parser.ParseArguments(args, typeof(Info), typeof(Availability), typeof(Download));

		await result.WithParsedAsync<OptionsBase>(opt => opt.Run());
	}
}
