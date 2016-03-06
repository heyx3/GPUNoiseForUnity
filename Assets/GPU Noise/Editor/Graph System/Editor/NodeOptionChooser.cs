using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GPUGraph;


namespace GPUGraph.Editor
{
	public class NodeOptionChooser : EditorWindow
	{
		public GraphEditor Parent = null;


		public void OnEnable()
		{
			titleContent = new GUIContent("Node Creator");
			minSize = new Vector2(300.0f, 500.0f);
		}
		public void OnGUI()
		{
			if (Parent == null)
			{
				GUILayout.Label("The graph editor has been closed!");
			}
			else if (Parent.Grph == null)
			{
				GUILayout.Label("No graph is currently being edited.");
			}
			else if (Parent.CurrentlyPlacing == null)
			{
				GUILayout.Label("Click on an option, then click on the graph to place it.");
				GUILayout.Label("Mouse over an option to get more info about it.");
				GUILayout.Space(25.0f);

				foreach (NodeTree_Element el in Parent.NewNodeOptions)
				{
					NodeTree_Element_Option opt = el.OnGUI();
					if (opt != null)
					{
						Parent.SelectOption(opt);
						return;
					}
				}
			}
			else
			{
				GUILayout.Label("Left-click in the graph to place " + Parent.CurrentlyPlacing.Name);
				GUILayout.Label("Right-click in the graph to cancel its placement");
			}
		}
	}
}