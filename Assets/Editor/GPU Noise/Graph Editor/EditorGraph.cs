using System;
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
		/// </summary>
		public Dictionary<long, Vector2> FuncCallPoses = new Dictionary<long, Vector2>();


		public EditorGraph() { }
		public EditorGraph(string filePath)
		{
			FilePath = filePath;
			GPUGraph = GraphUtils.LoadGraph(FilePath);

			//TODO: Generate positions for the func calls. 
		}
	}
}