using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;


namespace GPUGraph
{
	/// <summary>
	/// A collection of all parameters in a Graph.
	/// </summary>
	[Serializable]
	public class GraphParamCollection
	{
		public List<FloatParamInfo> FloatParams;
		public List<Texture2DParamInfo> Tex2DParams;


		public GraphParamCollection()
		{
			FloatParams = new List<FloatParamInfo>();
			Tex2DParams = new List<Texture2DParamInfo>();
		}
		/// <summary>
		/// Gets all the parameters in the given Graph.
		/// </summary>
		public GraphParamCollection(Graph g)
			: this()
		{
			Graph gCopy = g.Clone();
			gCopy.PreProcess();
			
			foreach (Node n in gCopy.Nodes)
			{
				if (n is ParamNode_Float)
					FloatParams.Add(((ParamNode_Float)n).Param);
				else if (n is ParamNode_Texture2D)
					Tex2DParams.Add(((ParamNode_Texture2D)n).Param);
			}
		}
		/// <summary>
		/// Gets all parameters from the given collection and recreates them for the given graph.
		/// </summary>
		public GraphParamCollection(Graph otherG, GraphParamCollection c)
			: this(otherG)
		{
			//Needed for lambdas.
			List<FloatParamInfo> myFloatParams = FloatParams;
			List<Texture2DParamInfo> myTex2DParams = Tex2DParams;

			for (int i = 0; i < FloatParams.Count; ++i)
			{
				int otherPMIndex = c.FloatParams.FindIndex(param2 => param2.Name == myFloatParams[i].Name);
				if (otherPMIndex == -1)
					Debug.LogError("Couldn't find an original value for scalar var '" + FloatParams[i].Name + "'");
				else
					FloatParams[i] = new FloatParamInfo(FloatParams[i],
														c.FloatParams[otherPMIndex].DefaultValue);
			}
			for (int i = 0; i < Tex2DParams.Count; ++i)
			{
				int otherPMIndex = c.Tex2DParams.FindIndex(param2 => param2.Name == myTex2DParams[i].Name);
				if (otherPMIndex == -1)
					Debug.LogError("Couldn't find an original value for Tex2D var '" + Tex2DParams[i].Name + "'");
				else
					Tex2DParams[i] = new Texture2DParamInfo(Tex2DParams[i].Name,
															c.Tex2DParams[otherPMIndex].DefaultVal);
			}
		}

		
		public FloatParamInfo? FindFloatParam(string name)
		{
			foreach (var param in FloatParams)
				if (param.Name == name)
					return param;
			return null;
		}
		public Texture2DParamInfo? FindTex2DParam(string name)
		{
			foreach (var param in Tex2DParams)
				if (param.Name == name)
					return param;
			return null;
		}

		/// <summary>
		/// Sets the given material to use these parameters, with their default values.
		/// </summary>
		public void SetParams(Material m, string prefix = null)
		{
			foreach (FloatParamInfo dat in FloatParams)
			{
				string datName = (prefix == null) ?
								     dat.Name :
									 (prefix + dat.Name);

				if (!m.HasProperty(datName))
				{
					Debug.LogWarning("Couldn't find property '" + datName +
										"'; Unity may have optimized it out");
				}
				else
				{
					m.SetFloat(datName,
							   (dat.IsSlider ?
									Mathf.Lerp(dat.SliderMin, dat.SliderMax, dat.DefaultValue) :
									dat.DefaultValue));
				}
			}
			foreach (Texture2DParamInfo dat in Tex2DParams)
			{
				string datName = (prefix == null) ?
								     dat.Name :
									 (prefix + dat.Name);

				if (!m.HasProperty(datName))
				{
					Debug.LogWarning("Couldn't find property '" + datName +
										"'; Unity may have optimized it out");
				}
				else
				{
					m.SetTexture(datName, dat.DefaultVal);
				}
			}
		}
		/// <summary>
		/// Sets this collection's parameter values from the given collection.
		/// Ignores parameters that don't exist on this collection.
		/// </summary>
		public void SetParams(GraphParamCollection from)
		{
			for (int i = 0; i < FloatParams.Count; ++i)
			{
				var toParam = FloatParams[i];
				var fromParam = from.FindFloatParam(toParam.Name);
				if (fromParam.HasValue)
					FloatParams[i] = new FloatParamInfo(toParam, fromParam.Value.DefaultValue);
			}
			for (int i = 0; i < Tex2DParams.Count; ++i)
			{
				var toParam = Tex2DParams[i];
				var fromParam = from.FindTex2DParam(toParam.Name);
				if (fromParam.HasValue)
					Tex2DParams[i] = new Texture2DParamInfo(toParam.Name, fromParam.Value.DefaultVal);
			}
		}

		/// <summary>
		/// Runs a GUI using EditorGUILayout for these parameters.
		/// This GUI can be used to modify each parameter's "default value" fields.
		/// Returns whether any values have been changed.
		/// </summary>
		public bool ParamEditorGUI()
		{
			bool changed = false;

			for (int i = 0; i < FloatParams.Count; ++i)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(StringUtils.PrettifyVarName(FloatParams[i].Name));
				float oldVal = FloatParams[i].DefaultValue;
				if (FloatParams[i].IsSlider)
				{
					GUILayout.Label(FloatParams[i].SliderMin.ToString());
					FloatParams[i] = new FloatParamInfo(FloatParams[i],
						Mathf.InverseLerp(FloatParams[i].SliderMin, FloatParams[i].SliderMax,
										  GUILayout.HorizontalSlider(Mathf.Lerp(FloatParams[i].SliderMin,
																				FloatParams[i].SliderMax,
																				FloatParams[i].DefaultValue),
																	 FloatParams[i].SliderMin,
																	 FloatParams[i].SliderMax,
																	 GUILayout.MinWidth(50.0f))));
					GUILayout.Label(FloatParams[i].SliderMax.ToString());
				}
				else
				{
					FloatParams[i] = new FloatParamInfo(FloatParams[i],
														EditorGUILayout.FloatField(FloatParams[i].DefaultValue));
				}

				changed = (changed || Node.AreFloatsDifferent(oldVal, FloatParams[i].DefaultValue));

				GUILayout.EndHorizontal();
			}
			for (int i = 0; i < Tex2DParams.Count; ++i)
			{
				GUILayout.BeginHorizontal();

				GUILayout.Label(StringUtils.PrettifyVarName(Tex2DParams[i].Name));

				Texture2D oldVal = Tex2DParams[i].DefaultVal;
				Tex2DParams[i] = new Texture2DParamInfo(Tex2DParams[i].Name,
						(Texture2D)EditorGUILayout.ObjectField(Tex2DParams[i].DefaultVal,
															   typeof(Texture2D), false));

				changed = (oldVal != Tex2DParams[i].DefaultVal);

				GUILayout.EndHorizontal();
			}

			return changed;
		}
	}
}