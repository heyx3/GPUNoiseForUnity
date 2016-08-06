using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GPUGraph.Applications
{
	/// <summary>
	/// An example of how to use GPUGraphs at runtime.
	/// Creates two objects and sets their textures to the output of two different graphs.
	/// </summary>
	public class SampleGPUGScript : MonoBehaviour
	{
		public GPUGraph.RuntimeGraph MyGraph = new GPUGraph.RuntimeGraph();
		public GPUGraph.RuntimeGraph MyGraph2 = new GPUGraph.RuntimeGraph();

		public Material DisplayMat;
		public Mesh DisplayMesh;

		private Texture2D myTex1, myTex2;


		void Start()
		{
			myTex1 = MyGraph.GenerateToTexture(512, 512, TextureFormat.RGB24);

			GameObject go1 = new GameObject("1");
			go1.transform.parent = Camera.main.transform;
			go1.transform.localPosition = new Vector3(-10.0f, 0.0f, 20.0f);
			go1.transform.localRotation = Quaternion.identity;
			go1.transform.localScale = Vector3.one * 5.0f;
			go1.AddComponent<MeshFilter>().mesh = DisplayMesh;
			go1.AddComponent<MeshRenderer>().material = DisplayMat;
			go1.GetComponent<MeshRenderer>().material.mainTexture = myTex1;


			myTex2 = MyGraph2.GenerateToTexture(512, 512, TextureFormat.RGB24);

			GameObject go2 = new GameObject("2");
			go2.transform.parent = Camera.main.transform;
			go2.transform.localPosition = new Vector3(10.0f, 0.0f, 20.0f);
			go2.transform.localRotation = Quaternion.identity;
			go2.transform.localScale = Vector3.one * 5.0f;
			go2.AddComponent<MeshFilter>().mesh = DisplayMesh;
			go2.AddComponent<MeshRenderer>().material = DisplayMat;
			go2.GetComponent<MeshRenderer>().material.mainTexture = myTex2;
		}
	}
}