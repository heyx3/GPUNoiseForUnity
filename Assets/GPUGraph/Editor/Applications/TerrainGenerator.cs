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
			CreateInstance<TerrainGenerator>().Show();
		}


		private List<string> graphPaths = new List<string>();
		private GUIContent[] graphNameOptions;
		private int selectedGraphIndex = 0;

		private Vector2 scrollPos = Vector2.zero;
		private float heightScale = 1.0f,
					  uvz = 0.0f;

		private Graph graph;
		private GraphParamCollection gParams;
		private Texture2D previewTex;


		void OnEnable()
		{
			graphPaths.Clear();
			graphPaths = GraphEditorUtils.GetAllGraphsInProject();

			Func<string, GUIContent> selector = (gp => new GUIContent(Path.GetFileNameWithoutExtension(gp), gp));
			graphNameOptions = graphPaths.Select(selector).ToArray();

			this.titleContent = new GUIContent("Terrain Gen");
			this.minSize = new Vector2(200.0f, 250.0f);
			
			selectedGraphIndex = 0;
			if (graphPaths.Count > 0)
			{
				graph = new Graph(graphPaths[selectedGraphIndex]);
				if (graph.Load().Length == 0)
				{
					gParams = new GraphParamCollection(graph);
					GeneratePreview();
				}
				else
				{
					graph = null;
				}
			}
		}

		void OnGUI()
		{
			GUILayout.Space(10.0f);
			scrollPos = GUILayout.BeginScrollView(scrollPos);

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
					var newGraph = new Graph(graphPaths[selectedGraphIndex]);
					var errMsg = newGraph.Load();
					if (errMsg.Length > 0)
					{
						Debug.LogError(errMsg);
						selectedGraphIndex = oldIndex;
					}
					else
					{
						gParams = new GraphParamCollection(newGraph);
						graph = newGraph;
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
				EditorGUILayout.BeginHorizontal();
				uvz = EditorGUILayout.Slider("UV.z", uvz, 0, 1);
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

				//Preview texture.
				if (GUILayout.Button("Update Preview"))
					GeneratePreview();
				if (previewTex != null)
					GUILayout.Box(previewTex, GUILayout.Width(256), GUILayout.Height(256));
			}

			GUILayout.EndScrollView();
		}

		private void GeneratePreview()
		{
			previewTex = GraphEditorUtils.GenerateToTexture(graph, gParams, 1024, 1024, uvz,
															"rgb", 1.0f, TextureFormat.RGBAFloat);
		}
		private void Generate()
		{
			Terrain terr = Selection.activeGameObject.GetComponent<Terrain>();
			TerrainData dat = terr.terrainData;
			
			float[,] heights = GraphEditorUtils.GenerateToArray(graph, gParams,
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