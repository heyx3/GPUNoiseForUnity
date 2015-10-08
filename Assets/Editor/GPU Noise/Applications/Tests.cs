using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
using UnityEditor;
using GPUNoise;
using GPUNoise.Applications;


namespace GPUNoise.Applications.Tests
{
	public static class Tests
	{
		[MenuItem("GPU Noise/Tests/Graph Save and Load")]
		public static void RunTest()
		{
			//Create a graph.

			Graph g = new Graph();
			g.Hash2 = "float2(1.0, 1.0)";

			FuncCall noise1 = new FuncCall(FuncDefinitions.FunctionsByName["WhiteNoise1"]);
			noise1.Inputs[0] = new FuncInput(234.1241f);
			g.CreateFuncCall(noise1);

			FuncCall powNoise1 = new FuncCall(FuncDefinitions.FunctionsByName["Pow"]);
			powNoise1.Inputs[0] = new FuncInput(noise1.UID);
			powNoise1.Inputs[1] = new FuncInput(3.0f);
			g.CreateFuncCall(powNoise1);

			g.Output = new FuncInput(powNoise1.UID);


			//Save it to the "Tests" folder in "Assets", then try to load it again.

			string testsFolder = Path.Combine(Application.dataPath, "Tests");
			
			DirectoryInfo inf = new DirectoryInfo(testsFolder);
			bool destroyWhenDone = !inf.Exists;
			if (!inf.Exists)
			{
				inf.Create();
			}

			string graphFile = Path.Combine(testsFolder, "SaveLoadTest." + GraphUtils.Extension);
			if (!GraphUtils.SaveGraph(g, graphFile))
			{
				Debug.LogError("Failed to save graph; exiting test");
				if (destroyWhenDone)
					inf.Delete();
				return;
			}

			Debug.Log("Graph saved successfully.");

			g = GraphUtils.LoadGraph(graphFile);
			if (g == null)
			{
				Debug.LogError("Failed to load graph; exiting test");
				File.Delete(graphFile);
				if (destroyWhenDone)
					inf.Delete();
				return;
			}

			Debug.Log("Graph loaded successfully.");


			//Test the graph to make sure its data is unchanged.

			if (g.Hash2 != "float2(1.0, 1.0)")
			{
				Debug.LogError("Graph's Hash2 should have been 'float2(1.0, 1.0)', but it's " + g.Hash2);
				File.Delete(graphFile);
				if (destroyWhenDone)
					inf.Delete();
				return;
			}
			if (g.Output.IsAConstantValue)
			{
				Debug.LogError("Graph's output should have been a Func call value, but it's not");
				File.Delete(graphFile);
				if (destroyWhenDone)
					inf.Delete();
				return;
			}
			FuncCall call = g.UIDToFuncCall[g.Output.FuncCallID];
			if (call.Calling.Name != "Pow")
			{
				Debug.LogError("Graph's output should have been a call to 'Pow', but it's a call to " +
							   call.Calling.Name);
				File.Delete(graphFile);
				if (destroyWhenDone)
					inf.Delete();
				return;
			}
			if (call.Inputs[0].IsAConstantValue)
			{
				Debug.LogError("First argument for 'Pow' is supposed to be a call to 'WhiteNoise1', but it's a constant: " +
							   call.Inputs[0].ConstantValue.ToString());
				File.Delete(graphFile);
				if (destroyWhenDone)
					inf.Delete();
				return;
			}
			if (g.UIDToFuncCall[call.Inputs[0].FuncCallID].Calling.Name != "WhiteNoise1")
			{
				Debug.LogError("First argument for 'Pow' is supposed to be a call to 'WhiteNoise1', but it's calling '" +
							   g.UIDToFuncCall[call.Inputs[0].FuncCallID].Calling.Name + "'");
				File.Delete(graphFile);
				if (destroyWhenDone)
					inf.Delete();
				return;
			}
			if (!call.Inputs[1].IsAConstantValue)
			{
				Debug.LogError("Second argument for 'Pow' should be a constant, but it's a call to " +
							   g.UIDToFuncCall[call.Inputs[1].FuncCallID].Calling.Name);
				File.Delete(graphFile);
				if (destroyWhenDone)
					inf.Delete();
				return;
			}
			if (call.Inputs[1].ConstantValue != 3.0f)
			{
				Debug.LogError("Second argument for 'Pow' should be 3.0, but it's " +
							   call.Inputs[1].ConstantValue.ToString());
				File.Delete(graphFile);
				if (destroyWhenDone)
					inf.Delete();
				return;
			}

			Debug.Log("Graph was deserialized correctly!");


			//Try saving/compiling the graph's shader.

			string shaderCode = g.GenerateShader("GPU Noise Test/PowNoise");
			if (shaderCode == null)
			{
				Debug.LogError("Failed to generate shader; exiting test");
				File.Delete(graphFile);
				if (destroyWhenDone)
					inf.Delete();
				return;
			}

			int response = EditorUtility.DisplayDialogComplex("Shader results",
															  "What do you want to do with the generated shader?",
															  "Output to 'TestShader.shader'",
															  "Nothing",
															  "Print to console");

			if (response == 0)
			{
				if (GraphUtils.SaveShader(g, Path.Combine(testsFolder, "TestShader.shader"),
										  "GPU Noise Test/TestShader") == null)
				{
					Debug.LogError("Failed to save shader; exiting test");
					File.Delete(graphFile);
					if (destroyWhenDone)
						inf.Delete();
					return;
				}
			}
			else if (response == 2)
			{
				Debug.Log(shaderCode);
			}

			Debug.Log("Shader code was successfully generated.");
			File.Delete(graphFile);
			if (destroyWhenDone)
				inf.Delete();
		}

		[MenuItem("GPU Noise/Make Sample Graph")]
		public static void MakeSampleGraph()
		{
			Graph g = new Graph();
			g.Output = new FuncInput(0.25f);

			string path = EditorUtility.SaveFilePanel("Choose file location.",
													  Application.dataPath,
													  "My Graph." + GraphUtils.Extension,
													  GraphUtils.Extension);
			GraphUtils.SaveGraph(g, path);
		}
	}
}