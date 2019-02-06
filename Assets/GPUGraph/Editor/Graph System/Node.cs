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
	/// A shader operation that takes in several floats and outputs a single float.
	/// Note that this class implements ISerializable,
	///     so sub-classes *must* implement the constructor that goes with it.
	/// </summary>
	[Serializable]
	public abstract class Node : ISerializable
	{
		/// <summary>
		/// Positioning constants for the GUI window.
		/// </summary>
		private static readonly float OutputHeight = 30.0f,
									  TitleBarHeight = 30.0f,
									  InputSpacing = 20.0f;

		/// <summary>
		/// Compares two floats like normal, except that if they are both NaN, they are considered to be *equal*.
		/// </summary>
		public static bool AreFloatsDifferent(float f1, float f2)
		{
			bool nan1 = float.IsNaN(f1),
				 nan2 = float.IsNaN(f2);
			return (nan1 && !nan2) ||
				   (!nan1 && nan2) ||
				   (!nan1 && !nan2 && f1 != f2);
		}



		/// <summary>
		/// A unique identifier in the graph.
		/// </summary>
		public int UID = -1;


		/// <summary>
		/// The graph that owns this instance.
		/// </summary>
		public Graph Owner;

		/// <summary>
		/// This node's window's position in the editor.
		/// </summary>
		public Rect Pos;

		/// <summary>
		/// The inputs into this node.
		/// Anyone outside this class should never modify it!
		/// Any child classes should take care that "InputNames" and "InputDefaultVals"
		///     always have the same number of elements as this list.
		/// </summary>
		public List<NodeInput> Inputs = new List<NodeInput>();
		protected List<string> InputNames = new List<string>();
		protected List<float> InputDefaultVals = new List<float>();


		public virtual Color GUIColor { get { return Color.white; } }

		/// <summary>
		/// Gets the name of the output variable this node computes.
		/// </summary>
		public virtual string OutputName
		{
			get { return GetType().ToString().RemoveEverythingBefore('.') + UID; }
		}

		/// <summary>
		/// A display name for this node in the editor.
		/// </summary>
		public abstract string PrettyName { get; }


		public Node(Rect pos, List<NodeInput> inputs, List<string> inputNames, List<float> inputDefaultVals)
		{
			Pos = pos;
			Inputs = inputs;
			InputNames = inputNames;
			InputDefaultVals = inputDefaultVals;
		}
		protected Node() { }


		public string GetInputName(int index) { return InputNames[index]; }
		public float GetInputDefaultValue(int index) { return InputDefaultVals[index]; }

		/// <summary>
		/// Generates a new node identical to this one, including the UID,
		/// but with a different graph owner.
		/// </summary>
		public Node Clone(Graph newOwner, bool addToOwner, int idOffset = 0)
		{
			Node n = MakeClone();
			n.UID = UID + idOffset;
			n.Owner = newOwner;
			n.Pos = Pos;
			n.Inputs = Inputs.ToList();
			n.InputNames = InputNames.ToList();
			n.InputDefaultVals = InputDefaultVals.ToList();

			for (int i = 0; i < n.Inputs.Count; ++i)
				if (!n.Inputs[i].IsAConstant)
					n.Inputs[i] = new NodeInput(n.Inputs[i].NodeID + idOffset);

			if (addToOwner)
				newOwner.AddNode(this);
			return n;
		}
		/// <summary>
		/// Generates a new node identical to this one, including the UID,
		/// but with no graph owner.
		/// </summary>
		public Node Clone() { return Clone(null, false); }
		/// <summary>
		/// Should create the right type of node with the same properties as this one.
		/// The base properties of all nodes don't need to be filled in by this method.
		/// </summary>
		protected abstract Node MakeClone();

		/// <summary>
		/// Called when this node's owner/uid is set.
		/// </summary>
		public virtual void OnAddedToGraph() { }
		/// <summary>
		/// Called after this node's owner is done loading from a file.
		/// This is better than a method with OnDeserializedAttribute
		///     because this method is only called *after* the graph itself is fully loaded.
		/// </summary>
		public virtual void OnGraphLoaded() { }


		/// <summary>
		/// Called right before the graph generates a shader.
		/// Special nodes can take this opportunity to modify the graph however they want.
		/// Returns any new nodes that have been added to the graph.
		/// </summary>
		public virtual IEnumerable<Node> OnPreProcess() { yield break; }

		/// <summary>
		/// Appends any necessary Shaderlab property definitions to the given shader.
		/// </summary>
		/// <param name="paramPrefix">
		/// A prefix that should be applied to all Property names.
		/// </param>
		public virtual void EmitProperties(StringBuilder outCode) { }
		/// <summary>
		/// Appends any necessary definitions to the given shader.
		/// </summary>
		/// <param name="paramPrefix">
		/// A prefix that should be applied to all Property names.
		/// </param>
		public virtual void EmitDefs(StringBuilder outCode) { }
		/// <summary>
		/// Appends the actual shader code that computes the output variable.
		/// </summary>
		/// <param name="paramPrefix">
		/// A prefix that should be applied to all Property names.
		/// </param>
		public virtual void EmitCode(StringBuilder outCode) { }


		public enum GUIResults
		{
			/// <summary>
			/// Nothing happened.
			/// </summary>
			Nothing,
			/// <summary>
			/// One of the input buttons was clicked.
			/// </summary>
			ClickInput,
			/// <summary>
			/// The output button was clicked.
			/// </summary>
			ClickOutput,
			/// <summary>
			/// The "Duplicate" button was clicked.
			/// </summary>
			Duplicate,
			/// <summary>
			/// The "Delete" button was clicked.
			/// </summary>
			Delete,
			/// <summary>
			/// This node was changed in some other way.
			/// </summary>
			Other,
		}
		/// <summary>
		/// Runs the GUI display window for this node.
		/// Returns what happened.
		/// </summary>
		/// <param name="clickedInput">
		/// If the user clicked an input,
		/// the index of that input will be stored in this variable.
		/// </param>
		/// <param name="isSelected">
		/// If this node's output was previously selected, pass -1.
		/// If an input was selected, pass the index of that input.
		/// Otherwise, pass anything else.
		/// </param>
		public GUIResults OnGUI(ref int clickedInput, int isSelected)
		{
			GUIResults result = GUIResults.Nothing;

			GUILayout.BeginHorizontal();

			GUILayout.BeginVertical();

			for (int i = 0; i < Inputs.Count; ++i)
			{
				GUILayout.BeginHorizontal();

				GUILayout.Label(InputNames[i]);

				//Button to select input.
				string buttStr = (isSelected == i ? "x" : "X");
				if (GUILayout.Button(buttStr))
				{
					result = GUIResults.ClickInput;
					clickedInput = i;
				}

				//If this input is a constant, expose a text box to edit it.
				if (Inputs[i].IsAConstant)
				{
					float newVal = EditorGUILayout.FloatField(Inputs[i].ConstantValue);
					if (AreFloatsDifferent(newVal, Inputs[i].ConstantValue))
					{
						result = GUIResults.Other;
						Inputs[i] = new NodeInput(newVal);
					}
				}
				//Otherwise, expose a button to release the connection.
				else
				{
					Rect otherPos = Owner.GetNode(Inputs[i].NodeID).Pos;
					Vector2 endPos = new Vector2(otherPos.xMax, otherPos.yMin + OutputHeight) - Pos.min;

					GUIUtil.DrawLine(new Vector2(0.0f, TitleBarHeight + ((float)i * InputSpacing)),
									 endPos, 2.0f, Color.white);

					if (GUILayout.Button("Disconnect"))
					{
						Inputs[i] = new NodeInput(InputDefaultVals[i]);

						result = GUIResults.Other;
					}
				}

				GUILayout.EndHorizontal();
			}

			if (CustomGUI())
			{
				result = GUIResults.Other;
			}

			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical();

			//Output button.
			if (GUILayout.Button(isSelected == -1 ? "o" : "O"))
			{
				result = GUIResults.ClickOutput;
			}

			GUILayout.EndVertical();

			GUILayout.EndHorizontal();

			//"Duplicate" button.
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Duplicate"))
			{
				result = GUIResults.Duplicate;
			}

			GUILayout.FlexibleSpace();

			//"Delete" button.
			if (GUILayout.Button("Delete"))
			{
				result = GUIResults.Delete;
			}

			GUILayout.EndHorizontal();

			return result;
		}
		/// <summary>
		/// Child nodes can add custom GUI stuff through this method.
		/// Must return whether anything has actually changed.
		/// </summary>
		protected virtual bool CustomGUI() { return false;  }


		//Serialization stuff:

		/// <summary>
		/// Outputs class data to a serializer.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("UID", UID);
			info.AddValue("PosX", Pos.x);
			info.AddValue("PosY", Pos.y);
			info.AddValue("PosWidth", Pos.width);
			info.AddValue("PosHeight", Pos.height);

			info.AddValue("NInputs", Inputs.Count);
			for (int i = 0; i < Inputs.Count; ++i)
			{
				info.AddValue("Input" + i.ToString(), Inputs[i], typeof(NodeInput));
				info.AddValue("InputName" + i.ToString(), InputNames[i]);
				info.AddValue("InputDefaultVal" + i.ToString(), InputDefaultVals[i]);
			}
		}
		public Node(SerializationInfo info, StreamingContext context)
		{
			UID = info.GetInt32("UID");
			Pos = new Rect();
			Pos.x = info.GetSingle("PosX");
			Pos.y = info.GetSingle("PosY");
			Pos.width = info.GetSingle("PosWidth");
			Pos.height = info.GetSingle("PosHeight");

			int nIns = info.GetInt32("NInputs");
			for (int i = 0; i < nIns; ++i)
			{
				Inputs.Add((NodeInput)info.GetValue("Input" + i.ToString(), typeof(NodeInput)));
				InputNames.Add(info.GetString("InputName" + i.ToString()));
				InputDefaultVals.Add(info.GetSingle("InputDefaultVal" + i.ToString()));
			}
		}
	}
}