using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace GPUNoise
{
	public static class GraphSaveLoad
	{
		public static string Extension = "gpug";

		/// <summary>
		/// Loads a graph from the given file.
		/// Returns "null" and prints to the Debug console if there was an error.
		/// </summary>
		public static Graph LoadGraph(string filePath)
		{
			IFormatter formatter = new BinaryFormatter();
			Stream s = null;
			Graph g = null;

			try
			{
				s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				g = (Graph)formatter.Deserialize(s);
			}
			catch (System.Exception e)
			{
				UnityEngine.Debug.LogError("Error opening file: " + e.Message);
			}
			finally
			{
				s.Close();
			}

			return g;
		}
		/// <summary>
		/// Saves the given graph to the given file, overwriting it if it exists.
		/// Prints to the Debug console and returns false if there was an error.
		/// Otherwise, returns true.
		/// </summary>
		public static bool SaveGraph(Graph g, string filePath)
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = null;
			bool failed = false;
			try
			{
				stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
				formatter.Serialize(stream, g);
			}
			catch (System.Exception e)
			{
				failed = true;
				UnityEngine.Debug.LogError("Error opening/writing to file: " + e.Message);
			}
			finally
			{
				if (stream != null)
				{
					stream.Close();
				}
			}

			return !failed;
		}
	}
}