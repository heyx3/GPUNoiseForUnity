using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using GPUNoise;
using GPUNoise.Applications;


namespace GPUNoise.Editor
{
	/// <summary>
	/// The visual representation of a Graph.
	/// </summary>
	public class EditorGraph
	{
		private static Rect ToR(float x, float y)
		{
			return new Rect(Mathf.RoundToInt(x), Mathf.RoundToInt(y), 100.0f, 50.0f);
		}


		/// <summary>
		/// The full path to the graph file.
		/// </summary>
		public string FilePath = "C:/MyGraph." + GraphEditorUtils.Extension;

		/// <summary>
		/// The graph for the shader.
		/// </summary>
		public Graph GPUGraph = new Graph(new FuncInput(1.0f));

		/// <summary>
		/// The position of each FuncCall on the visual graph.
		/// The graph output is also stored in this dictionary, with a UID of -1.
		/// </summary>
		public Dictionary<long, Rect> FuncCallPoses = new Dictionary<long, Rect>();


		public EditorGraph() { }

		public EditorGraph(string filePath, Rect viewRect)
		{
			FilePath = filePath;
			GPUGraph = GraphEditorUtils.LoadGraph(FilePath);

			//Position all nodes coming out of the graph output.
			List<long> usedNodes = PositionNodesFromRoot(viewRect, -1);

			//Position all nodes that aren't actually plugged into the output.
			foreach (var kvp in GPUGraph.UIDToFuncCall.Where(v => !usedNodes.Contains(v.Key)))
			{
				FuncCallPoses[kvp.Key] = ToR(Mathf.Lerp(viewRect.xMin, viewRect.xMax,
														UnityEngine.Random.Range(0.0f, 0.7f)),
											 Mathf.Lerp(viewRect.yMin, viewRect.yMax,
														UnityEngine.Random.Range(0.5f, 0.7f)));
			}
		}
		private List<long> PositionNodesFromRoot(Rect area, long node)
		{
			List<long> usedNodes = new List<long>();

			//First, store each node by its depth and then by its position along that depth (i.e. breadth).
			List<List<long>> nodesByDepth = new List<List<long>> { new List<long>() { node } };
			TraverseDepthFirst(nodesByDepth, GPUGraph.Output, 1);

			//Next, position all those nodes.
			usedNodes.Capacity = nodesByDepth.Sum(l => l.Count);
			for (int col = 0; col < nodesByDepth.Count; ++col)
			{
				float lerpX = (float)col / (float)nodesByDepth.Count;
				for (int row = 0; row < nodesByDepth[col].Count; ++row)
				{
					float lerpY = (float)row / (float)nodesByDepth[col].Count;

					Rect pos;
					if (nodesByDepth.Count == 1)
					{
						pos = ToR((area.xMin + area.xMax) / 2.0f,
								  (area.yMin + area.yMax) / 2.0f);
					}
					else
					{
						pos = ToR(Mathf.Lerp(area.xMax, area.xMin, lerpX) - 25.0f,
								  Mathf.Lerp(area.yMin, area.yMax, lerpY));
					}

					long id = nodesByDepth[col][row];
					usedNodes.Add(id);

					if (FuncCallPoses.ContainsKey(id))
					{
						FuncCallPoses[id] = pos;
					}
					else
					{
						FuncCallPoses.Add(id, pos);
					}
				}
			}

			return usedNodes;
		}
		private void TraverseDepthFirst(List<List<long>> nodesByDepth, FuncInput input, int inputDepth)
		{
			if (input.IsAConstantValue)
			{
				return;
			}
			
			//Get the column this node is in.
			List<long> myColumn;
			if (nodesByDepth.Count <= inputDepth)
			{
				nodesByDepth.Add(new List<long>());
			}
			myColumn = nodesByDepth[inputDepth];

			//Add this node to the group.
			myColumn.Add(input.FuncCallID);

			//Now process this node's inputs.
			int newDepth = inputDepth + 1;
			FuncCall call = GPUGraph.UIDToFuncCall[input.FuncCallID];
			for (int i = 0; i < call.Inputs.Length; ++i)
			{
				TraverseDepthFirst(nodesByDepth, call.Inputs[i], newDepth);
			}
		}

		public bool Resave()
		{
			return GraphEditorUtils.SaveGraph(GPUGraph, FilePath);
		}
	}
}