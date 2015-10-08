using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using GPUNoise;


namespace GPUNoise.Applications
{
	public class TextureGenerator : EditorWindow
	{
		//TODO: Make things easier for the user by showing a dropdown of all graphs inside the Assets folder.
		//TODO: Have checkboxes for whether to use Red, Green, Blue, and Alpha channels in the texture.
		
		[MenuItem("GPU Noise/Generate texture with graph")]
		public static void GenerateTexture()
		{
			ScriptableObject.CreateInstance<TextureGenerator>().Show();
		}


		public int X = 512, Y = 512;
		public string SavePath = "MyTex.bmp",
					  GraphPath = null;

		void OnGUI()
		{
			X = EditorGUILayout.IntField("Width", X);
			Y = EditorGUILayout.IntField("Height", Y);
			if (GraphPath != null)
			{
				EditorGUILayout.LabelField("Graph: " + GraphPath);
			}
			if (GUILayout.Button("Choose graph to use"))
			{
				GraphPath = EditorUtility.OpenFilePanel("Choose the graph shader to use.",
														Application.dataPath,
														GraphSaveLoad.Extension);
			}
			if (GUILayout.Button("Choose Save Location"))
			{
				SavePath = EditorUtility.SaveFilePanel("Choose where to save the texture.",
													   Application.dataPath, "MyTex.bmp", ".bmp");
			}
			if (GUILayout.Button("Generate Texture"))
			{
				//Generate a shader from the graph and have Unity compile it.
				Graph g = GraphSaveLoad.LoadGraph(GraphPath);
				if (g == null)
				{
					return;
				}

				string shaderPath = Path.Combine(Application.dataPath, "gpuNoiseShaderTemp.shader");
				Shader shader = GraphSaveLoad.SaveShader(g, shaderPath, "TempGPUNoiseShader");
				if (shader == null)
				{
					return;
				}

				//Create a material and render into a render texture.
				//TODO: Implement. Use this: http://forum.unity3d.com/threads/creating-a-totally-custom-scene-editor-in-the-editor.118065/#post-791686
				//Material m = new Material(shader);

				//Now that we're finished, delete the shader.
				File.Delete(shaderPath);
			}
		}
	}
}