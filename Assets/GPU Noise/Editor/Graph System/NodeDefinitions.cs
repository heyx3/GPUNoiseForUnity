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
	public class SimpleNode : Node
	{
		public struct Param
		{
			public string Name;
			public float DefaultValue;

			public Param(string name, float defaultVal = float.NaN)
			{
				Name = name;
				DefaultValue = defaultVal;
			}
		}


		public override string PrettyName
		{
			get { return Name; }
		}

		public string Expr, Name;


		/// <summary>
		/// Creates a new node that calls a function with a constant number of inputs.
		/// </summary>
		/// <param name="expr">An expression using the given inputs.</param>
		/// <param name="inputs">The inputs used by the given expression.</param>
		public SimpleNode(Rect pos, string expr, string name, params Param[] inputs)
			: base(pos,
				   inputs.Select(p => new NodeInput(p.DefaultValue)).ToList(),
				   inputs.Select(p => p.Name).ToList(),
				   inputs.Select(p => p.DefaultValue).ToList())
		{
			Expr = expr;
			Name = name;
		}

		private SimpleNode(string expr, string name) { Expr = expr; Name = name; }


		protected override Node MakeClone()
		{
			return new SimpleNode(Expr, Name);
		}

		public override void EmitCode(StringBuilder outCode)
		{
			outCode.Append("float ");
			outCode.Append(OutputName);
			outCode.Append(" = (");
			string expr = Expr;
			for (int i = 0; i < Inputs.Count; ++i)
				expr = expr.Replace(GetInputName(i), Inputs[i].GetExpression(Owner));
			outCode.Append(expr);
			outCode.AppendLine(");");
		}


		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Expression", Expr);
			info.AddValue("MyName", Name);
		}
		public SimpleNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Expr = info.GetString("Expression");
			Name = info.GetString("MyName");
		}
	}


	[Serializable]
	public class ParamNode_Float : Node
	{
		public string Name;
		public float DefaultValue;

		public bool IsSlider;
		public float SliderMin, SliderMax;


		public override string OutputName { get { return Name; } }
		public override string PrettyName { get { return "Scalar Param"; } }


		public ParamNode_Float(Rect pos, string name, float defaultVal = 0.0f)
			: base(pos, new List<NodeInput>(), new List<string>(), new List<float>())
		{
			Name = name;
			DefaultValue = defaultVal;
			IsSlider = false;
		}
		public ParamNode_Float(Rect pos, string name, float sliderMin, float sliderMax, float defaultLerp = 0.5f)
			: base(pos, new List<NodeInput>(), new List<string>(), new List<float>())
		{
			Name = name;
			DefaultValue = defaultLerp;
			IsSlider = true;
			SliderMin = sliderMin;
			SliderMax = sliderMax;
		}
		private ParamNode_Float() { }


		protected override Node MakeClone()
		{
			ParamNode_Float fl = new ParamNode_Float();
			fl.Name = Name;
			fl.DefaultValue = DefaultValue;
			fl.IsSlider = IsSlider;
			fl.SliderMin = SliderMin;
			fl.SliderMax = SliderMax;
			return fl;
		}

		public override void EmitProperties(StringBuilder outCode)
		{
			outCode.Append("\t\t\t");
			outCode.Append(Name);
			outCode.Append(" (\"");
			outCode.Append(StringUtils.PrettifyVarName(Name));
			outCode.Append("\", ");
			if (IsSlider)
			{
				outCode.Append("Range(");
				outCode.Append(SliderMin);
				outCode.Append(", ");
				outCode.Append(SliderMax);
				outCode.Append(")) = ");
				outCode.Append(Mathf.Lerp(SliderMin, SliderMax, DefaultValue));
			}
			else
			{
				outCode.Append("Float) = ");
				outCode.Append(DefaultValue);
			}
			outCode.AppendLine();
		}
		public override void EmitDefs(StringBuilder outCode)
		{
			outCode.Append("\t\t\t\tfloat ");
			outCode.Append(Name);
			outCode.AppendLine(";");
		}

		protected override bool CustomGUI()
		{
			string _name = Name;
			float _defVal = DefaultValue;
			bool _isSlider = IsSlider;
			float _min = SliderMin;
			float _max = SliderMax;

			Name = GUILayout.TextField(Name);

			if (IsSlider)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Min:");
				SliderMin = EditorGUILayout.FloatField(SliderMin);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Max:");
				SliderMax = EditorGUILayout.FloatField(SliderMax);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Default value:");
				DefaultValue = Mathf.InverseLerp(SliderMin, SliderMax,
												 GUILayout.HorizontalSlider(Mathf.Lerp(SliderMin, SliderMax,
																					   DefaultValue),
																			SliderMin, SliderMax));
				GUILayout.EndHorizontal();

				IsSlider = !GUILayout.Button("Remove slider");
			}
			else
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Default value:");
				DefaultValue = EditorGUILayout.FloatField(DefaultValue);
				GUILayout.EndHorizontal();

				IsSlider = GUILayout.Button("Make slider");
			}

			return Name != _name ||
				   AreFloatsDifferent(DefaultValue, _defVal) ||
				   _isSlider != IsSlider ||
				   AreFloatsDifferent(_min, SliderMin) ||
				   AreFloatsDifferent(_max, SliderMax);
		}


		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("VarName", Name);
			info.AddValue("DefaultVal", DefaultValue);
			info.AddValue("IsSlider", IsSlider);
			if (IsSlider)
			{
				info.AddValue("SliderMin", SliderMin);
				info.AddValue("SliderMax", SliderMax);
			}
		}
		public ParamNode_Float(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Name = info.GetString("VarName");
			DefaultValue = info.GetSingle("DefaultVal");
			IsSlider = info.GetBoolean("IsSlider");
			if (IsSlider)
			{
				SliderMin = info.GetSingle("SliderMin");
				SliderMax = info.GetSingle("SliderMax");
			}
		}
	}
	
	[Serializable]
	public class TexCoordNode : Node
	{
		public bool IsX;


		public override string OutputName { get { return "IN.texcoord." + (IsX ? "x" : "y"); } }
		public override string PrettyName { get { return "UV " + (IsX ? "X" : "Y"); } }


		public TexCoordNode(Rect pos, bool isX)
			: base(pos, new List<NodeInput>(), new List<string>(), new List<float>())
		{
			IsX = isX;
		}
		private TexCoordNode(bool isX) { IsX = isX; }


		protected override Node MakeClone()
		{
			return new TexCoordNode(IsX);
		}

		protected override bool CustomGUI()
		{
			bool _isX = IsX;

			int result = EditorGUILayout.Popup((IsX ? 0 : 1), new string[] { "X", "Y" });
			IsX = (result == 0 ? true : false);

			return IsX != _isX;
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("IsX", IsX);
		}
		public TexCoordNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			IsX = info.GetBoolean("IsX");
		}
	}

	//TODO: ParamNode_Texture, SubGraphNode, CustomWorleyNode, LayeredNoiseNode (each layer has changeable weight, scale, and function).
}