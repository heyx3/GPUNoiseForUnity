using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;


namespace GPUGraph
{
	/// <summary>
	/// A node whose output can be expressed as a single expression like "sin(x)" or "a + b".
	/// </summary>
	[Serializable]
	public class SimpleNode : Node
	{
		public struct Param
		{
			public string Name;
			public float DefaultValue;

			public Param(string name, float defaultVal = float.NaN)
			{
				Name = name;
				DefaultValue = defaultVal;
			}
		}


		public string Expr, Name;


		public override string PrettyName
		{
			get { return Name; }
		}


		/// <summary>
		/// Creates a new node that calls a function with a constant number of inputs.
		/// </summary>
		/// <param name="expr">
		/// An expression using the given inputs.
		/// NOTE: in the expression, an input should be surrounded by apostrophes
		///     to distinguish it from the rest of the code.
		/// </param>
		/// <param name="inputs">The inputs used by the given expression.</param>
		public SimpleNode(Rect pos, string expr, string name, params Param[] inputs)
			: base(pos,
				   inputs.Select(p => new NodeInput(p.DefaultValue)).ToList(),
				   inputs.Select(p => p.Name).ToList(),
				   inputs.Select(p => p.DefaultValue).ToList())
		{
			Expr = expr;
			Name = name;
		}

		private SimpleNode(string expr, string name) { Expr = expr; Name = name; }


		protected override Node MakeClone()
		{
			return new SimpleNode(Expr, Name);
		}

		public override void EmitCode(StringBuilder outCode)
		{
			outCode.Append("float ");
			outCode.Append(OutputName);
			outCode.Append(" = (");
			string expr = Expr;
			for (int i = 0; i < Inputs.Count; ++i)
				expr = expr.Replace("'" + GetInputName(i) + "'", Inputs[i].GetExpression(Owner));
			outCode.Append(expr);
			outCode.AppendLine(");");
		}


		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Expression", Expr);
			info.AddValue("MyName", Name);
		}
		public SimpleNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Expr = info.GetString("Expression");
			Name = info.GetString("MyName");
		}
	}
}