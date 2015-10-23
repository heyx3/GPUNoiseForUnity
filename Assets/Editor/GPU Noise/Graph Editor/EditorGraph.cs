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
		private static readonly float FuncCallCellSize = 50.0f;

		private static Rect ToR(float x, float y)
		{
			return new Rect(Mathf.RoundToInt(x), Mathf.RoundToInt(y), 100.0f, 50.0f);
		}


		/// <summary>
		/// The full path to the graph file.
		/// </summary>
		public string FilePath = "C:/MyGraph." + GraphUtils.Extension;

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
			GPUGraph = GraphUtils.LoadGraph(FilePath);

			//First, store each node by its depth and then by its position along that depth (i.e. breadth).
			List<List<long>> nodesByDepth = new List<List<long>> { new List<long>() { -1 } };
			TraverseDepthFirst(nodesByDepth, GPUGraph.Output, 1);

			//Next, position all those nodes.
			for (int col = 0; col < nodesByDepth.Count; ++col)
			{
				float lerpX = (float)col / (float)nodesByDepth.Count;
				for (int row = 0; row < nodesByDepth[col].Count; ++row)
				{
					float lerpY = (float)row / (float)nodesByDepth[col].Count;

					FuncCallPoses.Add(nodesByDepth[col][row],
									  ToR(Mathf.Lerp(viewRect.xMax, viewRect.xMin, lerpX) -
											(0.5f * viewRect.width / nodesByDepth.Count),
										  Mathf.Lerp(viewRect.yMin, viewRect.yMax, lerpY)));
				}
			}
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
			return GraphUtils.SaveGraph(GPUGraph, FilePath);
		}
	}
}