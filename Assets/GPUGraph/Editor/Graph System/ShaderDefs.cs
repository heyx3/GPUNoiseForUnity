using System;
using System.Text;
using System.Collections.Generic;


namespace GPUGraph
{
	/// <summary>
	/// Definitions used in the creation of a graph's shader.
	/// </summary>
	public static class ShaderDefs
	{
		public static readonly string DefaultHash1 =
	"frac(sin(x * 78.233) * 43758.5453)";
		public static readonly string DefaultHash2 =
	"frac(sin(dot(x, float2(12.9898, 78.233))) * 43758.5453)";
		public static readonly string DefaultHash3 =
	"frac(sin(dot(x, float3(12.9898, 78.233, 36.34621))) * 43758.5453)";

		public static string GetHashFuncs(string hash1, string hash2, string hash3)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(@"
	float hashValue1(float x)
	{
		return (");
			sb.Append(hash1);
			sb.Append(@");
	}
	float hashValue2(float2 x)
	{
		return (");
			sb.Append(hash2);
			sb.Append(@");
	}
	float hashValue3(float3 x)
	{
		return (");
			sb.Append(hash3);
			sb.Append(@");
	}");
			return sb.ToString();
		}

		public static readonly string Functions = @"
	//The body (no open/close braces) for a function that interpolates between noise values on a 1D grid.
	//Allows customization of the interpolant (see LinearNoise1 and SmoothNoise1 for simple examples).
	#define INTERP_NOISE1(vModifier) \
		float minV = floor(inV), \
			  maxV = ceil(inV); \
	\
		float v = inV - minV; \
		v = vModifier; \
	\
		return lerp(hashValue1(minV), \
					hashValue1(maxV), \
					v);
	//The body (no open/close braces) for a function that interpolates between noise values on a 2D grid.
	//Allows customization of the interpolant (see LinearNoise2 and SmoothNoise2 for simple examples).
	#define INTERP_NOISE2(vModifier) \
		float2 minV = floor(inV), \
			   maxV = ceil(inV); \
	\
		float2 v = inV - minV; \
		v = vModifier; \
	\
		return lerp(lerp(hashValue2(minV), \
						 hashValue2(float2(maxV.x, minV.y)), \
						 v.x), \
					lerp(hashValue2(float2(minV.x, maxV.y)), \
						 hashValue2(maxV), \
						 v.x), \
					v.y);
	//The body (no open/close braces) for a function that interpolates between noise values on a 3D grid.
	//Allows customization of the interpolant (see LinearNoise3 and SmoothNoise3 for simple examples).
	#define INTERP_NOISE3(vModifier) \
		float3 minV = floor(inV), \
			   maxV = ceil(inV); \
	\
		float3 v = inV - minV; \
		v = vModifier; \
	\
		return lerp(lerp(lerp(hashValue3(minV), \
							  hashValue3(float3(maxV.x, minV.y, minV.z)), \
							  v.x), \
						 lerp(hashValue3(float3(minV.x, maxV.y, minV.z)), \
							  hashValue3(float3(maxV.xy, minV.z)), \
							  v.x), \
						 v.y), \
					lerp(lerp(hashValue3(float3(minV.xy, maxV.z)), \
							  hashValue3(float3(maxV.x, minV.y, maxV.z)), \
							  v.x), \
						 lerp(hashValue3(float3(minV.x, maxV.y, maxV.z)), \
							  hashValue3(maxV), \
							  v.x), \
						 v.y), \
					v.z);
	float GridNoise1(float f)
	{
		return hashValue1(floor(f));
	}
	float GridNoise2(float2 f)
	{
		return hashValue2(floor(f));
	}
	float GridNoise3(float3 f)
	{
		return hashValue3(floor(f));
	}
	float LinearNoise1(float inV)
	{
		INTERP_NOISE1(v)
	}
	float LinearNoise2(float2 inV)
	{
		INTERP_NOISE2(v)
	}
	float LinearNoise3(float3 inV)
	{
		INTERP_NOISE3(v)
	}
	float SmoothNoise1(float inV)
	{
		INTERP_NOISE1(smoothstep(0.0, 1.0, v))
	}
	float SmoothNoise2(float2 inV)
	{
		INTERP_NOISE2(smoothstep(0.0, 1.0, v))
	}
	float SmoothNoise3(float3 inV)
	{
		INTERP_NOISE3(smoothstep(0.0, 1.0, v))
	}
	float SmootherNoise1(float inV)
	{
		INTERP_NOISE1(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, v)))
	}
	float SmootherNoise2(float2 inV)
	{
		INTERP_NOISE2(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, v)))
	}
	float SmootherNoise3(float3 inV)
	{
		INTERP_NOISE3(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, v)))
	}
	float PerlinNoise1(float f)
	{
		float minX = floor(f),
			  maxX = minX + 1.0,
			  lerpVal = f - minX;

		float minX_V = -1.0 + (2.0 * hashValue1(minX));
		float toMin = -lerpVal;

		float maxX_V = -1.0 + (2.0 * hashValue1(maxX));
		float toMax = maxX - f;

		float outVal = lerp(dot(minX_V, toMin),
							dot(maxX_V, toMax),
							smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, lerpVal)));
		return 0.5 + (0.5 * outVal);
	}
	float PerlinNoise2(float2 f)
	{
		float2 minXY = floor(f),
			   maxXY = minXY + float2(1.0, 1.0),
			   minXmaxY = float2(minXY.x, maxXY.y),
			   maxXminY = float2(maxXY.x, minXY.y),
			   lerpVal = f - minXY;

		float temp = hashValue2(minXY);
		float2 minXY_V = -1.0 + (2.0 * float2(temp, hashValue1(temp)));
		float2 toMinXY = -lerpVal;

		temp = hashValue2(maxXY);
		float2 maxXY_V = -1.0 + (2.0 * float2(temp, hashValue1(temp)));
		float2 toMaxXY = maxXY - f;

		temp = hashValue2(minXmaxY);
		float2 minXmaxY_V = -1.0 + (2.0 * float2(temp, hashValue1(temp)));
		float2 toMinXmaxY = minXmaxY - f;

		temp = hashValue2(maxXminY);
		float2 maxXminY_V = -1.0 + (2.0 * float2(temp, hashValue1(temp)));
		float2 toMaxXminY = maxXminY - f;

		lerpVal = smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, lerpVal));
		float outVal = lerp(lerp(dot(minXY_V, toMinXY),
								 dot(maxXminY_V, toMaxXminY),
								 lerpVal.x),
							lerp(dot(minXmaxY_V, toMinXmaxY),
								 dot(maxXY_V, toMaxXY),
								 lerpVal.x),
							lerpVal.y);
		return 0.5 + (0.5 * outVal);
	}
	float PerlinNoise3(float3 f)
	{
		float3 minXYZ = floor(f),
			   maxXYZ = minXYZ + float3(1.0, 1.0, 1.0),
			   minXYmaxZ =    float3(minXYZ.xy, maxXYZ.z),
			   minXmaxYminZ = float3(minXYZ.x, maxXYZ.y, minXYZ.z),
			   minXmaxYZ =    float3(minXYZ.x, maxXYZ.y, maxXYZ.z),
			   maxXminYZ =    float3(maxXYZ.x, minXYZ.y, minXYZ.z),
			   maxXminYmaxZ = float3(maxXYZ.x, minXYZ.y, maxXYZ.z),
			   maxXYminZ =    float3(maxXYZ.xy, minXYZ.z),
			   lerpVal = f - minXYZ;

		float temp = hashValue3(minXYZ),
			  temp2 = hashValue1(temp);
		float3 minXYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashValue1(temp2)));
		float3 toMinXYZ = -lerpVal;

		temp = hashValue3(maxXYZ);
		temp2 = hashValue1(temp);
		float3 maxXYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashValue1(temp2)));
		float3 toMaxXYZ = maxXYZ - f;

		temp = hashValue3(minXYmaxZ);
		temp2 = hashValue1(temp);
		float3 minXYmaxZ_V = -1.0 + (2.0 * float3(temp, temp2, hashValue1(temp2)));
		float3 toMinXYmaxZ = minXYmaxZ - f;

		temp = hashValue3(minXmaxYminZ);
		temp2 = hashValue1(temp);
		float3 minXmaxYminZ_V = -1.0 + (2.0 * float3(temp, temp2, hashValue1(temp2)));
		float3 toMinXmaxYminZ = minXmaxYminZ - f;

		temp = hashValue3(minXmaxYZ);
		temp2 = hashValue1(temp);
		float3 minXmaxYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashValue1(temp2)));
		float3 toMinXmaxYZ = minXmaxYZ - f;

		temp = hashValue3(maxXminYZ);
		temp2 = hashValue1(temp);
		float3 maxXminYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashValue1(temp2)));
		float3 toMaxXminYZ = maxXminYZ - f;

		temp = hashValue3(maxXminYmaxZ);
		temp2 = hashValue1(temp);
		float3 maxXminYmaxZ_V = -1.0 + (2.0 * float3(temp, temp2, hashValue1(temp2)));
		float3 toMaxXminYmaxZ = maxXminYmaxZ - f;

		temp = hashValue3(maxXYminZ);
		temp2 = hashValue1(temp);
		float3 maxXYminZ_V = -1.0 + (2.0 * float3(temp, temp2, hashValue1(temp2)));
		float3 toMaxXYminZ = maxXYminZ - f;

		lerpVal = smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, lerpVal));
		float outVal = lerp(lerp(lerp(dot(minXYZ_V, toMinXYZ),
									  dot(maxXminYZ_V, toMaxXminYZ),
									  lerpVal.x),
								 lerp(dot(minXmaxYminZ_V, toMinXmaxYminZ),
									  dot(maxXYminZ_V, toMaxXYminZ),
									  lerpVal.x),
								 lerpVal.y),
							lerp(lerp(dot(minXYmaxZ_V, toMinXYmaxZ),
									  dot(maxXminYmaxZ_V, toMaxXminYmaxZ),
									  lerpVal.x),
								 lerp(dot(minXmaxYZ_V, toMinXmaxYZ),
									  dot(maxXYZ_V, toMaxXYZ),
									  lerpVal.x),
								 lerpVal.y),
							lerpVal.z);
		return 0.5 + (0.5 * outVal);
	}
	float WorleyNoise1(float f, float cellVariance)
	{
		float cellThis = floor(f),
			  cellLess = cellThis - 1.0,
			  cellMore = cellThis + 1.0;
					     
	#define VAL(var) abs((var + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashValue1(var))) - f)
		return min(VAL(cellThis), min(VAL(cellLess), VAL(cellMore)));
	#undef VAL
	}
	float WorleyNoise2(float2 f, float2 cellVariance)
	{
		const float3 zon = float3(0.0, 1.0, -1.0);
		float2 cellMidXY = floor(f),
			   cellMinXY = cellMidXY + zon.zz,
			   cellMidXMinY = cellMidXY + zon.xz,
			   cellMaxXMinY = cellMidXY + zon.yz,
			   cellMinXMidY = cellMidXY + zon.zx,
			   cellMaxXMidY = cellMidXY + zon.yx,
			   cellMinXMaxY = cellMidXY + zon.zy,
			   cellMidXMaxY = cellMidXY + zon.xy,
			   cellMaxXY = cellMidXY + zon.yy;
	#define VAL(var) distance(f, var + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashValue2(var)))
	#define MIN3(a, b, c) min(a, min(b, c))
	return MIN3(MIN3(VAL(cellMinXY),    VAL(cellMidXMinY), VAL(cellMaxXMinY)),
				MIN3(VAL(cellMinXMidY), VAL(cellMidXY),    VAL(cellMaxXMidY)),
				MIN3(VAL(cellMinXMaxY), VAL(cellMidXMaxY), VAL(cellMaxXY)));
	#undef VAL
	#undef MIN3
	}
	float WorleyNoise3(float3 f, float3 cellVariance)
	{
		float3 cellyyy = floor(f);

		const float3 c = float3(-1.0, 0.0, 1.0);
	#define MAKE_VAL(swizzle) float3 cell##swizzle = cellyyy + c.swizzle;
		MAKE_VAL(xxx)
		MAKE_VAL(xxy)
		MAKE_VAL(xxz)
		MAKE_VAL(xyx)
		MAKE_VAL(xyy)
		MAKE_VAL(xyz)
		MAKE_VAL(xzx)
		MAKE_VAL(xzy)
		MAKE_VAL(xzz)
		MAKE_VAL(yxx)
		MAKE_VAL(yxy)
		MAKE_VAL(yxz)
		MAKE_VAL(yyx)
		MAKE_VAL(yyz)
		MAKE_VAL(yzx)
		MAKE_VAL(yzy)
		MAKE_VAL(yzz)
		MAKE_VAL(zxx)
		MAKE_VAL(zxy)
		MAKE_VAL(zxz)
		MAKE_VAL(zyx)
		MAKE_VAL(zyy)
		MAKE_VAL(zyz)
		MAKE_VAL(zzx)
		MAKE_VAL(zzy)
		MAKE_VAL(zzz)
	#define VAL(swizzle) distance(f, cell##swizzle + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashValue3(cell##swizzle)))
	#define MIN3(a, b, c) min(a, min(b, c))
	#define MIN9(a, b, c, d, e, f, g, h, i) MIN3(MIN3(a, b, c), MIN3(d, e, f), MIN3(g, h, i))
		return MIN3(MIN9(VAL(xxx), VAL(xxy), VAL(xxz),
						 VAL(xyx), VAL(xyy), VAL(xyz),
						 VAL(xzx), VAL(xzy), VAL(xzz)),
					MIN9(VAL(yxx), VAL(yxy), VAL(yxz),
						 VAL(yyx), VAL(yyy), VAL(yyz),
						 VAL(yzx), VAL(yzy), VAL(yzz)),
					MIN9(VAL(zxx), VAL(zxy), VAL(zxz),
						 VAL(zyx), VAL(zyy), VAL(zyz),
						 VAL(zzx), VAL(zzy), VAL(zzz)));
	#undef MAKE_VAL
	#undef VAL
	#undef MIN3
	#undef MIN9
	}";
	}
}