using System;
using System.Linq;
using Path = System.IO.Path;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
using GPUNoise;


namespace GPUNoise.Editor
{
	public class EditorGraphWindow : EditorWindow
	{
		[UnityEditor.MenuItem("GPU Noise/Show Editor")]
		public static void ShowEditor()
		{
			UnityEditor.EditorWindow.GetWindow(typeof(EditorGraphWindow));
		}

		private static readonly float OutputHeight = 30.0f;
		private static readonly float TitleBarHeight = 30.0f,
									  InputSpacing = 20.0f;


		public EditorGraph Editor = null;

		private int selectedGraph = -1;

		private List<string> GraphPaths;
		private GUIContent[] graphSelections;

		private long reconnectingOutput = -1,
					 reconnectingInput = -2;
		private int reconnectingInput_Index = 0;

		private int activeWindowID = -1,
					copiedWindowID = -1;

		private bool unsavedChanges = false;

		private Texture2D previewNoise = null;
		private GUIStyle previewStyle = null;
		private bool autoUpdatePreview = false;

		private static readonly Vector2 MinLeftSize = new Vector2(256.0f, 705.0f),
										MinGraphSize = new Vector2(500.0f, 500.0f);

		private Rect WindowRect { get { return new Rect(0.0f, 0.0f, position.width - MinLeftSize.x, position.height); } }


		private bool ConfirmLoseUnsavedChanges()
		{
			if (unsavedChanges)
			{
				if (EditorUtility.DisplayDialog("Unsaved changes",
												"You have unsaved changes. Are you sure you want to lose them?",
												"Yes", "Cancel"))
				{
					unsavedChanges = false;
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

			GraphPaths = GPUNoise.Applications.GraphUtils.GetAllGraphsInProject();

			Func<string, GUIContent> selector = (s => new GUIContent(Path.GetFileNameWithoutExtension(s), s));
			graphSelections = GraphPaths.Select(selector).ToArray();
			
			selectedGraph = -1;

			titleContent = new GUIContent("GPUG Editor");
			minSize = new Vector2(MinLeftSize.x + MinGraphSize.x,
								  Mathf.Max(MinLeftSize.y, MinGraphSize.y));

			previewStyle = new GUIStyle();
			previewStyle.imagePosition = ImagePosition.ImageOnly;
		}
		void OnDestroy()
		{
			if (unsavedChanges &&
				EditorUtility.DisplayDialog("Unsaved changes",
											"You have unsaved changes to graph '" +
												graphSelections[selectedGraph].text +
												"'. Do you want to save them?",
											"Yes", "No"))
			{
				Editor.Resave();
			}
			
			unsavedChanges = false;
		}

		void OnGUI()
		{
			const float leftSpace = 256.0f;

			//Draw the side-bar.
			GUILayout.BeginArea(new Rect(0, 0, leftSpace, position.height));
			GUILeftArea();
			GUILayout.EndArea();
			GUIUtil.DrawLine(new Vector2(leftSpace + 10.0f, 0.0f), new Vector2(leftSpace + 10.0f, position.height), 5.0f, Color.black);
			if (Editor == null)
				return;

			//Respond to UI events.
			Rect graphArea = new Rect(leftSpace, 0.0f, position.width - leftSpace, position.height);
			GUIHandleEvents(graphArea, leftSpace);
			

			//Draw the various windows.

			GUILayout.BeginArea(graphArea);
			BeginWindows();

			long[] keys = Editor.FuncCallPoses.Keys.ToArray();
			foreach (long uid in keys)
			{
				FuncCall call = (uid == -1 ? new FuncCall(-1, null, new FuncInput[0]) :
											 Editor.GPUGraph.UIDToFuncCall[uid]);
				Editor.FuncCallPoses[uid] = GUINode(Editor.FuncCallPoses[uid], call);
			}

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
					Editor = new EditorGraph(GraphPaths[selectedGraph], WindowRect);
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
					Graph g = new Graph(new FuncInput(0.5f));
					if (Applications.GraphUtils.SaveGraph(g, savePath))
					{
						Editor = new EditorGraph(savePath, WindowRect);
						
						GraphPaths = GPUNoise.Applications.GraphUtils.GetAllGraphsInProject();

						Func<string, GUIContent> selector = (s => new GUIContent(Path.GetFileNameWithoutExtension(s), s));
						graphSelections = GraphPaths.Select(selector).ToArray();

						selectedGraph = -1;
						for (int i = 0; i < graphSelections.Length; ++i)
						{
							if (graphSelections[i].text == Path.GetFileNameWithoutExtension(savePath))
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

			if (Editor != null && unsavedChanges)
			{
				if (GUILayout.Button("Save Changes"))
				{
					Editor.Resave();
					unsavedChanges = false;
				}

				if (GUILayout.Button("Discard Changes"))
				{
					if (ConfirmLoseUnsavedChanges())
					{
						Editor = new EditorGraph(GraphPaths[selectedGraph], WindowRect);
					}
				}
			}
			else
			{
				//Leave extra space for the buttons to appear in once a change is made.
				GUILayout.Space(42.0f);
			}

			GUILayout.Space(35.0f);


			if (Editor != null)
			{
				GUILayout.Label("1D Hash:");
				string oldHash = Editor.GPUGraph.Hash1;
				Editor.GPUGraph.Hash1 = GUILayout.TextField(Editor.GPUGraph.Hash1);
				if (oldHash != Editor.GPUGraph.Hash1)
				{
					unsavedChanges = true;
					if (autoUpdatePreview)
						UpdatePreview();
				}

				GUILayout.Space(10.0f);

				GUILayout.Label("2D Hash:");
				oldHash = Editor.GPUGraph.Hash2;
				Editor.GPUGraph.Hash2 = GUILayout.TextField(Editor.GPUGraph.Hash2);
				if (oldHash != Editor.GPUGraph.Hash2)
				{
					unsavedChanges = true;
					if (autoUpdatePreview)
						UpdatePreview();
				}

				GUILayout.Space(10.0f);

				GUILayout.Label("3D Hash:");
				oldHash = Editor.GPUGraph.Hash3;
				Editor.GPUGraph.Hash3 = GUILayout.TextField(Editor.GPUGraph.Hash3);
				if (oldHash != Editor.GPUGraph.Hash3)
				{
					unsavedChanges = true;
					if (autoUpdatePreview)
						UpdatePreview();
				}
			}

			GUILayout.Space(30.0f);
			

			if (Editor != null)
			{
				autoUpdatePreview = GUILayout.Toggle(autoUpdatePreview, "Auto-Update Preview");
				if (!autoUpdatePreview)
				{
					if (GUILayout.Button("Update Preview"))
					{
						UpdatePreview();
					}
				}

				if (previewNoise != null)
				{
					GUILayout.Box(previewNoise);
				}
			}


			//Update the title bar as well.
			if (Editor == null)
			{
				titleContent = new GUIContent("GPUG Editor");
			}
			else if (unsavedChanges)
			{	
				titleContent = new GUIContent("*" + Path.GetFileNameWithoutExtension(Editor.FilePath) + "*");
			}
			else
			{
				titleContent = new GUIContent(Path.GetFileNameWithoutExtension(Editor.FilePath));
			}
		}
		private void GUIHandleEvents(Rect graphArea, float leftSpace)
		{
			Event evt = Event.current;
			switch (evt.type)
			{
				case EventType.MouseDown:
					activeWindowID = -2;
					Vector2 mPos = evt.mousePosition + new Vector2(-leftSpace, 0.0f);
					foreach (KeyValuePair<long, Rect> uidAndR in Editor.FuncCallPoses)
					{
						if (uidAndR.Value.Contains(mPos))
						{
							activeWindowID = (int)uidAndR.Key;
						}
					}

					break;
					
				case EventType.ContextClick:
					Vector2 mousePos = evt.mousePosition;
					if (graphArea.Contains(mousePos))
					{
						GenericMenu popupMenu = new GenericMenu();
						foreach (Func fu in FuncDefinitions.Functions)
						{
							AddNodeData dat = new AddNodeData();
							dat.Pos = evt.mousePosition + new Vector2(-leftSpace, 0.0f);
							dat.Name = fu.Name;
							popupMenu.AddItem(new GUIContent(fu.Name), false, OnAddNode, dat);
						}
						popupMenu.ShowAsContext();
						evt.Use();
					}
					break;

				case EventType.ValidateCommand:
					switch (evt.commandName)
					{
						case "Copy":
							copiedWindowID = activeWindowID;
							break;

						case "Paste":

							Rect pos = Editor.FuncCallPoses[copiedWindowID];
							FuncCall fc = Editor.GPUGraph.UIDToFuncCall[copiedWindowID];

							FuncCall copy = new FuncCall(-1, fc.Calling, fc.Inputs);
							Editor.GPUGraph.CreateFuncCall(copy);
							Editor.FuncCallPoses.Add(copy.UID, new Rect(pos.x, pos.y + pos.height,
																		pos.width, pos.height));

							if (reconnectingOutput >= 0)
							{
								if (copy.Inputs.Length > 0)
								{
									unsavedChanges = true;
									if (autoUpdatePreview)
										UpdatePreview();

									copy.Inputs[0] = new FuncInput(reconnectingOutput);
								}
								reconnectingOutput = -1;
							}
							else if (reconnectingInput >= 0)
							{
								unsavedChanges = true;
								if (autoUpdatePreview)
									UpdatePreview();

								FuncCall rI = Editor.GPUGraph.UIDToFuncCall[reconnectingInput];
								rI.Inputs[reconnectingInput_Index] = new FuncInput(copy);

								reconnectingInput = -2;
							}
							else if (reconnectingInput == -1)
							{
								unsavedChanges = true;
								if (autoUpdatePreview)
									UpdatePreview();

								Editor.GPUGraph.Output = new FuncInput(copy);
								reconnectingInput = -2;
							}

							Repaint();
							break;
					}
					break;

				case EventType.KeyDown:
					switch (evt.keyCode)
					{
						case KeyCode.Delete:
						case KeyCode.Backspace:
							if (activeWindowID >= 0 && Editor.FuncCallPoses.ContainsKey(activeWindowID))
							{
								Editor.FuncCallPoses.Remove(activeWindowID);
								Editor.GPUGraph.RemoveFuncCall(activeWindowID);
								
								unsavedChanges = true;
								if (autoUpdatePreview)
									UpdatePreview();

								Repaint();
							}
							break;
					}
					break;
			}
		}
		private Rect GUINode(Rect nodeRect, FuncCall node)
		{
			if (node.UID > int.MaxValue)
			{
				Debug.LogError("UID of " + node.UID + " is too big!");
			}

			string name = (node.Calling == null ? "Output" : node.Calling.Name);
			nodeRect = GUILayout.Window((int)node.UID, nodeRect, GUINodeWindow, name,
										GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			return nodeRect;
		}
		private void GUINodeWindow(int windowID)
		{
			long uid = (long)windowID;
			if (!Editor.FuncCallPoses.ContainsKey(uid))
				return;

			Rect r = Editor.FuncCallPoses[uid];

			if (uid == -1)
			{
				GUILayout.BeginVertical();

				GUILayout.BeginHorizontal();

				//Button to connect input to an output.
				string buttStr = (reconnectingInput == -1 ? "x" : "X");
				if (GUILayout.Button(buttStr))
				{
					if (reconnectingOutput >= 0)
					{
						Editor.GPUGraph.Output = new FuncInput(reconnectingOutput);

						unsavedChanges = true;
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
				FuncInput graphOut = Editor.GPUGraph.Output;
				if (graphOut.IsAConstantValue)
				{
					Editor.GPUGraph.Output = new FuncInput(EditorGUILayout.FloatField(graphOut.ConstantValue));
					if (autoUpdatePreview)
						UpdatePreview();
					unsavedChanges = true;
				}
				else
				{
					if (GUILayout.Button("Disconnect"))
					{
						Editor.GPUGraph.Output = new FuncInput(0.5f);
						
						unsavedChanges = true;
						if (autoUpdatePreview)
							UpdatePreview();

						reconnectingInput = -2;
						reconnectingOutput = -1;
					}

					Rect inR = Editor.FuncCallPoses[graphOut.FuncCallID];
					Vector2 endPos = new Vector2(inR.xMax, inR.yMin + OutputHeight) - r.min;
					GUIUtil.DrawLine(new Vector2(0.0f, r.height * 0.5f), endPos, 4.0f, Color.red);
				}

				GUILayout.EndHorizontal();

				GUILayout.EndVertical();
			}
			else
			{
				GUILayout.BeginHorizontal();

				GUILayout.BeginVertical();

				FuncCall fc = Editor.GPUGraph.UIDToFuncCall[uid];
				for (int i = 0; i < fc.Inputs.Length; ++i)
				{
					GUILayout.BeginHorizontal();

					GUILayout.Label(fc.Calling.Params[i].Name);

					//Button to connect input to an output.
					string buttStr = "X";
					if (reconnectingInput == uid && reconnectingInput_Index == i)
					{
						buttStr = "x";
					}
					if (GUILayout.Button(buttStr))
					{
						if (reconnectingOutput >= 0)
						{
							fc.Inputs[i] = new FuncInput(reconnectingOutput);
							unsavedChanges = true;
							if (autoUpdatePreview)
								UpdatePreview();

							reconnectingOutput = -1;
						}
						else
						{
							reconnectingInput = uid;
							reconnectingInput_Index = i;
						}
					}
					if (fc.Inputs[i].IsAConstantValue)
					{
						float newVal = EditorGUILayout.FloatField(fc.Inputs[i].ConstantValue);
						if (newVal != fc.Inputs[i].ConstantValue)
						{
							fc.Inputs[i] = new FuncInput(newVal);
							unsavedChanges = true;
							if (autoUpdatePreview)
								UpdatePreview();
						}
					}
					else
					{
						Rect inR = Editor.FuncCallPoses[fc.Inputs[i].FuncCallID];
						Vector2 endPos = new Vector2(inR.xMax, inR.yMin + OutputHeight) - r.min;

						GUIUtil.DrawLine(new Vector2(0.0f, TitleBarHeight + ((float)i * InputSpacing)),
										 endPos,
										 2.0f, Color.white);

						if (GUILayout.Button("Disconnect"))
						{
							fc.Inputs[i] = new FuncInput(fc.Calling.Params[i].DefaultValue);
							
							unsavedChanges = true;
							if (autoUpdatePreview)
								UpdatePreview();

							reconnectingInput = -2;
							reconnectingOutput = -1;
						}
					}

					GUILayout.EndHorizontal();
				}

				if (fc.Calling.CustomGUI(fc.CustomDat))
				{
					unsavedChanges = true;
					if (autoUpdatePreview)
						UpdatePreview();
				}

				GUILayout.EndVertical();

				GUILayout.BeginVertical();

				//Output button.
				string buttonStr = "O";
				if (reconnectingOutput == uid)
					buttonStr = "o";
				if (GUILayout.Button(buttonStr))
				{
					if (reconnectingInput < -1)
					{
						reconnectingOutput = uid;
					}
					else
					{
						if (reconnectingInput == -1)
						{
							Editor.GPUGraph.Output = new FuncInput(uid);
							unsavedChanges = true;
							if (autoUpdatePreview)
								UpdatePreview();
						}
						else
						{
							FuncInput[] inputs = Editor.GPUGraph.UIDToFuncCall[reconnectingInput].Inputs;
							inputs[reconnectingInput_Index] = new FuncInput(uid);
							
							unsavedChanges = true;
							if (autoUpdatePreview)
								UpdatePreview();
						}

						reconnectingInput = -2;
					}
				}

				GUILayout.EndVertical();

				GUILayout.EndHorizontal();
			}

			GUI.DragWindow();
		}
		private void UpdatePreview()
		{
			previewNoise = Applications.GraphUtils.RenderToTexture(Editor.GPUGraph, 256, 256,
																   "rgb", 1.0f, TextureFormat.RGBAFloat);
		}

		private struct AddNodeData { public Vector2 Pos; public string Name; }
		private void OnAddNode(object datObj)
		{
			AddNodeData dat = (AddNodeData)datObj;

			FuncCall fc = new FuncCall(dat.Name);
			Editor.GPUGraph.CreateFuncCall(fc);
			Editor.FuncCallPoses.Add(fc.UID, new Rect(dat.Pos.x, dat.Pos.y, 100.0f, 50.0f));
			
			unsavedChanges = true;
			if (autoUpdatePreview)
				UpdatePreview();
		}
	}
}