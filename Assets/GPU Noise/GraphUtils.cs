using System;
using System.IO;
using System.Linq;
using UnityEngine;


namespace GPUGraph
{
	/// <summary>
	/// Provides support for saving/loading things like Graphs and shaders.
	/// This is the only code in the whole GPUGraph system that can be used at run-time!
	/// </summary>
	public static class GraphUtils
	{
		private static RenderTexture rendTex = null;
		private static Texture2D colTex = null;
		
		private static void SetUpTextures(int width, int height)
		{
			if (rendTex == null || rendTex.width != width || rendTex.height != height)
			{
				if (rendTex != null)
					rendTex.Release();

				//Find a valid texture format.
				RenderTextureFormat rtFMT = RenderTextureFormat.RFloat;
				TextureFormat fmt = TextureFormat.RFloat;
				if (!SystemInfo.SupportsRenderTextureFormat(rtFMT) ||
					!SystemInfo.SupportsTextureFormat(fmt))
				{
					rtFMT = RenderTextureFormat.R8;
					fmt = TextureFormat.R16;
					if (!SystemInfo.SupportsRenderTextureFormat(rtFMT) ||
						!SystemInfo.SupportsTextureFormat(fmt))
					{
						rtFMT = RenderTextureFormat.ARGB32;
						fmt = TextureFormat.ARGB32;
						if (!SystemInfo.SupportsRenderTextureFormat(rtFMT) ||
							!SystemInfo.SupportsTextureFormat(fmt))
						{
							Debug.LogError("Platform doesn't support RFloat, R8/R16, or ARGB32 texture formats." +
										   "Rewrite \"GraphUtils.SetUpRendTex\" to use a different format.");
							return;
						}
					}
				}

				//Create the render texture.
				rendTex = new RenderTexture(width, height, 16, rtFMT);
				rendTex.Create();

				//Create the color texture.
				if (colTex == null)
					colTex = new Texture2D(width, height, fmt, false);
				else
					colTex.Resize(width, height);
			}
		}

		
		/// <summary>
		/// Renders the given material into the given render target using a full-screen quad.
		/// Assumes the material uses a shader generated from a Graph.
		/// Optionally copies the resulting texture data into a Texture2D for further processing.
		/// </summary>
		public static void GenerateToTexture(RenderTexture rendTarget, Material noiseMat, Texture2D copyTo = null)
		{
			//Set up rendering state.
			RenderTexture activeTarget = RenderTexture.active;
			RenderTexture.active = rendTarget;
			noiseMat.SetPass(0);

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
				copyTo.Apply();
			}

			//Reset rendering state.
			RenderTexture.active = activeTarget;
		}
		/// <summary>
		/// Uses the given noise material to generate noise into the given array.
		/// </summary>
		public static void GenerateToArray(float[,] outData, Material noiseMat)
		{
			//Generate the noise.
			SetUpTextures(outData.GetLength(0), outData.GetLength(1));
			GenerateToTexture(rendTex, noiseMat, colTex);

			//Read the noise into the array.
			Color[] cols = colTex.GetPixels();
			for (int y = 0; y < outData.GetLength(1); ++y)
				for (int x = 0; x < outData.GetLength(0); ++x)
					outData[x, y] = cols[x + (outData.GetLength(0) * y)].r;
		}
	}
}