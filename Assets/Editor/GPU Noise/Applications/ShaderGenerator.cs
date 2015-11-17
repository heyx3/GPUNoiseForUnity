﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using GPUNoise;


namespace GPUNoise.Applications
{
	public class ShaderGenerator : EditorWindow
	{
		[MenuItem("GPU Noise/Generate Shader")]
		public static void GenerateTexture()
		{
			ScriptableObject.CreateInstance<ShaderGenerator>().Show();
		}

		
		private bool useRed = true,
					 useGreen = true,
					 useBlue = true,
					 useAlpha = false;
		private float unusedColor = 1.0f;
		private string shaderName = "Noise/My Noise";
		private int selectedGraphIndex = 0;

		private List<string> graphPaths = new List<string>();
		private GUIContent[] graphNameOptions;


		void OnEnable()
		{
			graphPaths.Clear();
			graphPaths = GraphUtils.GetAllGraphsInProject();

			Func<string, GUIContent> selector = (gp => new GUIContent(Path.GetFileNameWithoutExtension(gp), gp));
			graphNameOptions = graphPaths.Select(selector).ToArray();

			this.titleContent = new GUIContent("Shader Gen");
			this.minSize = new Vector2(200.0f, 250.0f);

			selectedGraphIndex = 0;
		}

		void OnGUI()
		{
			useRed = GUILayout.Toggle(useRed, "Use Red?");
			useGreen = GUILayout.Toggle(useGreen, "Use Green?");
			useBlue = GUILayout.Toggle(useBlue, "Use Blue?");
			useAlpha = GUILayout.Toggle(useAlpha, "Use Alpha?");
			if (!useRed && !useGreen && !useBlue && !useAlpha)
			{
				useRed = true;
			}
			
			unusedColor = EditorGUILayout.FloatField("Unused color value", unusedColor);

			EditorGUILayout.Space();

			int oldIndex = selectedGraphIndex;
			selectedGraphIndex = EditorGUILayout.Popup(selectedGraphIndex, graphNameOptions);
			if (oldIndex != selectedGraphIndex)
			{
				Graph g = GraphUtils.LoadGraph(graphPaths[selectedGraphIndex]);
				if (g == null)
				{
					selectedGraphIndex = oldIndex;
				}
			}

			EditorGUILayout.Space();


			if (GUILayout.Button("Generate Shader"))
			{
				string savePath = EditorUtility.SaveFilePanel("Choose where to save the shader.",
															  Application.dataPath, "MyNoiseShader.shader",
															  "shader");
				Graph g = GraphUtils.LoadGraph(graphPaths[selectedGraphIndex]);
				if (g != null)
				{
					string outComponents = "";
					if (useRed)
						outComponents += "r";
					if (useGreen)
						outComponents += "g";
					if (useBlue)
						outComponents += "b";
					if (useAlpha)
						outComponents += "a";

					GraphUtils.SaveShader(g, savePath, shaderName, outComponents, unusedColor);
				}
			}

			EditorGUILayout.Space();
		}
	}
}