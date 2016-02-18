using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace GPUNoise
{
	/// <summary>
	/// A float value for inputting into a Func.
	/// Either a constant value or the output from a FuncCall.
	/// FuncCalls are stored by their UID.
	/// </summary>
	[Serializable]
	public struct FuncInput
	{
		private float constValue;
		private long funcCallID;


		public float ConstantValue { get { return constValue; } }
		public long FuncCallID { get { return funcCallID; } }

		public bool IsAConstantValue { get { return FuncCallID < 0; } }
		public bool IsValid { get { return !IsAConstantValue || !Single.IsNaN(ConstantValue); } }


		public FuncInput(float constantValue) { constValue = constantValue; funcCallID = -1; }
		public FuncInput(long funcCallUID) { funcCallID = funcCallUID; constValue = float.NaN; }
		public FuncInput(FuncCall call) : this(call.UID) { }


		/// <summary>
		/// Gets the shader code expression that evaluates to this instance's value.
		/// </summary>
		public string GetShaderExpression(Graph g)
		{
			if (IsAConstantValue)
			{
				if (float.IsNaN(ConstantValue))
					return "0.0";
				else
				{
					string str = ConstantValue.ToString();
					if (!str.Contains('.'))
					{
						str += ".0";
					}
					return str;
				}
			}
			else
			{
				FuncCall c = g.UIDToFuncCall[FuncCallID];

				System.Text.StringBuilder sb = new System.Text.StringBuilder();

				for (int i = 0; i < c.Inputs.Length; ++i)
				{
					if (i > 0)
					{
						sb.Append(", ");
					}

					sb.Append(c.Inputs[i].GetShaderExpression(g));
				}

				return c.Calling.GetInvocation(c.CustomDat, sb.ToString());
			}
		}
	}
}