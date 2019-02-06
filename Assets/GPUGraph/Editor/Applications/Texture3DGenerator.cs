using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using GPUGraph;


namespace GPUGraph.Applications
{
	[Serializable]
	public class Texture3DGenerator : TextureGenerator
	{
		[MenuItem("Assets/GPU Graph/Generate 3D Texture", false, 4)]
		public static void ShowWindow()
		{
			ScriptableObject.CreateInstance<Texture3DGenerator>().Show();
		}


		public int X = 128, Y = 128, Z = 128;
		public TextureFormat Format = TextureFormat.ARGB32;
		public FilterMode Filtering = FilterMode.Bilinear;
		public TextureWrapMode Wrapping = TextureWrapMode.Repeat;
		public bool UseMipmaps = false,
					LeaveReadable = true;

		public bool GenerateNormals = true;
		public int Normals_X = 128, Normals_Y = 128, Normals_Z = 128;
		public TextureFormat Normals_Format = TextureFormat.ARGB32;
		public FilterMode Normals_Filtering = FilterMode.Bilinear;
		public TextureWrapMode Normals_Wrapping = TextureWrapMode.Repeat;
		public bool Normals_UseMipmaps = false,
					Normals_LeaveReadable = true;


		protected override void OnEnable()
		{
			base.OnEnable();

			titleContent = new GUIContent("Texture Gen");
			minSize = new Vector2(200.0f, 450.0f + Y);

			if (HasGraph)
				GetPreview(true);
		}

		protected override void DoCustomGUI()
		{
			EditorGUI.BeginChangeCheck();
			{
				EditTextureGUI(ref X, ref Y, ref Z, ref Format, ref Filtering, ref Wrapping,
							   ref UseMipmaps, ref LeaveReadable);

				GUILayout.Space(7.5f);

				GenerateNormals = GUILayout.Toggle(GenerateNormals, "Also Generate Normals");
				if (GenerateNormals)
				{
					EditTextureGUI(ref Normals_X, ref Normals_Y, ref Normals_Z, ref Normals_Format,
								   ref Normals_Filtering, ref Normals_Wrapping,
								   ref Normals_UseMipmaps, ref Normals_LeaveReadable);
				}
			}
			if (EditorGUI.EndChangeCheck())
				GetPreview(false);

			GUILayout.Space(15.0f);
		}

		protected override void GeneratePreview(ref Texture2D outTex, Material noiseMat)
		{
			if (outTex == null || outTex.width != X || outTex.height != Y)
			{
				outTex = new Texture2D(X, Y, TextureFormat.ARGB32, false);
				outTex.wrapMode = TextureWrapMode.Clamp;
				outTex.filterMode = FilterMode.Point;
			}

			GraphUtils.GenerateToTexture(RenderTexture.GetTemporary(outTex.width, outTex.height),
										 noiseMat, outTex, true);
		}
		protected override void GenerateTexture()
		{
			string savePath = EditorUtility.SaveFilePanelInProject(
								  "Save Noise Texture", "MyTex.asset", "asset",
								  "Choose where to save the noise texture.", Application.dataPath);
			if (savePath.Length > 0)
			{
				string normalsSavePath = null;
				if (GenerateNormals)
				{
					normalsSavePath = EditorUtility.SaveFilePanelInProject(
									      "Save Normal Texture",
										  "Normal_" + Path.GetFileName(savePath), "asset",
										  "Choose where to save the normal texture.",
										  Path.GetDirectoryName(savePath));
				}

				if (GenerateTextures(savePath, normalsSavePath))
					EditorUtility.RevealInFinder(savePath);
			}
		}

        private void EditTextureGUI(ref int sizeX, ref int sizeY, ref int sizeZ,
                                    ref TextureFormat format,
									ref FilterMode filtering, ref TextureWrapMode wrapping,
                                    ref bool useMips, ref bool leaveReadable)
        {
            sizeX = EditorGUILayout.IntField("Width", sizeX);
            sizeY = EditorGUILayout.IntField("Height", sizeY);
            sizeZ = EditorGUILayout.IntField("Depth", sizeZ);

            GUILayout.Space(5.0f);

            format = (TextureFormat)EditorGUILayout.EnumPopup("Format", format);
			filtering = (FilterMode)EditorGUILayout.EnumPopup("Filtering", filtering);
			wrapping = (TextureWrapMode)EditorGUILayout.EnumPopup("Wrapping", wrapping);

            GUILayout.Space(5.0f);

            useMips = EditorGUILayout.Toggle("Use Mipmaps?", useMips);
            leaveReadable = EditorGUILayout.Toggle("Leave Readable?", leaveReadable);
        }
        /// <summary>
        /// Generates both textures and saves them to the given file paths.
        /// Returns whether the operation succeeded.
        /// </summary>
        private bool GenerateTextures(string texPath, string normalTexPath = null)
        {
            //The base data set should be large enough for both textures.
            int baseDataSizeX = X,
                baseDataSizeY = Y,
                baseDataSizeZ = Z;
            if (GenerateNormals)
            {
                baseDataSizeX = Math.Max(baseDataSizeX, Normals_X);
                baseDataSizeY = Math.Max(baseDataSizeY, Normals_Y);
                baseDataSizeZ = Math.Max(baseDataSizeZ, Normals_Z);
            }

			//TODO: Support other modes.
			if (OutputMode != ColorModes.Gradient)
				throw new NotImplementedException("Don't support non-Gradient modes yet!");

			var graphs = LoadGraphs();
			if (graphs == null)
				return false;
			var graph = graphs[0];

			GraphParamCollection graphParams = GetParams(0);

            //Generate the initial data.
            float[,,] baseData = GraphEditorUtils.GenerateToArray(graph, graphParams,
                                                                  baseDataSizeX,
                                                                  baseDataSizeY,
                                                                  baseDataSizeZ);


            //Generate the value texture.

            float[,,] valueTexData = baseData.ResampleFull(Mathf.Lerp, X, Y, Z);
			Gradient colorGradient = MakeGradient();
            Color[] valueTexPixels = new Color[X * Y * Z];

            for (int z = 0; z < Z; ++z)
            {
                for (int y = 0; y < Y; ++y)
                {
                    for (int x = 0; x < X; ++x)
                    {
                        float value = valueTexData[x, y, z];
                        int index = x +
                                    (y * X) +
                                    (z * X * Y);

						valueTexPixels[index] = colorGradient.Evaluate(value);
                    }
                }
            }

            Texture3D valueTex = new Texture3D(X, Y, Z, TextureFormat.ARGB32, false);
			valueTex.wrapMode = Wrapping;
			valueTex.filterMode = Filtering;
            valueTex.SetPixels(valueTexPixels);
            valueTex.Apply(UseMipmaps, !LeaveReadable);
            AssetDatabase.CreateAsset(valueTex, StringUtils.GetRelativePath(texPath, "Assets"));


            //Generate the normals texture.

            if (normalTexPath == null)
                return true;

            float[,,] normalsValueData = baseData.ResampleFull(Mathf.Lerp,
                                                               Normals_X, Normals_Y, Normals_Z);
            Color[] normalsPixels = new Color[Normals_X * Normals_Y * Normals_Z];

            for (int z = 0; z < Normals_Z; ++z)
            {
                for (int y = 0; y < Normals_Y; ++y)
                {
                    for (int x = 0; x < Normals_X; ++x)
                    {
                        //The first digit is the X, the second is the Y, the third is the Z.
                        float _000, _100, _010, _110, _001, _101, _011, _111;
                        normalsValueData.Sample(x, y, z, true,
                                                out _000, out _100, out _010, out _110,
                                                out _001, out _101, out _011, out _111);

                        Vector3 gradient = new Vector3();
                        gradient.x = (_100 - _000) +
                                     (_110 - _010) +
                                     (_101 - _001) +
                                     (_111 - _011);
                        gradient.y = (_010 - _000) +
                                     (_110 - _100) +
                                     (_011 - _001) +
                                     (_111 - _101);
                        gradient.z = (_001 - _000) +
                                     (_101 - _100) +
                                     (_011 - _010) +
                                     (_111 - _110);

                        //Normalize.
                        if (gradient == Vector3.zero)
                            gradient = Vector3.up;
                        else
                            gradient = gradient.normalized;

                        //Pack into a color.
                        int index = x + (y * Normals_X) + (z * Normals_X * Normals_Y);
                        normalsPixels[index] = new Color(0.5f + (0.5f * gradient.x),
                                                         0.5f + (0.5f * gradient.y),
                                                         0.5f + (0.5f * gradient.z));
                    }
                }
            }

            Texture3D normalsTex = new Texture3D(Normals_X, Normals_Y, Normals_Z,
                                                 Normals_Format, Normals_UseMipmaps);
			normalsTex.wrapMode = Normals_Wrapping;
			normalsTex.filterMode = Normals_Filtering;
            normalsTex.SetPixels(normalsPixels);
            normalsTex.Apply(Normals_UseMipmaps, !Normals_LeaveReadable);
            AssetDatabase.CreateAsset(normalsTex, StringUtils.GetRelativePath(normalTexPath, "Assets"));


            return true;
        }
	}
}