using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Assert = UnityEngine.Assertions.Assert;


namespace GPUGraph.Applications
{
	public class TerrainGenerator : EditorWindow
	{
		[MenuItem("Assets/GPU Graph/Generate Terrain Heightmap", false, 3)]
		public static void GenerateHeightmap()
		{
			ScriptableObject.CreateInstance<TerrainGenerator>().Show();
		}


		private List<string> graphPaths = new List<string>();
		private GUIContent[] graphNameOptions;
		private int selectedGraphIndex = 0;

		private float heightScale = 1.0f;

		private GraphParamCollection gParams;


		void OnEnable()
		{
			graphPaths.Clear();
			graphPaths = GraphEditorUtils.GetAllGraphsInProject();

			Func<string, GUIContent> selector = (gp => new GUIContent(Path.GetFileNameWithoutExtension(gp), gp));
			graphNameOptions = graphPaths.Select(selector).ToArray();

			this.titleContent = new GUIContent("Terrain Gen");
			this.minSize = new Vector2(200.0f, 250.0f);

			gParams = new GraphParamCollection();

			selectedGraphIndex = 0;
			if (graphPaths.Count > 0)
			{
				Graph g = new Graph(graphPaths[selectedGraphIndex]);
				if (g.Load().Length == 0)
				{
					gParams = new GraphParamCollection(g);
				}
			}
		}

		void OnGUI()
		{
			GUILayout.Space(10.0f);

			if (Selection.activeGameObject == null ||
				Selection.activeGameObject.GetComponent<Terrain>() == null)
			{
				EditorGUILayout.LabelField("No terrain is selected in the editor.");
			}
			else
			{
				//Graph selection.
				GUILayout.BeginHorizontal();
				GUILayout.Label("Graph:");
				int oldIndex = selectedGraphIndex;
				selectedGraphIndex = EditorGUILayout.Popup(selectedGraphIndex, graphNameOptions);
				if (oldIndex != selectedGraphIndex)
				{
					Graph g = new Graph(graphPaths[selectedGraphIndex]);
					if (g.Load().Length == 0)
					{
						selectedGraphIndex = oldIndex;
					}
					else
					{
						gParams = new GraphParamCollection(g);
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(15.0f);

				//Graph params.
				if (graphPaths.Count > 0)
				{
					gParams.ParamEditorGUI();
				}

				GUILayout.Space(15.0f);

				//Other params.
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Height scale:");
				heightScale = EditorGUILayout.FloatField(heightScale);
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(15.0f);

				//Button to generate heightmap.
				if (graphPaths.Count > 0)
				{
					if (GUILayout.Button("Generate Heightmap"))
					{
						Generate();
					}
				}
				else
				{
					GUILayout.Space(15.0f);
					GUILayout.Label("No graph files detected in the project!");
				}
			}
		}
		private void Generate()
		{
			Terrain terr = Selection.activeGameObject.GetComponent<Terrain>();
			TerrainData dat = terr.terrainData;
			Graph g = new Graph(graphPaths[selectedGraphIndex]);

			string err = g.Load();
			if (err.Length > 0)
			{
				Debug.LogError("Error loading graph: " + err);
				return;
			}

			float[,] heights = GraphEditorUtils.GenerateToArray(g, new GraphParamCollection(g, gParams),
																dat.heightmapWidth,
																dat.heightmapHeight);
			if (heights == null)
			{
				return;
			}

			for (int x = 0; x < heights.GetLength(0); ++x)
				for (int y = 0; y < heights.GetLength(1); ++y)
					heights[x, y] *= heightScale;
			dat.SetHeights(0, 0, heights);
		}
	}
}