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
		/// <param name="uvZ">The Z coordinate of the UVs, in case the graph uses it for 3D noise.</param>
		/// <param name="leaveReadable">
		/// Whether the texture's pixel data can still be read from the CPU after this operation.
		/// </param>
		public static Texture2D GenerateToTexture(Graph g, GraphParamCollection c,
												  int width, int height, float uvZ,
												  string outputComponents, float defaultColor,
												  TextureFormat format = TextureFormat.RGBAFloat,
												  bool leaveReadable = false)
		{
			//Generate a shader/material from the graph.
			Shader shader = ShaderUtil.CreateShaderAsset(g.GenerateShader("TempGPUNoiseShader",
																		  outputComponents,
																		  defaultColor));
			if (shader == null)
				return null;
			Material mat = new Material(shader);
			c.SetParams(mat);
			mat.SetFloat(GraphUtils.Param_UVz, uvZ);

			//Render the shader's output into a render texture and copy the data to a Texture2D.
			RenderTexture target = RenderTexture.GetTemporary(width, height, 16,
															  RenderTextureFormat.ARGBFloat);
			Texture2D resultTex = new Texture2D(width, height, format, false, true);

			//Generate.
			GraphUtils.GenerateToTexture(target, mat, resultTex, leaveReadable);

			//Clean up.
			RenderTexture.ReleaseTemporary(target);

			return resultTex;
		}
		/// <summary>
		/// Generates a texture containing the given graph's noise output.
		/// If this is being called very often, create a permanent render target and material and
		///     use the other version of this method instead for much better performance.
		/// If an error occurred, outputs to the Unity debug console and returns "null".
		/// </summary>
		/// <param name="gradientRampName">The name of the gradient ramp texture param.</param>
		/// <param name="uvZ">The Z coordinate of the UVs, in case the graph uses it for 3D noise.</param>
		/// <param name="leaveReadable">
		/// Whether to leave the texture data readable on the CPU after the operation.
		/// </param>
		public static Texture2D GenerateToTexture(Graph g, GraphParamCollection c,
												  int width, int height, float uvZ,
												  Gradient gradientRamp,
												  TextureFormat format = TextureFormat.RGBAFloat,
												  bool leaveReadable = false)
		{
			//Generate a shader/material from the graph.
			Shader shader = ShaderUtil.CreateShaderAsset(g.GenerateShader("TempGPUNoiseShader",
																		  "_MyGradientRamp14123"));
			if (shader == null)
				return null;
			Material mat = new Material(shader);
			c.SetParams(mat);
			mat.SetFloat(GraphUtils.Param_UVz, uvZ);

			//Generate a texture from the gradient.
			Texture2D myRamp = new Texture2D(1024, 1, TextureFormat.RGBA32, false);
			Color[] cols = new Color[myRamp.width];
			for (int i = 0; i < cols.Length; ++i)
				cols[i] = gradientRamp.Evaluate((float)i / (float)(cols.Length - 1));
			myRamp.SetPixels(cols);
			myRamp.Apply(false, true);
			mat.SetTexture("_MyGradientRamp14123", myRamp);

			//Render the shader's output into a render texture and copy the data to a Texture2D.
			RenderTexture target = RenderTexture.GetTemporary(width, height, 16,
															  RenderTextureFormat.ARGBFloat);
			Texture2D resultTex = new Texture2D(width, height, format, false, true);

			//Generate.
			GraphUtils.GenerateToTexture(target, mat, resultTex, leaveReadable);

			//Clean up.
			RenderTexture.ReleaseTemporary(target);

			return resultTex;
		}

        /// <summary>
        /// Generates a 3D texture containing the given graph's noise output.
        /// </summary>
        /// <param name="outputComponents">
        /// The texture output.
        /// For example, pass "rgb" or "xyz" to output the noise into the red, green, and blue channels
        ///     but not the alpha channel.
        /// </param>
        /// <param name="defaultColor">
        /// The color (generally 0-1) of the color components which aren't set by the noise.
        /// </param>
        /// <param name="useMipmaps">Whether the 3D texture object uses mipmapping.</param>
        /// <param name="leaveTextureReadable">
        /// Whether to let the texture keep a CPU copy of its data on hand for later reading.
        /// </param>
        public static Texture3D GenerateToTexture(Graph g, GraphParamCollection c,
                                                  int width, int height, int depth,
                                                  string outputComponents, float defaultColor,
                                                  bool useMipmaps, bool leaveTextureReadable,
                                                  TextureFormat format = TextureFormat.RGBA32)
        {
			//Generate a shader/material from the graph.
			Shader shader = ShaderUtil.CreateShaderAsset(g.GenerateShader("TempGPUNoiseShader",
																		  outputComponents,
																		  defaultColor));
			if (shader == null)
				return null;
            Material mat = new Material(shader);
            c.SetParams(mat);


            //For every Z layer in the texture, generate a 2D texture representing that layer.

            Color32[] finalPixels = new Color32[width * height * depth];

            RenderTexture target = RenderTexture.GetTemporary(width, height, 16,
															  RenderTextureFormat.ARGBFloat);
            Texture2D resultTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);


            for (int depthI = 0; depthI < depth; ++depthI)
            {
                //Get the UV.z coordinate.
                float uvZ = (float)depthI / depth;
                mat.SetFloat(GraphUtils.Param_UVz, uvZ);

                GraphUtils.GenerateToTexture(target, mat, resultTex, true);

                //Copy the resulting data into part of the 3D texture.
                Color32[] layerPixels = resultTex.GetPixels32();
                int pixelOffset = depthI * (width * height);
                for (int pixelI = 0; pixelI < (width * height); ++pixelI)
                    finalPixels[pixelI + pixelOffset] = layerPixels[pixelI];
            }


            //Create the actual texture object.
            Texture3D finalTex = new Texture3D(width, height, depth, format, useMipmaps);
            finalTex.SetPixels32(finalPixels);
            finalTex.Apply(useMipmaps, !leaveTextureReadable);

			//Clean up.
			RenderTexture.ReleaseTemporary(target);

            return finalTex;
        }
        /// <summary>
        /// Generates a 3D texture containing the given graph's noise output.
        /// </summary>
        /// <param name="useMipmaps">Whether the 3D texture object uses mipmapping.</param>
        /// <param name="leaveTextureReadable">
        /// Whether to let the texture keep a CPU copy of its data on hand for later reading.
        /// </param>
        public static Texture3D GenerateToTexture(Graph g, GraphParamCollection c,
                                                  int width, int height, int depth, Gradient gradientRamp,
                                                  bool useMipmaps, bool leaveTextureReadable,
                                                  TextureFormat format = TextureFormat.RGBA32)
        {
			//Generate a shader/material from the graph.
			Shader shader = ShaderUtil.CreateShaderAsset(g.GenerateShader("TempGPUNoiseShader",
																		  "_MyGradientRamp14123"));
			if (shader == null)
				return null;
			Material mat = new Material(shader);
			c.SetParams(mat);

            //Generate a texture from the gradient.
            Texture2D myRamp = new Texture2D(1024, 1, TextureFormat.RGBA32, false);
            Color[] cols = new Color[myRamp.width];
            for (int i = 0; i < cols.Length; ++i)
                cols[i] = gradientRamp.Evaluate((float)i / (float)(cols.Length - 1));
            myRamp.SetPixels(cols);
            myRamp.Apply(false, true);
            mat.SetTexture("_MyGradientRamp14123", myRamp);

            //For every Z layer in the texture, generate a 2D texture representing that layer.

            Color32[] finalPixels = new Color32[width * height * depth];

            RenderTexture target = RenderTexture.GetTemporary(width, height, 16,
															  RenderTextureFormat.ARGBFloat);
            Texture2D resultTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);


            for (int depthI = 0; depthI < depth; ++depthI)
            {
                //Get the UV.z coordinate.
                float uvZ = (float)depthI / depth;
                mat.SetFloat(GraphUtils.Param_UVz, uvZ);

                GraphUtils.GenerateToTexture(target, mat, resultTex, true);

                //Copy the resulting data into part of the 3D texture.
                Color32[] layerPixels = resultTex.GetPixels32();
                int pixelOffset = depthI * (width * height);
                for (int pixelI = 0; pixelI < (width * height); ++pixelI)
                    finalPixels[pixelI + pixelOffset] = layerPixels[pixelI];
            }


            //Create the actual texture object.
            Texture3D finalTex = new Texture3D(width, height, depth, format, useMipmaps);
            finalTex.SetPixels32(finalPixels);
            finalTex.Apply(useMipmaps, !leaveTextureReadable);

			//Clean up.
			RenderTexture.ReleaseTemporary(target);

            return finalTex;
        }

        /// <summary>
        /// Generates a 2D grid of noise from the given graph.
        /// If an error occurred, outputs to the Unity debug console and returns "null".
        /// </summary>
        public static float[,] GenerateToArray(Graph g, GraphParamCollection c, int width, int height)
		{
			Texture2D t = GenerateToTexture(g, c, width, height, 0.0f, "r", 0.0f,
											TextureFormat.RGBAFloat, true);
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
        /// <summary>
        /// Generates a 3D grid of noise from the given graph.
        /// If an error occurred, outputs to the Unity debug console and returns "null".
        /// </summary>
        public static float[,,] GenerateToArray(Graph g, GraphParamCollection c,
                                                int width, int height, int depth)
        {
            //Generate a 3D texture using the graph's shader.
            Texture3D t = GenerateToTexture(g, c, width, height, depth, "r", 0.0f, false, true,
                                            TextureFormat.RGBA32);
            if (t == null)
            {
                return null;
            }

            //Read the texture data and put it into a 3D array.
            Color[] cols = t.GetPixels();
            float[,,] vals = new float[width, height, depth];
            int i = 0;
            for (int z = 0; z < depth; ++z)
                for (int y = 0; y < height; ++y)
                    for (int x = 0; x < width; ++x)
                        vals[x, y, z] = cols[i++].r;

            return vals;
        }
    }
}