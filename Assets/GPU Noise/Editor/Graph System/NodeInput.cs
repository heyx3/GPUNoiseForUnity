using System;
using System.Collections.Generic;

namespace GPUGraph
{
	/// <summary>
	/// A float value for inputting into a Node.
	/// Either a constant value or the output from a different Node.
	/// Nodes are stored by their UID.
	/// </summary>
	[Serializable]
	public struct NodeInput
	{
		private float constValue;
		private int nodeID;


		public bool IsValid { get { return Graph.IsValidUID(nodeID) || !Single.IsNaN(constValue); } }

		public bool IsAConstant { get { return !Graph.IsValidUID(nodeID); } }


		public float ConstantValue { get { return constValue; } }
		public int NodeID { get { return nodeID; } }


		public NodeInput(float constVal) { constValue = constVal; nodeID = -1; }

		public NodeInput(int _nodeID) { nodeID = _nodeID; constValue = float.NaN; }
		public NodeInput(Node n) { nodeID = n.UID; constValue = float.NaN; }


		/// <summary>
		/// Returns an expession that evaluates to what this input represents.
		/// </summary>
		public string GetExpression(Graph g)
		{
			if (IsAConstant)
			{
				if (float.IsNaN(ConstantValue))
				{
					return "0.0";
				}
				else
				{
					return ConstantValue.ToCodeString();
				}
			}
			else
			{
				return g.GetNode(nodeID).OutputName;
			}
		}
	}
}