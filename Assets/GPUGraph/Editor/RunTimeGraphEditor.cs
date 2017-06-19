using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;


namespace GPUGraph
{
	/// <summary>
	/// Can be used to display a graph field in a custom inspector.
	/// Graphs can't actually be loaded at run-time, so the actual field being edited is a Shader.
	/// This Shader can be utilized in GraphUtils.
	/// </summary>
	[Serializable]
	[CustomPropertyDrawer(typeof(RuntimeGraph))]
	public class RuntimeGraphEditor : PropertyDrawer
	{
		public bool IsEditorNew = true;

		public List<string> AvailableGraphs = null;
		public GUIContent[] AvailableGraphsGUI = null;
		public int CurrentGraph = -1;

		public bool IsFolded = false,
					IsFloatParamListFolded = false,
					IsTex2DParamListFolded = false;


		private const float oneLine = 15.0f,
							smallSpace = 2.0f,
							normalSpace = 4.0f,
							midSpace = 8.0f,
							largeSpace = 16.0f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			RuntimeGraph graph = (RuntimeGraph)fieldInfo.GetValue(property.serializedObject.targetObject);

			if (!IsFolded)
				return oneLine;
			else
			{
				float height = oneLine;

				if (IsFolded)
				{
					if (CurrentGraph < 0)
					{
						height += normalSpace + oneLine + normalSpace + oneLine + normalSpace;
					}
					else
					{
						height += normalSpace + oneLine + normalSpace + oneLine + largeSpace +
									oneLine + normalSpace + oneLine + normalSpace + oneLine + 40.0f;
					}
				}

				if (graph._PreviewTex != null)
				{
					height += oneLine + normalSpace + oneLine + midSpace +
								(graph._PreviewTex.height * graph._PreviewTexScale);
				}

				if (IsFloatParamListFolded && graph.FloatParams != null)
				{
					height += (oneLine + smallSpace) * graph.FloatParams.Count;
				}
				if (IsTex2DParamListFolded && graph.Tex2DParams != null)
				{
					foreach (Tex2DParamKVP tx in graph.Tex2DParams)
						height += tx.Value.height;
				}
				height += oneLine;

				return height;
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			RuntimeGraph graph = (RuntimeGraph)fieldInfo.GetValue(property.serializedObject.targetObject);

			EditorGUI.BeginProperty(position, label, property);

			//Do any necessary initialization.
			if (graph._ShaderFile == null || graph._ShaderFile == "" ||
				!File.Exists(Path.Combine(Application.dataPath, graph._ShaderFile)))
			{
				graph._ShaderFile = GetNewShaderFile();
				IsEditorNew = true;
			}
			if (IsEditorNew)
			{
				IsEditorNew = false;
				RefreshGraphs(graph);
			}


			IsFolded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, oneLine),
										 IsFolded, label);

			if (IsFolded)
			{
				position.x += 20.0f;
				position.y += oneLine + normalSpace;

				float viewWidth = position.width;
				position.width = 50.0f;

				//If no graphs are available, say so.
				if (CurrentGraph < 0)
				{
					GUI.Label(new Rect(position.x, position.y, 150.0f, oneLine),
							  "No graphs available");
				}
				//Otherwise, allow the user to change selected graphs.
				else
				{
					GUI.Label(new Rect(position.x, position.y, 50.0f, oneLine), "Graph");
					int newGraph = EditorGUI.Popup(new Rect(position.x + 45.0f, position.y, 150.0f, oneLine),
												   CurrentGraph, AvailableGraphsGUI);
					if (CurrentGraph != newGraph)
					{
						Undo.RecordObject(property.serializedObject.targetObject, "Inspector");

						CurrentGraph = newGraph;
						graph._GraphFile = AvailableGraphs[CurrentGraph];
						UpdateGraph(graph);
					}
				}

				position.y += oneLine + normalSpace;

				//"Refresh Graphs" button.
				if (GUI.Button(new Rect(position.x, position.y, 150.0f, oneLine),
							   "Refresh Graphs"))
				{
					RefreshGraphs(graph);
				}

				//If no graph is selected, stop here.
				if (CurrentGraph < 0)
				{
					EditorGUI.EndProperty();
					return;
				}

				position.y += oneLine + normalSpace;

				//Shader path.
				GUI.Label(new Rect(position.x, position.y, 100.0f, oneLine), "Shader File Path");
				string newPath = EditorGUI.TextField(new Rect(position.x + 105.0f, position.y,
															  viewWidth - 200.0f, oneLine),
													  graph._ShaderFile);
				if (newPath != graph._ShaderFile)
				{
					string dirName = Path.GetDirectoryName(graph._ShaderFile);
					if (!Directory.Exists(Path.Combine(Application.dataPath, dirName)))
						Directory.CreateDirectory(Path.Combine(Application.dataPath, dirName));

					AssetDatabase.MoveAsset(graph._ShaderFile, newPath);

					graph._ShaderFile = newPath;
				}
				position.y += oneLine + largeSpace;

				//Parameters.
				const float paramIndent = 20.0f;
				bool paramsChanged = false;
				//Float.
				if (graph.FloatParams.Count > 0)
				{
					IsFloatParamListFolded = EditorGUI.Foldout(new Rect(position.x + 15.0f, position.y,
															   position.width, oneLine),
															   IsFloatParamListFolded, "Float Params");
					position.y += oneLine;
					if (IsFloatParamListFolded)
					{
						for (int i = 0; i < graph.FloatParams.Count; ++i)
						{
							GUI.Label(new Rect(position.x + paramIndent, position.y, 100.0f, oneLine),
									  graph.FloatParams[i].Key);

							float newVal;
							if (graph._FloatParams[i].Value.IsSlider)
							{
								newVal = GUI.HorizontalSlider(new Rect(position.x + 105.0f + paramIndent,
																	   position.y,
																	   100.0f, oneLine),
															  graph.FloatParams[i].Value,
															  graph._FloatParams[i].Value.SliderMin,
															  graph._FloatParams[i].Value.SliderMax);
							}
							else
							{
								newVal = EditorGUI.DelayedFloatField(new Rect(position.x + 105.0f + paramIndent,
																			  position.y,
																			  25.0f, oneLine),
																	 graph.FloatParams[i].Value);
							}

							if (newVal != graph.FloatParams[i].Value)
							{
								paramsChanged = true;
								Undo.RecordObject(property.serializedObject.targetObject, "Inspector");
								graph.FloatParams[i].Value = newVal;
							}

							position.y += oneLine + smallSpace;
						}
					}
					position.y += normalSpace;
				}
				//Tex2D.
				if (graph.Tex2DParams.Count > 0)
				{
					IsTex2DParamListFolded = EditorGUI.Foldout(new Rect(position.x + 15.0f, position.y,
																		position.width, oneLine),
															   IsTex2DParamListFolded, "Tex2D Params");
					position.y += oneLine;
					if (IsTex2DParamListFolded)
					{
						for (int i = 0; i < graph.Tex2DParams.Count; ++i)
						{
							GUI.Label(new Rect(position.x + paramIndent, position.y, 100.0f, oneLine),
									  graph.Tex2DParams[i].Key);

							Texture2D newVal =
								(Texture2D)EditorGUI.ObjectField(new Rect(position.x + 105.0f + paramIndent,
																		  position.y,
																		  graph.Tex2DParams[i].Value.width,
																		  graph.Tex2DParams[i].Value.height),
																 graph.Tex2DParams[i].Value,
																 typeof(Texture2D), true);

							if (newVal != graph.Tex2DParams[i].Value)
							{
								paramsChanged = true;
								Undo.RecordObject(property.serializedObject.targetObject, "Inspector");
								graph.Tex2DParams[i].Value = newVal;
							}

							position.y += graph.Tex2DParams[i].Value.height;
						}
					}
					position.y += normalSpace;
				}
				//UV Z coordinate.
				GUI.Label(new Rect(position.x + paramIndent, position.y, 100.0f, oneLine), "UV.z");
				float newUVz = EditorGUI.FloatField(new Rect(position.x + 105.0f + paramIndent,
															 position.y,
														     position.width, oneLine),
													graph.UVz);
				position.y += largeSpace;

				if (graph.UVz != newUVz)
				{
					paramsChanged = true;
					Undo.RecordObject(property.serializedObject.targetObject, "Inspector");
					graph.UVz = newUVz;
				}

				if (paramsChanged)
				{
					UpdatePreviewTex(graph);
				}

				//Preview texture stuff.
				if (graph._PreviewTex != null)
				{
					GUI.Label(new Rect(position.x, position.y, 90.0f, oneLine), "Preview Scale");
					float newPreviewTexScale = EditorGUI.Slider(new Rect(position.x + 90.0f, position.y,
																	     position.width, oneLine),
															    graph._PreviewTexScale,
															    0.1f, 10.0f);
					position.y += oneLine + normalSpace;

					const float labelWidth = 78.0f,
								intBoxWidth = 40.0f;
					GUI.Label(new Rect(position.x, position.y, labelWidth, oneLine),
							  "Preview Size");
					int newPreviewTexWidth =
						Math.Max(1, EditorGUI.IntField(new Rect(position.x + labelWidth + 10.0f, position.y,
																intBoxWidth, oneLine),
													   graph._PreviewTexWidth));
					int newPreviewTexHeight =
						Math.Max(1, EditorGUI.IntField(new Rect(position.x + labelWidth + intBoxWidth + 20.0f,
																position.y,
																intBoxWidth, oneLine),
													   graph._PreviewTexHeight));

					if (newPreviewTexScale != graph._PreviewTexScale ||
						newPreviewTexWidth != graph._PreviewTexWidth ||
						newPreviewTexHeight != graph._PreviewTexHeight)
					{
						bool updatePreview = (newPreviewTexWidth != graph._PreviewTexWidth ||
											  newPreviewTexHeight != graph._PreviewTexHeight);

						Undo.RecordObject(property.serializedObject.targetObject, "Inspector");
						graph._PreviewTexScale = newPreviewTexScale;
						graph._PreviewTexWidth = newPreviewTexWidth;
						graph._PreviewTexHeight = newPreviewTexHeight;

						if (updatePreview)
							UpdatePreviewTex(graph);
					}

					position.y += oneLine + midSpace;

					//NOTE: There is a unity bug that makes the preview texture flicker.
					//Nothing I can do about it.
					//https://issuetracker.unity3d.com/issues/a-texture-drawn-from-a-custom-propertydrawer-is-sometimes-not-drawn
					Rect texPos = new Rect(position.x, position.y,
										   graph._PreviewTex.width * graph._PreviewTexScale,
										   graph._PreviewTex.height * graph._PreviewTexScale);
					//EditorGUI.LabelField(texPos, new GUIContent(graph._PreviewTex));
					EditorGUI.DrawPreviewTexture(texPos, graph._PreviewTex);
					position.y += graph._PreviewTex.height * graph._PreviewTexScale;
				}
			}

			//Occasionally refresh the texture preview.
			//This lets the window "react" to undo/redo changes.
			//Have to do it this way because UnityEditor.Undo is a huge PITA.
			if (graph._ShaderFile != null && UnityEngine.Random.Range(0.0f, 1.0f) > 0.85f)
				UpdatePreviewTex(graph);


			EditorGUI.EndProperty();
		}

		/// <summary>
		/// Gets a new, unused shader file name and creates a dummy shader file at that path.
		/// </summary>
		private string GetNewShaderFile()
		{
			const string dir = "GPUGraph/Resources";
			if (!Directory.Exists(Path.Combine(Application.dataPath, dir)))
				Directory.CreateDirectory(Path.Combine(Application.dataPath, dir));

			int i = 0;
			string path = Path.Combine(dir, "MyGPUGShader0.shader");
			while (File.Exists(Path.Combine(Application.dataPath, path)))
			{
				i += 1;
				path = Path.Combine(dir, "MyGPUGShader" + i + ".shader");
			}

			if (!Directory.Exists(Path.Combine(Application.dataPath, dir)))
			{
				Directory.CreateDirectory(Path.Combine(Application.dataPath, dir));
			}

			File.WriteAllText(Path.Combine(Application.dataPath, path), "");

			return path;
		}

		/// <summary>
		/// Updates the given RuntimeGraph's params to be consistent with the given GPUGraph.
		/// </summary>
		private void LoadParams(RuntimeGraph gR, Graph gE)
		{
			GraphParamCollection paramSet = new GraphParamCollection(gE);

			//Get all float params for the graph.
			var newFloatParams = new List<RuntimeGraph._SerializableFloatParamKVP>();
			foreach (FloatParamInfo fp in paramSet.FloatParams)
			{
				RuntimeGraph._SerializableFloatParamKVP sfp = new RuntimeGraph._SerializableFloatParamKVP();
				sfp.Key = fp.Name;
				sfp.Value = new RuntimeGraph._SerializableFloatParamInfo();
				sfp.Value.IsSlider = fp.IsSlider;
				sfp.Value.SliderMin = fp.SliderMin;
				sfp.Value.SliderMax = fp.SliderMax;
				sfp.Value.DefaultValue = fp.DefaultValue;

				newFloatParams.Add(sfp);
			}
			//Remove vestigial params from the RuntimeGraph.
			for (int i = 0; i < gR.FloatParams.Count; ++i)
			{
				if (!newFloatParams.Any((kvp) => kvp.Key == gR.FloatParams[i].Key))
				{
					gR.FloatParams.RemoveAt(i);
					gR._FloatParams.RemoveAt(i);
					i -= 1;
				}
			}
			//Update/add the params to the RuntimeGraph.
			for (int i = 0; i < newFloatParams.Count; ++i)
			{
				var param = new FloatParamKVP(newFloatParams[i].Key,
											  (newFloatParams[i].Value.IsSlider ?
												  Mathf.Lerp(newFloatParams[i].Value.SliderMin,
															 newFloatParams[i].Value.SliderMax,
															 newFloatParams[i].Value.DefaultValue) :
												  newFloatParams[i].Value.DefaultValue));

				int found = gR.FloatParams.IndexOf(p => p.Key == newFloatParams[i].Key);
				if (found == -1)
				{
					gR.FloatParams.Add(param);
					gR._FloatParams.Add(newFloatParams[i]);
				}
				else
				{
					param.Value = gR.FloatParams[found].Value;
					gR.FloatParams[found] = param;
					gR._FloatParams[found] = newFloatParams[i];
				}
			}

			//Get all Tex2D params for the graph.
			var newTex2DParams = new List<RuntimeGraph._SerializableTex2DParamKVP>();
			foreach (Texture2DParamInfo tp in paramSet.Tex2DParams)
			{
				RuntimeGraph._SerializableTex2DParamKVP stp = new RuntimeGraph._SerializableTex2DParamKVP();
				stp.Key = tp.Name;
				stp.Value = new RuntimeGraph._SerializableTex2DParamInfo();
				stp.Value.DefaultValue = tp.DefaultVal;

				newTex2DParams.Add(stp);
			}
			//Remove vestigial params from the RuntimeGraph.
			for (int i = 0; i < gR.Tex2DParams.Count; ++i)
			{
				if (!newTex2DParams.Any((kvp) => kvp.Key == gR.Tex2DParams[i].Key))
				{
					gR.Tex2DParams.RemoveAt(i);
					gR._Tex2DParams.RemoveAt(i);
					i -= 1;
				}
			}
			//Update/add new params to the RuntimeGraph.
			for (int i = 0; i < newTex2DParams.Count; ++i)
			{
				var param = new Tex2DParamKVP(newTex2DParams[i].Key,
											  newTex2DParams[i].Value.DefaultValue);

				int found = gR.Tex2DParams.IndexOf(p => p.Key == newTex2DParams[i].Key);
				if (found == -1)
				{
					gR.Tex2DParams.Add(param);
					gR._Tex2DParams.Add(newTex2DParams[i]);
				}
				else
				{
					param.Value = gR.Tex2DParams[found].Value;
					gR.Tex2DParams[found] = param;
					gR._Tex2DParams[found] = newTex2DParams[i];
				}
			}
		}

		/// <summary>
		/// Gets all available GPUGraph files.
		/// If the given RuntimeGraph already had a graph selected,
		///     this method attempts to preserve that selection.
		/// </summary>
		private void RefreshGraphs(RuntimeGraph graph)
		{
			//Get the available graphs.
			AvailableGraphs = GPUGraph.GraphEditorUtils.GetAllGraphsInProject();
			AvailableGraphsGUI =
				AvailableGraphs.Select(
					s => new GUIContent(Path.GetFileNameWithoutExtension(s), s)).ToArray();

			//If the RuntimeGraph didn't already have a graph chosen,
			//    choose the first available one.
			if (graph._GraphFile == null)
			{
				if (AvailableGraphs.Count > 0)
				{
					CurrentGraph = 0;
					graph._GraphFile = AvailableGraphs[CurrentGraph];
				}
				else
				{
					CurrentGraph = -1;
				}
			}
			//Otherwise, try and find the graph in "AvailableGraphs".
			else
			{
				CurrentGraph = AvailableGraphs.IndexOf(graph._GraphFile);
				if (CurrentGraph < 0 && AvailableGraphs.Count > 0)
				{
					CurrentGraph = 0;
					graph._GraphFile = null;
				}
				else if (CurrentGraph >= 0 && CurrentGraph < AvailableGraphs.Count)
				{
					graph._GraphFile = AvailableGraphs[CurrentGraph];
				}
				else
				{
					graph._GraphFile = null;
				}
			}

			//If a graph is still selected, update it.
			if (CurrentGraph >= 0)
				UpdateGraph(graph);
		}
		/// <summary>
		/// Reloads the graph used by the given RuntimeGraph.
		/// Returns the loaded graph in case any other code wants to use it.
		/// </summary>
		private Graph UpdateGraph(RuntimeGraph graph)
		{
			Graph gpuG = null;
			gpuG = new Graph(graph._GraphFile);
			string err = gpuG.Load();
			if (err.Length > 0)
				return null;

			//Generate the shader.
			graph.GraphShader = GPUGraph.GraphEditorUtils.SaveShader(gpuG, Path.Combine(Application.dataPath,
																						graph._ShaderFile),
																	 "Hidden/" +
																		Path.GetFileNameWithoutExtension(graph._ShaderFile),
																	 "rgb", 0.0f);

			if (graph.GraphShader == null)
				return gpuG;

			LoadParams(graph, gpuG);

			//Make sure the material is up-to-date.
			if (graph._PreviewMat == null)
				graph._PreviewMat = new Material(graph.GraphShader);
			else
				graph._PreviewMat.shader = graph.GraphShader;

			//Update the preview of the graph's output.
			UpdatePreviewTex(graph);

			return gpuG;
		}
		/// <summary>
		/// Updates the preview texture of the graph's noise.
		/// </summary>
		private void UpdatePreviewTex(RuntimeGraph graph)
		{
			//Make sure the Texture2D is up to date.
			if (graph._PreviewTex == null)
			{
				graph._PreviewTex = new Texture2D(graph._PreviewTexWidth,
												  graph._PreviewTexHeight);
			}
			else if (graph._PreviewTex.width != graph._PreviewTexWidth ||
					 graph._PreviewTex.height != graph._PreviewTexHeight)
			{
				graph._PreviewTex.Resize(graph._PreviewTexWidth, graph._PreviewTexHeight);
			}

			graph.GenerateToTexture(graph._PreviewTex, true);
		}
	}
}