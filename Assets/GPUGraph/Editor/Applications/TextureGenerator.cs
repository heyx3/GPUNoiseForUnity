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
	public abstract class TextureGenerator : EditorWindow
	{
		public int SelectedGraphIndex = 0;

		public List<Color> GradientRamp_Colors = null;
		public List<float> GradientRamp_Times = null;


		private List<string> graphPaths = new List<string>();
		private GUIContent[] graphNameOptions;

		private GraphParamCollection gParams;

		private Texture2D previewTex = null;
		private Material previewMat = null;
		private float previewScale = 1.0f;
		private float previewUVz = 0.0f,
					  previewUVzMin = 0.0f,
					  previewUVzMax = 1.0f;


		protected bool HasGraph { get { return graphPaths.Count > 0; } }
		protected string SelectedGraphPath { get { return graphPaths[SelectedGraphIndex]; } }
		protected GraphParamCollection GraphParams { get { return gParams; } }

		protected Graph LoadGraph()
		{
			Graph graph = new Graph(SelectedGraphPath);
			string errMsg = graph.Load();
			if (errMsg.Length > 0)
			{
				Debug.LogError("Error loading graph " + graphPaths[SelectedGraphIndex] +
							   ": " + errMsg);
				return null;
			}

			return graph;
		}
		protected Gradient MakeGradient()
		{
			Gradient gradient = new Gradient();
			gradient.SetKeys(GradientRamp_Colors.Select((c, i) =>
							     new GradientColorKey(c, GradientRamp_Times[i])).ToArray(),
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
				//Render the gradient ramp to a texture.
				Gradient gradient = MakeGradient();
				Texture2D myRamp = new Texture2D(1024, 1, TextureFormat.RGBA32, false);
				myRamp.wrapMode = TextureWrapMode.Clamp;
				Color[] cols = new Color[myRamp.width];
				for (int i = 0; i < cols.Length; ++i)
					cols[i] = gradient.Evaluate((float)i / (float)(cols.Length - 1));
				myRamp.SetPixels(cols);
				myRamp.Apply(false, true);

				Graph graph = new Graph(graphPaths[SelectedGraphIndex]);
				string errMsg = graph.Load();
				if (errMsg.Length > 0)
				{
					Debug.LogError("Error loading graph " + graphPaths[SelectedGraphIndex] +
								   ": " + errMsg);
					return null;
				}

				Shader shader = ShaderUtil.CreateShaderAsset(graph.GenerateShader(
																 "Graph editor temp shader",
																 "_textureGeneratorWindowGradient"));
				previewMat = new Material(shader);
				previewMat.SetTexture("_textureGeneratorWindowGradient", myRamp);
			}

			//Set parameters.
			gParams.SetParams(previewMat);
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

		//Defined in the same order they're called.
		protected virtual void OnGUI_BelowGraphSelection() { }
		protected virtual void OnGUI_BelowGradientAboveParams() { }
		protected virtual void OnGUI_AboveGenerationButton() { }

		private void OnGUI()
		{
			GUILayout.Space(15.0f);

			//Choose the graph to use.
			GUILayout.BeginHorizontal();
			GUILayout.Label("Graph:");
			int oldIndex = SelectedGraphIndex;
			SelectedGraphIndex = EditorGUILayout.Popup(SelectedGraphIndex, graphNameOptions);
			if (oldIndex != SelectedGraphIndex)
			{
				Graph g = new Graph(graphPaths[SelectedGraphIndex]);
				if (g.Load().Length > 0)
				{
					SelectedGraphIndex = oldIndex;
				}
				else
				{
					gParams = new GraphParamCollection(g);
				}

				GetPreview(true);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10.0f);

			OnGUI_BelowGraphSelection();

			//Choose the color gradient.
			GUILayout.Label("Gradient");
			GUILayout.BeginHorizontal();
			GUILayout.Space(15.0f);
			GUILayout.BeginVertical();
			for (int i = 0; i < GradientRamp_Colors.Count; ++i)
			{
				GUILayout.BeginHorizontal();

				//Edit the color value.
				EditorGUI.BeginChangeCheck();
				GradientRamp_Colors[i] = EditorGUILayout.ColorField(GradientRamp_Colors[i]);
				if (i > 0)
					GradientRamp_Times[i] = EditorGUILayout.Slider(GradientRamp_Times[i],
																   0.0f, 1.0f);
				if (EditorGUI.EndChangeCheck())
					GetPreview(true);

				//Button to insert a new element.
				if (i > 0 && GUILayout.Button("+"))
				{
					GradientRamp_Colors.Insert(i, GradientRamp_Colors[i]);
					GradientRamp_Times.Insert(i, GradientRamp_Times[i] - 0.00000001f);

					GetPreview(true);
				}
				//Button to remove this element.
				if (i > 0 && GradientRamp_Colors.Count > 2 && GUILayout.Button("-"))
				{
					GradientRamp_Colors.RemoveAt(i);
					GradientRamp_Times.RemoveAt(i);
					i -= 1;

					GetPreview(true);
				}
				GUILayout.EndHorizontal();

				//Make sure elements are in order.
				if (i > 0 && GradientRamp_Times[i] < GradientRamp_Times[i - 1])
				{
					GradientRamp_Times[i] = GradientRamp_Times[i - 1] + 0.000001f;
					GetPreview(true);
				}
				else if (i < GradientRamp_Colors.Count - 1 &&
						 GradientRamp_Times[i] > GradientRamp_Times[i + 1])
				{
					GradientRamp_Times[i] = GradientRamp_Times[i + 1] - 0.00001f;
					GetPreview(true);
				}
			}
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUILayout.Space(15.0f);

			OnGUI_BelowGradientAboveParams();

			//Edit the graph's parameters.
			GUILayout.Label("Graph Parameters:");
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical();
			{
				if (graphPaths.Count > 0 && gParams.ParamEditorGUI())
					GetPreview(false);
			}
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(10.0f);

			OnGUI_AboveGenerationButton();

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

					//NOTE: There is a unity bug that makes the preview texture flicker.
					//Nothing I can do about it.
					//https://issuetracker.unity3d.com/issues/a-texture-drawn-from-a-custom-propertydrawer-is-sometimes-not-drawn
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
		}

		private float Smoothstep(float t)
		{
			return (t * t * (3.0f - (2.0f * t)));
		}
		private float SmoothstepInv(float t)
		{
			//From https://stackoverflow.com/questions/28740544/inverted-smoothstep
			return t + (t - ((t * t * (3.0f - (2.0f * t)))));
		}
	}
}