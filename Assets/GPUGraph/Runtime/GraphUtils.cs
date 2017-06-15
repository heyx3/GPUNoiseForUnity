using System;
using System.IO;
using System.Linq;
using UnityEngine;


namespace GPUGraph
{
	/// <summary>
	/// Provides support for saving/loading things like Graphs and shaders.
	/// It is recommended to just use RuntimeGraph instead of using this class directly.
	/// </summary>
	public static class GraphUtils
	{
		//TODO: Allow setting UVz when making 2D texture, or scaling/offsetting the UVz when making 3D texture.

		public static readonly string Param_UVz = "__UV_z__";


		/// <summary>
		/// Renders the given material into given texture.
		/// Assumes the material uses a shader generated from a Graph.
		/// </summary>
		/// <param name="leaveReadable">
		/// Whether to leave the texture data readable on the CPU after the operation.
		/// </param>
		public static void GenerateToTexture(Material noiseMat, Texture2D dest, bool leaveReadable = false)
		{
			var tempTarget = RenderTexture.GetTemporary(dest.width, dest.height, 16,
														RenderTextureFormat.ARGB32);
			GenerateToTexture(tempTarget, noiseMat, dest, leaveReadable);
			RenderTexture.ReleaseTemporary(tempTarget);
		}
		/// <summary>
		/// Renders the given material into the given render target using a full-screen quad.
		/// Assumes the material uses a shader generated from a Graph.
		/// Optionally copies the resulting texture data into a Texture2D for further processing.
		/// </summary>
		/// <param name="rendTarget">
		/// If set to "null", the noise will be rendered onto the screen.
		/// </param>
		/// <param name="leaveReadable">
		/// Whether to leave the texture data readable on the CPU after the operation.
		/// </param>
		public static void GenerateToTexture(RenderTexture rendTarget, Material noiseMat,
											 Texture2D copyTo = null, bool leaveReadable = false)
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
				copyTo.Apply(true, !leaveReadable);
			}

			//Reset rendering state.
			RenderTexture.active = activeTarget;
		}

        /// <summary>
        /// Renders the given material into the given 3D texture.
        /// </summary>
        /// <param name="leaveTextureReadable">
        /// Whether to let the texture keep a CPU copy of its data on hand for later reading.
        /// </param>
        /// <param name="depth">The depth of the 3D texture (i.e. its size along the Z axis).</param>
        public static void GenerateToTexture(int depth, Material noiseMat, Texture3D copyTo,
										     bool leaveTextureReadable)
        {
			var rendTarget = RenderTexture.GetTemporary(copyTo.width, copyTo.height,
													    16, RenderTextureFormat.ARGB32);
            int width = (rendTarget == null ? Screen.width : rendTarget.width),
                height = (rendTarget == null ? Screen.height : rendTarget.height);

            //For every Z layer in the texture, generate a 2D texture representing that layer.

            Color32[] finalPixels = new Color32[width * height * depth];
            SetUpColorTex(width, height);
            for (int depthI = 0; depthI < depth; ++depthI)
            {
                //Get the UV.z coordinate.
                float uvZ = (float)depthI / depth;
                noiseMat.SetFloat(Param_UVz, uvZ);

                GenerateToTexture(rendTarget, noiseMat, colorTex, true);

                //Copy the resulting data into part of the 3D texture.
                Color32[] layerPixels = colorTex.GetPixels32();
                int pixelOffset = depthI * (width * height);
                for (int pixelI = 0; pixelI < (width * height); ++pixelI)
                    finalPixels[pixelI + pixelOffset] = layerPixels[pixelI];
            }


            //Create the actual texture object.
            copyTo.SetPixels32(finalPixels);
            copyTo.Apply(true, !leaveTextureReadable);

			//Clean up.
			RenderTexture.ReleaseTemporary(rendTarget);
        }

		/// <summary>
		/// Uses the given noise material to generate noise into the given array.
		/// </summary>
		public static void GenerateToArray(float[,] outData, Material noiseMat)
		{
			RenderTexture rendTex = RenderTexture.GetTemporary(outData.GetLength(0), outData.GetLength(1));

			//Generate the noise.
			SetUpColorTex(outData.GetLength(0), outData.GetLength(1));
			GenerateToTexture(rendTex, noiseMat, colorTex, true);

			//Read the noise into the array.
			Color[] cols = colorTex.GetPixels();
			for (int y = 0; y < outData.GetLength(1); ++y)
				for (int x = 0; x < outData.GetLength(0); ++x)
					outData[x, y] = cols[x + (outData.GetLength(0) * y)].r;

			RenderTexture.ReleaseTemporary(rendTex);
		}
        /// <summary>
        /// Uses the given noise material to generate noise into the given array.
        /// </summary>
        public static void GenerateToArray(float[,,] outData, Material noiseMat)
        {
            //Generate the noise.
            SetUpColorTex3(outData.GetLength(0), outData.GetLength(1), outData.GetLength(2));
            GenerateToTexture(outData.GetLength(2), noiseMat, colorTex3, true);

            //Read the noise into the array.
            Color[] cols = colorTex.GetPixels();
            int i = 0;
            for (int z = 0; z < outData.GetLength(2); ++z)
                for (int y = 0; y < outData.GetLength(1); ++y)
                    for (int x = 0; x < outData.GetLength(0); ++x)
                        outData[x, y, z] = cols[i++].r;
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

        private static Texture3D colorTex3 = null;
        private static void SetUpColorTex3(int width, int height, int depth)
        {
            if (colorTex3 == null)
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

                colorTex3 = new Texture3D(width, height, depth, fmt.Value, false);
            }
            else if (colorTex3.width != width || colorTex3.height != height || colorTex3.depth != depth)
            {
                colorTex3 = new Texture3D(width, height, depth, colorTex3.format, false);
            }
        }
	}
}