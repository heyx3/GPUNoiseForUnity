using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace GPUGraph
{
	/// <summary>
	/// An invocation of a Func.
	/// </summary>
	[Serializable]
	public class FuncCall : ISerializable
	{
		public static FuncCall CreateFloatParam(string varName, float defaultValue)
		{
			FuncCall fc = new FuncCall("FloatParam");
			FloatParamNode.FloatParamData dat = (FloatParamNode.FloatParamData)fc.CustomDat;

			dat.VarName = varName;
			dat.DefaultValue = defaultValue;

			return fc;
		}
		public static FuncCall CreateSliderParam(string varName, float min, float max, float defaultLerp)
		{
			FuncCall fc = new FuncCall("SliderParam");
			SliderParamNode.SliderParamData dat = (SliderParamNode.SliderParamData)fc.CustomDat;

			dat.VarName = varName;
			dat.DefaultLerp = defaultLerp;
			dat.Min = min;
			dat.Max = max;

			return fc;
		}


		/// <summary>
		/// A unique identifier for this instance in the graph.
		/// Should always be non-negative.
		/// </summary>
		public long UID;

		/// <summary>
		/// The function being called.
		/// </summary>
		public Func Calling;
		/// <summary>
		/// Extra data that special Func instances may need.
		/// </summary>
		public Func.ExtraData CustomDat = null;

		/// <summary>
		/// The inputs into the Func. Must match the number of parameters in that Func!
		/// </summary>
		public FuncInput[] Inputs;


		/// <summary>
		/// Starts with default constant values for all inputs (using NaN for inputs with no default).
		/// The UID is initialized to -1; it will be set when added to a Graph.
		/// </summary>
		public FuncCall(Func calling)
		{
			UID = -1;
			Calling = calling;
			CustomDat = Calling.InitCustomGUI();

			Inputs = new FuncInput[calling.Params.Count];
			for (int i = 0; i < Inputs.Length; ++i)
				Inputs[i] = new FuncInput(calling.Params[i].DefaultValue);

		}
		/// <summary>
		/// Calls the Func from FuncDefinitions with the given name.
		/// Starts with default constant values for all inputs (using NaN for inputs with no default).
		/// The UID is initialized to -1; it will be set when added to a Graph.
		/// </summary>
		public FuncCall(string funcName) : this(FuncDefinitions.FunctionsByName[funcName]) { }
		/// <summary>
		/// Explicitly sets the data of this FuncCall.
		/// </summary>
		public FuncCall(long funcUID, Func calling, FuncInput[] inputs)
		{
			UID = funcUID;
			Calling = calling;
			Inputs = inputs;

			if (calling != null)
				CustomDat = Calling.InitCustomGUI();
		}


		//Serialization support.
		protected FuncCall(SerializationInfo info, StreamingContext context)
		{
			UID = info.GetInt32("UID");

			string fName = info.GetString("Func");
			if (!FuncDefinitions.FunctionsByName.ContainsKey(fName))
			{
				Debug.Log("Function '" + fName + "' not found!");
				throw new SerializationException();
			}
			Calling = FuncDefinitions.FunctionsByName[fName];

			CustomDat = (Func.ExtraData)info.GetValue("CustomData", typeof(Func.ExtraData));

			int nInputs = info.GetInt32("NInputs"),
				nToGet = nInputs;
			if (nInputs != Calling.Params.Count)
			{
				Debug.LogWarning("Expected node type '" + fName + "' to have " +
								 nInputs + " params, but it has " + Calling.Params.Count +
								 ". Delete and recreate the node to fix this.");
				if (nInputs > Calling.Params.Count)
					nToGet = Calling.Params.Count;
				nInputs = Calling.Params.Count;
			}
			Inputs = new FuncInput[nInputs];

			for (int i = 0; i < nInputs; ++i)
			{
				if (i < nToGet)
					Inputs[i] = (FuncInput)info.GetValue("Input" + i.ToString(), typeof(FuncInput));
				else
					Inputs[i] = new FuncInput(Calling.Params[i].DefaultValue);
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("UID", UID);
			info.AddValue("Func", Calling.Name);

			info.AddValue("CustomData", CustomDat, typeof(Func.ExtraData));

			info.AddValue("NInputs", Inputs.Length);
			for (int i = 0; i < Inputs.Length; ++i)
			{
				info.AddValue("Input" + i.ToString(), Inputs[i]);
			}
		}
	}
}