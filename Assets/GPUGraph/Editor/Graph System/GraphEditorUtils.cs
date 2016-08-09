using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEditor;


namespace GPUGraph
{
	public static class GraphEditorUtils
	{
		/// <summary>
		/// The standard file extension for Graphs.
		/// </summary>
		public static readonly string Extension = "gpug";


		/// <summary>
		/// Gets the full paths for all graphs in this Unity project.
		/// </summary>
		public static List<string> GetAllGraphsInProject(string excludeGraph = null,
														 bool makeRelativeToAssets = false)
		{
			DirectoryInfo inf = new DirectoryInfo(Application.dataPath);
			FileInfo[] files = inf.GetFiles("*." + Extension, SearchOption.AllDirectories);
			return files
					.Select(f => (makeRelativeToAssets ?
									StringUtils.GetRelativePath(f.FullName, "Assets") :
									f.FullName))
					.Where(f => (f != excludeGraph))
					.ToList();
		}

		
		/// <summary>
		/// Saves the shader for the given graph to the given file with the given name.
		/// Also forces Unity to immediately recognize and compile the shader
		///     so that it can be immediately used after calling this function.
		/// If the shader fails to load for some reason, an error is output to the Unity console
		///     and "null" is returned.
		/// </summary>
		/// <param name="outputComponents">
		/// The texture output.
		/// For example, pass "rgb" or "xyz" to output the noise into the red, green, and blue channels
		///     but not the alpha channel.
		/// </param>
		/// <param name="defaultColor">
		/// The color (generally 0-1) of the color components which aren't set by the noise.
		/// </param>
		public static Shader SaveShader(Graph g, string filePath, string shaderName,
										string outputComponents, float defaultColor)
		{
			string relativePath = StringUtils.GetRelativePath(filePath, "Assets");

			try
			{
				//Get the shader code.
				string shad = g.GenerateShader(shaderName, outputComponents, defaultColor);
				if (shad == null)
				{
					return null;
				}

				//Write to the file.
				DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(filePath));
				if (!dir.Exists)
					dir.Create();
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
		/// Saves the shader for the given graph to the given file with the given name.
		/// Also forces Unity to immediately recognize and compile the shader
		///     so that it can be immediately used after calling this function.
		/// If the shader fails to load for some reason, an error is output to the Unity console
		///     and "null" is returned.
		/// </summary>
		/// <param name="gradientRampName">The name of the gradient ramp texture param.</param>
		public static Shader SaveShader(Graph g, string filePath, string shaderName,
										string gradientRampName)
		{
			string relativePath = StringUtils.GetRelativePath(filePath, "Assets");

			try
			{
				//Get the shader code.
				string shad = g.GenerateShader(shaderName, gradientRampName);
				if (shad == null)
				{
					return null;
				}

				//Write to the file.
				DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(filePath));
				if (!dir.Exists)
					dir.Create();
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
		/// Generates a texture containing the given graph's noise output.
		/// If this is being called very often, create a permanent render target and material and
		///     use the other version of this method instead for much better performance.
		/// If an error occurred, outputs to the Unity debug console and returns "null".
		/// </summary>
		/// <param name="outputComponents">
		/// The texture output.
		/// For example, pass "rgb" or "xyz" to output the noise into the red, green, and blue channels
		///     but not the alpha channel.
		/// </param>
		/// <param name="defaultColor">
		/// The color (generally 0-1) of the color components which aren't set by the noise.
		/// </param>
		public static Texture2D GenerateToTexture(Graph g, GraphParamCollection c, int width, int height,
												  string outputComponents, float defaultColor,
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

			//Create the material and set its parameters.
			Material mat = new Material(shader);
			c.SetParams(mat);

			GraphUtils.GenerateToTexture(target, mat, resultTex);

			//Clean up.
			target.Release();
			if (!AssetDatabase.DeleteAsset(StringUtils.GetRelativePath(shaderPath, "Assets")))
			{
				Debug.LogError("Unable to delete temp file: " + shaderPath);
			}

			return resultTex;
		}
		/// <summary>
		/// Generates a texture containing the given graph's noise output.
		/// If this is being called very often, create a permanent render target and material and
		///     use the other version of this method instead for much better performance.
		/// If an error occurred, outputs to the Unity debug console and returns "null".
		/// </summary>
		/// <param name="gradientRampName">The name of the gradient ramp texture param.</param>
		public static Texture2D GenerateToTexture(Graph g, GraphParamCollection c, int width, int height,
												  Gradient gradientRamp,
												  TextureFormat format = TextureFormat.RGBAFloat)
		{
			//Generate a shader from the graph and have Unity compile it.
			string shaderPath = Path.Combine(Application.dataPath, "gpuNoiseShaderTemp.shader");
			Shader shader = SaveShader(g, shaderPath, "TempGPUNoiseShader", "_MyGradientRamp14123");
			if (shader == null)
			{
				return null;
			}

			//Generate a texture from the gradient.
			Texture2D myRamp = new Texture2D(1024, 1, TextureFormat.RGBA32, false);
			Color[] cols = new Color[myRamp.width];
			for (int i = 0; i < cols.Length; ++i)
				cols[i] = gradientRamp.Evaluate((float)i / (float)(cols.Length - 1));
			myRamp.SetPixels(cols);
			myRamp.Apply(false, true);

			//Render the shader's output into a render texture and copy the data to a Texture2D.
			RenderTexture target = new RenderTexture(width, height, 16, RenderTextureFormat.ARGBFloat);
			target.Create();
			Texture2D resultTex = new Texture2D(width, height, format, false, true);

			//Create the material and set its parameters.
			Material mat = new Material(shader);
			mat.SetTexture("_MyGradientRamp14123", myRamp);
			c.SetParams(mat);

			GraphUtils.GenerateToTexture(target, mat, resultTex);

			//Clean up.
			target.Release();
			if (!AssetDatabase.DeleteAsset(StringUtils.GetRelativePath(shaderPath, "Assets")))
			{
				Debug.LogError("Unable to delete temp file: " + shaderPath);
			}

			return resultTex;
		}

		/// <summary>
		/// Generates a 2D grid of noise from the given graph.
		/// If an error occurred, outputs to the Unity debug console and returns "null".
		/// </summary>
		public static float[,] GenerateToArray(Graph g, GraphParamCollection c, int width, int height)
		{
			Texture2D t = GenerateToTexture(g, c, width, height, "r", 0.0f);
			if (t == null)
			{
				return null;
			}

			Color[] cols = t.GetPixels();
			float[,] vals = new float[width, height];
			for (int i = 0; i < cols.Length; ++i)
			{
				int x = i % width,
					y = i / width;

				vals[x, y] = cols[i].r;
			}

			return vals;
		}
	}
}