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
	public class Texture2DGenerator : TextureGenerator
	{
		[MenuItem("Assets/GPU Graph/Generate 2D Texture", false, 4)]
		public static void ShowWindow()
		{
			ScriptableObject.CreateInstance<Texture2DGenerator>().Show();
		}


		public int X = 128,
				   Y = 128;


		protected override void OnEnable()
		{
			base.OnEnable();

			titleContent = new GUIContent("2D Tex");
			minSize = new Vector2(200.0f, 300.0f + Y);

			if (HasGraph)
				GetPreview(true);
		}

		protected override void DoCustomGUI()
		{
			EditorGUI.BeginChangeCheck();
			{
				X = EditorGUILayout.DelayedIntField("Width", X);
				Y = EditorGUILayout.DelayedIntField("Height", Y);
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
			string savePath = EditorUtility.SaveFilePanel("Choose where to save the texture.",
														  Application.dataPath, "MyTex.png", "png");
			if (savePath.Length == 0)
				return;

			//Write out the texture as a PNG.
			Texture2D noiseTex = GetPreview(false);
			try
			{
				File.WriteAllBytes(savePath, noiseTex.EncodeToPNG());
			}
			catch (Exception e)
			{
				Debug.LogError("Unable to save texture to file: " + e.Message);
			}

			//Finally, open explorer to show the user the texture.
			EditorUtility.RevealInFinder(StringUtils.FixDirectorySeparators(savePath));
		}
	}
}