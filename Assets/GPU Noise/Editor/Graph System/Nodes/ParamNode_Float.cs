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
	/// A node whose output is the value of a shader parameter.
	/// </summary>
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
}