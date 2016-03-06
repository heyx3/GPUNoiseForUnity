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
	/// A node that can output various kinds of noise.
	/// </summary>
	[Serializable]
	public class NoiseNode : Node
	{
		public enum NoiseTypes
		{
			White, Blocky, Linear, Smooth, Smoother, Perlin, Worley
		}

		public static string GetFunc(NoiseTypes type, int nDimensions)
		{
			switch (type)
			{
				case NoiseTypes.White: return "WhiteNoise" + nDimensions;
				case NoiseTypes.Blocky: return "GridNoise" + nDimensions;
				case NoiseTypes.Linear: return "LinearNoise" + nDimensions;
				case NoiseTypes.Smooth: return "SmoothNoise" + nDimensions;
				case NoiseTypes.Smoother: return "SmootherNoise" + nDimensions;
				case NoiseTypes.Perlin: return "PerlinNoise" + nDimensions;
				case NoiseTypes.Worley: return "WorleyNoise" + nDimensions;
				default: throw new NotImplementedException(type.ToString());
			}
		}

		private static List<NodeInput> GenerateInputs(NoiseTypes t, int nDimensions,
													  List<NodeInput> currentInputs = null)
		{
			List<NodeInput> ni = new List<NodeInput>();

			//Seed.
			for (int i = 0; i < nDimensions; ++i)
			{
				if (currentInputs != null && currentInputs.Count > i)
					ni.Add(currentInputs[i]);
				else
					ni.Add(new NodeInput(0.0f));
			}

			//Scale.
			if (currentInputs != null && currentInputs.Count > nDimensions)
				ni.Add(currentInputs[nDimensions]);
			else
				ni.Add(new NodeInput(1.0f));
			//Weight.
			if (currentInputs != null && currentInputs.Count > nDimensions + 1)
				ni.Add(currentInputs[nDimensions + 1]);
			else
				ni.Add(new NodeInput(1.0f));

			//Noise-specific extras.
			switch (t)
			{
				case NoiseTypes.White:
				case NoiseTypes.Blocky:
				case NoiseTypes.Linear:
				case NoiseTypes.Smooth:
				case NoiseTypes.Smoother:
				case NoiseTypes.Perlin:
					break;
				case NoiseTypes.Worley:
					if (currentInputs != null && currentInputs.Count == 7)
					{
						ni.Add(currentInputs[5]);
						ni.Add(currentInputs[6]);
					}
					else
					{
						ni.Add(new NodeInput(0.5f));
						ni.Add(new NodeInput(0.5f));
					}
					break;
				default: throw new NotImplementedException(t.ToString());
			}

			return ni;
		}
		private static List<string> GetInputNames(NoiseTypes t, int nDimensions)
		{
			List<string> n = new List<string>();

			//Seeds.
			n.Add("x");
			if (nDimensions > 1)
				n.Add("y");
			if (nDimensions > 2)
				n.Add("z");

			//Scale and weight.
			n.Add("Scale");
			n.Add("Weight");
			
			//Noise-specific extras.
			switch (t)
			{
				case NoiseTypes.White:
				case NoiseTypes.Blocky:
				case NoiseTypes.Linear:
				case NoiseTypes.Smooth:
				case NoiseTypes.Smoother:
				case NoiseTypes.Perlin:
					break;
				case NoiseTypes.Worley:
					n.Add("Cell Variance X");
					n.Add("Cell Variance Y");
					break;
				default: throw new NotImplementedException(t.ToString());
			}
			return n;
		}
		private static List<float> GetInputDefaultVals(NoiseTypes t, int nDimensions)
		{
			List<float> n = new List<float>();

			//Seeds.
			for (int i = 0; i < nDimensions; ++i)
				n.Add(0.0f);

			//Scale and weight.
			n.Add(1.0f);
			n.Add(1.0f);

			//Noise-specific extras.
			switch (t)
			{
				case NoiseTypes.White:
				case NoiseTypes.Blocky:
				case NoiseTypes.Linear:
				case NoiseTypes.Smooth:
				case NoiseTypes.Smoother:
				case NoiseTypes.Perlin:
					break;
				case NoiseTypes.Worley:
					n.Add(0.5f);
					n.Add(0.5f);
					break;
				default: throw new NotImplementedException(t.ToString());
			}
			return n;
		}


		public NoiseTypes NoiseType;

		public int NDimensions;
		private static string[] DimensionsStrArray = new string[] { "1D", "2D", "3D" };
		private static int[] DimensionsArray = new int[] { 1, 2, 3 };

		//TODO: If using worley noise, store string fields to calculate distance and output, and calculate the noise inline.


		public override string PrettyName
		{
			get
			{
				string nd = NDimensions.ToString();
				switch (NoiseType)
				{
					case NoiseTypes.White: return nd + "D White Noise";
					case NoiseTypes.Blocky: return nd + "D Blocky Noise";
					case NoiseTypes.Linear: return nd + "D Linear Noise";
					case NoiseTypes.Smooth: return nd + "D Smooth Noise";
					case NoiseTypes.Smoother: return nd + "D Smoother Noise";
					case NoiseTypes.Perlin: return nd + "D Perlin Noise";
					case NoiseTypes.Worley: return nd + "D Worley Noise";
					default: throw new NotImplementedException(NoiseType.ToString());
				}
			}
		}


		public NoiseNode(Rect pos, NoiseTypes noiseType, int nDimensions)
			: base(pos, GenerateInputs(noiseType, nDimensions),
				   GetInputNames(noiseType, nDimensions), GetInputDefaultVals(noiseType, nDimensions))
		{
			NoiseType = noiseType;
			NDimensions = nDimensions;
		}

		private NoiseNode() { }


		protected override Node MakeClone()
		{
			NoiseNode n = new NoiseNode();
			n.NoiseType = NoiseType;
			n.NDimensions = NDimensions;
			return n;
		}

		public override void EmitCode(StringBuilder outCode)
		{
			outCode.Append("float ");
			outCode.Append(OutputName);
			outCode.Append(" = ");
			outCode.Append(Inputs[NDimensions + 1].GetExpression(Owner));
			outCode.Append(" * ");
			outCode.Append(GetFunc(NoiseType, NDimensions));
			outCode.Append("(");
			outCode.Append(Inputs[NDimensions].GetExpression(Owner));
			outCode.Append(" * float");
			outCode.Append(NDimensions);
			outCode.Append("(");
			for (int i = 0; i < NDimensions; ++i)
			{
				outCode.Append(Inputs[i].GetExpression(Owner));
				if (i < NDimensions - 1)
				{
					outCode.Append(", ");
				}
			}
			outCode.Append(")");
			switch (NoiseType)
			{
				case NoiseTypes.White:
				case NoiseTypes.Blocky:
				case NoiseTypes.Linear:
				case NoiseTypes.Smooth:
				case NoiseTypes.Smoother:
				case NoiseTypes.Perlin:
					break;
				case NoiseTypes.Worley:
					outCode.Append(", float2(");
					outCode.Append(Inputs[NDimensions + 2].GetExpression(Owner));
					outCode.Append(", ");
					outCode.Append(Inputs[NDimensions + 3].GetExpression(Owner));
					outCode.Append(")");
					break;
				default: throw new NotImplementedException(NoiseType.ToString());
			}
			outCode.AppendLine(");");
		}

		protected override bool CustomGUI()
		{
			bool changed = false;

			NoiseTypes newType = (NoiseTypes)EditorGUILayout.EnumPopup(NoiseType);
			if (newType != NoiseType)
			{
				changed = true;
				NoiseType = newType;
			}

			int newDim = EditorGUILayout.IntPopup(NDimensions,
												  DimensionsStrArray, DimensionsArray);
			if (newDim != NDimensions)
			{
				changed = true;
				NDimensions = newDim;
			}


			//If anything changed, regenerate the inputs.
			if (changed)
			{
				Inputs = GenerateInputs(NoiseType, NDimensions, Inputs);
				InputNames = GetInputNames(NoiseType, NDimensions);
				InputDefaultVals = GetInputDefaultVals(NoiseType, NDimensions);
			}

			return changed;
		}


		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("NoiseType", (int)NoiseType);
			info.AddValue("NDimensions", NDimensions);
		}
		public NoiseNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			NoiseType = (NoiseTypes)info.GetInt32("NoiseType");
			NDimensions = info.GetInt32("NDimensions");
		}
	}
}