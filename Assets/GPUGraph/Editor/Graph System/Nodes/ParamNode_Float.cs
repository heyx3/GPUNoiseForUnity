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
	public struct FloatParamInfo
	{
		public string Name;

		public bool IsSlider;
		public float SliderMin, SliderMax;

		/// <summary>
		/// If this parameter is a slider, this is actually the t value (between 0 and 1).
		/// </summary>
		public float DefaultValue;


		public FloatParamInfo(string name, float defaultVal = 0.0f)
		{
			Name = name;
			DefaultValue = defaultVal;
			IsSlider = false;
			SliderMin = 0.0f;
			SliderMax = 1.0f;
		}
		public FloatParamInfo(string name, float sliderMin, float sliderMax, float currentValue)
		{
			Name = name;

			IsSlider = true;
			SliderMin = sliderMin;
			SliderMax = sliderMax;

			DefaultValue = 0.0f;
		}

		public FloatParamInfo(FloatParamInfo original, float newDefaultVal)
		{
			Name = original.Name;
			IsSlider = original.IsSlider;
			SliderMin = original.SliderMin;
			SliderMax = original.SliderMax;
			DefaultValue = newDefaultVal;
		}
		public FloatParamInfo(FloatParamInfo original, string newName)
		{
			Name = newName;
			IsSlider = original.IsSlider;
			SliderMin = original.SliderMin;
			SliderMax = original.SliderMax;
			DefaultValue = original.DefaultValue;
		}
	}


	/// <summary>
	/// A node whose output is the value of a shader parameter.
	/// </summary>
	[Serializable]
	public class ParamNode_Float : Node
	{
		public FloatParamInfo Param;


		public override Color GUIColor { get { return new Color(0.85f, 1.0f, 1.0f); } }
		public override string OutputName { get { return Param.Name; } }
		public override string PrettyName { get { return "Scalar Param"; } }


		public ParamNode_Float(Rect pos, FloatParamInfo param)
			: base(pos, new List<NodeInput>(), new List<string>(), new List<float>())
		{
			Param = param;
		}
		private ParamNode_Float() { }


		protected override Node MakeClone()
		{
			ParamNode_Float fl = new ParamNode_Float();
			fl.Param = Param;
			return fl;
		}

		public override void EmitProperties(StringBuilder outCode)
		{
			outCode.Append("\t\t\t");
			outCode.Append(Param.Name);
			outCode.Append(" (\"");
			outCode.Append(StringUtils.PrettifyVarName(Param.Name));
			outCode.Append("\", ");
			if (Param.IsSlider)
			{
				outCode.Append("Range(");
				outCode.Append(Param.SliderMin);
				outCode.Append(", ");
				outCode.Append(Param.SliderMax);
				outCode.Append(")) = ");
				outCode.Append(Mathf.Lerp(Param.SliderMin, Param.SliderMax,
										  Param.DefaultValue).ToCodeString());
			}
			else
			{
				outCode.Append("Float) = ");
				outCode.Append(Param.DefaultValue);
			}
			outCode.AppendLine();
		}
		public override void EmitDefs(StringBuilder outCode)
		{
			outCode.Append("\t\t\t\tfloat ");
			outCode.Append(Param.Name);
			outCode.AppendLine(";");
		}

		protected override bool CustomGUI()
		{
			string _name = Param.Name;
			float _defVal = Param.DefaultValue;
			bool _isSlider = Param.IsSlider;
			float _min = Param.SliderMin;
			float _max = Param.SliderMax;

			Param.Name = GUILayout.TextField(Param.Name);

			if (Param.IsSlider)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Min:");
				Param.SliderMin = EditorGUILayout.FloatField(Param.SliderMin);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Max:");
				Param.SliderMax = EditorGUILayout.FloatField(Param.SliderMax);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Default value:");
				Param.DefaultValue = Mathf.InverseLerp(Param.SliderMin, Param.SliderMax,
													   GUILayout.HorizontalSlider(Mathf.Lerp(Param.SliderMin,
																							 Param.SliderMax,
														  								     Param.DefaultValue),
																				  Param.SliderMin,
																				  Param.SliderMax,
																				  GUILayout.ExpandWidth(true)));
				GUILayout.EndHorizontal();

				Param.IsSlider = !GUILayout.Button("Remove slider");
			}
			else
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Default value:");
				Param.DefaultValue = EditorGUILayout.FloatField(Param.DefaultValue);
				GUILayout.EndHorizontal();

				Param.IsSlider = GUILayout.Button("Make slider");
			}

			return Param.Name != _name ||
				   AreFloatsDifferent(Param.DefaultValue, _defVal) ||
				   _isSlider != Param.IsSlider ||
				   AreFloatsDifferent(_min, Param.SliderMin) ||
				   AreFloatsDifferent(_max, Param.SliderMax);
		}


		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("VarName", Param.Name);
			info.AddValue("DefaultVal", Param.DefaultValue);
			info.AddValue("IsSlider", Param.IsSlider);
			if (Param.IsSlider)
			{
				info.AddValue("SliderMin", Param.SliderMin);
				info.AddValue("SliderMax", Param.SliderMax);
			}
		}
		public ParamNode_Float(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Param.Name = info.GetString("VarName");
			Param.DefaultValue = info.GetSingle("DefaultVal");
			Param.IsSlider = info.GetBoolean("IsSlider");
			if (Param.IsSlider)
			{
				Param.SliderMin = info.GetSingle("SliderMin");
				Param.SliderMax = info.GetSingle("SliderMax");
			}
		}
	}
}