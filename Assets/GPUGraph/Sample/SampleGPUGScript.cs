using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GPUGraph.Examples
{
	/// <summary>
	/// An example of how to use GPUGraphs at runtime.
	/// Creates two objects with a given mesh/material
	///     and sets their textures to the output of two different graphs.
	/// </summary>
	public class SampleGPUGScript : MonoBehaviour
	{
		public GPUGraph.RuntimeGraph MyGraph = new GPUGraph.RuntimeGraph();
		public GPUGraph.RuntimeGraph MyGraph2 = new GPUGraph.RuntimeGraph();

		private Texture2D myTex1, myTex2;


		void Start()
		{
			//Generate the textures.
			myTex1 = MyGraph.GenerateToTexture(512, 512, TextureFormat.RGB24,
											   FilterMode.Bilinear, false, true);
			myTex2 = MyGraph2.GenerateToTexture(512, 512, TextureFormat.RGB24,
												FilterMode.Bilinear, false, true);
		}
		private void OnGUI()
		{
			//Draw the textures.
			float borderPercent = 0.1f,
				  borderW = Screen.width * borderPercent,
				  borderH = Screen.height * borderPercent,
				  screenHalfW = Screen.width * 0.5f;
			GUI.DrawTexture(new Rect(borderW, borderH,
									 screenHalfW - (borderW * 2.0f),
									 Screen.height - (borderH * 2.0f)),
							myTex1,
							ScaleMode.ScaleToFit);
			GUI.DrawTexture(new Rect(screenHalfW + borderW, borderH,
									 screenHalfW - (borderW * 2.0f),
									 Screen.height - (borderH * 2.0f)),
							myTex2,
							ScaleMode.ScaleToFit);


			//Show a little UI for editing float parameters.
			//If anything actually changed, regenerate the noise texture.

			GUILayout.BeginArea(new Rect(0.0f, 0.0f, borderW, Screen.height));
			{
				bool changed = false;
				foreach (var param in MyGraph.FloatParams)
					changed = ParamGUILayout(param) | changed;
				if (changed)
					MyGraph.GenerateToTexture(myTex1, true);
			}
			GUILayout.EndArea();

			GUILayout.BeginArea(new Rect(Screen.width - borderW, 0.0f, borderW, Screen.height));
			{
				bool changed = false;
				foreach (var param in MyGraph2.FloatParams)
					changed = ParamGUILayout(param) | changed;
				if (changed)
					MyGraph2.GenerateToTexture(myTex2, true);
			}
			GUILayout.EndArea();
		}

		/// <summary>
		/// Shows a GUILayout widget for the given graph parameter.
		/// Returns whether the parameter's value changed.
		/// </summary>
		private bool ParamGUILayout(FloatParamKVP param)
		{
			GUILayout.BeginHorizontal();

			GUILayout.Label(param.Key);

			float oldVal = param.Value;
			param.Value = GUILayout.HorizontalSlider(param.Value, 0.0f, 10.0f,
												     GUILayout.MinWidth(50.0f));

			GUILayout.EndHorizontal();

			return oldVal != param.Value;
		}
	}
}