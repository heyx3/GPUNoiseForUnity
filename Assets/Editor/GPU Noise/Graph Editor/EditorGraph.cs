using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using GPUNoise;


namespace GPUNoise.Editor
{
	/// <summary>
	/// The visual representation of a Graph.
	/// </summary>
	[Serializable]
	public class EditorGraph : ISerializable
	{
		/// <summary>
		/// The name of this graph.
		/// </summary>
		public string Name;
		/// <summary>
		/// A description of this graph.
		/// </summary>
		public string Description;

		/// <summary>
		/// The graph for the shader.
		/// </summary>
		public Graph GPUGraph = new Graph(new FuncInput(1.0f));

		/// <summary>
		/// The position of each FuncCall on the visual graph.
		/// </summary>
		public Dictionary<long, Vector2> FuncCallPoses = new Dictionary<long, Vector2>();


		public EditorGraph(string name, string description)
		{
			Name = name;
			Description = description;
		}


		//Serialization support.
		protected EditorGraph(SerializationInfo info, StreamingContext context)
		{
			Name = info.GetString("Name");
			Description = info.GetString("Description");

			GPUGraph = (Graph)info.GetValue("Graph", typeof(Graph));

			int nCalls = info.GetInt32("NFuncCalls");
			for (int i = 0; i < nCalls; ++i)
			{
				string iStr = i.ToString();
				FuncCallPoses.Add(info.GetInt64("UID" + iStr),
								  new Vector2(info.GetSingle("PosX" + iStr),
											  info.GetSingle("PosY" + iStr)));
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Name", Name);
			info.AddValue("Description", Description);

			info.AddValue("Graph", GPUGraph);

			info.AddValue("NFuncCalls", FuncCallPoses.Count);
			int count = 0;
			foreach (KeyValuePair<long, Vector2> kvp in FuncCallPoses)
			{
				string cStr = count.ToString();
				info.AddValue("UID" + cStr, kvp.Key);
				info.AddValue("PosX" + cStr, kvp.Value.x);
				info.AddValue("PosY" + cStr, kvp.Value.y);
			}
		}
	}
}