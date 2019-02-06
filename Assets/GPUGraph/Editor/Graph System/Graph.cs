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
		#region Shader template

		/// <summary>
		/// The template for a Graph shader.
		/// Is a normal Image-Effect-type shader, with the following tokens:
		///     #GPUG_ShaderName: replace with the shader's name
		///     #GPUG_Properties: replace with the shader's properties
		///     #GPUG_CgDefs: replace with the shader's various uniforms/functions
		///     #GPUG_FragBody: replace with the body of the fragment shader.
		///						Access the UVs with "IN.texcoord.xy".
		/// </summary>
		public static readonly string ShaderTemplate =
@"Shader ""#GPUG_ShaderName""
{
	Properties {
		#GPUG_Properties
	}
	SubShader
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
                float4 vertex    : POSITION;
                float4 color     : COLOR;
                float2 texcoord  : TEXCOORD0;
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
			
			#GPUG_CgDefs

			float4 frag(v2f IN) : COLOR
			{
				#GPUG_FragBody
			}
		ENDCG
		}
	}
}";

		public static readonly string ShaderToken_ShaderName = "#GPUG_ShaderName",
									  ShaderToken_Properties = "#GPUG_Properties",
									  ShaderToken_CgDefs = "#GPUG_CgDefs",
									  ShaderToken_FragBody = "#GPUG_FragBody";
		
		#endregion
		
		/// <summary>
		/// Generates a shader that uses the given set of graphs.
		/// The parameters in each graph are prefixed with "pN",
		///     where N is the 1-based index of that graph.
		/// </summary>
		/// <param name="returnFromFragShaderGivenGraphOutputPrefix">
		/// Given the StringBuilder for the fragment shader's body,
		///     emits a return statement for the fragment shader.
		/// To get the output from graph N (1-based index),
		///     use the shader variable "graphResultN".
		/// </param>
		public static string GenerateShader(string shaderName, ICollection<Graph> graphs,
										    Action<StringBuilder> returnFromFragShader)
		{
			if (graphs.Count == 0)
				return null;

			var shaderTxt = new StringBuilder(ShaderTemplate);
			var propBlock = new StringBuilder();
			var cgDefs = new StringBuilder();
			var fragBody = new StringBuilder();

			int nIDsPerGraph = int.MaxValue / graphs.Count;
			int i = 0;
			foreach (var graph in graphs)
			{
				int idOffset = i * nIDsPerGraph;
				string graphExpr = graph.InsertShaderCode(
								       propBlock, cgDefs, fragBody,
									   idOffset, "p" + (i + 1), (i == 0));

				i += 1;
				fragBody.Append("float graphResult");
				fragBody.Append(i);
				fragBody.Append(" = ");
				fragBody.Append(graphExpr);
				fragBody.AppendLine(";");
			}

			returnFromFragShader(fragBody);

			shaderTxt.Replace(ShaderToken_ShaderName, shaderName);
			shaderTxt.Replace(ShaderToken_Properties, propBlock.ToString());
			shaderTxt.Replace(ShaderToken_CgDefs, cgDefs.ToString());
			shaderTxt.Replace(ShaderToken_FragBody, fragBody.ToString());

			return shaderTxt.ToString();
		}

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


		/// <summary>
		/// Creates a graph for the given file path/GUID.
		/// </summary>
		public Graph(string file, bool isGUID = false)
			: this()
		{
			if (isGUID)
				file = UnityEditor.AssetDatabase.GUIDToAssetPath(file);
			FilePath = file;
		}
		public Graph()
		{
			Output = new NodeInput(0.5f);
			OutputPos = new Rect(200.0f, 0.0f, 100.0f, 50.0f);
		}


		public Graph Clone(int idOffset = 0)
		{
			Graph g = new Graph();
			g.NextUID = NextUID + idOffset;
			g.FilePath = FilePath;
			g.Output = (Output.IsAConstant ? Output : new NodeInput(Output.NodeID + idOffset));
			g.OutputPos = OutputPos;

			foreach (Node n in nodes)
				g.nodes.Add(n.Clone(g, false, idOffset));
			foreach (Node n in g.nodes)
				g.uidToNode.Add(n.UID, n);

			return g;
		}

		/// <summary>
		/// Simplifies this graph by running its nodes through the pre-processing stage.
		/// The whole graph will be reduced to its final form (i.e. no "weird" nodes like SubGraphNode).
		/// </summary>
		public void PreProcess()
		{
			List<Node> currentNodes = new List<Node>(nodes),
					   newNodes = new List<Node>();

			//Pre-process all nodes currently owned by the graph,
			//    and keep track of any new ones that were created.
			foreach (Node n in currentNodes)
				newNodes.AddRange(n.OnPreProcess());

			//Keep pre-processing the new nodes until no more new nodes are created.
			int infiniteLoopCounter = 0;
			while (newNodes.Count > 0)
			{
				infiniteLoopCounter += 1;
				if (infiniteLoopCounter > 500)
				{
					throw new Exception("Infinite loop of graph pre-processing!");
				}

				List<Node> newerNodes = new List<Node>();
				foreach (Node n in newNodes)
					newerNodes.AddRange(n.OnPreProcess());
				newNodes = newerNodes;
			}
		}

		public void AddNode(Node n, bool generateNewUID = true)
		{
			if (n.Owner != null)
			{
				n.Owner.RemoveNode(n);
			}

			if (generateNewUID)
			{
				n.UID = NextUID;
				NextUID += 1;
			}

			n.Owner = this;
			if (!nodes.Contains(n))
			{
				nodes.Add(n);
				uidToNode.Add(n.UID, n);
			}

			n.OnAddedToGraph();
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


        /// <summary>
        /// Inserts code for this graph into the given strings,
        ///     and outputs an expression that evaluates to the output of this graph.
        /// </summary>
        /// <param name="idOffset">
        /// Offsets the ids of the nodes in this graph
        ///     so that they do not conflict with the ID's of any other nodes in other graphs used by this shader.
        /// This offset should be larger than any ID of a node in any other graph.
        /// </param>
		/// <param name="paramPrefix">
		/// A prefix added to all of this graph's parameters when emitting shader code.
		/// </param>
        /// <param name="isFirstGraph">
        /// Indicates whether this is the first time that
        ///     generated code from a Graph is being inserted into this shader.
        /// If true, certain function definitions will be included into the shader code.
        /// </param>
        public string InsertShaderCode(StringBuilder shaderProperties, StringBuilder shaderCGDefines,
                                       StringBuilder shaderBody,
                                       int idOffset, string paramPrefix, bool isFirstGraph)
        {
            //Clone this graph so it can be pre-processed without changing the original.
            Graph g = Clone(idOffset);
			foreach (var node in g.nodes)
			{
				var fParam = node as ParamNode_Float;
				if (fParam != null)
				{
					fParam.Param = new FloatParamInfo(fParam.Param, paramPrefix + fParam.Param.Name);
					continue;
				}

				var tParam = node as ParamNode_Texture2D;
				if (tParam != null)
				{
					tParam.Param = new Texture2DParamInfo(paramPrefix + tParam.Param.Name, tParam.Param.DefaultVal);
					continue;
				}
			}

            return g.InsertShad(shaderProperties, shaderCGDefines, shaderBody, isFirstGraph);
        }
        private string InsertShad(StringBuilder properties, StringBuilder cgProperties,
                                  StringBuilder body, bool addDefines)
        {
			PreProcess();

            if (addDefines)
            {
                cgProperties.AppendLine(ShaderDefs.Functions);
            }

            //Emit properties in no particular order.
            foreach (Node n in nodes)
            {
                n.EmitProperties(properties);
                n.EmitDefs(cgProperties);
            }

            //Emit code for all nodes in proper order.
            List<Node> toProcess = new List<Node>();
            HashSet<Node> processedAlready = new HashSet<GPUGraph.Node>();
            Dictionary<int, bool> uidDoneYet = new Dictionary<int, bool>();
            if (!Output.IsAConstant)
            {
				var seedNode = GetNode(Output.NodeID);
				if (seedNode == null)
					Debug.LogError("Couldn't find the start node, id: " + Output.NodeID);
				else
				{
					toProcess.Add(GetNode(Output.NodeID));
					uidDoneYet.Add(Output.NodeID, false);
				}
            }
            while (toProcess.Count > 0)
            {
                Node n = toProcess[toProcess.Count - 1];

                //If the next node hasn't been processed yet, add its inputs to the stack.
                if (!uidDoneYet[n.UID])
                {
                    foreach (NodeInput ni in n.Inputs)
                    {
                        if (!ni.IsAConstant)
                        {
							if (uidDoneYet.ContainsKey(ni.NodeID))
							{
								//Move the node up to the top of the stack.
								Node n2 = GetNode(ni.NodeID);
								if (n2 == null)
									Debug.LogError("Couldn't find node with id " + ni.NodeID);
								else
								{
									toProcess.Remove(n2);
									toProcess.Add(n2);
								}
							}
							else
                            {
								var nextN = GetNode(ni.NodeID);
								if (nextN == null)
									Debug.LogError("Couldn't find node with id " + ni.NodeID);
                                else
									toProcess.Add(nextN);
                                uidDoneYet.Add(ni.NodeID, false);
                            }
                        }
                    }

                    uidDoneYet[n.UID] = true;
                }
                //Otherwise, let the node emit its code and then remove it from the stack.
                else
                {
                    toProcess.RemoveAt(toProcess.Count - 1);
                    if (!processedAlready.Contains(n))
                    {
                        processedAlready.Add(n);
                        n.EmitCode(body);
                    }
                }
            }

            return Output.GetExpression(this);
        }

		/// <summary>
		/// Generates a shader that outputs this graph's noise in the given channels.
		/// </summary>
		/// <param name="shaderName">The shader's name in the file.</param>
		/// <param name="outputs">The channels to output this graph's noise into.</param>
		/// <param name="defaultVal">
		/// The value held by channels that didn't receive this graph's noise.
		/// </param>
		public string GenerateShader(string shaderName, string outputs, float defaultVal)
		{
			return GenerateShader(shaderName, (sb) => { }, (sb) => { },
				(o, sb) =>
				{
					string defaultValStr = defaultVal.ToString();

					sb.Append("return float4(");
					if (outputs.Contains('x') || outputs.Contains('r'))
						sb.Append(o);
					else
						sb.Append(defaultValStr);
					sb.Append(", ");
					if (outputs.Contains('y') || outputs.Contains('g'))
						sb.Append(o);
					else
						sb.Append(defaultValStr);
					sb.Append(", ");
					if (outputs.Contains('z') || outputs.Contains('b'))
						sb.Append(o);
					else
						sb.Append(defaultValStr);
					sb.Append(", ");
					if (outputs.Contains('w') || outputs.Contains('a'))
						sb.Append(o);
					else
						sb.Append(defaultValStr);
					sb.AppendLine(");");
				});
		}
		/// <summary>
		/// Generates a shader that outputs a color based on the graph's noise and a color ramp texture.
		/// </summary>
		/// <param name="shaderName">The shader's name in the file.</param>
		public string GenerateShader(string shaderName, string colorRampParamName)
		{
			return GenerateShader(shaderName,
				(sb) =>
				{
					sb.Append("\t\t\t");
					sb.Append(colorRampParamName);
					sb.Append(" (\"");
					sb.Append(StringUtils.PrettifyVarName(colorRampParamName));
					sb.AppendLine("\", 2D) = \"\" {}");
				},
				(sb) =>
				{
					sb.Append("\t\t\t\tsampler2D ");
					sb.Append(colorRampParamName);
					sb.AppendLine(";");
				},
				(o, sb) =>
				{
					sb.Append("return tex2D(");
					sb.Append(colorRampParamName);
					sb.Append(", float2(");
					sb.Append(o);
					sb.AppendLine(", 0.0));");
				});
		}
		/// <summary>
		/// Generates a shader with customized output.
		/// </summary>
		/// <param name="shaderName">The shader's name in the file.</param>
		/// <param name="addToProperties">Adds any Unity Shaderlab Properties to the given string.</param>
		/// <param name="addToDefs">Adds any Cg declarations to the given string.</param>
		/// <param name="returnFragmentColor">
		/// Outupts shader instructions that return a float4 color
		///     given the variable holding the graph's noise output.
		/// </param>
		public string GenerateShader(string shaderName,
									 Action<StringBuilder> addToProperties,
									 Action<StringBuilder> addToDefs,
									 Action<string, StringBuilder> returnFragmentColor)
		{
			//Generate the important parts of the shader code.
            StringBuilder properties = new StringBuilder(),
                          cgProperties = new StringBuilder(),
                          body = new StringBuilder();
            string graphOutExpr = InsertShaderCode(properties, cgProperties, body, 0, "", true);
			addToProperties(properties);
			addToDefs(cgProperties);
			body.Append("float graphNoiseResult = ");
			body.Append(graphOutExpr);
			body.AppendLine(";");
			returnFragmentColor("graphNoiseResult", body);

			//Build the shader from the template.
			var shaderTxt = new StringBuilder(ShaderTemplate);
			shaderTxt.Replace(ShaderToken_ShaderName, shaderName);
			shaderTxt.Replace(ShaderToken_Properties, properties.ToString());
			shaderTxt.Replace(ShaderToken_CgDefs, cgProperties.ToString());
			shaderTxt.Replace(ShaderToken_FragBody, body.ToString());

			//Add an extra line to the end of the file,
			//    then make sure all line endings are Unity-friendly.
			shaderTxt.Append("\r\n");
			shaderTxt.Replace("\r\n", "\n");

			return shaderTxt.ToString();
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
				return "Error opening/reading file: " + e.Message + "\n" + e.StackTrace;
			}
			finally
			{
				if (s != null)
					s.Close();
			}

			NextUID = g.NextUID;
			Output = g.Output;
			OutputPos = g.OutputPos;
			nodes = g.nodes;
			uidToNode = g.uidToNode;

			foreach (Node n in nodes)
				n.Owner = this;
			foreach (Node n in nodes)
				n.OnGraphLoaded();

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
				n.OnAddedToGraph();
			}
		}
	}
}