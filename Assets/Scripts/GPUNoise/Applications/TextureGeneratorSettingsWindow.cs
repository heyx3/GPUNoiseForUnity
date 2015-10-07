using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


namespace GPUNoise
{
	public class TextureGeneratorOptionsWindow : EditorWindow
	{
		//TODO: Make things easier for the user by showing a dropdown of all graphs inside the Assets folder.
		
		private static string tempShaderName = "gpuNoiseShaderTemp.shader";


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

				string shaderPath = Path.Combine(Application.dataPath, tempShaderName);
				Shader shader = null;
				try
				{
					File.WriteAllText(shaderPath, g.GenerateShader("TempGPUNoiseShader"));
					shader = Resources.Load("TempGPUNoiseShader") as Shader;
					string path = AssetDatabase.GetAssetPath(shader);
					AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				}
				catch (Exception e)
				{
					Debug.LogError("Error generating/writing/compiling shader: " + e.Message);
					return;
				}

				if (shader == null)
				{
					return;
				}

				//Create a material and render into a render texture.
				//TODO: Implement.
				//Material m = new Material(shader);

				//Now that we're finished, delete the shader.
				File.Delete(shaderPath);
			}
		}
	}
}