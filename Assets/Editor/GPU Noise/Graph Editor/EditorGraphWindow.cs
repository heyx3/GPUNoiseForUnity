using System;
using System.Linq;
using Path = System.IO.Path;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
using GPUNoise;


namespace GPUNoise.Editor
{
	public class EditorGraphWindow : EditorWindow
	{
		[UnityEditor.MenuItem("GPU Noise/Show Editor")]
		public static void ShowEditor()
		{
			UnityEditor.EditorWindow.GetWindow(typeof(EditorGraphWindow));
		}


		public EditorGraph Editor = new EditorGraph();

		private int selectedGraph = 0;

		private List<string> GraphPaths;
		private GUIContent[] graphSelections;


		void OnEnable()
		{
			wantsMouseMove = true;

			GraphPaths = GPUNoise.Applications.GraphUtils.GetAllGraphsInProject();

			Func<string, GUIContent> selector = (s => new GUIContent(Path.GetFileNameWithoutExtension(s), s));
			graphSelections = GraphPaths.Select(selector).ToArray();
			
			selectedGraph = 0;
			if (GraphPaths.Count > 0)
			{
				Editor = new EditorGraph(GraphPaths[0]);
			}
		}
		void OnGUI()
		{
			const float leftSpace = 200.0f;

			GUILayout.BeginArea(new Rect(0, 0, leftSpace, position.height));

			GUILayout.Space(10.0f);

			GUILayout.Label(Path.GetFileNameWithoutExtension(Editor.FilePath));

			GUILayout.Space(10.0f);

			int oldVal = selectedGraph;
			selectedGraph = EditorGUILayout.Popup(selectedGraph, graphSelections);
			if (selectedGraph != oldVal)
			{
				Editor = new EditorGraph(GraphPaths[selectedGraph]);
			}

			GUILayout.Space(50.0f);

			GUILayout.EndArea();


			GUILayout.BeginArea(new Rect(leftSpace, 0, position.width - leftSpace, 500));
			
			//TODO: Render this view somehow.

			GUILayout.EndArea();
		}
	}
}