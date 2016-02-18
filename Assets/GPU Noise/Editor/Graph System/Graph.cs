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
		/// The 1D, 2D, and 3D hash functions this graph is using.
		/// </summary>
		public string Hash1, Hash2, Hash3;

		public Dictionary<long, FuncCall> UIDToFuncCall { get; private set; }

		/// <summary>
		/// The UID to use for the next FuncCall that's created.
		/// </summary>
		private long nextUID;


		public Graph(FuncInput output = new FuncInput())
		{
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
		/// <param name="outputComponent">Which components (r, g, b, or a) to output to.</param>
		/// <param name="defaultColor">
		/// The color (generally 0-1) of the color components which aren't set by the noise.
		/// </param>
		public string GenerateShader(string shaderName, string outputComponents = "rgb",
									 float defaultColor = 0.0f)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
		
			sb.Append("Shader \"");
			sb.Append(shaderName);
			sb.Append("\"");
			sb.AppendLine(@"
	{
		Properties
		{");
			foreach (FuncCall fc in UIDToFuncCall.Values)
				fc.Calling.GetPropertyDeclarations(fc.CustomDat, sb);
			sb.AppendLine(@"
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

				//------------Params---------------
				//--------------------------------");
			foreach (FuncCall fc in UIDToFuncCall.Values)
				fc.Calling.GetParamDeclarations(fc.CustomDat, sb);
			sb.AppendLine(@"

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
					fixed4 outCol = fixed4(");
			for (int i = 0; i < 4; ++i)
			{
				if (i != 0)
					sb.Append(", ");
				sb.Append(defaultColor.ToString());
			}
			sb.Append(@");
					outCol.");
			sb.Append(outputComponents);
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
		
		/// <summary>
		/// Gets all params used by this Graph.
		/// </summary>
		public void GetParams(List<FloatParamNode.FloatParamData> outFloatParams,
							  List<SliderParamNode.SliderParamData> outSliderParams)
		{
			foreach (FuncCall fc in UIDToFuncCall.Values)
			{
				if (fc.Calling is FloatParamNode)
				{
					outFloatParams.Add((FloatParamNode.FloatParamData)fc.CustomDat);
				}
				else if (fc.Calling is SliderParamNode)
				{
					outSliderParams.Add((SliderParamNode.SliderParamData)fc.CustomDat);
				}
			}
		}

		/// <summary>
		/// Removes all references to the given func call from this graph, if it exists.
		/// Returns whether any references to it actually existed.
		/// </summary>
		public bool RemoveFuncCall(FuncCall call) { return RemoveFuncCall(call.UID); }
		/// <summary>
		/// Removes all references to the given func call from this graph, if it exists.
		/// Returns whether any references to it actually existed.
		/// </summary>
		public bool RemoveFuncCall(long callUID)
		{
			if (!UIDToFuncCall.Remove(callUID))
			{
				return false;
			}

			//Replace all its outputs with default values.
			foreach (FuncCall fc in UIDToFuncCall.Values)
			{
				for (int i = 0; i < fc.Inputs.Length; ++i)
				{
					if (!fc.Inputs[i].IsAConstantValue && fc.Inputs[i].FuncCallID == callUID)
					{
						fc.Inputs[i] = new FuncInput(fc.Calling.Params[i].DefaultValue);
					}
				}
			}
			if (!Output.IsAConstantValue && Output.FuncCallID == callUID)
			{
				Output = new FuncInput(0.5f);
			}

			return true;
		}


		//Serialization support.

		private List<FuncCall> deserializedCalls = new List<FuncCall>();
		protected Graph(SerializationInfo info, StreamingContext context)
		{
			Output = (FuncInput)info.GetValue("Output", typeof(FuncInput));
			nextUID = info.GetInt64("NextUID");
		
			Hash1 = info.GetString("Hash1");
			Hash2 = info.GetString("Hash2");
			Hash3 = info.GetString("Hash3");

			int nCalls = info.GetInt32("NFuncCalls");
			for (int i = 0; i < nCalls; ++i)
			{
				deserializedCalls.Add((FuncCall)info.GetValue("FuncCall" + i.ToString(), typeof(FuncCall)));
			}
		}
		[OnDeserialized]
		private void FinalizeSerializedStuff(StreamingContext context)
		{
			UIDToFuncCall = new Dictionary<long, FuncCall>(deserializedCalls.Count);
			foreach (FuncCall c in deserializedCalls)
			{
				if (UIDToFuncCall.ContainsKey(c.UID))
				{
					throw new SerializationException("Two FuncCall instances have UID of " + c.UID);
				}
				else
				{
					UIDToFuncCall.Add(c.UID, c);
				}
			}
			deserializedCalls.Clear();
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
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