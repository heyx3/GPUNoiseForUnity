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
		//TODO: Have float box for default value for unused channels.
		
		[MenuItem("GPU Noise/Generate texture with graph")]
		public static void GenerateTexture()
		{
			ScriptableObject.CreateInstance<TextureGenerator>().Show();
		}


		public int X = 512, Y = 512;
		public string SavePath = null,
					  GraphPath = null;

		void OnGUI()
		{
			X = EditorGUILayout.IntField("Width", X);
			Y = EditorGUILayout.IntField("Height", Y);
			
			EditorGUILayout.Space();

			if (GraphPath != null)
			{
				EditorGUILayout.LabelField("Graph: " + Path.GetFileNameWithoutExtension(GraphPath));
			}
			if (GUILayout.Button("Choose graph to use"))
			{
				GraphPath = EditorUtility.OpenFilePanel("Choose the graph shader to use.",
														Application.dataPath,
														GraphUtils.Extension);
			}

			EditorGUILayout.Space();

			if (SavePath != null)
			{
				EditorGUILayout.LabelField("Save to: " + SavePath);
			}
			if (GUILayout.Button("Choose Save Location"))
			{
				SavePath = EditorUtility.SaveFilePanel("Choose where to save the texture.",
													   Application.dataPath, "MyTex.png", "png");
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Generate Texture"))
			{
				//Generate a shader from the graph and have Unity compile it.

				Graph g = GraphUtils.LoadGraph(GraphPath);
				if (g == null)
				{
					return;
				}

				string shaderPath = Path.Combine(Application.dataPath, "gpuNoiseShaderTemp.shader");
				Shader shader = GraphUtils.SaveShader(g, shaderPath, "TempGPUNoiseShader", "rgb", 1.0f);
				if (shader == null)
				{
					return;
				}


				//Render the shader's output into a render texture.

				RenderTexture tex = new RenderTexture(X, Y, 16, RenderTextureFormat.ARGBFloat);
				tex.Create();
				RenderTexture activeTex = RenderTexture.active;
				RenderTexture.active = tex;
				
				Material mat = new Material(shader);
				mat.SetPass(0);
				
				GL.PushMatrix();
				GL.LoadIdentity();
				GL.Viewport(new Rect(0, 0, X, Y));
				GL.Begin(GL.TRIANGLE_STRIP);
				GL.Vertex3(-1.0f, -1.0f, 0.0f);
				GL.Vertex3(1.0f, -1.0f, 0.0f);
				GL.Vertex3(-1.0f, 1.0f, 0.0f);
				GL.Vertex3(1.0f, 1.0f, 0.0f);
				GL.End();
				GL.PopMatrix();


				//Save the contents of the texture.
				Texture2D resultTex = new Texture2D(X, Y, TextureFormat.RGBAFloat, false, true);
				resultTex.ReadPixels(new Rect(0, 0, X, Y), 0, 0);
				try
				{
					File.WriteAllBytes(SavePath, resultTex.EncodeToPNG());
				}
				catch (Exception e)
				{
					Debug.LogError("Unable to save texture to file: " + e.Message);
				}


				//Now that we're finished, clean up.
				RenderTexture.active = activeTex;
				tex.Release();
				AssetDatabase.DeleteAsset(PathUtils.GetRelativePath(shaderPath, "Assets"));
				AssetDatabase.ImportAsset(PathUtils.GetRelativePath(SavePath, "Assets"));

				//Finally, open explorer to show the user the texture.
				if (Application.platform == RuntimePlatform.WindowsEditor)
				{
					System.Diagnostics.Process.Start("explorer.exe",
													 "/select," +
													   PathUtils.FixDirectorySeparators(SavePath));
				}
				else if (Application.platform == RuntimePlatform.OSXEditor)
				{
					try
					{
						System.Diagnostics.Process proc = new System.Diagnostics.Process();
						proc.StartInfo.FileName = "open";
						proc.StartInfo.Arguments = "-n -R \"" +
													PathUtils.FixDirectorySeparators(SavePath) +
													"\"";
						proc.StartInfo.UseShellExecute = false;
						proc.StartInfo.RedirectStandardError = false;
						proc.StartInfo.RedirectStandardOutput = false;
						proc.ErrorDataReceived += (s, a) => Debug.Log(a.Data);
						if (proc.Start())
						{
							proc.BeginErrorReadLine();
							proc.BeginOutputReadLine();
						}
						else
						{
							Debug.LogError("Error opening Finder to show texture file");
						}
					}
					catch (Exception e)
					{
						Debug.LogError("Error opening Finder to show texture file: " + e.Message);
					}
				}
			}
		}
	}
}