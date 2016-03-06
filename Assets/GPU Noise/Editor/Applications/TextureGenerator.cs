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
		public int SelectedGraphIndex = 0;

		private List<string> graphPaths = new List<string>();
		private GUIContent[] graphNameOptions;
		

		private GraphParamCollection gParams;


		void OnEnable()
		{
			graphPaths.Clear();
			graphPaths = GraphEditorUtils.GetAllGraphsInProject();

			Func<string, GUIContent> selector = (gp => new GUIContent(Path.GetFileNameWithoutExtension(gp), gp));
			graphNameOptions = graphPaths.Select(selector).ToArray();

			this.titleContent = new GUIContent("Texture Gen");
			this.minSize = new Vector2(200.0f, 270.0f);

			gParams = new GraphParamCollection();

			SelectedGraphIndex = 0;
			if (graphPaths.Count > 0)
			{
				Graph g = new Graph(graphPaths[SelectedGraphIndex]);
				if (g.Load().Length == 0)
				{
					gParams = new GraphParamCollection(g);
				}
			}
		}

		void OnGUI()
		{
			GUILayout.Space(10.0f);

			X = EditorGUILayout.IntField("Width", X);
			Y = EditorGUILayout.IntField("Height", Y);
			
			GUILayout.Space(15.0f);
			

			UseRed = GUILayout.Toggle(UseRed, "Use Red?");
			UseGreen = GUILayout.Toggle(UseGreen, "Use Green?");
			UseBlue = GUILayout.Toggle(UseBlue, "Use Blue?");
			UseAlpha = GUILayout.Toggle(UseAlpha, "Use Alpha?");
			if (!UseRed && !UseGreen && !UseBlue && !UseAlpha)
			{
				UseRed = true;
			}
			
			UnusedColor = EditorGUILayout.FloatField("Unused color value", UnusedColor);
			
			GUILayout.Space(15.0f);


			GUILayout.BeginHorizontal();
			GUILayout.Label("Graph:");
			int oldIndex = SelectedGraphIndex;
			SelectedGraphIndex = EditorGUILayout.Popup(SelectedGraphIndex, graphNameOptions);
			if (oldIndex != SelectedGraphIndex)
			{
				Graph g = new Graph(graphPaths[SelectedGraphIndex]);
				if (g.Load().Length == 0)
				{
					SelectedGraphIndex = oldIndex;
				}
				else
				{
					gParams = new GraphParamCollection(g);
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10.0f);


			if (graphPaths.Count > 0)
			{
				gParams.ParamEditorGUI();
			}

			GUILayout.Space(10.0f);


			if (graphPaths.Count > 0)
			{
				if (GUILayout.Button("Generate Texture"))
				{
					string savePath = EditorUtility.SaveFilePanel("Choose where to save the texture.",
																  Application.dataPath, "MyTex.png", "png");
					if (savePath.Length > 0)
					{
						//Load the graph.
						Graph g = new Graph(graphPaths[SelectedGraphIndex]);
						if (g.Load().Length > 0)
						{
							return;
						}

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
						Texture2D tex = GraphEditorUtils.GenerateToTexture(g, new GraphParamCollection(g, gParams),
																		   X, Y, components.ToString(),
																		   UnusedColor);
						if (tex == null)
						{
							return;
						}

						try
						{
							File.WriteAllBytes(savePath, tex.EncodeToPNG());
						}
						catch (Exception e)
						{
							Debug.LogError("Unable to save texture to file: " + e.Message);
						}


						//Now that we're finished, clean up.
						AssetDatabase.ImportAsset(StringUtils.GetRelativePath(savePath, "Assets"));

						//Finally, open explorer to show the user the texture.
						if (Application.platform == RuntimePlatform.WindowsEditor)
						{
							System.Diagnostics.Process.Start("explorer.exe",
															 "/select," +
															   StringUtils.FixDirectorySeparators(savePath));
						}
						else if (Application.platform == RuntimePlatform.OSXEditor)
						{
							try
							{
								System.Diagnostics.Process proc = new System.Diagnostics.Process();
								proc.StartInfo.FileName = "open";
								proc.StartInfo.Arguments = "-n -R \"" +
															StringUtils.FixDirectorySeparators(savePath) +
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
			else
			{
				GUILayout.Space(15.0f);
				GUILayout.Label("No graph files detected in the project!");
			}
		}
	}
}