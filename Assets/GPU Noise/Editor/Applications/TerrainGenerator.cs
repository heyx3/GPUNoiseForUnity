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
		[MenuItem("GPU Noise/Generate Terrain Heightmap")]
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
				Graph g = GraphEditorUtils.LoadGraph(graphPaths[selectedGraphIndex]);
				if (g != null)
				{
					gParams = new GraphParamCollection(g);
				}
			}
		}

		void OnGUI()
		{
			if (Selection.activeGameObject == null ||
				Selection.activeGameObject.GetComponent<Terrain>() == null)
			{
				EditorGUILayout.LabelField("No terrain is selected in the editor.");
			}
			else
			{
				//Graph selection.
				int oldIndex = selectedGraphIndex;
				selectedGraphIndex = EditorGUILayout.Popup(selectedGraphIndex, graphNameOptions);
				if (oldIndex != selectedGraphIndex)
				{
					Graph g = GraphEditorUtils.LoadGraph(graphPaths[selectedGraphIndex]);
					if (g == null)
					{
						selectedGraphIndex = oldIndex;
					}
					else
					{
						gParams = new GraphParamCollection(g);
					}
				}

				EditorGUILayout.Space();

				//Graph params.
				gParams.ParamEditorGUI();

				EditorGUILayout.Space();

				//Other params.
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Height scale:");
				heightScale = EditorGUILayout.FloatField(heightScale);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();

				//Button to generate heightmap.
				if (GUILayout.Button("Generate Heightmap"))
				{
					Generate();
				}
			}
		}
		private void Generate()
		{
			Terrain terr = Selection.activeGameObject.GetComponent<Terrain>();
			TerrainData dat = terr.terrainData;
			Graph g = GraphEditorUtils.LoadGraph(graphPaths[selectedGraphIndex]);

			if (g == null)
			{
				return;
			}

			float[,] heights = GraphEditorUtils.GenerateToArray(g, dat.heightmapWidth, dat.heightmapHeight);
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