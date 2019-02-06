using System;
using System.Linq;
using Path = System.IO.Path;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
using GPUGraph;


namespace GPUGraph.Editor
{
	public class GraphEditor : EditorWindow
	{
		[UnityEditor.MenuItem("Assets/GPU Graph/Editor", false, 1)]
		public static void ShowEditor()
		{
			EditorWindow.GetWindow(typeof(GraphEditor));
		}


		private static readonly float OutputHeight = 30.0f,
									  NodeChoiceSpace = 170.0f,
									  OptionsSpace = 256.0f;

		private static readonly Vector2 MinGraphSize = new Vector2(700.0f, 500.0f);


		private Graph graph = null;
		public GraphParamCollection graphParams;
		public List<NodeTree_Element> NewNodeOptions = null;

		public NodeTree_Element_Option CurrentlyPlacing = null;


		private int selectedGraph = -1;

		private List<string> GraphPaths;
		private GUIContent[] graphSelections;

		private int reconnectingOutput = -1,
					reconnectingInput = -2,
					reconnectingInput_Index = 0;

		private Vector2 CamOffset = Vector2.zero;

		private int activeWindowID = -1;

		private bool draggingMouseDown = false;
		private int draggingWindowID = -2;

		private string unsavedStr = "";

		private Texture2D previewNoise = null;
		private Material cachedPreviewMat = null;
		private bool autoUpdatePreview = false;

		private float uvZ = 0.0f,
					  uvZMax = 1.0f;


		public void SelectOption(NodeTree_Element_Option option)
		{
			CurrentlyPlacing = option;
		}


		private bool ConfirmLoseUnsavedChanges()
		{
			if (unsavedStr.Length > 0)
			{
				if (EditorUtility.DisplayDialog("Unsaved changes",
												"You have unsaved changes: " +
													unsavedStr.Substring(0, unsavedStr.Length - 2) +
													". Are you sure you want to lose that?",
												"Yes", "Cancel"))
				{
					unsavedStr = "";
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return true;
			}
		}

		void OnEnable()
		{
			wantsMouseMove = true;

			graph = null;

			OnFocus();

			titleContent = new GUIContent("GPUG Editor");
			minSize = new Vector2(OptionsSpace + MinGraphSize.x, MinGraphSize.y);
		}
		void OnDestroy()
		{
			if (unsavedStr.Length > 0 &&
				EditorUtility.DisplayDialog("Unsaved changes",
											"You have unsaved changes to graph '" +
												graphSelections[selectedGraph].text +
												"': " + unsavedStr.Substring(0, unsavedStr.Length - 2) +
												". Do you want to save them?",
											"Save", "Discard"))
			{
				graph.Save();
			}

			unsavedStr = "";
		}
		void OnFocus()
		{
			//Check what graphs are available.
			GraphPaths = GraphEditorUtils.GetAllGraphsInProject();
			Func<string, GUIContent> selector = (s => new GUIContent(Path.GetFileNameWithoutExtension(s), s));
			graphSelections = GraphPaths.Select(selector).ToArray();

			//Keep the same graph selected even when the list of graphs changes.
			selectedGraph = GraphPaths.IndexOf((graph == null ? "" : graph.FilePath));


			//If this graph was deleted, reset the editor.
			if (selectedGraph == -1)
			{
				unsavedStr = "";

				graph = null;

				reconnectingOutput = -1;
				reconnectingInput = -2;
				reconnectingInput_Index = 0;

				activeWindowID = -1;
			}

			NewNodeOptions = NodeOptionsGenerator.GenerateList();
		}

		void OnGUI()
		{
			//Draw the node creation choices.
			Rect nodeChoicesArea = new Rect(0.0f, 0.0f, NodeChoiceSpace, position.height);
			GUI.Box(nodeChoicesArea, GUIUtil.WhitePixel);
			GUILayout.BeginArea(nodeChoicesArea);
			if (CurrentlyPlacing == null)
			{
				if (selectedGraph < 0)
				{
					GUILayout.Label("No graph is\nselected for editing.");
				}
				else
				{
					GUILayout.Label("Click on an option, then\nclick in the graph to place it.");
					GUILayout.Label("Mouse over an option to\nget more info about it.");
					GUILayout.Space(25.0f);

					foreach (NodeTree_Element el in NewNodeOptions)
					{
						NodeTree_Element_Option opt = el.OnGUI();
						if (opt != null)
						{
							SelectOption(opt);
							break;
						}
					}
				}
			}
			else
			{
				GUILayout.Label("Left-click in the graph\nto place " + CurrentlyPlacing.Name);
				GUILayout.Label("Right-click in the graph\nto cancel its placement");
			}
			GUILayout.EndArea();


			//Draw the side-bar.
			Rect sidebarArea = new Rect(NodeChoiceSpace, 0, OptionsSpace, position.height);
			GUI.Box(sidebarArea, GUIUtil.WhitePixel);
			GUILayout.BeginArea(sidebarArea);
			GUILeftArea();
			GUILayout.EndArea();
			GUIUtil.DrawLine(new Vector2(NodeChoiceSpace + OptionsSpace + 4.0f, 0.0f),
							 new Vector2(NodeChoiceSpace + OptionsSpace + 4.0f, position.height),
							 2.0f, Color.black);
			if (graph == null)
				return;

			//Respond to UI events.
			Rect graphArea = new Rect(NodeChoiceSpace + OptionsSpace, 0.0f,
									  position.width - NodeChoiceSpace - OptionsSpace,
									  position.height);
			GUIHandleEvents(graphArea, NodeChoiceSpace + OptionsSpace);

			//Draw the various windows.
			GUILayout.BeginArea(graphArea);
			BeginWindows();
			Rect oldPos, newPos;
			foreach (Node n in graph.Nodes)
			{
				oldPos = new Rect(n.Pos.position - CamOffset, n.Pos.size);
				newPos = GUINode(oldPos, n);

				if (Mathf.Abs(oldPos.x - newPos.x) >= 2.0f ||
					Mathf.Abs(oldPos.y - newPos.y) >= 2.0f)
				{
					if (!unsavedStr.Contains("moved node"))
						unsavedStr += "moved node, ";
				}

				newPos.position += CamOffset;
				n.Pos = newPos;
			}

			oldPos = new Rect(graph.OutputPos.position - CamOffset, graph.OutputPos.size);
			newPos = GUINode(oldPos, null);
			if (Mathf.Abs(oldPos.x - newPos.x) >= 2.0f ||
				Mathf.Abs(oldPos.y - newPos.y) >= 2.0f)
			{
				if (!unsavedStr.Contains("moved graph output node"))
					unsavedStr += "moved graph output node, ";
			}
			newPos.position += CamOffset;
			graph.OutputPos = newPos;

			EndWindows();
			GUILayout.EndArea();
		}
		private void GUILeftArea()
		{
			GUILayout.Space(10.0f);

			GUILayout.Label("Graphs:");

			int oldVal = selectedGraph;
			selectedGraph = EditorGUILayout.Popup(selectedGraph, graphSelections);
			if (selectedGraph != oldVal)
			{
				if (ConfirmLoseUnsavedChanges())
				{
					graph = new Graph(GraphPaths[selectedGraph]);
					string err = graph.Load();
					CamOffset = graph.OutputPos.position - new Vector2(Mathf.RoundToInt(position.width * 0.5f),
																	  Mathf.RoundToInt(position.height * 0.5f));
					if (err.Length > 0)
					{
						graphParams = new GPUGraph.GraphParamCollection(graph);
						Debug.LogError("Error loading graph: " + err);
					}
					else
					{
						graphParams = new GraphParamCollection(graph);
					}

					UpdatePreview();
				}
				else
				{
					selectedGraph = oldVal;
				}
			}

			GUILayout.Space(35.0f);


			if (GUILayout.Button("New Graph") && ConfirmLoseUnsavedChanges())
			{
				string savePath = EditorUtility.SaveFilePanelInProject("Choose Graph location",
																	   "MyGraph.gpug", "gpug",
																	   "Choose where to save the graph.");
				if (savePath != "")
				{
					Graph g = new Graph(savePath);
					string err = g.Save();
					if (err.Length > 0)
					{
						EditorUtility.DisplayDialog("Error saving new graph",
													"Error saving graph " + g.FilePath + ": " + err,
													"OK");
					}
					else
					{
						graph = g;
						CamOffset = graph.OutputPos.position - new Vector2(Mathf.RoundToInt(position.width * 0.5f),
																		  Mathf.RoundToInt(position.height * 0.5f));
						GraphPaths = GraphEditorUtils.GetAllGraphsInProject();
						NewNodeOptions = NodeOptionsGenerator.GenerateList();

						Func<string, GUIContent> selector = (s => new GUIContent(Path.GetFileNameWithoutExtension(s), s));
						graphSelections = GraphPaths.Select(selector).ToArray();

						selectedGraph = -1;
						string toFind = Path.GetFileNameWithoutExtension(graph.FilePath);
						for (int i = 0; i < graphSelections.Length; ++i)
						{
							if (graphSelections[i].text == toFind)
							{
								selectedGraph = i;
								break;
							}
						}

						UpdatePreview();
					}
				}
			}

			GUILayout.Space(30.0f);

			if (graph != null && unsavedStr.Length > 0)
			{
				if (GUILayout.Button("Save Changes"))
				{
					string err = graph.Save();
					if (err.Length > 0)
					{
						Debug.LogError("Error saving graph: " + err);
					}
					unsavedStr = "";
				}

				if (GUILayout.Button("Discard Changes"))
				{
					if (ConfirmLoseUnsavedChanges())
					{
						string err = graph.Load();
						if (err.Length > 0)
						{
							graphParams = new GPUGraph.GraphParamCollection()
							{
								FloatParams = new List<GPUGraph.FloatParamInfo>(),
								Tex2DParams = new List<GPUGraph.Texture2DParamInfo>(),
							};
							Debug.LogError("Unable to reload graph: " + err);
						}
						else
						{
							graphParams = new GraphParamCollection(graph);
							UpdatePreview();
						}
					}
				}
			}
			else
			{
				//Leave extra space for the buttons to appear once a change is made.
				GUILayout.Space(42.0f);
			}

			GUILayout.Space(35.0f);


			//Noise previewing.
			if (graph != null)
			{
				bool oldAutoUpdate = autoUpdatePreview;
				autoUpdatePreview = GUILayout.Toggle(autoUpdatePreview, "Auto-Update Preview");
				if (autoUpdatePreview && !oldAutoUpdate)
				{
					UpdatePreview();
				}

				if (!autoUpdatePreview)
				{
					if (GUILayout.Button("Update Preview"))
					{
						UpdatePreview();
					}
				}

				if (previewNoise != null)
				{
					//Flip the image vertically for unity GUI.
					Rect texR = EditorGUILayout.GetControlRect(GUILayout.Width(previewNoise.width),
															   GUILayout.Height(previewNoise.height));
					EditorGUI.DrawPreviewTexture(texR, previewNoise);

					//Draw a slider for the UV.z coordinate.
					GUILayout.BeginHorizontal();
					{
						float oldUVz = uvZ;

						GUILayout.Label("UV.z coord:");
						GUILayout.Space(10.0f);
						GUILayout.Label("0");
						uvZ = GUILayout.HorizontalSlider(uvZ, 0.0f, uvZMax, GUILayout.Width(80.0f));
						uvZMax = EditorGUILayout.FloatField(uvZMax, GUILayout.Width(25.0f));
						GUILayout.FlexibleSpace();

						if (oldUVz != uvZ)
							UpdatePreview(false);
					}
					GUILayout.EndHorizontal();

					//Edit parameters.
					EditorGUI.BeginChangeCheck();
					for (int i = 0; i < graphParams.FloatParams.Count; ++i)
					{
						var param = graphParams.FloatParams[i];

						GUILayout.BeginHorizontal();
						GUILayout.Label(param.Name);

						if (param.IsSlider)
						{
							param.DefaultValue =
								GUILayout.HorizontalSlider(Mathf.Lerp(param.SliderMin, param.SliderMax,
																	  param.DefaultValue),
														   param.SliderMin, param.SliderMax,
														   GUILayout.ExpandWidth(true),
														   GUILayout.MinWidth(80.0f));
							param.DefaultValue = Mathf.InverseLerp(param.SliderMin, param.SliderMax,
																   param.DefaultValue);

							GUILayout.FlexibleSpace();
						}
						else
						{
							param.DefaultValue = EditorGUILayout.DelayedFloatField(param.DefaultValue);
						}

						GUILayout.EndHorizontal();

						graphParams.FloatParams[i] = param;
					}
					for (int i = 0; i < graphParams.Tex2DParams.Count; ++i)
					{
						var param = graphParams.Tex2DParams[i];

						GUILayout.BeginHorizontal();
						GUILayout.Label(param.Name);

						param.DefaultVal = (Texture2D)EditorGUILayout.ObjectField(param.Name,
																				  param.DefaultVal,
																				  typeof(Texture2D),
																				  false);

						GUILayout.EndHorizontal();

						graphParams.Tex2DParams[i] = param;
					}
					if (EditorGUI.EndChangeCheck())
						UpdatePreview(false);
				}
			}


			//Update the title bar as well.
			if (graph == null)
			{
				titleContent = new GUIContent("GPUG Editor");
			}
			else if (unsavedStr.Length > 0)
			{
				titleContent = new GUIContent("*" + Path.GetFileNameWithoutExtension(graph.FilePath) + "*");
			}
			else
			{
				titleContent = new GUIContent(Path.GetFileNameWithoutExtension(graph.FilePath));
			}
		}
		private void GUIHandleEvents(Rect graphArea, float graphXOffset)
		{
			Event evt = Event.current;
			Vector2 mPos = evt.mousePosition + new Vector2(-graphXOffset, 0.0f),
					localMPos = mPos + CamOffset;
			switch (evt.type)
			{
				case EventType.MouseUp:
					draggingMouseDown = false;
					break;
				case EventType.MouseDrag:
					if (draggingMouseDown && draggingWindowID < -1)
					{
						CamOffset -= evt.delta;
						Repaint();
					}
					break;
				case EventType.MouseDown:
					if (evt.button == 0)
					{
						if (CurrentlyPlacing != null)
						{
							Node n = CurrentlyPlacing.NodeFactory(graph, new Rect(localMPos, Vector2.one));

							//Double-check that the node is serializable.
							if ((n.GetType().Attributes & System.Reflection.TypeAttributes.Serializable) == 0)
							{
								EditorUtility.DisplayDialog("Not serializable!",
															"This node, type '" + n.GetType().ToString() +
																"', isn't marked with the 'Serializable' attribute! " +
																"Fix this problem in code before using this node.",
															"OK");
							}
							else
							{
								graph.AddNode(n);
								if (!unsavedStr.Contains("added node"))
									unsavedStr = "added node, ";
								Repaint();
							}

							CurrentlyPlacing = null;
						}
						else
						{
							activeWindowID = -2; //TODO: Should this be -1? Also check the conditional below.
							foreach (Node n in graph.Nodes)
							{
								if (n.Pos.Contains(localMPos))
								{
									activeWindowID = n.UID;
								}
							}

							if (activeWindowID == -2)
							{
								EditorGUIUtility.editingTextField = false;
							}
						}
					}
					else if (evt.button == 1)
					{
						//If a node is currently being placed, cancel it.
						if (CurrentlyPlacing != null)
						{
							CurrentlyPlacing = null;
						}
						//Otherwise, see if we can drag the view.
						else
						{
							draggingMouseDown = true;
							draggingWindowID = -2;

							foreach (Node n in graph.Nodes)
							{
								if (n.Pos.Contains(localMPos))
								{
									activeWindowID = n.UID;
									draggingWindowID = activeWindowID;
								}
							}
							if (graph.OutputPos.Contains(mPos))
							{
								draggingWindowID = -1;
							}
						}
					}
					break;

				case EventType.ContextClick:
					//If a node is currently being placed, cancel it.
					if (CurrentlyPlacing != null)
						CurrentlyPlacing = null;
					break;

				case EventType.ValidateCommand:
					//Keeping this here in case we want to react to special events later.
					if (!EditorGUIUtility.editingTextField)
					{
						//switch (evt.commandName)
						//{

						//}
					}
					break;
				case EventType.KeyDown:
					//Add certain kinds of nodes for different keystrokes.
					/*
					Node nd = null;
					switch (evt.keyCode)
					{
						case KeyCode.Plus:
						case KeyCode.KeypadPlus:
							if (!EditorGUIUtility.editingTextField)
							{
								nd = new SimpleNode(new Rect(localMPos, Vector2.one),
													"'f1' + 'f2'", "Add",
													new SimpleNode.Param("f1", 0.0f),
													new SimpleNode.Param("f2", 0.0f));
							}
							break;
						case KeyCode.Minus:
						case KeyCode.KeypadMinus:
							if (!EditorGUIUtility.editingTextField)
							{
								nd = new SimpleNode(new Rect(localMPos, Vector2.one),
													"'f1' - 'f2'", "Subtract",
													new SimpleNode.Param("f1", 0.0f),
													new SimpleNode.Param("f2", 0.0f));
							}
							break;
						case KeyCode.Asterisk:
						case KeyCode.KeypadMultiply:
							if (!EditorGUIUtility.editingTextField)
							{
								nd = new SimpleNode(new Rect(localMPos, Vector2.one),
													"'f1' * 'f2'", "Multiply",
													new SimpleNode.Param("f1", 1.0f),
													new SimpleNode.Param("f2", 1.0f));
							}
							break;
						case KeyCode.Slash:
						case KeyCode.Backslash:
						case KeyCode.KeypadDivide:
							if (!EditorGUIUtility.editingTextField)
							{
								nd = new SimpleNode(new Rect(localMPos, Vector2.one),
													"'f1' / 'f2'", "Divide",
													new SimpleNode.Param("f1", float.NaN),
													new SimpleNode.Param("f2", 1.0f));
							}
							break;
					}
					if (nd != null && !EditorGUIUtility.editingTextField)
					{
						graph.AddNode(nd);
						if (!unsavedStr.Contains("added node"))
							unsavedStr += "added node, ";
						Repaint();
					}
					*/
					break;
			}
		}

		private Rect GUINode(Rect nodeRect, Node node)
		{
			Color oldCol = GUI.color;
			GUI.color = (node == null ? new Color(0.65f, 0.65f, 0.65f) : node.GUIColor);
			nodeRect = GUILayout.Window((node == null ? -1 : (int)node.UID),
										nodeRect, GUINodeWindow,
										(node == null ? "Output" : node.PrettyName),
										GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			GUI.color = oldCol;

			return nodeRect;
		}
		private void GUINodeWindow(int windowID)
		{
			//A UID of -1 indicates this is the graph's output node.
			if (windowID == -1)
			{
				GUILayout.BeginVertical();

				GUILayout.BeginHorizontal();

				//Button to connect input to an output.
				string buttStr = (reconnectingInput == -1 ? "x" : "X");
				if (GUILayout.Button(buttStr))
				{
					if (reconnectingOutput >= 0)
					{
						graph.Output = new NodeInput(reconnectingOutput);

						if (!unsavedStr.Contains("connect nodes"))
							unsavedStr += "connect nodes, ";
						if (autoUpdatePreview)
							UpdatePreview();

						reconnectingOutput = -1;
					}
					else
					{
						reconnectingInput = -1;
						reconnectingInput_Index = 0;
					}
				}
				if (graph.Output.IsAConstant)
				{
					float oldOut = graph.Output.ConstantValue;
					graph.Output = new NodeInput(EditorGUILayout.FloatField(graph.Output.ConstantValue));
					if (oldOut != graph.Output.ConstantValue)
					{
						if (autoUpdatePreview)
							UpdatePreview();
						if (!unsavedStr.Contains("modify graph output"))
							unsavedStr += "modify graph output, ";
					}
				}
				else
				{
					if (GUILayout.Button("Disconnect"))
					{
						graph.Output = new NodeInput(0.5f);

						if (!unsavedStr.Contains("disconnect nodes"))
							unsavedStr += "disconnect nodes, ";
						if (autoUpdatePreview)
							UpdatePreview();

						reconnectingInput = -2;
						reconnectingOutput = -1;
					}
					else
					{
						Rect inR = graph.GetNode(graph.Output.NodeID).Pos;
						Vector2 endPos = new Vector2(inR.xMax, inR.yMin + OutputHeight) - graph.OutputPos.min;
						GUIUtil.DrawLine(new Vector2(0.0f, graph.OutputPos.height * 0.5f),
										 endPos, 4.0f, Color.red);
					}
				}

				GUILayout.EndHorizontal();

				GUILayout.EndVertical();
			}
			//Any other valid UID represents a normal node.
			else
			{
				Node n = graph.GetNode(windowID);
				UnityEngine.Assertions.Assert.IsNotNull(n, "Node " + windowID + " not found");

				int isSelected = -2;
				if (reconnectingOutput == windowID)
					isSelected = -1;
				else if (reconnectingInput == windowID)
					isSelected = reconnectingInput_Index;

				int tempInput = -1;
				Node.GUIResults result = n.OnGUI(ref tempInput, isSelected);

				switch (result)
				{
					case Node.GUIResults.ClickInput:
						if (reconnectingOutput >= 0)
						{
							n.Inputs[tempInput] = new NodeInput(reconnectingOutput);

						if (!unsavedStr.Contains("connect nodes"))
							unsavedStr += "connect nodes, ";
							if (autoUpdatePreview)
								UpdatePreview();

							reconnectingOutput = -1;
						}
						else
						{
							reconnectingInput = windowID;
							reconnectingInput_Index = tempInput;
						}
						break;

					case Node.GUIResults.ClickOutput:
						if (reconnectingInput < -1)
						{
							reconnectingOutput = windowID;
						}
						else
						{
							if (reconnectingInput == -1)
							{
								graph.Output = new NodeInput(windowID);
								if (!unsavedStr.Contains("connect node to graph output"))
									unsavedStr += "connect node to graph output, ";
								if (autoUpdatePreview)
									UpdatePreview();
							}
							else
							{
								graph.GetNode(reconnectingInput).Inputs[reconnectingInput_Index] =
									new NodeInput(windowID);

								if (!unsavedStr.Contains("connect nodes"))
									unsavedStr += "connect nodes, ";
								if (autoUpdatePreview)
									UpdatePreview();
							}

							reconnectingInput = -2;
						}
						break;

					case Node.GUIResults.Delete:
						graph.RemoveNode(graph.GetNode(windowID));

						reconnectingInput = -2;
						reconnectingOutput = -1;

						if (!unsavedStr.Contains("deleted node"))
							unsavedStr += "deleted node, ";
						if (autoUpdatePreview)
							UpdatePreview();

						Repaint();
						break;

					case Node.GUIResults.Duplicate:

						Node copy = graph.GetNode(windowID).Clone();
						graph.AddNode(copy);
						copy.Pos.y += copy.Pos.height;

						if (!unsavedStr.Contains("duplicated node"))
							unsavedStr += "duplicated node, ";

						if (reconnectingOutput >= 0)
						{
							if (copy.Inputs.Count > 0)
							{
								copy.Inputs[0] = new NodeInput(reconnectingOutput);

								if (autoUpdatePreview)
									UpdatePreview();
							}

							reconnectingOutput = -1;
						}
						else if (reconnectingInput >= 0)
						{
							Node connecting = graph.GetNode(reconnectingInput);
							connecting.Inputs[reconnectingInput_Index] = new NodeInput(copy);

							reconnectingInput = -2;

							if (autoUpdatePreview)
								UpdatePreview();
						}
						else if (reconnectingInput == -1)
						{
							if (autoUpdatePreview)
								UpdatePreview();

							graph.Output = new NodeInput(copy);
							reconnectingInput = -2;
						}

						Repaint();
						break;

					case Node.GUIResults.Other:
						if (!unsavedStr.Contains("modified node"))
							unsavedStr += "modified node, ";

						if (autoUpdatePreview)
							UpdatePreview();

						reconnectingInput = -2;
						reconnectingOutput = -1;
						break;

					case Node.GUIResults.Nothing:
						break;

					default:
						throw new NotImplementedException("Unknown case " + result.ToString());
				}
			}

			GUI.DragWindow();
		}

		/// <summary>
		/// Updates the preview texture for this editor.
		/// </summary>
		/// <param name="regenerateShader">
		/// If true, regenerates the graph's shader, which takes a lot longer.
		/// This should be true whenever the graph itself changes.
		/// </param>
		private void UpdatePreview(bool regenerateShader = true)
		{
			//Update params.
			var oldParams = graphParams;
			graphParams = new GPUGraph.GraphParamCollection(graph);
			graphParams.SetParams(oldParams);

			//Create shader.
			if (regenerateShader || cachedPreviewMat == null)
			{
				string shaderText = graph.GenerateShader("Graph editor temp shader", "rgb", 1.0f);
				Shader shader = ShaderUtil.CreateShaderAsset(shaderText);

				if (shader == null)
					Debug.LogError("Shader: " + shaderText);
				else
					cachedPreviewMat = new Material(shader);
			}

			//Create texture.
			if (previewNoise == null)
			{
				previewNoise = new Texture2D(256, 256, TextureFormat.RGBA32, false);
			}

			//Set params and generate.
			graphParams.SetParams(cachedPreviewMat);
			cachedPreviewMat.SetFloat(GraphUtils.Param_UVz, uvZ);
			GraphUtils.GenerateToTexture(cachedPreviewMat, previewNoise, true);
		}
	}
}