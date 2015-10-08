using System;
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


		EditorGraph Editor = new EditorGraph("Graph Name Here", "Graph description here");

		void OnEnable()
		{
			wantsMouseMove = true;
		}
		void OnGUI()
		{
			const float leftSpace = 200.0f;

			GUILayout.BeginArea(new Rect(0, 0, leftSpace, position.height));
			GUILayout.Space(10.0f);
			Editor.Name = GUILayout.TextField(Editor.Name);
			GUILayout.Space(50.0f);
			Editor.Description = GUILayout.TextArea(Editor.Description);
			GUILayout.EndArea();

			GUILayout.BeginArea(new Rect(leftSpace, 0, position.width - leftSpace, 500));
			
			//TODO: Render this view somehow.

			GUILayout.EndArea();
		}
	}
}