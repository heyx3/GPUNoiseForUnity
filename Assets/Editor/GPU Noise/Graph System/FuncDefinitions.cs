using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
using ParamList = System.Collections.Generic.List<GPUNoise.Func.Param>;
using StringBuilder = System.Text.StringBuilder;


namespace GPUNoise
{
	/// <summary>
	/// Definitions of Func instances and of the shader code used to generate a material with them.
	/// </summary>
	public static class FuncDefinitions
	{
		/// <summary>
		/// Turns the given variable name into a nice display name.
		/// </summary>
		public static string PrettifyVarName(string name)
		{
			StringBuilder sb = new StringBuilder(name);
			for (int i = 0; i < sb.Length; ++i)
			{
				if (sb[i] == '_')
				{
					sb[i] = ' ';

					//Make the next letter uppercase.
					if (i + 1 < sb.Length && sb[i + 1] >= 'a' && sb[i + 1] <= 'z')
					{
						sb[i + 1] -= (char)('a' - 'A');
					}
				}
			}

			return sb.ToString().Trim();
		}


		//TODO: Two Funcs for getting the UV.x and UV.y, and another Func that gets a shader parameter.


		//Need a standard way to hash float, float2, and float3.
		public static string DefaultHash1 =
	"frac(sin(x * 78.233) * 43758.5453)";
		public static string DefaultHash2 =
	"frac(sin(dot(x, float2(12.9898, 78.233))) * 43758.5453)";
		public static string DefaultHash3 =
	"frac(sin(dot(x, float3(12.9898, 78.233, 36.34621))) * 43758.5453)";

		//Hash functions can be customized by the user.
		public static string Hash1SearchToken = "HASH1",
							 Hash2SearchToken = "HASH2",
							 Hash3SearchToken = "HASH3";

		public static string ShaderCodeBeginning =
			#region Shader code
	@"
	float hashValue1(float x)
	{
		return HASH1;
	}
	float hashValue2(float2 x)
	{
		return HASH2;
	}
	float hashValue3(float3 x)
	{
		return HASH3;
	}

	//The body (no open/close braces) for a function that interpolates between noise values on a 1D grid.
	//Allows customization of the interpolant (see LinearNoise1 and SmoothNoise1 for simple examples).
	#define INTERP_NOISE1(vModifier) \
		float minV = floor(inV), \
			  maxV = ceil(inV); \
	\
		float v = inV - minV; \
		v = vModifier; \
	\
		return lerp(hashValue1(minV), \
					hashValue1(maxV), \
					v);
	//The body (no open/close braces) for a function that interpolates between noise values on a 2D grid.
	//Allows customization of the interpolant (see LinearNoise2 and SmoothNoise2 for simple examples).
	#define INTERP_NOISE2(vModifier) \
		float2 minV = floor(inV), \
			   maxV = ceil(inV); \
	\
		float2 v = inV - minV; \
		v = vModifier; \
	\
		return lerp(lerp(hashValue2(minV), \
						 hashValue2(float2(maxV.x, minV.y)), \
						 v.x), \
					lerp(hashValue2(float2(minV.x, maxV.y)), \
						 hashValue2(maxV), \
						 v.x), \
					v.y);
	//The body (no open/close braces) for a function that interpolates between noise values on a 3D grid.
	//Allows customization of the interpolant (see LinearNoise3 and SmoothNoise3 for simple examples).
	#define INTERP_NOISE3(vModifier) \
		float3 minV = floor(inV), \
			   maxV = ceil(inV); \
	\
		float3 v = inV - minV; \
		v = vModifier; \
	\
		return lerp(lerp(lerp(hashValue3(minV), \
							  hashValue3(float3(maxV.x, minV.y, minV.z)), \
							  v.x), \
						 lerp(hashValue3(float3(minV.x, maxV.y, minV.z)), \
							  hashValue3(float3(maxV.xy, minV.z)), \
							  v.x), \
						 v.y), \
					lerp(lerp(hashValue3(float3(minV.xy, maxV.z)), \
							  hashValue3(float3(maxV.x, minV.y, maxV.z)), \
							  v.x), \
						 lerp(hashValue3(float3(minV.x, maxV.y, maxV.z)), \
							  hashValue3(maxV), \
							  v.x), \
						 v.y), \
					v.z);
	";
			#endregion

		/// <summary>
		/// Creates a function that has one parameter, "f".
		/// </summary>
		/// <param name="expression">An expression that evaluates to a float, e.x. "f + 2.0".</param>
		private static Func MakeSimple1(string name, string expression,
										float defaultVal = float.NaN, string paramName = "f")
		{
			return new Func(name, new ParamList() { new Func.Param(paramName, defaultVal) },
							"{ return " + expression + "; }");
		}
		/// <summary>
		/// Creates a function that has two parameters, "f1" and "f2".
		/// </summary>
		/// <param name="expression">An expression that evaluates to a float, e.x. "f1 + f2".</param>
		private static Func MakeSimple2(string name, string expression,
										float defaultVal1 = float.NaN, float defaultVal2 = float.NaN,
										string paramName1 = "f1", string paramName2 = "f2")
		{
			return new Func(name,
							new ParamList() {
								new Func.Param(paramName1, defaultVal1),
								new Func.Param(paramName2, defaultVal2) },
							"{ return " + expression + "; }");
		}
		/// <summary>
		/// Creates a function that has three parameters, "f1", "f2", and "f3".
		/// </summary>
		/// <param name="expression">An expression that evaluates to a float, e.x. "f1 + f2 + f3".</param>
		private static Func MakeSimple3(string name, string expression,
										float defaultVal1 = float.NaN, float defaultVal2 = float.NaN,
										float defaultVal3 = float.NaN,
										string paramName1 = "f1", string paramName2 = "f2",
										string paramName3 = "f3")
		{
			return new Func(name,
							new ParamList() {
								new Func.Param(paramName1, defaultVal1),
								new Func.Param(paramName2, defaultVal2),
								new Func.Param(paramName3, defaultVal3) },
							"{ return " + expression + "; }");
		}
		public static Func[] Functions = new Func[]
		{
			#region Functions
			 
			new Func(@"float WhiteNoise1(float f)
					{
						return hashValue1(f);
					}"),
			new Func(@"float WhiteNoise2(float fX, float fY)
					{
						return hashValue2(float2(fX, fY));
					}"),
			new Func(@"float WhiteNoise3(float fX, float fY, float fZ)
					{
						return hashValue3(float3(fX, fY, fZ));
					}"),
			new Func(@"float GridNoise1(float f)
					{
						return hashValue1(floor(f));
					}"),
			new Func(@"float GridNoise2(float fX, float fY)
					{
						return hashValue2(floor(float2(fX, fY)));
					}"),
			new Func(@"float GridNoise3(float fX, float fY, float fZ)
					{
						return hashValue3(floor(float3(fX, fY, fZ)));
					}"),
			new Func(@"float LinearNoise1(float inV)
					{
						INTERP_NOISE1(v)
					}"),
			new Func(@"float LinearNoise2(float fX, float fY)
					{
						float2 inV = float2(fX, fY);
						INTERP_NOISE2(v)
					}"),
			new Func(@"float LinearNoise3(float fX, float fY, float fZ)
					{
						float3 inV = float3(fX, fY, fZ);
						INTERP_NOISE3(v)
					}"),
			new Func(@"float SmoothNoise1(float inV)
					{
						INTERP_NOISE1(smoothstep(0.0, 1.0, v))
					}"),
			new Func(@"float SmoothNoise2(float fX, float fY)
					{
						float2 inV = float2(fX, fY);
						INTERP_NOISE2(smoothstep(0.0, 1.0, v))
					}"),
			new Func(@"float SmoothNoise3(float fX, float fY, float fZ)
					{
						float3 inV = float3(fX, fY, fZ);
						INTERP_NOISE3(smoothstep(0.0, 1.0, v))
					}"),
			new Func(@"float SmootherNoise1(float inV)
					{
						INTERP_NOISE1(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, v)))
					}"),
			new Func(@"float SmootherNoise2(float fX, float fY)
					{
						float2 inV = float2(fX, fY);
						INTERP_NOISE2(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, v)))
					}"),
			new Func(@"float SmootherNoise3(float fX, float fY, float fZ)
					{
						float3 inV = float3(fX, fY, fZ);
						INTERP_NOISE3(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, v)))
					}"),
			MakeSimple1("Fract", "frac(f)"),
			MakeSimple1("Ceil", "ceil(f)"),
			MakeSimple1("Floor", "floor(f)"),
			MakeSimple1("Truncate", "trunc(f)"),
			MakeSimple1("RoundToInt", "round(f)"),
			MakeSimple1("Sign", "sign(f)"),
			MakeSimple1("Abs", "abs(f)"),
			MakeSimple1("Cos", "cos(f)"),
			MakeSimple1("Sin", "sin(f)"),
			MakeSimple1("Tan", "tan(f)"),
			MakeSimple1("Acos", "acos(f)"),
			MakeSimple1("Asin", "asin(f)"),
			MakeSimple1("Atan", "atan(f)"),
			MakeSimple1("Sqrt", "sqrt(f)"),
			MakeSimple1("Log", "log(f)"),
			MakeSimple2("Add", "f1 + f2", 0.0f, 0.0f),
			MakeSimple2("Subtract", "f1 - f2", 0.0f, 0.0f),
			MakeSimple2("Multiply", "f1 * f2", 1.0f, 1.0f),
			MakeSimple2("Divide", "f1 / f2", 1.0f, 1.0f),
			MakeSimple2("Max", "max(f1, f2)", 0.0f, 0.0f),
			MakeSimple2("Min", "min(f1, f2)", 1.0f, 1.0f),
			MakeSimple2("Pow", "pow(x, y)", float.NaN, 1.0f, "x", "y"),
			MakeSimple2("Step", "step(y, x)", 0.5f, float.NaN, "y", "x"),
			MakeSimple2("Atan2", "atan2(y, x)", float.NaN, float.NaN, "y", "x"),
			MakeSimple3("Clamp", "clamp(f, low, high)", 0.0f, 1.0f, float.NaN, "low", "high", "f"),
			MakeSimple3("Lerp", "lerp(x, y, t)", float.NaN, float.NaN, float.NaN, "x", "y", "t"),
			MakeSimple3("Smoothstep", "smoothstep(x, y, t)", 0.0f, 1.0f, float.NaN, "x", "y", "t"),
			new Func(@"float Remap(float srcMin = 0, float srcMax = 1, float destMin = 0, float destMax = 1, float srcValue)
					{
						return lerp(destMin, destMax, (srcValue - srcMin) / (srcMax - srcMin));
					}"),

			new TexCoordNode("x"),
			new TexCoordNode("y"),
			new FloatParamNode(),
			new SliderParamNode(),

			#endregion
		};

		public static Dictionary<string, Func> FunctionsByName;

		static FuncDefinitions()
		{
			FunctionsByName = new Dictionary<string, Func>();
			foreach (Func f in Functions)
				FunctionsByName.Add(f.Name, f);
		}
	}


	#region Special Nodes

	public class TexCoordNode : Func
	{
		private string XOrY;

		public TexCoordNode(string xOrY)
			: base("UV_" + xOrY, new ParamList(), "{ return 0.0f; }")
		{
			XOrY = xOrY;
		}

		public override string GetInvocation(Func.ExtraData customDat, string paramList)
		{
			return "IN.texcoord." + XOrY;
		}

	}
	public class FloatParamNode : Func
	{
		[Serializable]
		public class FloatParamData : ExtraData
		{
			public string VarName;
			public float DefaultValue;

			public FloatParamData(string varName, float defaultValue)
			{
				VarName = varName;
				DefaultValue = defaultValue;
			}
		}


		public FloatParamNode()
			: base("FloatParam", new ParamList(), "{ return 0.0f; }")
		{

		}


		public override Func.ExtraData InitCustomGUI()
		{
			return new FloatParamData("MyFloat", 0.0f);
		}
		public override void CustomGUI(Func.ExtraData myData)
		{
			FloatParamData dat = (FloatParamData)myData;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Var Name:");
			dat.VarName = GUILayout.TextField(dat.VarName).Trim().Replace(' ', '_').Replace("\t", "");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Default Value:");
			dat.DefaultValue = EditorGUILayout.FloatField(dat.DefaultValue);
			EditorGUILayout.EndHorizontal();
		}

		public override string GetInvocation(Func.ExtraData customDat, string paramList)
		{
			FloatParamData dat = (FloatParamData)customDat;
			return dat.VarName;
		}

		public override void GetPropertyDeclarations(Func.ExtraData customDat, StringBuilder shaderText)
		{
			FloatParamData dat = (FloatParamData)customDat;

			shaderText.Append("\t\t\t");
			shaderText.Append(dat.VarName);
			shaderText.Append(" (\"");
			shaderText.Append(FuncDefinitions.PrettifyVarName(dat.VarName));
			shaderText.Append("\", Float) = ");
			shaderText.AppendLine(dat.DefaultValue.ToString());
		}
		public override void GetParamDeclarations(ExtraData customDat, StringBuilder shaderText)
		{
			FloatParamData dat = (FloatParamData)customDat;

			shaderText.Append("\t\t\t\tfloat ");
			shaderText.Append(dat.VarName);
			shaderText.AppendLine(";");
		}
	}
	public class SliderParamNode : Func
	{
		[Serializable]
		public class SliderParamData : ExtraData
		{
			public string VarName;
			public float DefaultLerp;
			public float Min, Max;

			public SliderParamData(string varName, float min, float max, float defaultLerp)
			{
				VarName = varName;
				DefaultLerp = defaultLerp;
				Min = min;
				Max = max;
			}
		}


		public SliderParamNode()
			: base("SliderParam", new ParamList(), "{ return 0.0f; }")
		{

		}


		public override Func.ExtraData InitCustomGUI()
		{
			return new SliderParamData("MyFloat", 0.0f, 1.0f, 0.5f);
		}
		public override void CustomGUI(Func.ExtraData myData)
		{
			SliderParamData dat = (SliderParamData)myData;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Var Name:");
			dat.VarName = GUILayout.TextField(dat.VarName).Trim().Replace(' ', '_').Replace("\t", "");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Min:");
			dat.Min = EditorGUILayout.FloatField(dat.Min);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Max:");
			dat.Max = EditorGUILayout.FloatField(dat.Max);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Default Value:");
			float newDefault = EditorGUILayout.Slider(Mathf.Lerp(dat.Min, dat.Max, dat.DefaultLerp),
													  dat.Min, dat.Max);
			dat.DefaultLerp = Mathf.InverseLerp(dat.Min, dat.Max, newDefault);
			EditorGUILayout.EndHorizontal();
		}

		public override string GetInvocation(Func.ExtraData customDat, string paramList)
		{
			SliderParamData dat = (SliderParamData)customDat;
			return dat.VarName;
		}

		public override void GetPropertyDeclarations(Func.ExtraData customDat, StringBuilder shaderText)
		{
			SliderParamData dat = (SliderParamData)customDat;

			shaderText.Append("\t\t\t");
			shaderText.Append(dat.VarName);
			shaderText.Append(" (\"");
			shaderText.Append(FuncDefinitions.PrettifyVarName(dat.VarName));
			shaderText.Append("\", Range(");
			shaderText.Append(dat.Min);
			shaderText.Append(", ");
			shaderText.Append(dat.Max);
			shaderText.Append(")) = ");
			shaderText.AppendLine(Mathf.Lerp(dat.Min, dat.Max, dat.DefaultLerp).ToString());
		}
		public override void GetParamDeclarations(ExtraData customDat, StringBuilder shaderText)
		{
			SliderParamData dat = (SliderParamData)customDat;

			shaderText.Append("\t\t\t\tfloat ");
			shaderText.Append(dat.VarName);
			shaderText.AppendLine(";");
		}
	}

	#endregion
}