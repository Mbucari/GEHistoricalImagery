using System;
using System.Collections.Generic;
using System.Text;

namespace GEHistoricalImagery;

internal class PathHelper
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
	public static string ReplaceUnixHomeDir(string path)
	{
		if (!path.StartsWith('~'))
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
}
