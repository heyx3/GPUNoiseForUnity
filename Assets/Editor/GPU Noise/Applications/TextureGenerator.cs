using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using GPUNoise;


namespace GPUNoise.Applications
{
	public class TextureGenerator : EditorWindow
	{
		[MenuItem("GPU Noise/Generate Texture")]
		public static void GenerateTexture()
		{
			ScriptableObject.CreateInstance<TextureGenerator>().Show();
		}


		public int X = 512, Y = 512;
		public bool UseRed = true,
					UseGreen = true,
					UseBlue = true,
					UseAlpha = false;
		public float UnusedColor = 1.0f;
		public string SavePath = null;
		public int SelectedGraphIndex = 0;

		private List<string> graphPaths = new List<string>();
		private GUIContent[] graphNameOptions;
		

		private GraphParamCollection gParams;


		void OnEnable()
		{
			graphPaths.Clear();
			graphPaths = GraphUtils.GetAllGraphsInProject();

			Func<string, GUIContent> selector = (gp => new GUIContent(Path.GetFileNameWithoutExtension(gp), gp));
			graphNameOptions = graphPaths.Select(selector).ToArray();

			this.titleContent = new GUIContent("Tex Gen");
			this.minSize = new Vector2(200.0f, 250.0f);

			gParams = new GraphParamCollection();

			SelectedGraphIndex = 0;
			if (graphPaths.Count > 0)
			{
				Graph g = GraphUtils.LoadGraph(graphPaths[SelectedGraphIndex]);
				if (g != null)
				{
					gParams = new GraphParamCollection(g);
				}
			}
		}

		void OnGUI()
		{
			X = EditorGUILayout.IntField("Width", X);
			Y = EditorGUILayout.IntField("Height", Y);
			
			EditorGUILayout.Space();
			

			UseRed = GUILayout.Toggle(UseRed, "Use Red?");
			UseGreen = GUILayout.Toggle(UseGreen, "Use Green?");
			UseBlue = GUILayout.Toggle(UseBlue, "Use Blue?");
			UseAlpha = GUILayout.Toggle(UseAlpha, "Use Alpha?");
			if (!UseRed && !UseGreen && !UseBlue && !UseAlpha)
			{
				UseRed = true;
			}
			
			UnusedColor = EditorGUILayout.FloatField("Unused color value", UnusedColor);

			EditorGUILayout.Space();


			int oldIndex = SelectedGraphIndex;
			SelectedGraphIndex = EditorGUILayout.Popup(SelectedGraphIndex, graphNameOptions);
			if (oldIndex != SelectedGraphIndex)
			{
				Graph g = GraphUtils.LoadGraph(graphPaths[SelectedGraphIndex]);
				if (g == null)
				{
					SelectedGraphIndex = oldIndex;
				}
				else
				{
					gParams = new GraphParamCollection(g);
				}
			}

			EditorGUILayout.Space();


			gParams.ParamEditorGUI();

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
				//Load the graph.
				Graph g = GraphUtils.LoadGraph(graphPaths[SelectedGraphIndex]);
				if (g == null)
				{
					return;
				}

				gParams.OverwriteParamValues(g);

				//Render the noise to a texture.
				System.Text.StringBuilder components = new System.Text.StringBuilder();
				if (UseRed)
				{
					components.Append("r");
				}
				if (UseGreen)
				{
					components.Append("g");
				}
				if (UseBlue)
				{
					components.Append("b");
				}
				if (UseAlpha)
				{
					components.Append("a");
				}
				Texture2D tex = GraphUtils.RenderToTexture(g, X, Y, components.ToString(), UnusedColor);
				if (tex == null)
				{
					return;
				}

				try
				{
					File.WriteAllBytes(SavePath, tex.EncodeToPNG());
				}
				catch (Exception e)
				{
					Debug.LogError("Unable to save texture to file: " + e.Message);
				}


				//Now that we're finished, clean up.
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