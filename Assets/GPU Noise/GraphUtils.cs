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
		/// <summary>
		/// Renders the given material into the given render target using a full-screen quad.
		/// Assumes the material uses a shader generated from a Graph.
		/// Optionally copies the resulting texture data into a Texture2D for further processing.
		/// </summary>
		/// <param name="rendTarget">
		/// If set to "null", the noise will be rendered onto the screen.
		/// </param>
		public static void GenerateToTexture(RenderTexture rendTarget, Material noiseMat,
											 Texture2D copyTo = null)
		{
			int width = (rendTarget == null ? Screen.width : rendTarget.width),
				height = (rendTarget == null ? Screen.height : rendTarget.height);

			//Set up rendering state.
			RenderTexture activeTarget = RenderTexture.active;
			RenderTexture.active = rendTarget;
			noiseMat.SetPass(0);

			//Render a quad using immediate mode.
			GL.PushMatrix();
			GL.LoadIdentity();
			GL.Viewport(new Rect(0, 0, width, height));
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
				if (copyTo.width != width || copyTo.height != height)
					copyTo.Resize(width, height);

				copyTo.ReadPixels(new Rect(0, 0, width, height), 0, 0);
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
			RenderTexture rendTex = RenderTexture.GetTemporary(outData.GetLength(0), outData.GetLength(1));

			//Generate the noise.
			SetUpColorTex(outData.GetLength(0), outData.GetLength(1));
			GenerateToTexture(rendTex, noiseMat, colorTex);

			//Read the noise into the array.
			Color[] cols = colorTex.GetPixels();
			for (int y = 0; y < outData.GetLength(1); ++y)
				for (int x = 0; x < outData.GetLength(0); ++x)
					outData[x, y] = cols[x + (outData.GetLength(0) * y)].r;

			RenderTexture.ReleaseTemporary(rendTex);
		}


		private static Texture2D colorTex = null;
		private static void SetUpColorTex(int width, int height)
		{
			if (colorTex == null)
			{
				//Find the best-possible supported format for this.
				TextureFormat[] fmts = new TextureFormat[]
					{ TextureFormat.RFloat, TextureFormat.RGBAFloat,
					  TextureFormat.RHalf, TextureFormat.RGBAHalf,
					  TextureFormat.BGRA32, TextureFormat.RGBA32, TextureFormat.ARGB32 };
				TextureFormat? fmt = null;
				for (int i = 0; i < fmts.Length; ++i)
				{
					if (SystemInfo.SupportsTextureFormat(fmts[i]))
					{
						fmt = fmts[i];
						break;
					}
				}
				if (!fmt.HasValue)
				{
					Debug.LogError("Couldn't find a reasonable texture format for GPUG");
					return;
				}

				colorTex = new Texture2D(width, height, fmt.Value, false);
			}
			else if (colorTex.width != width || colorTex.height != height)
			{
				colorTex.Resize(width, height);
			}
		}
	}
}