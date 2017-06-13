using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;
using UnityEditor;


namespace GPUGraph
{
	[Serializable]
	public class SubGraphNode : Node
	{
		public string GraphGUID = null;


		private List<string> guids = null;
		private string[] names = null;
		private int selected = -1;
        private bool convertParamsToInputs = true;


        private string ThisGraphGUID
		{
			get
			{
				return AssetDatabase.AssetPathToGUID(StringUtils.GetRelativePath(Owner.FilePath,
																				 "Assets"));
			}
		}

		private void UpdateNameArray()
		{
			names = guids
					  .Select(g => AssetDatabase.GUIDToAssetPath(g))
					  .Select(p => System.IO.Path.GetFileNameWithoutExtension(p))
					  .ToArray();
		}
		private void UpdateGraphPaths()
		{
			guids = GraphEditorUtils.GetAllGraphsInProject(null, true)
						.Select(p => AssetDatabase.AssetPathToGUID(p))
						.ToList();
			//Remove this graph from the list of possible selections.
			if (Owner != null)
			{
				string thisGraphGUID = ThisGraphGUID;
				int i = (Owner == null ? -1 : guids.IndexOf(g => (g == thisGraphGUID)));
				if (i >= 0)
				{
					guids.RemoveAt(i);
				}
			}

			UpdateNameArray();
		}

		private Graph TryLoadGraph()
		{
			Graph g = new Graph(GraphGUID, true);
			string err = g.Load();
			if (err.Length > 0)
			{
				Debug.LogError("Error opening graph " + g.FilePath + ": " + err);
				return null;
			}
			else
			{
				return g;
			}
		}


		public override Color GUIColor { get { return new Color(1.0f, 0.85f, 0.85f); } }

		public override string PrettyName
		{
			get
			{
				return "Sub-Graph: " +
						System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(GraphGUID));
			}
		}


		public SubGraphNode(Rect pos)
			: base(pos, new List<NodeInput>(), new List<string>(), new List<float>()) { }
		private SubGraphNode() { }


		public override void OnGraphLoaded()
		{
			CheckOutGraphs(true);
		}
		public override void OnAddedToGraph()
		{
			CheckOutGraphs(false);
		}

		/// <summary>
		/// Refreshes the list of graphs and makes sure this node is still valid.
		/// Returns whether anything changed.
		/// </summary>
		private bool CheckOutGraphs(bool warnIfNotFound)
		{
			UpdateGraphPaths();

			//See if the current sub-graph being used still exists.
			selected = guids.IndexOf(GraphGUID);
			if (selected >= 0)
			{
				Graph g = TryLoadGraph();
				if (g == null)
				{
					selected = -1;
					ChangeGraph();
					return true;
				}
				else
				{
					//See if the number of inputs changed.
					GraphParamCollection gParams = new GraphParamCollection(g);
					if (convertParamsToInputs && Inputs.Count != gParams.FloatParams.Count)
					{
						SetInputsFrom(gParams.FloatParams);
						return true;
					}
				}
			}
			else
			{
				//Couldn't find the sub-graph anymore!
				if (warnIfNotFound)
				{
					Debug.LogWarning("Couldn't find sub-graph at " +
										AssetDatabase.GUIDToAssetPath(GraphGUID));
				}
				selected = -1;
				ChangeGraph();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Updates this node based on the value of "selected".
		/// </summary>
		private void ChangeGraph()
		{
			if (selected < 0)
			{
				GraphGUID = null;
				Inputs = new List<NodeInput>();
				InputNames = new List<string>();
				InputDefaultVals = new List<float>();
			}
			else
			{
				GraphGUID = guids[selected];

				Graph g = TryLoadGraph();
				if (g != null)
				{
                    if (convertParamsToInputs)
                    {
						SetInputsFrom(new GraphParamCollection(g).FloatParams);
                    }
                    else
                    {
                        Inputs.Clear();
                        InputNames.Clear();
                        InputDefaultVals.Clear();
                    }
				}

				UpdateGraphPaths();
				selected = guids.IndexOf(GraphGUID);
			}
		}


		protected override Node MakeClone()
		{
			SubGraphNode sgn = new SubGraphNode();
			sgn.GraphGUID = GraphGUID;
			sgn.guids = guids.ToList();
			sgn.names = names.ToArray();
            sgn.convertParamsToInputs = convertParamsToInputs;
			sgn.selected = selected;
			return sgn;
		}

		public override IEnumerable<Node> OnPreProcess()
		{
			//Don't do anything if this node isn't valid.
			if (selected < 0)
				return new List<Node>();


			//Load the sub-graph.
			Graph g = TryLoadGraph();
			if (g == null)
			{
				return new List<Node>();
			}

			//Clone the sub-graph's nodes.
			List<ParamNode_Float> floatParams = new List<ParamNode_Float>();
			List<Node> newNodes = new List<Node>(g.Nodes.Select(n => n.Clone()));

			//Add the new nodes to the graph, with new UID's.
			//Keep track of the float param nodes because they map to this node's inputs.
			int baseUID = Owner.NextUID;
			Owner.NextUID += newNodes.Count;
			foreach (Node n in newNodes)
			{
				//Offset all the UID's so there's no conflicts with the rest of the graph.
				n.UID = baseUID + n.UID;
				Owner.NextUID = Mathf.Max(Owner.NextUID, n.UID + 1);
				for (int i = 0; i < n.Inputs.Count; ++i)
					if (!n.Inputs[i].IsAConstant)
						n.Inputs[i] = new NodeInput(n.Inputs[i].NodeID + baseUID);

				Owner.AddNode(n, false);

				//If this node is another sub-graph, make sure there's no infinite loop.
				if (convertParamsToInputs && n is ParamNode_Float)
				{
					floatParams.Add((ParamNode_Float)n);
				}
			}

			//Error-checking.
			if (floatParams.Count != Inputs.Count)
			{
				Debug.LogError("Expected " + Inputs.Count + " float params in the graph but found " +
								    floatParams.Count);
				return new List<Node>();
			}

			//Sort the float params so they line up with this node's inputs.
			{
				List<ParamNode_Float> sortedFloatParams = new List<ParamNode_Float>();
				for (int i = 0; i < InputNames.Count; ++i)
				{
					for (int j = 0; j < floatParams.Count; ++j)
					{
						if (floatParams[j].Param.Name == InputNames[i])
						{
							sortedFloatParams.Add(floatParams[j]);
							break;
						}
					}
					if (sortedFloatParams.Count <= i)
					{
						Debug.LogError("This sub-graph had an unnecessary input \"" +
										   InputNames[i] + "\"");
					}
				}
				floatParams = sortedFloatParams;
			}

			//Replace all references to those float params with this node's inputs.
			foreach (Node n in newNodes)
			{
				for (int i = 0; i < n.Inputs.Count; ++i)
				{
					for (int j = 0; j < floatParams.Count; ++j)
					{
						if (!n.Inputs[i].IsAConstant && n.Inputs[i].NodeID == floatParams[j].UID)
						{
							n.Inputs[i] = Inputs[j];
						}
					}
				}
			}

			//Remove the float params.
			foreach (Node n in floatParams)
				Owner.RemoveNode(n);

			//Replace any references to this node with references to the output of the sub-graph.
			NodeInput gOut = (g.Output.IsAConstant ?
								g.Output :
								new NodeInput(g.Output.NodeID + baseUID));
			if (!Owner.Output.IsAConstant && Owner.Output.NodeID == UID)
				Owner.Output = gOut;
			foreach (Node nd in Owner.Nodes)
				for (int i = 0; i < nd.Inputs.Count; ++i)
					if (!nd.Inputs[i].IsAConstant && nd.Inputs[i].NodeID == UID)
						nd.Inputs[i] = gOut;

			//Finally, remove this node.
			Owner.RemoveNode(this);

			return newNodes;
		}

		protected override bool CustomGUI()
		{
			bool changed = false;

            {
                bool oldConverted = convertParamsToInputs;
                convertParamsToInputs = GUILayout.Toggle(convertParamsToInputs, "Convert params to inputs");
                if (oldConverted && !convertParamsToInputs)
                {
					changed = true;
                    Inputs.Clear();
                    InputNames.Clear();
                    InputDefaultVals.Clear();
                }
                else if (!oldConverted && convertParamsToInputs)
                {
					changed = true;
                    Graph g = TryLoadGraph();
                    if (g == null)
                    {
                        convertParamsToInputs = false;
                    }
                    else
                    {
						SetInputsFrom(new GraphParamCollection(g).FloatParams);
                    }
                }
            }

			Vector2 textDims = GUI.skin.label.CalcSize(new GUIContent("Graph:"));
			EditorGUIUtility.labelWidth = textDims.x;
			int newIndex = EditorGUILayout.Popup("Graph:", selected, names, GUILayout.MinWidth(100.0f));

			if (selected != newIndex)
			{
				selected = newIndex;
				ChangeGraph();
				changed = true;
			}

			if (GUILayout.Button("Refresh graphs"))
			{
				changed = CheckOutGraphs(true) || changed;
			}

			return changed;
		}

		public override void EmitCode(StringBuilder outCode)
		{
			//This should only be called if there was an error when pre-processing.
			outCode.Append("float ");
			outCode.Append(OutputName);
			outCode.AppendLine(" = 0.0; //ERROR");
		}

		private void SetInputsFrom(List<FloatParamInfo> floatParams)
		{
			Inputs = floatParams.Select(fn =>
				{
					return new NodeInput(fn.IsSlider ?
										     Mathf.Lerp(fn.SliderMin, fn.SliderMax, fn.DefaultValue) :
											 fn.DefaultValue);
				}).ToList();

			InputNames = floatParams.Select(fn => fn.Name).ToList();

			InputDefaultVals = floatParams.Select(fn =>
				{
					return (fn.IsSlider ?
								Mathf.Lerp(fn.SliderMin, fn.SliderMax, fn.DefaultValue) :
								fn.DefaultValue);
				}).ToList();
		}


		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("GraphGUID", GraphGUID);
            info.AddValue("ConvertParamsToInputs", convertParamsToInputs);
		}
		public SubGraphNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
            convertParamsToInputs = info.GetBoolean("ConvertParamsToInputs");

			GraphGUID = info.GetString("GraphGUID");
			UpdateGraphPaths();
			selected = guids.IndexOf(GraphGUID);
		}
	}
}
