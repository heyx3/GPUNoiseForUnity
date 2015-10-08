using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
	public static class GraphUtils
	{
		/// <summary>
		/// The standard file extension for Graphs.
		/// </summary>
		public static readonly string Extension = "gpug";


		/// <summary>
		/// Gets the full paths for all graphs in the Unity project.
		/// </summary>
		public static List<string> GetAllGraphsInProject()
		{
			DirectoryInfo inf = new DirectoryInfo(Application.dataPath);
			FileInfo[] files = inf.GetFiles("*." + GraphUtils.Extension, SearchOption.AllDirectories);
			return files.Select(f => f.FullName).ToList();
		}

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
		/// Also forces Unity to immediately recognize and compile the shader
		///     so that it can be immediately used after calling this function.
		/// If the shader fails to load for some reason, an error is output to the Unity console
		///     and "null" is returned.
		/// </summary>
		/// <param name="outputComponents">
		/// The texture output. Can optionally output into a subset of the full RGBA components.
		/// For example, greyscale textures only need "r".
		/// </param>
		/// <param name="defaultColor">
		/// The color (generally 0-1) of the color components which aren't set by the noise.
		/// </param>
		public static Shader SaveShader(Graph g, string filePath,
										string shaderName = "GPU Noise/My Shader",
										string outputComponents = "rgb",
										float defaultColor = 0.0f)
		{
			string relativePath = PathUtils.GetRelativePath(filePath, "Assets");

			try
			{
				//Get the shader code.
				string shad = g.GenerateShader(shaderName, outputComponents, defaultColor);
				if (shad == null)
				{
					return null;
				}

				//Write to the file.
				File.WriteAllText(filePath, shad);
				

				//Tell Unity to load/compile it.
				AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceSynchronousImport);
			}
			catch (Exception e)
			{
				Debug.LogError("Error saving/loading shader to/from file: " + e.Message);
			}

			return AssetDatabase.LoadAssetAtPath<Shader>(relativePath);
		}

		/// <summary>
		/// Renders the given material into the given render target using a full-screen quad.
		/// Assumes the material uses a shader generated from a Graph.
		/// Optionally copies the resulting texture data into a Texture2D for further processing.
		/// </summary>
		public static void RenderToTexture(RenderTexture rendTarget, Material mat, Texture2D copyTo = null)
		{
			//Set up rendering state.
			RenderTexture activeTarget = RenderTexture.active;
			RenderTexture.active = rendTarget;
			mat.SetPass(0);

			//Render a quad using immediate mode.
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Viewport(new Rect(0, 0, rendTarget.width, rendTarget.height));
			GL.Begin(GL.TRIANGLE_STRIP);
			GL.Color(Color.white);
			GL.TexCoord(new Vector3(0.0f, 0.0f, 0.0f));
			GL.Vertex3(-1.0f, -1.0f, 0.0f);
			GL.TexCoord(new Vector3(1.0f, 0.0f, 0.0f));
			GL.Vertex3(1.0f, -1.0f, 0.0f);
			GL.TexCoord(new Vector3(0.0f, 1.0f, 0.0f));
			GL.Vertex3(-1.0f, 1.0f, 0.0f);
			GL.TexCoord(new Vector3(1.0f, 1.0f, 0.0f));
			GL.Vertex3(1.0f, 1.0f, 0.0f);
			GL.End();
			GL.PopMatrix();

			//Copy the results into the Texture2D.
			if (copyTo != null)
			{
				if (copyTo.width != rendTarget.width || copyTo.height != rendTarget.height)
				{
					copyTo.Resize(rendTarget.width, rendTarget.height);
				}
				copyTo.ReadPixels(new Rect(0, 0, rendTarget.width, rendTarget.height), 0, 0);
			}

			//Reset rendering state.
			RenderTexture.active = activeTarget;
		}

		/// <summary>
		/// Generates a texture containing the given graph's noise output.
		/// If this is being called very often, create a permanent render target and material and
		///     use the other version of this method instead for much better performance.
		/// If an error occurred, outputs to the Unity debug console and returns "null".
		/// </summary>
		/// <param name="outputComponents">
		/// The texture output. Can optionally output into a subset of the full RGBA components.
		/// For example, greyscale textures only need "r".
		/// </param>
		/// <param name="defaultColor">
		/// The color (generally 0-1) of the color components which aren't set by the noise.
		/// </param>
		public static Texture2D RenderToTexture(Graph g, int width, int height,
												string outputComponents = "rgb",
												float defaultColor = 0.0f,
												TextureFormat format = TextureFormat.RGBAFloat)
		{
			//Generate a shader from the graph and have Unity compile it.
			string shaderPath = Path.Combine(Application.dataPath, "gpuNoiseShaderTemp.shader");
			Shader shader = SaveShader(g, shaderPath, "TempGPUNoiseShader", outputComponents, defaultColor);
			if (shader == null)
			{
				return null;
			}

			//Render the shader's output into a render texture and copy the data to a Texture2D.
			RenderTexture target = new RenderTexture(width, height, 16, RenderTextureFormat.ARGBFloat);
			target.Create();
			Texture2D resultTex = new Texture2D(width, height, format, false, true);
			RenderToTexture(target, new Material(shader), resultTex);

			//Clean up.
			target.Release();
			if (!AssetDatabase.DeleteAsset(PathUtils.GetRelativePath(shaderPath, "Assets")))
			{
				Debug.LogError("Unable to delete temp file: " + shaderPath);
			}

			return resultTex;
		}
	}
}