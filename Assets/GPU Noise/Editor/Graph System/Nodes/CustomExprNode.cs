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
	/// A node whose output is a custom expression typed in by the user.
	/// Inputs are specified as variables starting like $1, $2, $3, etc.
	/// </summary>
	[Serializable]
	public class CustomExprNode : Node
	{
		public string Expr = "$1";


		public override string PrettyName { get { return "Custom Expression"; } }


		public CustomExprNode(Rect pos, string expr)
			: base(pos, new List<NodeInput>(), new List<string>(), new List<float>())
		{
			Expr = expr;
			SetUpInputs();
		}


		protected override Node MakeClone()
		{
			return new CustomExprNode(new Rect(), Expr);
		}
		protected override bool CustomGUI()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Expr");
			GUILayout.Space(15.0f);
			string newExpr = GUILayout.TextField(Expr);
			GUILayout.EndHorizontal();

			if (newExpr != Expr)
			{
				Expr = newExpr;
				SetUpInputs();
				return true;
			}
			return false;
		}
		public override void EmitCode(StringBuilder outCode)
		{
			outCode.Append("float ");
			outCode.Append(OutputName);
			outCode.Append(" = (");
			
			string expr = Expr;
			for (int i = 0; i < Inputs.Count; ++i)
			{
				expr = expr.Replace("$" + (i + 1),
									Inputs[i].GetExpression(Owner));
			}
			outCode.Append(expr);
			outCode.AppendLine(");");
		}
		
		private void SetUpInputs()
		{
			//Get all new inputs.

			List<NodeInput> inps = new List<NodeInput>();
			List<string> inpNames = new List<string>();
			List<float> inpDefaultVals = new List<float>();

			int varI = 1;
			while (Expr.Contains("$" + varI.ToString()))
			{
				inps.Add(new NodeInput(0.0f));
				inpNames.Add("In" + varI.ToString());
				inpDefaultVals.Add(0.0f);

				varI += 1;
			}

			//If any old inputs are left, use their values.
			for (int i = 0; i < inps.Count && i < Inputs.Count; ++i)
				inps[i] = Inputs[i];

			Inputs = inps;
			InputNames = inpNames;
			InputDefaultVals = inpDefaultVals;
		}


		//Serialization:
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Expr", Expr);
		}
		public CustomExprNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Expr = info.GetString("Expr");
		}
	}
}