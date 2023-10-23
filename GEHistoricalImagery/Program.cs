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

#if DEBUG
		args = new[] { "help", "download" };
		args = new[] { "info", "-l", "39.24798,-106.297207" };
		args = new[] { "download", "--lower-left", "39.222947,-106.345208", "--upper-right", "39.247598,-106.297207", "-z", "20", "--date", "2019/10/03" };
		args = new[] { "availability", "--lower-left", "39.222947,-106.345208", "--upper-right", "39.247598,-106.297207", "-z", "20" };
		args = new[] { "info", "-l", "39.24798,-106.297207" };
		args = new[] { "availability", "--lower-left", "34.017456,-84.096750", "--upper-right", "34.028672,-84.085661", "-z", "20" };
		args = new[] { "download", "--lower-left", "34.017456,-84.096750", "--upper-right", "34.028672,-84.085661", "-z", "20", "--date", "2010/04/09" };
		args = new[] { "availability", "--lower-left", "41.913134,-73.518542", "--upper-right", "41.922498,-73.511277", "-z", "20" };
		args = new[] { "download", "--lower-left", "34.017456,-84.096750", "--upper-right", "34.028672,-84.085661", "-z", "20", "--date", "2010/04/09" };
		args = new[] { "availability", "--lower-left", "41.913134,-73.518542", "--upper-right", "41.922498,-73.511277", "-z", "20" };
		args = new[] { "availability", "--lower-left", "39.222947,-106.345208", "--upper-right", "39.247598,-106.297207", "-z", "19" };
		args = new[] { "download", "--lower-left", "39.222947,-106.345208", "--upper-right", "39.247598,-106.297207", "-z", "19", "--date", "2019/10/03", "--target-sr", "https://epsg.io/103248.wkt", "-o", @"C:\Users\mbuca\Downloads\Union Milling GeoTiff High.tif" };

		args = new[] { "info", "-l", "39.630575,-104.841299"};
		args = new[] { "availability", "--lower-left", "39.619819,-104.856121", "--upper-right", "39.638393,-104.824990", "-z", "20" };
		args = new[] { "download", "--lower-left", "39.619819,-104.856121", "--upper-right", "39.638393,-104.824990", "-z", "20", "--date", "2023/04/29", "--target-sr", "https://epsg.io/103248.wkt", "-o", @"C:\Users\mbuca\Downloads\Cherry Creek z20 - 2.tif" };


#endif
		var result = parser.ParseArguments(args, typeof(Info), typeof(Availability), typeof(Download));

		await result.WithParsedAsync<OptionsBase>(opt => opt.Run());
	}
}
