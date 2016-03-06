using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


namespace GPUGraph
{
	[Serializable]
	public class Graph : ISerializable
	{
		public static bool IsValidUID(int uid) { return uid >= 0; }


		/// <summary>
		/// The next node added to this graph will be given this ID.
		/// </summary>
		public int NextUID = 0;
		/// <summary>
		/// The filepath that this graph saves to/is read from.
		/// </summary>
		public string FilePath;

		/// <summary>
		/// The 1D, 2D, and 3D hash functions this graph is using.
		/// </summary>
		public string Hash1, Hash2, Hash3;

		/// <summary>
		/// The output of this graph.
		/// </summary>
		public NodeInput Output;
		/// <summary>
		/// The position of the "Output" node in the editor.
		/// </summary>
		public Rect OutputPos;


		private List<Node> nodes = new List<Node>();
		private Dictionary<int, Node> uidToNode = new Dictionary<int, Node>();


		/// <summary>
		/// All nodes currently in this graph.
		/// </summary>
		public IEnumerable<Node> Nodes { get { return nodes; } }


		public Graph(string filePath)
			: this()
		{
			FilePath = filePath;
		}
		public Graph()
		{
			Hash1 = ShaderDefs.DefaultHash1;
			Hash2 = ShaderDefs.DefaultHash2;
			Hash3 = ShaderDefs.DefaultHash3;
			Output = new NodeInput(0.5f);
			OutputPos = new Rect(200.0f, 0.0f, 100.0f, 50.0f);
		}


		public Graph Clone()
		{
			Graph g = new Graph();
			g.NextUID = NextUID;
			g.FilePath = FilePath;
			g.Output = Output;
			g.OutputPos = OutputPos;
			g.Hash1 = Hash1;
			g.Hash2 = Hash2;
			g.Hash3 = Hash3;

			foreach (Node n in nodes)
				g.nodes.Add(n.Clone(g, false));
			foreach (Node n in g.nodes)
				g.uidToNode.Add(n.UID, n);

			return this;
		}

		public void AddNode(Node n)
		{
			if (n.Owner != null)
			{
				n.Owner.RemoveNode(n);
			}

			n.UID = NextUID;
			n.Owner = this;
			if (!nodes.Contains(n))
			{
				nodes.Add(n);
				uidToNode.Add(n.UID, n);
			}

			NextUID += 1;
		}
		public void RemoveNode(Node n)
		{
			nodes.Remove(n);
			uidToNode.Remove(n.UID);
			n.Owner = null;

			//Remove any connections to the node.
			if (!Output.IsAConstant && Output.NodeID == n.UID)
				Output = new NodeInput(0.5f);
			foreach (Node n2 in nodes)
				for (int i = 0; i < n2.Inputs.Count; ++i)
					if (!n2.Inputs[i].IsAConstant && n.UID == n2.Inputs[i].NodeID)
						n2.Inputs[i] = new NodeInput(n2.GetInputDefaultValue(i));
		}

		/// <summary>
		/// Returns "null" if the given Node uid doesn't exist in this graph.
		/// </summary>
		public Node GetNode(int uid)
		{
			if (uidToNode.ContainsKey(uid))
			{
				return uidToNode[uid];
			}
			else
			{
				return null;
			}
		}


		public string GenerateShader(string shaderName, string outputs = "rgb", float defaultVal = 0.0f)
		{
			//Clone this graph so nodes can pre-process it before generating the shader.
			Graph g = Clone();
			return g.GenShad(shaderName, outputs, defaultVal);
		}
		private string GenShad(string shaderName, string outputs, float defaultVal)
		{
			//Let all nodes do pre-processing.
			{
				List<Node> currentNodes = new List<Node>(nodes),
						   newNodes = new List<Node>();
				foreach (Node n in currentNodes)
					newNodes.AddRange(n.OnPreProcess());

				while (newNodes.Count > 0)
				{
					List<Node> newerNodes = new List<Node>();
					foreach (Node n in newNodes)
						newerNodes.AddRange(n.OnPreProcess());
					newNodes = newerNodes;
				}
			}


			StringBuilder sb = new StringBuilder();
			sb.Append("Shader \"");
			sb.Append(shaderName);
			sb.Append("\"");
			sb.AppendLine(@"
	{
		Properties
		{");
			foreach (Node n in nodes)
				n.EmitProperties(sb);
			sb.AppendLine(@"
		}
		Subshader
		{
			Tags
			{
				""RenderType"" = ""Opaque""
				""PreviewType"" = ""Plane""
			}

			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }
			Blend One Zero

			Pass
			{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include ""UnityCG.cginc""

				struct appdata_t
				{
					float4 vertex	: POSITION;
					float4 color	: COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex	: SV_POSITION;
					fixed4 color	: COLOR;
					half2 texcoord	: TEXCOORD0;
				};

				v2f vert(appdata_t IN)
				{
					v2f OUT;
					OUT.vertex = (IN.vertex);
					OUT.texcoord = IN.texcoord;
					OUT.color = IN.color;

					return OUT;
				}

				//--------Generated stuff---------
				//--------------------------------");
				sb.AppendLine(ShaderDefs.GetHashFuncs(Hash1, Hash2, Hash3));
				sb.AppendLine(ShaderDefs.Functions);
				sb.AppendLine(@"

				//-----Node-generated stuff-----
				//------------------------------");
				foreach (Node n in nodes)
					n.EmitDefs(sb);
				sb.AppendLine(@"
				
				fixed4 frag(v2f IN) : COLOR
				{");

					//Emit code for all nodes in proper order.
					Stack<Node> toProcess = new Stack<Node>();
					Dictionary<int, bool> uidDoneYet = new Dictionary<int, bool>();
					if (!Output.IsAConstant)
					{
						toProcess.Push(GetNode(Output.NodeID));
						uidDoneYet.Add(Output.NodeID, false);
					}
					while (toProcess.Count > 0)
					{
						Node n = toProcess.Peek();

						//If the next node hasn't been processed yet, add its inputs to the stack.
						if (!uidDoneYet[n.UID])
						{
							foreach (NodeInput ni in n.Inputs)
							{
								if (!ni.IsAConstant)
								{
									if (!uidDoneYet.ContainsKey(ni.NodeID))
									{
										toProcess.Push(GetNode(ni.NodeID));
										uidDoneYet.Add(ni.NodeID, false);
									}
								}
							}

							uidDoneYet[n.UID] = true;
						}
						//Otherwise, let the node emit its code and then remove it from the stack.
						else
						{
							toProcess.Pop();
							n.EmitCode(sb);
						}
					}

					sb.Append("float outExpr = ");
					sb.Append(Output.GetExpression(this));
					sb.Append(";\n\t\t\t\t\t");
					sb.Append("return float4(");
					sb.Append(outputs.Contains('x') || outputs.Contains('r') ? "outExpr" : defaultVal.ToString());
					sb.Append(", ");
					sb.Append(outputs.Contains('y') || outputs.Contains('g') ? "outExpr" : defaultVal.ToString());
					sb.Append(", ");
					sb.Append(outputs.Contains('z') || outputs.Contains('b') ? "outExpr" : defaultVal.ToString());
					sb.Append(", ");
					sb.Append(outputs.Contains('w') || outputs.Contains('a') ? "outExpr" : defaultVal.ToString());
					sb.Append(@");
				}
			ENDCG
			}
		}
	}");

			return sb.ToString();
		}


		/// <summary>
		/// Saves this graph to its file-path.
		/// Returns an error message, or an empty string if nothing went wrong.
		/// </summary>
		public string Save()
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = null;
			try
			{
				stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None);
				formatter.Serialize(stream, this);
			}
			catch (Exception e)
			{
				return "Error opening/writing to file: " + e.Message;
			}
			finally
			{
				if (stream != null)
				{
					stream.Close();
				}
			}

			return "";
		}
		/// <summary>
		/// Re-loads this graph from its file-path, effectively wiping out any changes to it.
		/// Returns an error message, or an empty string if nothing went wrong.
		/// </summary>
		public string Load()
		{
			IFormatter formatter = new BinaryFormatter();
			Stream s = null;
			Graph g = null;

			try
			{
				s = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				g = (Graph)formatter.Deserialize(s);
			}
			catch (System.Exception e)
			{
				return "Error opening/reading file: " + e.Message;
			}
			finally
			{
				s.Close();
			}

			NextUID = g.NextUID;
			Hash1 = g.Hash1;
			Hash2 = g.Hash2;
			Hash3 = g.Hash3;
			Output = g.Output;
			OutputPos = g.OutputPos;
			nodes = g.nodes;
			uidToNode = g.uidToNode;

			return "";
		}


		//Serialization code:
		public Graph(SerializationInfo info, StreamingContext context)
		{
			NextUID = info.GetInt32("NextUID");
			Output = (NodeInput)info.GetValue("Output", typeof(NodeInput));

			float posX = info.GetSingle("OutputPosX"),
				  posY = info.GetSingle("OutputPosY"),
				  sizeX = info.GetSingle("OutputSizeX"),
				  sizeY = info.GetSingle("OutputSizeY");
			OutputPos = new Rect(posX, posY, sizeX, sizeY);
		
			Hash1 = info.GetString("Hash1");
			Hash2 = info.GetString("Hash2");
			Hash3 = info.GetString("Hash3");

			int nNodes = info.GetInt32("NNodes");
			for (int i = 0; i < nNodes; ++i)
			{
				nodes.Add((Node)info.GetValue("Node" + i.ToString(), typeof(Node)));
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("NextUID", NextUID);
			info.AddValue("Output", Output, typeof(NodeInput));

			info.AddValue("OutputPosX", OutputPos.x);
			info.AddValue("OutputPosY", OutputPos.y);
			info.AddValue("OutputSizeX", OutputPos.width);
			info.AddValue("OutputSizeY", OutputPos.height);

			info.AddValue("Hash1", Hash1);
			info.AddValue("Hash2", Hash2);
			info.AddValue("Hash3", Hash3);

			info.AddValue("NNodes", nodes.Count);
			for (int i = 0; i < nodes.Count; ++i)
			{
				info.AddValue("Node" + i.ToString(), nodes[i], typeof(Node));
			}
		}
		[OnDeserialized]
		private void FinalizeDeserialization(StreamingContext context)
		{
			uidToNode.Clear();
			foreach (Node n in nodes)
			{
				n.Owner = this;
				uidToNode.Add(n.UID, n);
			}
		}
	}
}