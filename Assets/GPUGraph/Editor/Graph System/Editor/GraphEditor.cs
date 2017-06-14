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


		public Graph Grph = null;
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
		private bool autoUpdatePreview = false;


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

			Grph = null;

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
				Grph.Save();
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
			selectedGraph = GraphPaths.IndexOf((Grph == null ? "" : Grph.FilePath));


			//If this graph was deleted, reset the editor.
			if (selectedGraph == -1)
			{
				unsavedStr = "";

				Grph = null;

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
			GUILayout.BeginArea(new Rect(0.0f, 0.0f, NodeChoiceSpace, position.height));
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
			GUILayout.BeginArea(new Rect(NodeChoiceSpace, 0, OptionsSpace, position.height));
			GUILeftArea();
			GUILayout.EndArea();
			GUIUtil.DrawLine(new Vector2(NodeChoiceSpace + OptionsSpace + 4.0f, 0.0f),
							 new Vector2(NodeChoiceSpace + OptionsSpace + 4.0f, position.height),
							 2.0f, Color.black);
			if (Grph == null)
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
			foreach (Node n in Grph.Nodes)
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

			oldPos = new Rect(Grph.OutputPos.position - CamOffset, Grph.OutputPos.size);
			newPos = GUINode(oldPos, null);
			if (Mathf.Abs(oldPos.x - newPos.x) >= 2.0f ||
				Mathf.Abs(oldPos.y - newPos.y) >= 2.0f)
			{
				if (!unsavedStr.Contains("moved graph output node"))
					unsavedStr += "moved graph output node, ";
			}
			newPos.position += CamOffset;
			Grph.OutputPos = newPos;

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
					Grph = new Graph(GraphPaths[selectedGraph]);
					string err = Grph.Load();
					CamOffset = Grph.OutputPos.position - new Vector2(Mathf.RoundToInt(position.width * 0.5f),
																	  Mathf.RoundToInt(position.height * 0.5f));
					if (err.Length > 0)
					{
						Debug.LogError("Error loading graph: " + err);
					}
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
						Grph = g;
						CamOffset = Grph.OutputPos.position - new Vector2(Mathf.RoundToInt(position.width * 0.5f),
																		  Mathf.RoundToInt(position.height * 0.5f));
						GraphPaths = GraphEditorUtils.GetAllGraphsInProject();
						NewNodeOptions = NodeOptionsGenerator.GenerateList();

						Func<string, GUIContent> selector = (s => new GUIContent(Path.GetFileNameWithoutExtension(s), s));
						graphSelections = GraphPaths.Select(selector).ToArray();

						selectedGraph = -1;
						string toFind = Path.GetFileNameWithoutExtension(Grph.FilePath);
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

			if (Grph != null && unsavedStr.Length > 0)
			{
				if (GUILayout.Button("Save Changes"))
				{
					string err = Grph.Save();
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
						string err = Grph.Load();
						if (err.Length > 0)
						{
							Debug.LogError("Unable to reload graph: " + err);
						}
						else
						{
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


			if (Grph != null)
			{
				GUILayout.Label("1D Hash:");
				string oldHash = Grph.Hash1;
				Grph.Hash1 = GUILayout.TextField(Grph.Hash1);
				if (oldHash != Grph.Hash1)
				{
					if (!unsavedStr.Contains("1D hash func"))
						unsavedStr += "1D hash func, ";
					if (autoUpdatePreview)
						UpdatePreview();
				}

				GUILayout.Space(10.0f);

				GUILayout.Label("2D Hash:");
				oldHash = Grph.Hash2;
				Grph.Hash2 = GUILayout.TextField(Grph.Hash2);
				if (oldHash != Grph.Hash2)
				{
					if (!unsavedStr.Contains("2D hash func"))
						unsavedStr += "2D hash func, ";
					if (autoUpdatePreview)
						UpdatePreview();
				}

				GUILayout.Space(10.0f);

				GUILayout.Label("3D Hash:");
				oldHash = Grph.Hash3;
				Grph.Hash3 = GUILayout.TextField(Grph.Hash3);
				if (oldHash != Grph.Hash3)
				{
					if (!unsavedStr.Contains("3D hash func"))
						unsavedStr += "3D hash func, ";
					if (autoUpdatePreview)
						UpdatePreview();
				}
			}

			GUILayout.Space(30.0f);


			//Noise previewing.
			if (Grph != null)
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
					GUI.DrawTextureWithTexCoords(texR, previewNoise, new Rect(0.0f, 1.0f, 1.0f, -1.0f));
				}
			}


			//Update the title bar as well.
			if (Grph == null)
			{
				titleContent = new GUIContent("GPUG Editor");
			}
			else if (unsavedStr.Length > 0)
			{
				titleContent = new GUIContent("*" + Path.GetFileNameWithoutExtension(Grph.FilePath) + "*");
			}
			else
			{
				titleContent = new GUIContent(Path.GetFileNameWithoutExtension(Grph.FilePath));
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
							Node n = CurrentlyPlacing.NodeFactory(Grph, new Rect(localMPos, Vector2.one));

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
								Grph.AddNode(n);
								if (!unsavedStr.Contains("added node"))
									unsavedStr = "added node, ";
								Repaint();
							}

							CurrentlyPlacing = null;
						}
						else
						{
							activeWindowID = -2; //TODO: Should this be -1? Also check the conditional below.
							foreach (Node n in Grph.Nodes)
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

							foreach (Node n in Grph.Nodes)
							{
								if (n.Pos.Contains(localMPos))
								{
									activeWindowID = n.UID;
									draggingWindowID = activeWindowID;
								}
							}
							if (Grph.OutputPos.Contains(mPos))
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
						Grph.AddNode(nd);
						if (!unsavedStr.Contains("added node"))
							unsavedStr += "added node, ";
						Repaint();
					}
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
						Grph.Output = new NodeInput(reconnectingOutput);

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
				if (Grph.Output.IsAConstant)
				{
					float oldOut = Grph.Output.ConstantValue;
					Grph.Output = new NodeInput(EditorGUILayout.FloatField(Grph.Output.ConstantValue));
					if (oldOut != Grph.Output.ConstantValue)
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
						Grph.Output = new NodeInput(0.5f);

						if (!unsavedStr.Contains("disconnect nodes"))
							unsavedStr += "disconnect nodes, ";
						if (autoUpdatePreview)
							UpdatePreview();

						reconnectingInput = -2;
						reconnectingOutput = -1;
					}
					else
					{
						Rect inR = Grph.GetNode(Grph.Output.NodeID).Pos;
						Vector2 endPos = new Vector2(inR.xMax, inR.yMin + OutputHeight) - Grph.OutputPos.min;
						GUIUtil.DrawLine(new Vector2(0.0f, Grph.OutputPos.height * 0.5f),
										 endPos, 4.0f, Color.red);
					}
				}

				GUILayout.EndHorizontal();

				GUILayout.EndVertical();
			}
			//Any other valid UID represents a normal node.
			else
			{
				Node n = Grph.GetNode(windowID);
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
								Grph.Output = new NodeInput(windowID);
								if (!unsavedStr.Contains("connect node to graph output"))
									unsavedStr += "connect node to graph output, ";
								if (autoUpdatePreview)
									UpdatePreview();
							}
							else
							{
								Grph.GetNode(reconnectingInput).Inputs[reconnectingInput_Index] =
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
						Grph.RemoveNode(Grph.GetNode(windowID));

						reconnectingInput = -2;
						reconnectingOutput = -1;

						if (!unsavedStr.Contains("deleted node"))
							unsavedStr += "deleted node, ";
						if (autoUpdatePreview)
							UpdatePreview();

						Repaint();
						break;

					case Node.GUIResults.Duplicate:

						Node copy = Grph.GetNode(windowID).Clone();
						Grph.AddNode(copy);
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
							Node connecting = Grph.GetNode(reconnectingInput);
							connecting.Inputs[reconnectingInput_Index] = new NodeInput(copy);

							reconnectingInput = -2;

							if (autoUpdatePreview)
								UpdatePreview();
						}
						else if (reconnectingInput == -1)
						{
							if (autoUpdatePreview)
								UpdatePreview();

							Grph.Output = new NodeInput(copy);
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

		private void UpdatePreview()
		{
			previewNoise = GraphEditorUtils.GenerateToTexture(Grph, new GraphParamCollection(Grph),
															  256, 256, "rgb", 1.0f,
															  TextureFormat.RGBAFloat);
		}
	}
}