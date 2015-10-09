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
		public Dictionary<long, Vector2> FuncCallPoses = new Dictionary<long, Vector2>();


		public EditorGraph() { }

		public EditorGraph(string filePath)
		{
			FilePath = filePath;
			GPUGraph = GraphUtils.LoadGraph(FilePath);

			//Generate positions for the FuncCalls. Assume each node only outputs to one other node.

			//First, list each node by its depth and then by its position along that depth (i.e. breadth).
			//Split each breadth into groups based on which parent each node has (this is the innermost list).
			List<List<List<long>>> nodesByDepth = new List<List<List<long>>> { new List<List<long>>() { new List<long>() { -1 } } };
			TraverseDepthFirst(nodesByDepth, GPUGraph.Output, 1, 0);

			//TODO: Finish.
		}
		private void TraverseDepthFirst(List<List<List<long>>> nodesByDepth, FuncInput input, int inputDepth, int parentGroup)
		{
			if (input.IsAConstantValue)
			{
				return;
			}

			//Get the column this node is in.
			List<List<long>> myColumn;
			if (nodesByDepth.Count <= inputDepth)
			{
				nodesByDepth.Add(new List<List<long>>());
			}
			myColumn = nodesByDepth[inputDepth];

			//Get the parent group this node is in.
			List<long> myParentGroup;
			if (myColumn.Count <= parentGroup)
			{
				myColumn.Add(new List<long>());
			}
			myParentGroup = myColumn[parentGroup];

			//Add this node to the group.
			myParentGroup.Add(input.FuncCallID);

			//Now process this node's inputs.
			int newDepth = inputDepth + 1;
			int newParentGroup = 0;
			if (nodesByDepth.Count > newDepth)
			{
				newParentGroup = nodesByDepth[newDepth].Count;
			}
			FuncCall call = GPUGraph.UIDToFuncCall[input.FuncCallID];
			for (int i = 0; i < call.Inputs.Length; ++i)
			{
				TraverseDepthFirst(nodesByDepth, call.Inputs[i], newDepth, newParentGroup);
			}
		}
	}
}