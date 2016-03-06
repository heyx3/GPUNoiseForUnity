using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;


namespace GPUGraph
{
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


		public string Name;
		public Texture2D DefaultVal;


		public override string PrettyName { get { return "2D Texture Parameter"; } }


		public ParamNode_Texture2D(Rect pos, string name)
			: base(pos, startInputs, startInputNames, startInputDefaultVals)
		{
			Name = name;
			DefaultVal = Texture2D.whiteTexture;
		}
		private ParamNode_Texture2D() { }


		protected override Node MakeClone()
		{
			ParamNode_Texture2D tx = new ParamNode_Texture2D();
			tx.Name = Name;
			tx.DefaultVal = DefaultVal;
			return tx;
		}

		public override void EmitProperties(StringBuilder outCode)
		{
			outCode.Append("\t\t\t");
			outCode.Append(Name);
			outCode.Append(" (\"");
			outCode.Append(StringUtils.PrettifyVarName(Name));
			outCode.Append("\", 2D) = \"\" {}");
		}
		public override void EmitDefs(StringBuilder outCode)
		{
			outCode.Append("\t\t\t\tsampler2D ");
			outCode.Append(Name);
			outCode.AppendLine(";");
		}
		public override void EmitCode(StringBuilder outCode)
		{
			outCode.Append("float ");
			outCode.Append(OutputName);
			outCode.Append(" = tex2D(");
			outCode.Append(Name);
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
			string _name = Name;
			Texture2D _defVal = DefaultVal;

			Name = GUILayout.TextField(Name);
			DefaultVal = (Texture2D)EditorGUILayout.ObjectField("Default value:", DefaultVal,
																typeof(Texture2D), false);
			if (DefaultVal != _defVal)
			{
				string newPath = AssetDatabase.GetAssetPath(DefaultVal);
				if (newPath == "Resources/unity_builtin_extra" ||
					newPath == "Resources\\unity_builtin_extra")
				{
					Debug.LogWarning("Built-in Unity textures cannot be saved/loaded correctly!");
				}
			}

			return DefaultVal != _defVal ||
				   _name != Name;
		}


		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("VarName", Name);
			info.AddValue("DefaultVal", AssetDatabase.GetAssetPath(DefaultVal));
		}
		public ParamNode_Texture2D(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Name = info.GetString("VarName");

			string path = info.GetString("DefaultVal");
			DefaultVal = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}
	}
}