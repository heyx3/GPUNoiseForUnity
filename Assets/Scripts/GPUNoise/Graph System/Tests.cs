using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
using UnityEditor;
using GPUNoise;


public static class Tests
{
	[MenuItem("GPU Noise/Tests/Graph 1")]
	public static void TestOutput()
	{
		string testsFolder = Path.Combine(Application.dataPath, "Tests");
		
		DirectoryInfo inf = new DirectoryInfo(testsFolder);
		if (!inf.Exists)
		{
			inf.Create();
		}


		Graph g = new Graph("TestG", "Testing Graph functionality");
		g.Hash2 = "float2(1.0, 1.0)";

		FuncCall noise1 = new FuncCall(FuncDefinitions.FunctionsByName["WhiteNoise1"]);
		noise1.Inputs[0] = new FuncInput(234.1241f);
		g.CreateFuncCall(noise1);

		FuncCall powNoise1 = new FuncCall(FuncDefinitions.FunctionsByName["Pow"]);
		powNoise1.Inputs[0] = new FuncInput(noise1.UID);
		powNoise1.Inputs[1] = new FuncInput(3.0f);
		g.CreateFuncCall(powNoise1);

		g.Output = new FuncInput(powNoise1.UID);

		IFormatter formatter = new BinaryFormatter();
		Stream stream = new FileStream(Path.Combine(testsFolder, "testGraph.gr"),
									   FileMode.Create, FileAccess.Write, FileShare.None);
		try
		{
			formatter.Serialize(stream, g);
		}
		catch (Exception e)
		{
			throw e;
		}
		finally
		{
			stream.Close();
		}

		Debug.Log("Successfully wrote graph to '" + testsFolder + "'.");
	}
	[MenuItem("GPU Noise/Tests/Graph 2")]
	public static void TestInput()
	{
		string testsFolder = Path.Combine(Application.dataPath, "Tests");

		DirectoryInfo inf = new DirectoryInfo(testsFolder);
		if (!inf.Exists)
		{
			Debug.LogError("Couldn't find folder '" + testsFolder + "'");
			return;
		}


		IFormatter formatter = new BinaryFormatter();
		Stream stream = new FileStream(Path.Combine(testsFolder, "testGraph.gr"),
									   FileMode.Open, FileAccess.Read, FileShare.Read);
		Graph g = null;
		try
		{
			g = (Graph)formatter.Deserialize(stream);
		}
		catch (Exception e)
		{
			throw e;
		}
		finally
		{
			stream.Close();
		}

		Debug.Log("Successfully read from file.");
		
		if (g.Hash2 != "float2(1.0, 1.0)")
		{
			Debug.LogError("Graph's Hash2 should have been 'float2(1.0, 1.0)', but it's " + g.Hash2);
			return;
		}
		if (g.Output.IsAConstantValue)
		{
			Debug.LogError("Graph's output should have been a Func call value, but it's not");
			return;
		}
		FuncCall call = g.UIDToFuncCall[g.Output.FuncCallID];
		if (call.Calling.Name != "Pow")
		{
			Debug.LogError("Graph's output should have been a call to 'Pow', but it's a call to " +
						   call.Calling.Name);
			return;
		}
		if (call.Inputs[0].IsAConstantValue)
		{
			Debug.LogError("First argument for 'Pow' is supposed to be a call to 'WhiteNoise1', but it's a constant: " +
						   call.Inputs[0].ConstantValue.ToString());
			return;
		}
		if (g.UIDToFuncCall[call.Inputs[0].FuncCallID].Calling.Name != "WhiteNoise1")
		{
			Debug.LogError("First argument for 'Pow' is supposed to be a call to 'WhiteNoise1', but it's calling '" +
						   g.UIDToFuncCall[call.Inputs[0].FuncCallID].Calling.Name + "'");
			return;
		}
		if (!call.Inputs[1].IsAConstantValue)
		{
			Debug.LogError("Second argument for 'Pow' should be a constant, but it's a call to " +
						   g.UIDToFuncCall[call.Inputs[1].FuncCallID].Calling.Name);
			return;
		}
		if (call.Inputs[1].ConstantValue != 3.0f)
		{
			Debug.LogError("Second argument for 'Pow' should be 3.0, but it's " +
						   call.Inputs[1].ConstantValue.ToString());
			return;
		}

		Debug.Log("Graph was deserialized correctly!");

		string shader = g.GenerateShader("GPUNoise Test/Test Shader");
		Debug.Log("Shader generated successfully");

		TextWriter textWrite = null;
		try
		{
			stream = new FileStream(Path.Combine(testsFolder, "testShader.shader"),
										FileMode.Create, FileAccess.Write, FileShare.None);
			textWrite = new StreamWriter(stream);
			textWrite.Write(shader);
		}
		catch (Exception e)
		{
			throw e;
		}
		finally
		{
			if (textWrite != null)
				textWrite.Close();
			stream.Close();
		}

		Debug.Log("Shader file was successfully saved to '" + testsFolder + "'.");
	}
}