using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

using FloatParam = GPUNoise.FloatParamNode.FloatParamData;
using SliderParam = GPUNoise.SliderParamNode.SliderParamData;


namespace GPUNoise
{
	/// <summary>
	/// A collection of all parameters in a Graph.
	/// </summary>
	public struct GraphParamCollection
	{
		public List<FloatParam> FloatParams;
		public List<SliderParam> SliderParams;


		/// <summary>
		/// Gets all the parameters in the given Graph.
		/// </summary>
		public GraphParamCollection(Graph g)
		{
			FloatParams = new List<FloatParam>();
			SliderParams = new List<SliderParam>();

			g.GetParams(FloatParams, SliderParams);
		}


		/// <summary>
		/// For any parameters in this collection that exist in the given graph,
		/// the graph parameter's default value is overwritten with this collection's parameter's default value.
		/// </summary>
		public void OverwriteParamValues(Graph g)
		{
			GraphParamCollection gParams = new GraphParamCollection(g);
			
			foreach (FloatParam fp in FloatParams)
			{
				int i = gParams.FloatParams.FindIndex(gFP => gFP.VarName == fp.VarName);
				if (i >= 0)
				{
					gParams.FloatParams[i].DefaultValue = fp.DefaultValue;
				}
			}
			foreach (SliderParam sp in SliderParams)
			{
				int i = gParams.SliderParams.FindIndex(gSP => gSP.VarName == sp.VarName);
				if (i >= 0)
				{
					gParams.SliderParams[i].DefaultLerp = sp.DefaultLerp;
				}
			}
		}
		/// <summary>
		/// Sets the given material to use these parameters, with their default values.
		/// </summary>
		public void SetParams(Material m)
		{
			foreach (FloatParam dat in FloatParams)
				m.SetFloat(dat.VarName, dat.DefaultValue);
			foreach (SliderParam dat in SliderParams)
				m.SetFloat(dat.VarName, Mathf.Lerp(dat.Min, dat.Max, dat.DefaultLerp));
		}
		/// <summary>
		/// Runs a GUI using EditorGUILayout for these parameters.
		/// This GUI can be used to modify each parameter's "default value" fields.
		/// </summary>
		public void ParamEditorGUI()
		{
			foreach (FloatParam fp in FloatParams)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(FuncDefinitions.PrettifyVarName(fp.VarName));
				fp.DefaultValue = EditorGUILayout.FloatField(fp.DefaultValue);
				EditorGUILayout.EndHorizontal();
			}

			foreach (SliderParam sp in SliderParams)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(FuncDefinitions.PrettifyVarName(sp.VarName));
				sp.DefaultLerp = Mathf.InverseLerp(sp.Min, sp.Max,
												   EditorGUILayout.Slider(Mathf.Lerp(sp.Min, sp.Max,
																					 sp.DefaultLerp),
																		  sp.Min, sp.Max));
				EditorGUILayout.EndHorizontal();
			}
		}
	}
}