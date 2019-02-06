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
	/// Optionally, this node can become a whole function instead of a one-line expression.
	/// </summary>
	[Serializable]
	public class CustomExprNode : Node
	{
		public bool IsLongForm = false;
		public string Expr = "$1";


		public override string PrettyName { get { return "Custom " + (IsLongForm ? "Function" : "Expression"); } }


		public CustomExprNode(Rect pos, string expr, bool isLongForm = false)
			: base(pos, new List<NodeInput>(), new List<string>(), new List<float>())
		{
			Expr = expr;
			IsLongForm = isLongForm;
			SetUpInputs();
		}


		protected override Node MakeClone()
		{
			return new CustomExprNode(new Rect(), Expr, IsLongForm);
		}
		protected override bool CustomGUI()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Expr");
			GUILayout.Space(15.0f);
			string newExpr;
			if (IsLongForm)
				newExpr = EditorGUILayout.TextArea(Expr);
			else
				newExpr = EditorGUILayout.TextField(Expr);
			GUILayout.EndHorizontal();

			bool newIsLongForm = EditorGUILayout.Toggle("Is full function", IsLongForm);

			if (newExpr != Expr || newIsLongForm != IsLongForm)
			{
				Expr = newExpr;
				IsLongForm = newIsLongForm;

				Pos.size = Vector2.one;

				SetUpInputs();
				return true;
			}
			return false;
		}

		public override void EmitDefs(StringBuilder outCode)
		{
			if (IsLongForm)
			{
				string expr = Expr;

				outCode.Append("float CustomFunc_");
				outCode.Append(UID);
				outCode.Append("(");
				for (int i = 0; i < Inputs.Count; ++i)
				{
					if (i > 0)
						outCode.Append(", ");
					outCode.Append("float in");
					outCode.Append(i);

					expr = expr.Replace("$" + (i + 1).ToString(), "in" + i.ToString());
				}
				outCode.AppendLine(")");
				outCode.AppendLine("{");
				outCode.AppendLine(expr);
				outCode.AppendLine("}");
			}
		}
		public override void EmitCode(StringBuilder outCode)
		{
			outCode.Append("float ");
			outCode.Append(OutputName);
			outCode.Append(" = ");

			if (IsLongForm)
			{
				outCode.Append("CustomFunc_");
				outCode.Append(UID);
				outCode.Append("(");
				for (int i = 0; i < Inputs.Count; ++i)
				{
					if (i > 0)
						outCode.Append(", ");
					outCode.Append(Inputs[i].GetExpression(Owner));
				}

				outCode.AppendLine(");");
			}
			else
			{
				outCode.Append("(");
				string expr = Expr;
				for (int i = 0; i < Inputs.Count; ++i)
				{
					expr = expr.Replace("$" + (i + 1),
										Inputs[i].GetExpression(Owner));
				}
				outCode.Append(expr);
				outCode.AppendLine(");");
			}
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
			info.AddValue("IsLongForm", IsLongForm);
		}
		public CustomExprNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Expr = info.GetString("Expr");
			IsLongForm = info.GetBoolean("IsLongForm");
		}
	}
}