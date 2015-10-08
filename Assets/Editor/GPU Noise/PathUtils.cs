using System;
using Path = System.IO.Path;
using System.Text;


/// <summary>
/// Some extra helper methods to complement the Path class.
/// </summary>
public static class PathUtils
{
	/// <summary>
	/// Turns the given full path into a relative one starting just above the given directory.
	/// For example, "GetRelativePath("C:/A/B/C", "B")" returns "B/C".
	/// Returns "null" if the given folder can't be found.
	/// </summary>
	/// <remarks>
	/// As a side effect, all '/' or '\' slashes will be changed
	/// to the correct directory separator char for this platform.
	/// </remarks>
	/// <param name="startFolder">
	/// The folder that will appear at the top of the returned path.
	/// </param>
	public static string GetRelativePath(string fullPath, string startFolder)
	{
		StringBuilder sb = new StringBuilder(fullPath);
		if ('/' != Path.DirectorySeparatorChar)
		{
			sb.Replace('/', Path.DirectorySeparatorChar);
		}
		if ('\\' != Path.DirectorySeparatorChar)
		{
			sb.Replace('\\', Path.DirectorySeparatorChar);
		}
		
		int folderLoc = sb.ToString().IndexOf(Path.DirectorySeparatorChar +
											  startFolder +
											  Path.DirectorySeparatorChar);
		if (folderLoc >= 0)
		{
			sb.Remove(0, folderLoc);
			if (sb[0] == Path.DirectorySeparatorChar)
				sb.Remove(0, 1);
			return sb.ToString();
		}
		else
		{
			return null;
		}
	}
	
	/// <summary>
	/// Replaces all directory separators with the correct one for the current platform (either / or \).
	/// </summary>
	public static string FixDirectorySeparators(string path)
	{
		StringBuilder sb = new StringBuilder(path);
		sb.Replace('/', Path.DirectorySeparatorChar);
		sb.Replace('\\', Path.DirectorySeparatorChar);
		return sb.ToString();
	}
}