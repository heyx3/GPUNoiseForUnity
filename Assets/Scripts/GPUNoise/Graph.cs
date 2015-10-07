using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace GPUNoise
{
	/// <summary>
	/// A set of Func calls that output a final float value.
	/// </summary>
	[Serializable]
	public class Graph : ISerializable
	{
		/// <summary>
		/// The final output value of this graph.
		/// </summary>
		public FuncInput Output;

		/// <summary>
		/// The name of this graph.
		/// </summary>
		public string Name;
		/// <summary>
		/// A description of this graph.
		/// </summary>
		public string Description;

		/// <summary>
		/// The 1D, 2D, and 3D hash functions this graph is using.
		/// </summary>
		public string Hash1, Hash2, Hash3;

		public Dictionary<long, FuncCall> UIDToFuncCall { get; private set; }

		/// <summary>
		/// The UID to use for the next FuncCall that's created.
		/// </summary>
		private long nextUID;


		public Graph(string name, string description, FuncInput output = new FuncInput())
		{
			Name = name;
			Description = description;
			Output = output;
			nextUID = 0;

			Hash1 = FuncDefinitions.DefaultHash1;
			Hash2 = FuncDefinitions.DefaultHash2;
			Hash3 = FuncDefinitions.DefaultHash3;

			UIDToFuncCall = new Dictionary<long, FuncCall>();
		}

		/// <summary>
		/// Adds the given Func call to this graph.
		/// </summary>
		public void CreateFuncCall(FuncCall call)
		{
			long uid = nextUID;
			nextUID += 1;

			call.UID = uid;
			UIDToFuncCall.Add(uid, call);
		}

		/// <summary>
		/// Generates the shader code for this graph.
		/// The shader assumes a quad with vertices from {-1, -1} to {1, 1}
		/// and renders this graph's noise into the render target (ignoring camera information).
		/// If an error occurred, it is output into Unity's debug console and "null" is returned.
		/// </summary>
		/// <param name="shaderName">The name to go at the top of the shader source.</param>
		/// <param name="outputComponent">Which component (r, g, b, or a) to output to.</param>
		public string GenerateShader(string shaderName, string outputComponent = "r")
		{
			if (outputComponent != "r" && outputComponent != "g" &&
				outputComponent != "b" && outputComponent != "a")
			{
				Debug.LogError("Output component must be 'r', 'b', 'g', or 'a', not '" +
							   outputComponent + "'");
				return null;
			}

			System.Text.StringBuilder sb = new System.Text.StringBuilder();
		
			sb.Append("Shader \"");
			sb.Append(shaderName);
			sb.Append("\"");
			sb.AppendLine(@"
	{
		Properties
		{
	
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
					float4 vertex	: POSITION;
					float4 color	: COLOR;
					float2 texcoord	: TEXCOORD0;
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
					OUT.vertex = IN.vertex;
					OUT.texcoord = IN.texcoord;
					OUT.color = IN.color;

					return OUT;
				}

				//-----------Func declarations---------
				//-------------------------------------");
			//TODO: Add shader parameters to the above code.
	
			//Generate declarations for Func stuff.
			string beginning = FuncDefinitions.ShaderCodeBeginning;
			beginning = beginning.Replace(FuncDefinitions.Hash1SearchToken, Hash1);
			beginning = beginning.Replace(FuncDefinitions.Hash2SearchToken, Hash2);
			beginning = beginning.Replace(FuncDefinitions.Hash3SearchToken, Hash3);
			sb.AppendLine(beginning);
			foreach (Func f in FuncDefinitions.Functions)
			{
				try
				{
					sb.AppendLine(f.GetFunctionDecl());
				}
				catch (Exception e)
				{
					Debug.LogError("Exception getting function code for '" + f.Name + ": " + e.Message);
					return null;
				}
			}

			//Generate shader body.
			sb.Append(
	@"			//----------------End Func declarations-----------
				//------------------------------------------------

				fixed4 frag(v2f IN) : COLOR
				{
					fixed4 outCol = fixed4(1.0, 1.0, 1.0, 1.0);
					outCol.");
			sb.Append(outputComponent);
			sb.Append(" = ");
			sb.Append(Output.GetShaderExpression(this));
			sb.Append(@";
					return outCol;
				}
			ENDCG
			}
		}
	}");

			return sb.ToString();
		}


		//Serialization support.
		//PRIORITY: Use callbacks instead for this class: https://msdn.microsoft.com/en-us/library/ty01x675(v=vs.110).aspx
		protected Graph(SerializationInfo info, StreamingContext context)
		{
			Name = info.GetString("Name");
			Description = info.GetString("Description");
			Output = (FuncInput)info.GetValue("Output", typeof(FuncInput));
			nextUID = info.GetInt64("NextUID");
		
			Hash1 = info.GetString("Hash1");
			Hash2 = info.GetString("Hash2");
			Hash3 = info.GetString("Hash3");

			int nCalls = info.GetInt32("NFuncCalls");
			UIDToFuncCall = new Dictionary<long, FuncCall>(nCalls);
		
			for (int i = 0; i < nCalls; ++i)
			{
				FuncCall call = (FuncCall)info.GetValue("FuncCall" + i.ToString(), typeof(FuncCall));
				if (UIDToFuncCall.ContainsKey(call.UID))
				{
					//throw new SerializationException("Two FuncCall instances have UID of " + call.UID);
				}
				else
				{
					UIDToFuncCall.Add(call.UID, call);
				}
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Name", Name);
			info.AddValue("Description", Description);
			info.AddValue("Output", Output);
			info.AddValue("NextUID", nextUID);

			info.AddValue("Hash1", Hash1);
			info.AddValue("Hash2", Hash2);
			info.AddValue("Hash3", Hash3);

			info.AddValue("NFuncCalls", UIDToFuncCall.Count);
		
			int count = 0;
			foreach (FuncCall call in UIDToFuncCall.Values)
			{
				info.AddValue("FuncCall" + count.ToString(), call);
				count += 1;
			}
		}
	}
}