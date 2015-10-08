using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace GPUNoise
{
	/// <summary>
	/// An invocation of a Func.
	/// </summary>
	[Serializable]
	public class FuncCall : ISerializable
	{
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


		//Serialization support.
		protected FuncCall(SerializationInfo info, StreamingContext context)
		{
			UID = info.GetInt32("UID");

			string fName = info.GetString("Func");
			if (!FuncDefinitions.FunctionsByName.ContainsKey(fName))
			{
				throw new SerializationException("Function '" + fName + "' not found!");
			}
			Calling = FuncDefinitions.FunctionsByName[fName];

			Inputs = new FuncInput[info.GetInt32("NInputs")];
			if (Inputs.Length != Calling.Params.Count)
			{
				throw new SerializationException("Expected function '" + fName + "' to have " +
												 Inputs.Length + " params, but it has " +
												 Calling.Params.Count);
			}

			for (int i = 0; i < Inputs.Length; ++i)
			{
				Inputs[i] = (FuncInput)info.GetValue("Input" + i.ToString(), typeof(FuncInput));
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("UID", UID);
			info.AddValue("Func", Calling.Name);

			info.AddValue("NInputs", Inputs.Length);
			for (int i = 0; i < Inputs.Length; ++i)
			{
				info.AddValue("Input" + i.ToString(), Inputs[i]);
			}
		}
	}
}