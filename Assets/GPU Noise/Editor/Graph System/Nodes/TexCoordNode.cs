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
	/// A node whose output is the X or Y value of the UV.
	/// </summary>
	[Serializable]
	public class TexCoordNode : Node
	{
		public bool IsX;


		public override Color GUIColor { get { return new Color(0.85f, 0.85f, 1.0f); } }
		public override string OutputName { get { return "IN.texcoord." + (IsX ? "x" : "y"); } }
		public override string PrettyName { get { return "UV " + (IsX ? "X" : "Y"); } }


		public TexCoordNode(Rect pos, bool isX)
			: base(pos, new List<NodeInput>(), new List<string>(), new List<float>())
		{
			IsX = isX;
		}
		private TexCoordNode(bool isX) { IsX = isX; }


		protected override Node MakeClone()
		{
			return new TexCoordNode(IsX);
		}

		protected override bool CustomGUI()
		{
			bool _isX = IsX;

			int result = EditorGUILayout.Popup((IsX ? 0 : 1), new string[] { "X", "Y" });
			IsX = (result == 0 ? true : false);

			return IsX != _isX;
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("IsX", IsX);
		}
		public TexCoordNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			IsX = info.GetBoolean("IsX");
		}
	}
}