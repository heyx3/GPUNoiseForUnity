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
	public class TextureGenerator : EditorWindow
	{
		[MenuItem("GPU Noise/Generate Texture")]
		public static void GenerateTexture()
		{
			ScriptableObject.CreateInstance<TextureGenerator>().Show();
		}


		public int X = 512, Y = 512;
		public int SelectedGraphIndex = 0;

		public List<Color> GradientRamp_Colors = null;
		public List<float> GradientRamp_Times = null;

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

			GradientRamp_Colors = new List<Color>() { Color.black, Color.white };
			GradientRamp_Times = new List<float>() { 0.0f, 1.0f };

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

			GUILayout.Label("Gradient");
			GUILayout.BeginHorizontal();
			GUILayout.Space(15.0f);
			GUILayout.BeginVertical();
			for (int i = 0; i < GradientRamp_Colors.Count; ++i)
			{
				GUILayout.BeginHorizontal();
				GradientRamp_Colors[i] = EditorGUILayout.ColorField(GradientRamp_Colors[i]);
				if (i > 0)
					GradientRamp_Times[i] = EditorGUILayout.Slider(GradientRamp_Times[i],
																   0.0f, 1.0f);
				if (i > 0 && GUILayout.Button("+"))
				{
					GradientRamp_Colors.Insert(i, GradientRamp_Colors[i]);
					GradientRamp_Times.Insert(i, GradientRamp_Times[i] - 0.00000001f);
				}
				if (i > 0 && GradientRamp_Colors.Count > 2 && GUILayout.Button("-"))
				{
					GradientRamp_Colors.RemoveAt(i);
					GradientRamp_Times.RemoveAt(i);
					i -= 1;
				}
				GUILayout.EndHorizontal();

				if (i > 0 && GradientRamp_Times[i] < GradientRamp_Times[i - 1])
				{
					GradientRamp_Times[i] = GradientRamp_Times[i - 1] + 0.000001f;
				}
				else if (i < GradientRamp_Colors.Count - 1 &&
						 GradientRamp_Times[i] > GradientRamp_Times[i + 1])
				{
					GradientRamp_Times[i] = GradientRamp_Times[i + 1] - 0.00001f;
				}
			}
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

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

			//Show some GUI elements for changing the parameters.
			if (graphPaths.Count > 0)
				gParams.ParamEditorGUI();

			GUILayout.Space(10.0f);

			//If a graph is selected, display a button to generate the texture.
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

						//Render the gradient ramp to a texture,
						//    then render the graph's noise to a texture.
						Gradient grd = new Gradient();
						grd.SetKeys(GradientRamp_Colors.Select((c, i) =>
										new GradientColorKey(c, GradientRamp_Times[i])).ToArray(),
									new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f) });
						Texture2D tex = GraphEditorUtils.GenerateToTexture(g, new GraphParamCollection(g, gParams),
																		   X, Y, grd);
						if (tex == null)
						{
							return;
						}

						//Write out the texture as a PNG.
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