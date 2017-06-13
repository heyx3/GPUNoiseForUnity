using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;


namespace GPUGraph
{
	[Serializable]
	public struct Texture2DParamInfo
	{
		public string Name;
		public Texture2D DefaultVal;

		public Texture2DParamInfo(string name, Texture2D defaultVal = null) { Name = name; DefaultVal = defaultVal; }
	}


	/// <summary>
	/// A node whose output is the Red component of a 2D texture.
	/// </summary>
	[Serializable]
	public class ParamNode_Texture2D : Node
	{
		private static List<NodeInput> startInputs = new List<NodeInput>()
		{
			new NodeInput(float.NaN), new NodeInput(float.NaN),
			new NodeInput(1.0f), new NodeInput(1.0f),
			new NodeInput(0.0f), new NodeInput(0.0f),
		};
		private static List<string> startInputNames = new List<string>()
		{
			"x", "y", "Scale X", "Scale Y", "Offset X", "Offset Y",
		};
		private static List<float> startInputDefaultVals = new List<float>()
		{
			float.NaN, float.NaN, 1.0f, 1.0f, 0.0f, 0.0f,
		};


		public Texture2DParamInfo Param;


		public override Color GUIColor { get { return new Color(0.85f, 1.0f, 0.85f); } }
		public override string PrettyName { get { return "2D Texture Parameter"; } }


		public ParamNode_Texture2D(Rect pos, Texture2DParamInfo param)
			: base(pos, startInputs, startInputNames, startInputDefaultVals)
		{
			Param = param;
		}
		private ParamNode_Texture2D() { }


		protected override Node MakeClone()
		{
			ParamNode_Texture2D tx = new ParamNode_Texture2D();
			tx.Param = Param;
			return tx;
		}

		public override void EmitProperties(StringBuilder outCode)
		{
			outCode.Append("\t\t\t");
			outCode.Append(Param.Name);
			outCode.Append(" (\"");
			outCode.Append(StringUtils.PrettifyVarName(Param.Name));
			outCode.Append("\", 2D) = \"\" {}");
		}
		public override void EmitDefs(StringBuilder outCode)
		{
			outCode.Append("\t\t\t\tsampler2D ");
			outCode.Append(Param.Name);
			outCode.AppendLine(";");
		}
		public override void EmitCode(StringBuilder outCode)
		{
			outCode.Append("float ");
			outCode.Append(OutputName);
			outCode.Append(" = tex2D(");
			outCode.Append(Param.Name);
			outCode.Append(", (float2(");
			outCode.Append(Inputs[0].GetExpression(Owner));
			outCode.Append(", ");
			outCode.Append(Inputs[1].GetExpression(Owner));
			outCode.Append(") * float2(");
			outCode.Append(Inputs[2].GetExpression(Owner));
			outCode.Append(", ");
			outCode.Append(Inputs[3].GetExpression(Owner));
			outCode.Append(")) + float2(");
			outCode.Append(Inputs[4].GetExpression(Owner));
			outCode.Append(", ");
			outCode.Append(Inputs[5].GetExpression(Owner));
			outCode.AppendLine(")).r;");
		}

		protected override bool CustomGUI()
		{
			string _name = Param.Name;
			Texture2D _defVal = Param.DefaultVal;

			Param.Name = GUILayout.TextField(Param.Name);
			Param.DefaultVal = (Texture2D)EditorGUILayout.ObjectField("Default value:", Param.DefaultVal,
																	  typeof(Texture2D), false);
			if (Param.DefaultVal != _defVal)
			{
				string newPath = AssetDatabase.GetAssetPath(Param.DefaultVal);
				if (newPath == "Resources/unity_builtin_extra" ||
					newPath == "Resources\\unity_builtin_extra")
				{
					Debug.LogWarning("Built-in Unity textures cannot be used as Tex2D parameter default values!");
				}
			}

			return Param.DefaultVal != _defVal ||
				   _name != Param.Name;
		}


		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("VarName", Param.Name);
			info.AddValue("DefaultVal", AssetDatabase.GetAssetPath(Param.DefaultVal));
		}
		public ParamNode_Texture2D(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Param.Name = info.GetString("VarName");

			string path = info.GetString("DefaultVal");
			Param.DefaultVal = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}
	}
}