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

		public string GetFunc(NoiseTypes type, int nDimensions, bool wraps)
		{
			switch (type)
			{
				case NoiseTypes.White: return "hashTo1";
				case NoiseTypes.Blocky: return "GridNoise";
				case NoiseTypes.Linear: return "LinearNoise";
				case NoiseTypes.Smooth: return "SmoothNoise";
				case NoiseTypes.Smoother: return "SmootherNoise";
				case NoiseTypes.Perlin: return "PerlinNoise";
				case NoiseTypes.Worley:
					if (IsWorleyDefault)
						return "WorleyNoise";
					else
						return "Worley_" + UID;
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
                    for (int i = 0; i < nDimensions; ++i)
                    {
                        int index = nDimensions + 2 + i;
                        if (currentInputs == null || currentInputs.Count <= index)
                            ni.Add(new NodeInput(0.5f));
                        else
                            ni.Add(currentInputs[index]);
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
			if (nDimensions > 3)
				n.Add("w");

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
                    if (nDimensions > 1)
					    n.Add("Cell Variance Y");
                    if (nDimensions > 2)
                        n.Add("Cell Variance Z");
					if (nDimensions > 3)
						n.Add("Cell Variance W");
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
                    if (nDimensions > 1)
					    n.Add(0.5f);
                    if (nDimensions > 2)
                        n.Add(0.5f);
					if (nDimensions > 3)
						n.Add(0.5f);
					break;
				default: throw new NotImplementedException(t.ToString());
			}
			return n;
		}


		public NoiseTypes NoiseType;

		public int NDimensions;
		private static string[] DimensionsStrArray = new string[] { "1D", "2D", "3D", "4D" };
		private static int[] DimensionsArray = new int[] { 1, 2, 3, 4 };

		public string Worley_DistanceCalc = "distance($1, $2)",
					  Worley_NoiseCalc = "$1";
        public bool Wraps = false;

		public bool IsWorleyDefault
		{
			get { return Worley_DistanceCalc == "distance($1, $2)" && Worley_NoiseCalc == "$1"; }
		}


		public override Color GUIColor { get { return new Color(1.0f, 0.85f, 0.85f); } }
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


		public override void EmitDefs(StringBuilder outCode)
		{
			//If using customized Worley noise, we need to emit a custom function to call in the shader.

			if (NoiseType != NoiseTypes.Worley || IsWorleyDefault)
				return;

			outCode.Append("#define DIST(a, b) (");
			outCode.AppendLine(Worley_DistanceCalc.Replace("$1", "a").Replace("$2", "b") + ")");
			outCode.Append("#define OUTVAL(a, b) (");
			outCode.AppendLine(Worley_NoiseCalc.Replace("$1", "a").Replace("$2", "b") + ")");

			outCode.AppendLine("#define INSERT_MIN(a, b, new) if (new < a) { b = a; a = new; } else { b = min(b, new); }");
            if (Wraps)
			    outCode.AppendLine("#define WORLEY_POS(p, nDims) (p + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashTo##nDims(WRAP(p))))");
            else
                outCode.AppendLine("#define WORLEY_POS(p, nDims) (p + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashTo##nDims(p)))");

			outCode.Append("float ");
			outCode.Append(GetFunc(NoiseType, NDimensions, Wraps));
			if (NDimensions == 1)
			{
                outCode.Append("(float seed, float cellVariance");
                if (Wraps)
                    outCode.Append(", float valMax");
				outCode.AppendLine(@")
{
	float cellThis = floor(seed),
		  cellLess = cellThis - 1.0,
		  cellMore = cellThis + 1.0;
	float noise1 = DIST(WORLEY_POS(cellThis, 1), seed),
		  noise2 = DIST(WORLEY_POS(cellLess, 1), seed),
		  noise3 = DIST(WORLEY_POS(cellMore, 1), seed);
	float min1 = min(noise1, noise2),
		  min2 = max(noise1, noise2);
	INSERT_MIN(min1, min2, noise3);
	return OUTVAL(min1, min2);
}");
			}
			else if (NDimensions == 2)
            {
                outCode.Append("(float2 seed, float2 cellVariance");
                if (Wraps)
                    outCode.Append(", float2 valMax");
                outCode.AppendLine(@")
{
	float2 centerCell = floor(seed);

	float min1, min2;
	const float3 c = float3(-1.0, 0.0, 1.0);

	//Calculate the first two noise values and store them in min1/min2.
	float2 cellPos = centerCell;
	float cellNoise = DIST(WORLEY_POS(cellPos, 2), seed);
	{
		cellPos = centerCell + c.xx;
		float cellNoise2 = DIST(WORLEY_POS(cellPos, 2), seed);

		min1 = min(cellNoise2, cellNoise);
		min2 = max(cellNoise2, cellNoise);
	}

	//Now calculate the rest of the noise values.
#define DO_VAL(swizzle) \
	cellPos = centerCell + c.swizzle; \
	cellNoise = DIST(WORLEY_POS(cellPos, 2), seed); \
	INSERT_MIN(min1, min2, cellNoise);

	DO_VAL(xy);
	DO_VAL(xz);
	DO_VAL(yx);
	DO_VAL(yz);
	DO_VAL(zx);
	DO_VAL(zy);
	DO_VAL(zz);

#undef DO_VAL

	return OUTVAL(min1, min2);
}");
			}
			else if (NDimensions == 3)
            {
                outCode.Append("(float3 seed, float3 cellVariance");
                if (Wraps)
                    outCode.Append(", float3 valMax");
                outCode.AppendLine(@")
{
	float3 cellyyy = floor(seed);

	float min1, min2;
	const float3 c = float3(-1.0, 0.0, 1.0);

	//Calculate the first two noise values and store them in min1/min2.
	float3 cellPos = cellyyy;
	float cellNoise = DIST(WORLEY_POS(cellPos, 3), seed);
	{
		cellPos = cellyyy + c.xxx;
		float cellNoise2 = DIST(WORLEY_POS(cellPos, 3), seed);

		min1 = min(cellNoise2, cellNoise);
		min2 = max(cellNoise2, cellNoise);
	}

	//Now calculate the rest of the noise values.
#define DO_VAL(swizzle) \
	cellPos = cellyyy + c.swizzle; \
	cellNoise = DIST(WORLEY_POS(cellPos, 3), seed); \
	INSERT_MIN(min1, min2, cellNoise);

	DO_VAL(xxy)
	DO_VAL(xxz)
	DO_VAL(xyx)
	DO_VAL(xyy)
	DO_VAL(xyz)
	DO_VAL(xzx)
	DO_VAL(xzy)
	DO_VAL(xzz)
	DO_VAL(yxx)
	DO_VAL(yxy)
	DO_VAL(yxz)
	DO_VAL(yyx)
	DO_VAL(yyz)
	DO_VAL(yzx)
	DO_VAL(yzy)
	DO_VAL(yzz)
	DO_VAL(zxx)
	DO_VAL(zxy)
	DO_VAL(zxz)
	DO_VAL(zyx)
	DO_VAL(zyy)
	DO_VAL(zyz)
	DO_VAL(zzx)
	DO_VAL(zzy)
	DO_VAL(zzz)

#undef DO_VAL

	return OUTVAL(min1, min2);
}");
			}
			else if (NDimensions == 4)
			{
				outCode.Append("(float4 seed, float4 cellVariance");
				if (Wraps)
					outCode.Append(", float4 valMax");
				outCode.AppendLine(@")
{
	float min1, min2;
	const float3 c = float3(-1.0, 0.0, 1.0);
	float4 cellPos;
	float cellNoise;

	//The center noise value.
	float4 cellyyyy = floor(seed);

	//Calculate the first two noise values and store them in min1/min2.
	cellPos = cellyyyy;
	cellNoise = DIST(WORLEY_POS(cellPos, 4), seed);
	{
		cellPos = cellyyyy + c.xxxx;
		float cellNoise2 = DIST(WORLEY_POS(cellPos, 4), seed);

		min1 = min(cellNoise2, cellNoise);
		min2 = max(cellNoise2, cellNoise);
	}

//Define a way to easily iterate over every possible swizzle of a 4D vector.
#define DO(swizzle) \
	cellPos = cellyyyy + c.swizzle; \
	cellNoise = DIST(WORLEY_POS(cellPos, 4), seed); \
	INSERT_MIN(min1, min2, cellNoise);
//Runs DO() on every swizzle of 'c' that has the given XYZ.
#define FOREACH_XYZ(swizzleXYZ) \
	DO(swizzleXYZ##x) DO(swizzleXYZ##y) DO(swizzleXYZ##z)
//Runs DO() on every swizzle of 'c' that has the given XY.
#define FOREACH_XY(swizzleXY) \
	FOREACH_XYZ(swizzleXY##x) FOREACH_XYZ(swizzleXY##y) FOREACH_XYZ(swizzleXY##z)
//Runs DO() on every swizzle of 'c' that has the given X.
#define FOREACH_X(swizzleX) \
	FOREACH_XY(swizzleX##x) FOREACH_XY(swizzleX##y) FOREACH_XY(swizzleX##z)
//Runs DO() on every swizzle, except yyyx and yyyy because we already did those.
#define FOREACH_DO \
	FOREACH_X(x) \
		FOREACH_XY(yx) \
			FOREACH_XYZ(yyx) \
				DO(yyyz) \
			FOREACH_XYZ(yyz) \
		FOREACH_XY(yz) \
	FOREACH_X(z)

	FOREACH_DO;
	return OUTVAL(min1, min2);

#undef FOREACH_XYZ
#undef FOREACH_XY
#undef FOREACH_X
#undef FOREACH_DO
#undef DO
}");
			}
			else
			{
				UnityEngine.Assertions.Assert.IsTrue(false, NDimensions.ToString());
			}

			outCode.AppendLine("#undef DIST");
			outCode.AppendLine("#undef OUTVAL");
			outCode.AppendLine("#undef INSERT_MIN");
			outCode.AppendLine("#undef WORLEY_POS");
		}
		public override void EmitCode(StringBuilder outCode)
		{
            string scaleStr = Inputs[NDimensions].GetExpression(Owner),
                   weightStr = Inputs[NDimensions + 1].GetExpression(Owner);

			outCode.Append("float ");
			outCode.Append(OutputName);
			outCode.Append(" = ");
			outCode.Append(weightStr);
			outCode.Append(" * ");
			outCode.Append(GetFunc(NoiseType, NDimensions, Wraps));
			outCode.Append("(");
			outCode.Append(scaleStr);
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
					outCode.Append(", float");
					outCode.Append(NDimensions);
					outCode.Append("(");
					for (int i = 0; i < NDimensions; ++i)
					{
						if (i > 0)
							outCode.Append(", ");

						int index = NDimensions + 2 + i;
						outCode.Append(Inputs[index].GetExpression(Owner));
					}
					outCode.Append(")");
					break;
				default: throw new NotImplementedException(NoiseType.ToString());
            }
            if (Wraps && NoiseType != NoiseTypes.White)
            {
                outCode.Append(", ");
                outCode.Append(scaleStr);
            }
            outCode.AppendLine(");");
		}

		protected override bool CustomGUI()
		{
			bool changed = false;

			//Edit noise type.
			NoiseTypes newType = (NoiseTypes)EditorGUILayout.EnumPopup(NoiseType);
			if (newType != NoiseType)
			{
				changed = true;
				NoiseType = newType;
			}

			//Edit the number of dimensions.
			int newDim = EditorGUILayout.IntPopup(NDimensions,
												  DimensionsStrArray, DimensionsArray);
			if (newDim != NDimensions)
			{
				changed = true;
				NDimensions = newDim;
			}

			//Edit whether the noise wraps.
			if (NoiseType != NoiseTypes.White)
			{
				GUILayout.BeginHorizontal();
				bool newWraps = GUILayout.Toggle(Wraps, "Wraps?");
				if (newWraps != Wraps)
				{
					changed = true;
					Wraps = newWraps;
				}
				if (Wraps && NoiseType == NoiseTypes.Worley)
				{
					GUILayout.Label("Only works if Scale = 2^n");
				}
				GUILayout.EndHorizontal();
			}

			//If using worley noise, edit the customizable aspects of it.
			if (NoiseType == NoiseTypes.Worley)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Distance Func");
				GUILayout.Space(15.0f);
				string newWorleyDist = GUILayout.TextField(Worley_DistanceCalc);
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Noise Expression:");
				GUILayout.Space(15.0f);
				string newWorleyNoise = GUILayout.TextField(Worley_NoiseCalc);
				GUILayout.EndHorizontal();

				if (Worley_DistanceCalc != newWorleyDist || Worley_NoiseCalc != newWorleyNoise)
				{
					Worley_DistanceCalc = newWorleyDist;
					Worley_NoiseCalc = newWorleyNoise;
					changed = true;
				}
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
            info.AddValue("Wraps", Wraps);
			info.AddValue("Worley_DistanceCalc", Worley_DistanceCalc);
			info.AddValue("Worley_NoiseCalc", Worley_NoiseCalc);
		}
		public NoiseNode(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
            //"Wraps" isn't in older graph files, so give it a default value.
            Wraps = false;

            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "NoiseType":
                        NoiseType = (NoiseTypes)(int)entry.Value;
                        break;
                    case "NDimensions":
                        NDimensions = (int)entry.Value;
                        break;
                    case "Wraps":
                        Wraps = (bool)entry.Value;
                        break;

					case "Worley_DistanceCalc":
						Worley_DistanceCalc = (string)entry.Value;
						break;
					case "Worley_NoiseCalc":
						Worley_NoiseCalc = (string)entry.Value;
						break;
                }
            }
        }
        protected override Node MakeClone()
        {
            NoiseNode n = new NoiseNode();
            n.NoiseType = NoiseType;
            n.NDimensions = NDimensions;
            n.Worley_DistanceCalc = Worley_DistanceCalc;
            n.Worley_NoiseCalc = Worley_NoiseCalc;
            n.Wraps = Wraps;
            return n;
        }
    }
}