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
	public enum ColorModes
	{
		Gradient,
		Make2DVec,
		Custom
	}

	[Serializable]
	public abstract class TextureGenerator : EditorWindow
	{
		[Serializable]
		private class GraphSelection
		{
			public int Index = 0;
			public GraphParamCollection Params;

			/// <summary>
			/// Does the GUI for this graph.
			/// Returns what changed.
			/// </summary>
			public ChangeTypes DoGUILayout(string label, List<string> graphPaths, GUIContent[] graphOptions)
			{
				var changes = ChangeTypes.None;
				
				GUILayout.BeginHorizontal();
				GUILayout.Label(label);
				int oldIndex = Index;
				Index = EditorGUILayout.Popup(oldIndex, graphOptions);
				if (oldIndex != Index)
				{
					Graph g = new Graph(graphPaths[Index]);
					if (g.Load().Length > 0)
					{
						Index = oldIndex;
					}
					else
					{
						Params = new GraphParamCollection(g);
						changes = ChangeTypes.Everything;
					}
				}
				GUILayout.EndHorizontal();

				GUILayout_TabIn(25.0f);

				if (Params != null && Params.ParamEditorGUI() && changes != ChangeTypes.Everything)
					changes = ChangeTypes.Params;

				GUILayout_TabOut(25.0f);

				return changes;
			}
			
			/// <summary>
			/// The different aspects of this graph selection that can change.
			/// </summary>
			public enum ChangeTypes
			{
				None, Params, Everything
			}
		}


		public ColorModes OutputMode = ColorModes.Gradient;
	
		public List<Color> Output_Gradient_Colors = new List<Color>()
			{ Color.black, Color.white };
		public List<float> Output_Gradient_Times = new List<float>()
			{ 0, 1 };

		public bool Output_AngleTo2DVec_Pack01 = true;

		public string Output_Custom_CodeBody = "//Put 3 white noise values in the GBA channels\n//    based on the R channel.\n" +
											   "return float4(graphResult1, hashTo3(graphResult1));";


		private List<GraphSelection> chosenGraphs = new List<GraphSelection>();
		private bool isLoaded = false;
	
		private List<string> graphPaths = new List<string>();
		private GUIContent[] graphNameOptions;
		
		private Texture2D previewTex = null;
		private Material previewMat = null;
		private float previewScale = 1.0f;
		private float previewUVz = 0.0f,
					  previewUVzMin = 0.0f,
					  previewUVzMax = 1.0f;

		[SerializeField]
		private Vector2 scrollPos = Vector2.zero;

		  
		protected bool HasGraph { get { return graphPaths.Count > 0; } }

		protected Graph[] LoadGraphs()
		{
			var graphs = new Graph[chosenGraphs.Count];

			for (int i = 0; i < graphs.Length; ++i)
			{
				var path = graphPaths[chosenGraphs[i].Index];
				graphs[i] = new Graph(path);
				string errMsg = graphs[i].Load();
				if (errMsg.Length > 0)
				{
					Debug.LogError("Error loading graph " + path + ": " + errMsg);
					return null;
				}
			}

			return graphs;
		}
		protected GraphParamCollection GetParams(int graphI)
		{
			return chosenGraphs[graphI].Params;
		}

		protected Gradient MakeGradient()
		{
			Gradient gradient = new Gradient();
			gradient.SetKeys(Output_Gradient_Colors.Select(
							     (c, i) => new GradientColorKey(c, Output_Gradient_Times[i]))
												   .ToArray(),
							 new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f) });
			return gradient;
		}


		/// <summary>
		/// Properly updates the preview texture for this window.
		/// Returns it in case anybody wants it (returns null if there was a problem loading the graph).
		/// </summary>
		public Texture2D GetPreview(bool regenerateShader)
		{
			//Regenerate shader.
			if (regenerateShader || previewMat == null)
			{
				var graphs = LoadGraphs();
				if (graphs == null)
					return null;

				switch (OutputMode)
				{
					case ColorModes.Gradient: {

						//Render the gradient ramp to a texture.
						Gradient gradient = MakeGradient();
						Texture2D myRamp = new Texture2D(1024, 1, TextureFormat.RGBA32, false);
						myRamp.wrapMode = TextureWrapMode.Clamp;
						Color[] cols = new Color[myRamp.width];
						for (int i = 0; i < cols.Length; ++i)
							cols[i] = gradient.Evaluate((float)i / (float)(cols.Length - 1));
						myRamp.SetPixels(cols);
						myRamp.Apply(false, true);

						//Generate a shader that uses the gradient.
						var shader = ShaderUtil.CreateShaderAsset(
									     graphs[0].GenerateShader("Graph editor temp shader",
															      "_textureGeneratorWindowGradient"));
						previewMat = new Material(shader);
						previewMat.SetTexture("_textureGeneratorWindowGradient", myRamp);
					} break;

					case ColorModes.Make2DVec: {
						var shaderStr = Graph.GenerateShader(
								"GraphEditorTempShader", graphs,
								(fragBody) =>
								{
									fragBody.Append(@"
	const float PI_2 = 3.14159265359 * 2.0;
	float angle = PI_2 * graphResult1,
		  magnitude = graphResult2;
	float2 dir = magnitude * float2(cos(angle), sin(angle));");
									if (Output_AngleTo2DVec_Pack01)
										fragBody.AppendLine(@"
    dir = 0.5 + (0.5 * dir);");
									fragBody.AppendLine(@"
	return float4(dir, 0, 1);");
								});
						var shader = UnityEditor.ShaderUtil.CreateShaderAsset(shaderStr);
						previewMat = new Material(shader);
					} break;

					case ColorModes.Custom: {
						var shaderStr = Graph.GenerateShader(
							"GraphEditorTempShader", graphs,
							(fragBody) =>
							{
								fragBody.AppendLine(Output_Custom_CodeBody);
							});
						var shader = UnityEditor.ShaderUtil.CreateShaderAsset(shaderStr);
						previewMat = new Material(shader);
					} break;

					default: throw new NotImplementedException(OutputMode.ToString());
				}
			}

			//Set parameters.
			for (int graphI = 0; graphI < chosenGraphs.Count; ++graphI)
			{
				var paramPrefix = (chosenGraphs.Count > 1) ?
								      ("p" + (graphI + 1)) :
									  "";
				chosenGraphs[graphI].Params.SetParams(previewMat, paramPrefix);
			}
			previewMat.SetFloat(GraphUtils.Param_UVz, previewUVz);

			//Generate.
			GeneratePreview(ref previewTex, previewMat);
			return previewTex;
		}

		protected virtual void OnEnable()
		{
			graphPaths.Clear();
			graphPaths = GraphEditorUtils.GetAllGraphsInProject();

			Func<string, GUIContent> selector = (gp => new GUIContent(Path.GetFileNameWithoutExtension(gp), gp));
			graphNameOptions = graphPaths.Select(selector).ToArray();

			if (!isLoaded)
			{
				isLoaded = true;
				chosenGraphs.Clear();
				if (graphPaths.Count > 0)
				{
					var graph = new Graph(graphPaths[0]);
					if (graph.Load().Length == 0)
						chosenGraphs.Add(new GraphSelection() { Index = 0, Params = new GraphParamCollection(graph) });
				}
			}
		}
		protected virtual void OnFocus()
		{
			OnEnable();
		}

		/// <summary>
		/// Generates a preview into the given texture with the given noise material.
		/// </summary>
		/// <param name="outTex">
		/// The texture to generate into.
		/// NOTE: the child class is responsible for making sure the texture exists and is the right size.
		/// </param>
		protected abstract void GeneratePreview(ref Texture2D outTex, Material noiseMat);
		/// <summary>
		/// Generates the full texture to a file.
		/// </summary>
		protected abstract void GenerateTexture();

		protected virtual void DoCustomGUI() { }


		private void OnGUI()
		{
			GUILayout.Space(15.0f);
		
			scrollPos = GUILayout.BeginScrollView(scrollPos);

			//Make sure we have enough graphs.
			int nGraphs = -1;
			switch (OutputMode)
			{
				case ColorModes.Custom:
					nGraphs = EditorGUILayout.DelayedIntField("# Graphs", chosenGraphs.Count);
				break;

				case ColorModes.Gradient:
					nGraphs = 1;
				break;

				case ColorModes.Make2DVec:
					nGraphs = 2;
				break;

				default: throw new NotImplementedException(OutputMode.ToString());
			}
			while (nGraphs > chosenGraphs.Count)
				chosenGraphs.Add(new GraphSelection());
			while (nGraphs < chosenGraphs.Count)
				chosenGraphs.RemoveAt(chosenGraphs.Count - 1);

			for (int graphI = 0; graphI < nGraphs; ++graphI)
			{
				GUILayout_TabIn(25.0f);

				var graph = chosenGraphs[graphI];
				var changes = graph.DoGUILayout("Graph " + (graphI + 1),
												graphPaths, graphNameOptions);
				
				switch (changes)
				{
					case GraphSelection.ChangeTypes.None:
						break;
					case GraphSelection.ChangeTypes.Params:
						GetPreview(false);
						break;
					case GraphSelection.ChangeTypes.Everything:
						GetPreview(true);
						break;

					default: throw new NotImplementedException(changes.ToString());
				}

				GUILayout_TabOut();

				GUILayout.Space(25.0f);
			}

			//Do color-mode-specific UI.
			OutputMode = (ColorModes)EditorGUILayout.EnumPopup("Mode", OutputMode);
			switch (OutputMode)
			{
				case ColorModes.Gradient:
					//Choose the color gradient.
					GUILayout.Label("Gradient");
					GUILayout.BeginHorizontal();
					GUILayout.Space(15.0f);
					GUILayout.BeginVertical();
					for (int i = 0; i < Output_Gradient_Colors.Count; ++i)
					{
						GUILayout.BeginHorizontal();

						//Edit the color value.
						EditorGUI.BeginChangeCheck();
						Output_Gradient_Colors[i] = EditorGUILayout.ColorField(Output_Gradient_Colors[i]);
						if (i > 0)
							Output_Gradient_Times[i] = EditorGUILayout.Slider(Output_Gradient_Times[i],
																		   0.0f, 1.0f);
						if (EditorGUI.EndChangeCheck())
							GetPreview(true);

						//Button to insert a new element.
						if (i > 0 && GUILayout.Button("+"))
						{
							Output_Gradient_Colors.Insert(i, Output_Gradient_Colors[i]);
							Output_Gradient_Times.Insert(i, Output_Gradient_Times[i] - 0.00000001f);

							GetPreview(true);
						}
						//Button to remove this element.
						if (i > 0 && Output_Gradient_Colors.Count > 2 && GUILayout.Button("-"))
						{
							Output_Gradient_Colors.RemoveAt(i);
							Output_Gradient_Times.RemoveAt(i);
							i -= 1;

							GetPreview(true);
						}
						GUILayout.EndHorizontal();

						//Make sure elements are in order.
						if (i > 0 && Output_Gradient_Times[i] < Output_Gradient_Times[i - 1])
						{
							Output_Gradient_Times[i] = Output_Gradient_Times[i - 1] + 0.000001f;
							GetPreview(true);
						}
						else if (i < Output_Gradient_Colors.Count - 1 &&
								 Output_Gradient_Times[i] > Output_Gradient_Times[i + 1])
						{
							Output_Gradient_Times[i] = Output_Gradient_Times[i + 1] - 0.00001f;
							GetPreview(true);
						}
					}
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
				break;

				case ColorModes.Make2DVec:
					GUILayout.BeginHorizontal();
					GUILayout.Space(30.0f);
					GUILayout.BeginVertical();
					EditorGUI.BeginChangeCheck();
					Output_AngleTo2DVec_Pack01 = EditorGUILayout.Toggle("Pack into range [0,1]",
																		Output_AngleTo2DVec_Pack01);
					if (EditorGUI.EndChangeCheck())
						GetPreview(true);
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
				break;

				case ColorModes.Custom:
					GUILayout.BeginHorizontal();
					GUILayout.Space(30.0f);
					GUILayout.BeginVertical();

					if (GUILayout.Button("Regenerate preview"))
						GetPreview(true);
					string args = "";
					for (int graphI = 0; graphI < chosenGraphs.Count; ++graphI)
					{
						if (graphI > 0)
							args += ", ";
						args += "float graphResult" + (graphI + 1);
					}
					GUILayout.Label("float4 getTexOutputColor(" + args + ")");
					GUILayout.Label("{");
					
					GUILayout.BeginHorizontal();
					GUILayout.Space(30.0f);
					GUILayout.BeginVertical();
					Output_Custom_CodeBody = EditorGUILayout.TextArea(Output_Custom_CodeBody);
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();

					GUILayout.Label("}");

					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
				break;

				default: throw new NotImplementedException(OutputMode.ToString());
			}

			GUILayout.Space(15.0f);

			DoCustomGUI();

			GUILayout.Space(15.0f);
			
			//Show the preview texture.
			if (previewTex != null)
			{
				//Preview scale slider.
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Preview Scale");

					//Use a nonlinear scale for the slider.
					float t = Mathf.Log10(previewScale);
					float resultT = GUILayout.HorizontalSlider(t, -2.0f, 2.0f,
															   GUILayout.Width(position.width - 40.0f));
					previewScale = Mathf.Pow(10.0f, resultT);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					//Slider for preview UV z.
					GUILayout.BeginVertical();
					{
						float oldZ = previewUVz;

						GUILayout.Label("Z");
						previewUVzMax = EditorGUILayout.FloatField(previewUVzMax, GUILayout.Width(20.0f));
						previewUVz = GUILayout.VerticalSlider(previewUVz, previewUVzMin, previewUVzMax,
															  GUILayout.Height(previewTex.height * previewScale - (15.0f * 3)));
						previewUVzMin = EditorGUILayout.FloatField(previewUVzMin, GUILayout.Width(20.0f));

						if (oldZ != previewUVz)
							GetPreview(false);
					}
					GUILayout.EndVertical();

					Rect texPos = EditorGUILayout.GetControlRect(GUILayout.Width(previewTex.width * previewScale),
																 GUILayout.Height(previewTex.height * previewScale));
					EditorGUI.DrawPreviewTexture(texPos, previewTex);

					GUILayout.FlexibleSpace();
				}
				GUILayout.EndHorizontal();
			}

			//If a graph is selected, display a button to generate the texture.
			if (graphPaths.Count > 0)
			{
				if (GUILayout.Button("Generate Texture"))
					GenerateTexture();
			}
			else
			{
				GUILayout.Space(15.0f);
				GUILayout.Label("No graph files detected in the project!");
			}

			GUILayout.EndScrollView();
		}
		
		private static void GUILayout_TabIn(float spaceBefore)
		{
			GUILayout.BeginHorizontal();
			if (spaceBefore < 0.0f)
				GUILayout.FlexibleSpace();
			else if (spaceBefore > 0.0f)
				GUILayout.Space(spaceBefore);
			GUILayout.BeginVertical();
		}
		private static void GUILayout_TabOut(float spaceAfter = 0.0f)
		{
			GUILayout.EndVertical();
			if (spaceAfter < 0.0f)
				GUILayout.FlexibleSpace();
			else if (spaceAfter > 0.0f)
				GUILayout.Space(spaceAfter);
			GUILayout.EndHorizontal();
		}
	}
}