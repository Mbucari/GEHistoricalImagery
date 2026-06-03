using CommandLine;

namespace GEHistoricalImagery.Cli;

internal interface IQuietCommand
{
	[Option('q', HelpText = "Quiet mode", Default = false)]
	bool Quiet { get; set; }
}
