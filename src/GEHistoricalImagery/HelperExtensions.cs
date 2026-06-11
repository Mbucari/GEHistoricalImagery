using System.Diagnostics.CodeAnalysis;

namespace GEHistoricalImagery;

internal static class HelperExtensions
{
	//https://stackoverflow.com/questions/73523278/expand-paths-with-tilde-in-net-core
	/// <summary>
	/// Checks if a path is a "~/..." unix-path and converts it to an absolute path if it is.
	/// </summary>
	/// <remarks>
	/// This is needed because in C#,
	/// "~/foo" is not "/home/users/johndoe/foo"
	/// but something like "app/foo".
	/// </remarks>
	[return: NotNullIfNotNull(nameof(path))]
	public static string? ReplaceUnixHomeDir(this string? path)
	{
		if (string.IsNullOrEmpty(path) || !path.StartsWith('~'))
		{
			return path;
		}

		string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		return path switch
		{
			"~" => userFolder,
			_ => Path.Combine(userFolder,
								// Substring from the third character to the end: remove "~/" from the start.
								path[2..]),
		};
	}

	public static string ToDateString(this DateOnly? date) => date?.ToDateString() ?? "N/A";
	public static string ToDateString(this DateOnly date) => date.ToString("yyyy/MM/dd");
}
