using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEditor;
using GPUNoise;



namespace GPUNoise.Applications
{
	/// <summary>
	/// Provides support for saving/loading things like Graphs and shaders.
	/// </summary>
	public static class GraphSaveLoad
	{
		/// <summary>
		/// The standard file extension for Graphs.
		/// </summary>
		public static readonly string Extension = "gpug";


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

		/// <summary>
		/// Saves the shader for the given graph to the given file with the given name.
		/// Optionally forces Unity to immediately recognize and compile the shader
		///     so that it can be immediately used.
		/// If the shader fails to load for some reason, an error is output to the Unity console
		///     and "null" is returned.
		/// </summary>
		/// <param name="outputComponents">
		/// The texture output. Can optionally output into a subset of the full RGBA components.
		/// For example, greyscale textures only need a Red component.
		/// </param>
		/// <param name="forceUnityLoad">
		/// Whether this method should force Unity to immediately recognize the shader
		///     so that it can be used right away.
		/// Only works if the shader is somewhere inside a "Resources" folder.
		/// </param>
		public static Shader SaveShader(Graph g, string filePath,
										string shaderName = "GPU Noise/My Shader",
										bool forceUnityLoad = false, string outputComponents = "rgba")
		{
			Shader shader = null;

			try
			{
				//Get the shader code.
				string shad = g.GenerateShader(shaderName, outputComponents);
				if (shad == null)
				{
					return null;
				}

				//Write to the file.
				File.WriteAllText(filePath, shad);

				//Tell Unity to load/compile it.
				if (forceUnityLoad)
				{
					shader = Resources.Load(Path.GetFileNameWithoutExtension(shaderName)) as Shader;
					string assetPath = AssetDatabase.GetAssetPath(shader);
					AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Error saving/loading shader to/from file: " + e.Message);
			}

			return shader;
		}
	}
}