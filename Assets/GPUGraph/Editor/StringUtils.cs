using System;
using Path = System.IO.Path;
using System.Text;


/// <summary>
/// Some extra helper methods to complement the Path class.
/// </summary>
public static class StringUtils
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

		//Get the start of the given folder in the path string.
		int folderLoc = sb.ToString().IndexOf(Path.DirectorySeparatorChar +
											  startFolder +
											  Path.DirectorySeparatorChar);
		if (folderLoc < 0 && sb.ToString().StartsWith(startFolder + Path.DirectorySeparatorChar))
		{
			folderLoc = 0;
		}

		//If the given folder was found, cut out everything before that.
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

	/// <summary>
	/// If the given string ends in a number, increments that number.
	/// Otherwise, appends a "2" to it.
	/// </summary>
	public static string IncrementNumberPostfix(string str)
	{
		StringBuilder number = new StringBuilder();
		int i;
		for (i = str.Length - 1; i >= 0; --i)
		{
			if (str[i] < '0' || str[i] > '9')
				break;
			number.Append(str[i]);
		}

		if (number.Length == 0)
			return str + "2";
		else
			return str.Substring(0, i + 1) + (int.Parse(number.ToString())).ToString();
	}

	/// <summary>
	/// Turns the given variable name into a nice display name.
	/// </summary>
	public static string PrettifyVarName(string name)
	{
		StringBuilder sb = new StringBuilder(name);
		for (int i = 0; i < sb.Length; ++i)
		{
			if (sb[i] == '_')
			{
				sb[i] = ' ';

				//Make the next letter uppercase.
				if (i + 1 < sb.Length && sb[i + 1] >= 'a' && sb[i + 1] <= 'z')
				{
					sb[i + 1] -= (char)('a' - 'A');
				}
			}
		}

		return sb.ToString().Trim();
	}

	/// <summary>
	/// Finds the last instance of the given char and removes it
	///     along with everything that came before it.
	/// </summary>
	public static string RemoveEverythingBefore(this string str, char c)
	{
		int i = str.Length - 1;
		while (i >= 0 && str[i] != c)
			i -= 1;
		return str.Substring(i + 1, str.Length - i - 1);
	}

	/// <summary>
	/// Gets this float as a code-parseable string.
	/// In other words, if this float's default string representation is scientific notation,
	///		this method automatically fixes that.
	/// </summary>
	public static string ToCodeString(this float f)
	{
		string str = f.ToString();

		int i = str.IndexOf("E");
		if (i >= 0)
		{
			if (str[i + 1] == '-')
			{
				return "0.0";
			}
			else
			{
				if (str[0] == '-')
				{
					return "-99999999999.0";
				}
				else
				{
					return "9999999999.0";
				}
			}
		}
		else
		{
			//Make sure it's parsed as a float value and not an integer.
			if (!str.Contains("."))
			{
				return str + ".0";
			}
			else
			{
				return str;
			}
		}
	}
}