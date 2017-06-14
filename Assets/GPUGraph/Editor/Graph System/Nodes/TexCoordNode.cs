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
		/// <summary>
		/// 0 for X, 1 for Y, 2 for Z.
		/// Z is a special index that is only used for generating 3D textures.
		/// </summary>
		public byte Index;
		private static readonly string[] indexChars = { "X", "Y", "Z" };


		public override Color GUIColor { get { return new Color(0.85f, 0.85f, 1.0f); } }
		public override string OutputName
		{
			get
			{
				if (Index == 2)
					return GraphUtils.Param_UVz;
				else
					return "IN.texcoord." + indexChars[Index].ToLower();
			}
		}
		public override string PrettyName { get { return "UV " + indexChars[Index]; } }


		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="index">0 for X, 1 for Y, 2 for Z.</param>
		public TexCoordNode(Rect pos, byte index)
			: base(pos, new List<NodeInput>(), new List<string>(), new List<float>())
		{
			Index = index;
		}
		private TexCoordNode(byte index) { Index = index; }


		protected override Node MakeClone()
		{
			return new TexCoordNode(Index);
		}

		protected override bool CustomGUI()
		{
			byte oldIndex = Index;
			Index = (byte)EditorGUILayout.Popup(Index, indexChars);
			return oldIndex != Index;
		}
		public override void EmitProperties(StringBuilder outCode)
		{
			//Don't emit the same property twice!
			if (!outCode.ToString().Contains(GraphUtils.Param_UVz))
			{
				outCode.Append('\t');
				outCode.Append(GraphUtils.Param_UVz);
				outCode.AppendLine(" (\"UV z\", Float) = 0.0");
			}
		}
		public override void EmitDefs(StringBuilder outCode)
		{
			//Don't emit the same uniform twice!
			string def = "float " + GraphUtils.Param_UVz;
			if (!outCode.ToString().Contains(def))
			{
				outCode.Append(def);
				outCode.AppendLine(";");
			}
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Index", Index);
		}
		public TexCoordNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			//Older versions of graph files used a "IsX" bool to choose between X and Y.
			//Newer versions that also support Z use the "Index" byte.

			foreach (SerializationEntry entry in info)
			{
				switch (entry.Name)
				{
					case "Index":
						Index = (byte)entry.Value;
						break;

					case "IsX":
						Index = (bool)entry.Value ? (byte)1 : (byte)0;
						break;
				}
			}
		}
	}
}