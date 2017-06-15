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
		//TODO: Add the concept of a seed.

		public static readonly string Functions = @"

	//Hash functions.
	//The hash functions are modified versions of this idea: https://www.shadertoy.com/view/4djSRW
	#define _HASH(p4, swizzle) \
		p4 = frac(p4 * float4(443.897, 441.423, 437.195, 444.129)); \
		p4 += dot(p4, p4.wzxy + 19.19); \
		return frac(dot(p.xyzw, p.zwxy) * p.swizzle);
	float _hashTo1(float4 p)
	{
		_HASH(p, x);
	}
	float2 _hashTo2(float4 p)
	{
		_HASH(p, xy);
	}
	float3 _hashTo3(float4 p)
	{
		_HASH(p, xyz);
	}
	float4 _hashTo4(float4 p)
	{
		_HASH(p, xyzw);
	}
	#undef _HASH
	float  hashTo1(float p)  { return _hashTo1(p.xxxx); }
	float  hashTo1(float2 p) { return _hashTo1(p.xyxy); }
	float  hashTo1(float3 p) { return _hashTo1(p.xyzx); }
	float  hashTo1(float4 p) { return _hashTo1(p);		}
	float2 hashTo2(float p)  { return _hashTo2(p.xxxx); }
	float2 hashTo2(float2 p) { return _hashTo2(p.xyxy); }
	float2 hashTo2(float3 p) { return _hashTo2(p.xyzx); }
	float2 hashTo2(float4 p) { return _hashTo2(p);		}
	float3 hashTo3(float p)  { return _hashTo3(p.xxxx); }
	float3 hashTo3(float2 p) { return _hashTo3(p.xyxy); }
	float3 hashTo3(float3 p) { return _hashTo3(p.xyzx); }
	float3 hashTo3(float4 p) { return _hashTo3(p);		}
	float4 hashTo4(float p)  { return _hashTo4(p.xxxx); }
	float4 hashTo4(float2 p) { return _hashTo4(p.xyxy); }
	float4 hashTo4(float3 p) { return _hashTo4(p.xyzx); }
	float4 hashTo4(float4 p) { return _hashTo4(p);		}

	//The body (no open/close braces) for a function that interpolates between noise values on a 1D grid.
	//Allows customization of the interpolant (see LinearNoise1 and SmoothNoise1 for simple examples).
	#define INTERP_NOISE1(tModifier, posModifier) \
		f = posModifier(f); \
        float minF = floor(f), \
			  maxF = ceil(f); \
		 \
		float t = f - minF; \
		t = tModifier; \
		 \
		return lerp(hashTo1(posModifier(minF)), \
					hashTo1(posModifier(maxF)), \
					t);
	//The body (no open/close braces) for a function that interpolates between noise values on a 2D grid.
	//Allows customization of the interpolant (see LinearNoise2 and SmoothNoise2 for simple examples).
	#define INTERP_NOISE2(tModifier, posModifier) \
		f = posModifier(f); \
		float2 minF = floor(f), \
			   maxF = ceil(f); \
		 \
		float2 t = f - minF; \
		t = tModifier; \
		 \
		return lerp(lerp(hashTo1(posModifier(minF)), \
						 hashTo1(posModifier(float2(maxF.x, minF.y))), \
						 t.x), \
					lerp(hashTo1(posModifier(float2(minF.x, maxF.y))), \
						 hashTo1(posModifier(maxF)), \
						 t.x), \
					t.y);
	//The body (no open/close braces) for a function that interpolates between noise values on a 3D grid.
	//Allows customization of the interpolant (see LinearNoise3 and SmoothNoise3 for simple examples).
	#define INTERP_NOISE3(tModifier, posModifier) \
		f = posModifier(f); \
		float3 minF = floor(f), \
			   maxF = ceil(f); \
		 \
		float3 t = f - minF; \
		t = tModifier; \
		 \
		return lerp(lerp(lerp(hashTo1(posModifier(minF)), \
							  hashTo1(posModifier(float3(maxF.x, minF.yz))), \
							  t.x), \
						 lerp(hashTo1(posModifier(float3(minF.x, maxF.y, minF.z))), \
							  hashTo1(posModifier(float3(maxF.xy, minF.z))), \
							  t.x), \
						 t.y), \
					lerp(lerp(hashTo1(posModifier(float3(minF.xy, maxF.z))), \
							  hashTo1(posModifier(float3(maxF.x, minF.y, maxF.z))), \
							  t.x), \
						 lerp(hashTo1(posModifier(float3(minF.x, maxF.yz))), \
							  hashTo1(posModifier(maxF)), \
							  t.x), \
						 t.y), \
					t.z);
	//The body (no open/close braces) for a function that interpolates between noise values on a 4D grid.
	//Allows customization of the interpolant (see LinearNoise4 and SmoothNoise4 for simple examples).
	#define INTERP_NOISE4(tModifier, posModifier) \
		f = posModifier(f); \
		float4 minF = floor(f), \
			   maxF = ceil(f); \
		 \
		float4 t = f - minF; \
		t = tModifier; \
		 \
		return lerp(lerp(lerp(lerp(hashTo1(posModifier(minF)), \
								   hashTo1(posModifier(float4(maxF.x, minF.yzw))), \
								   t.x), \
							  lerp(hashTo1(posModifier(float4(minF.x, maxF.y, minF.zw))), \
								   hashTo1(posModifier(float4(maxF.xy, minF.zw))), \
								   t.x), \
							  t.y), \
						 lerp(lerp(hashTo1(posModifier(float4(minF.xy, maxF.z, minF.w))), \
								   hashTo1(posModifier(float4(maxF.x, minF.y, maxF.z, minF.w))), \
								   t.x), \
							  lerp(hashTo1(posModifier(float4(minF.x, maxF.yz, minF.w))), \
								   hashTo1(posModifier(float4(maxF.xyz, minF.w))), \
								   t.x), \
							  t.y), \
						 t.z), \
					lerp(lerp(lerp(hashTo1(posModifier(float4(minF.xyz, maxF.w))), \
								   hashTo1(posModifier(float4(maxF.x, minF.yz, maxF.w))), \
								   t.x), \
							  lerp(hashTo1(posModifier(float4(minF.x, maxF.y, minF.z, maxF.w))), \
								   hashTo1(posModifier(float4(maxF.xy, minF.z, maxF.w))), \
								   t.x), \
							  t.y), \
						 lerp(lerp(hashTo1(posModifier(float4(minF.xy, maxF.zw))), \
								   hashTo1(posModifier(float4(maxF.x, minF.y, maxF.zw))), \
								   t.x), \
							  lerp(hashTo1(posModifier(float4(minF.x, maxF.yzw))), \
								   hashTo1(posModifier(maxF)), \
								   t.x), \
							  t.y), \
						 t.z), \
					t.w);

    #define IDENTITY(x) x
    #define WRAP(x) (frac((x) / valMax) * valMax)

	float GridNoise1(float f) { return hashTo1(floor(f)); }
	float GridNoise2(float2 f) { return hashTo1(floor(f)); }
	float GridNoise3(float3 f) { return hashTo1(floor(f)); }
	float GridNoise4(float4 f) { return hashTo1(floor(f)); }
    float GridNoise1_Wrap(float f, float valMax) { return GridNoise1(frac(f / valMax) * valMax); }
    float GridNoise2_Wrap(float2 f, float2 valMax) { return GridNoise2(frac(f / valMax) * valMax); }
    float GridNoise3_Wrap(float3 f, float3 valMax) { return GridNoise3(frac(f / valMax) * valMax); }
    float GridNoise4_Wrap(float4 f, float4 valMax) { return GridNoise4(frac(f / valMax) * valMax); }

	float LinearNoise1(float f) { INTERP_NOISE1(t, IDENTITY) }
	float LinearNoise2(float2 f) { INTERP_NOISE2(t, IDENTITY) }
	float LinearNoise3(float3 f) { INTERP_NOISE3(t, IDENTITY) }
	float LinearNoise4(float4 f) { INTERP_NOISE4(t, IDENTITY) }
    float LinearNoise1_Wrap(float f, float valMax) { INTERP_NOISE1(t, WRAP) }
    float LinearNoise2_Wrap(float2 f, float2 valMax) { INTERP_NOISE2(t, WRAP) }
    float LinearNoise3_Wrap(float3 f, float3 valMax) { INTERP_NOISE3(t, WRAP) }
    float LinearNoise4_Wrap(float4 f, float4 valMax) { INTERP_NOISE4(t, WRAP) }

	float SmoothNoise1(float f) { INTERP_NOISE1(smoothstep(0.0, 1.0, t), IDENTITY) }
	float SmoothNoise2(float2 f) { INTERP_NOISE2(smoothstep(0.0, 1.0, t), IDENTITY) }
	float SmoothNoise3(float3 f) { INTERP_NOISE3(smoothstep(0.0, 1.0, t), IDENTITY) }
	float SmoothNoise4(float4 f) { INTERP_NOISE4(smoothstep(0.0, 1.0, t), IDENTITY) }
	float SmoothNoise1_Wrap(float f, float valMax) { INTERP_NOISE1(smoothstep(0.0, 1.0, t), WRAP) }
	float SmoothNoise2_Wrap(float2 f, float2 valMax) { INTERP_NOISE2(smoothstep(0.0, 1.0, t), WRAP) }
	float SmoothNoise3_Wrap(float3 f, float3 valMax) { INTERP_NOISE3(smoothstep(0.0, 1.0, t), WRAP) }
	float SmoothNoise4_Wrap(float4 f, float4 valMax) { INTERP_NOISE4(smoothstep(0.0, 1.0, t), WRAP) }

	float SmootherNoise1(float f) { INTERP_NOISE1(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, t)), IDENTITY) }
	float SmootherNoise2(float2 f) { INTERP_NOISE2(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, t)), IDENTITY) }
	float SmootherNoise3(float3 f) { INTERP_NOISE3(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, t)), IDENTITY) }
	float SmootherNoise4(float4 f) { INTERP_NOISE4(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, t)), IDENTITY) }
	float SmootherNoise1_Wrap(float f, float valMax) { INTERP_NOISE1(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, t)), WRAP) }
	float SmootherNoise2_Wrap(float2 f, float2 valMax) { INTERP_NOISE2(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, t)), WRAP) }
	float SmootherNoise3_Wrap(float3 f, float3 valMax) { INTERP_NOISE3(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, t)), WRAP) }
	float SmootherNoise3_Wrap(float4 f, float4 valMax) { INTERP_NOISE4(smoothstep(0.0, 1.0, smoothstep(0.0, 1.0, t)), WRAP) }

	//TODO: Convert these perlin noise function pairs to three functions: one wrap, one non-wrap, and one helper function with the bulk of the code. Make the helper function based on the wrapping version of the noise.
	//TODO: Add 4d perlin and worley. Consider using iteration.

	float PerlinNoise1(float f)
	{
		float minX = floor(f),
			  maxX = minX + 1.0,
			  t = f - minX;

		float minX_V = -1.0 + (2.0 * hashTo1(minX));
		float toMin = -t;

		float maxX_V = -1.0 + (2.0 * hashTo1(maxX));
		float toMax = 1.0 - t;

        t = smoothstep(0.0, 1.0, t);
		float outVal = lerp(dot(minX_V, toMin),
							dot(maxX_V, toMax),
                            t);
		return 0.5 + (0.5 * outVal);
	}
	float PerlinNoise1_Wrap(float f, float valMax)
	{
		float minX = floor(f),
			  maxX = minX + 1.0,
			  t = f - minX;
        f = WRAP(f);
        minX = WRAP(minX);
        maxX = WRAP(maxX);

		float minX_V = -1.0 + (2.0 * hashTo1(minX));
		float toMin = -t;

		float maxX_V = -1.0 + (2.0 * hashTo1(maxX));
		float toMax = 1.0 - t;

        t = smoothstep(0.0, 1.0, t);
		float outVal = lerp(dot(minX_V, toMin),
							dot(maxX_V, toMax),
							t);
		return 0.5 + (0.5 * outVal);
	}

	float PerlinNoise2(float2 f)
	{
		float2 minXY = floor(f),
			   maxXY = minXY + 1.0,
			   minXmaxY = float2(minXY.x, maxXY.y),
			   maxXminY = float2(maxXY.x, minXY.y),
			   t = f - minXY;

		float temp = hashTo1(minXY);
		float2 minXY_V = -1.0 + (2.0 * float2(temp, hashTo1(temp)));
		float2 toMinXY = -t;

		temp = hashTo1(maxXY);
		float2 maxXY_V = -1.0 + (2.0 * float2(temp, hashTo1(temp)));
		float2 toMaxXY = 1.0 - t;

        float4 toMinAndMaxXY = float4(toMinXY, toMaxXY);

		temp = hashTo1(minXmaxY);
		float2 minXmaxY_V = -1.0 + (2.0 * float2(temp, hashTo1(temp)));
		float2 toMinXmaxY = toMinAndMaxXY.xw;

		temp = hashTo1(maxXminY);
		float2 maxXminY_V = -1.0 + (2.0 * float2(temp, hashTo1(temp)));
		float2 toMaxXminY = toMinAndMaxXY.zy;

		t = smoothstep(0.0, 1.0, t);
		float outVal = lerp(lerp(dot(minXY_V, toMinXY),
								 dot(maxXminY_V, toMaxXminY),
								 t.x),
							lerp(dot(minXmaxY_V, toMinXmaxY),
								 dot(maxXY_V, toMaxXY),
								 t.x),
							t.y);
		return 0.5 + (0.5 * outVal);
	}
	float PerlinNoise2_Wrap(float2 f, float valMax)
	{
		float2 minXY = floor(f),
			   maxXY = minXY + 1.0,
			   t = f - minXY;
        f = WRAP(f);
        minXY = WRAP(minXY);
        maxXY = WRAP(maxXY);

	    float2 minXmaxY = float2(minXY.x, maxXY.y),
			   maxXminY = float2(maxXY.x, minXY.y);

		float temp = hashTo1(minXY);
		float2 minXY_V = -1.0 + (2.0 * float2(temp, hashTo1(temp)));
		float2 toMinXY = -t;

		temp = hashTo1(maxXY);
		float2 maxXY_V = -1.0 + (2.0 * float2(temp, hashTo1(temp)));
		float2 toMaxXY = 1.0 - t;

        float4 toMinAndMaxXY = float4(toMinXY, toMaxXY);

		temp = hashTo1(minXmaxY);
		float2 minXmaxY_V = -1.0 + (2.0 * float2(temp, hashTo1(temp)));
		float2 toMinXmaxY = toMinAndMaxXY.xw;

		temp = hashTo1(maxXminY);
		float2 maxXminY_V = -1.0 + (2.0 * float2(temp, hashTo1(temp)));
		float2 toMaxXminY = toMinAndMaxXY.zy;

		t = smoothstep(0.0, 1.0, t);
		float outVal = lerp(lerp(dot(minXY_V, toMinXY),
								 dot(maxXminY_V, toMaxXminY),
								 t.x),
							lerp(dot(minXmaxY_V, toMinXmaxY),
								 dot(maxXY_V, toMaxXY),
								 t.x),
							t.y);
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
			   t = f - minXYZ;

		float temp = hashTo1(minXYZ),
			  temp2 = hashTo1(temp);
		float3 minXYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMinXYZ = -t;

		temp = hashTo1(maxXYZ);
		temp2 = hashTo1(temp);
		float3 maxXYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMaxXYZ = 1.0 - t;

		temp = hashTo1(minXYmaxZ);
		temp2 = hashTo1(temp);
		float3 minXYmaxZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMinXYmaxZ = minXYmaxZ - f;

		temp = hashTo1(minXmaxYminZ);
		temp2 = hashTo1(temp);
		float3 minXmaxYminZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMinXmaxYminZ = minXmaxYminZ - f;

		temp = hashTo1(minXmaxYZ);
		temp2 = hashTo1(temp);
		float3 minXmaxYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMinXmaxYZ = minXmaxYZ - f;

		temp = hashTo1(maxXminYZ);
		temp2 = hashTo1(temp);
		float3 maxXminYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMaxXminYZ = maxXminYZ - f;

		temp = hashTo1(maxXminYmaxZ);
		temp2 = hashTo1(temp);
		float3 maxXminYmaxZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMaxXminYmaxZ = maxXminYmaxZ - f;

		temp = hashTo1(maxXYminZ);
		temp2 = hashTo1(temp);
		float3 maxXYminZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMaxXYminZ = maxXYminZ - f;

		t = smoothstep(0.0, 1.0, t);
		float outVal = lerp(lerp(lerp(dot(minXYZ_V, toMinXYZ),
									  dot(maxXminYZ_V, toMaxXminYZ),
									  t.x),
								 lerp(dot(minXmaxYminZ_V, toMinXmaxYminZ),
									  dot(maxXYminZ_V, toMaxXYminZ),
									  t.x),
								 t.y),
							lerp(lerp(dot(minXYmaxZ_V, toMinXYmaxZ),
									  dot(maxXminYmaxZ_V, toMaxXminYmaxZ),
									  t.x),
								 lerp(dot(minXmaxYZ_V, toMinXmaxYZ),
									  dot(maxXYZ_V, toMaxXYZ),
									  t.x),
								 t.y),
							t.z);
		return 0.5 + (0.5 * outVal);
	}
	float PerlinNoise3_Wrap(float3 f, float3 valMax)
	{
		float3 minXYZ = floor(f),
			   maxXYZ = minXYZ + float3(1.0, 1.0, 1.0),
			   t = f - minXYZ;
        f = WRAP(f);
        minXYZ = WRAP(minXYZ);
        maxXYZ = WRAP(maxXYZ);

		float3 minXYmaxZ =    float3(minXYZ.xy, maxXYZ.z),
			   minXmaxYminZ = float3(minXYZ.x, maxXYZ.y, minXYZ.z),
			   minXmaxYZ =    float3(minXYZ.x, maxXYZ.y, maxXYZ.z),
			   maxXminYZ =    float3(maxXYZ.x, minXYZ.y, minXYZ.z),
			   maxXminYmaxZ = float3(maxXYZ.x, minXYZ.y, maxXYZ.z),
			   maxXYminZ =    float3(maxXYZ.xy, minXYZ.z);

		float temp = hashTo1(minXYZ),
			  temp2 = hashTo1(temp);
		float3 minXYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMinXYZ = -t;

		temp = hashTo1(maxXYZ);
		temp2 = hashTo1(temp);
		float3 maxXYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMaxXYZ = 1.0 - t;

		temp = hashTo1(minXYmaxZ);
		temp2 = hashTo1(temp);
		float3 minXYmaxZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMinXYmaxZ = float3(toMinXYZ.xy, toMaxXYZ.z);

		temp = hashTo1(minXmaxYminZ);
		temp2 = hashTo1(temp);
		float3 minXmaxYminZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMinXmaxYminZ = float3(toMinXYZ.x, toMaxXYZ.y, toMinXYZ.z);

		temp = hashTo1(minXmaxYZ);
		temp2 = hashTo1(temp);
		float3 minXmaxYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMinXmaxYZ = float3(toMinXYZ.x, toMaxXYZ.yz);

		temp = hashTo1(maxXminYZ);
		temp2 = hashTo1(temp);
		float3 maxXminYZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMaxXminYZ = float3(toMaxXYZ.x, toMinXYZ.yz);

		temp = hashTo1(maxXminYmaxZ);
		temp2 = hashTo1(temp);
		float3 maxXminYmaxZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMaxXminYmaxZ = float3(toMaxXYZ.x, toMinXYZ.y, toMaxXYZ.z);

		temp = hashTo1(maxXYminZ);
		temp2 = hashTo1(temp);
		float3 maxXYminZ_V = -1.0 + (2.0 * float3(temp, temp2, hashTo1(temp2)));
		float3 toMaxXYminZ = float3(toMaxXYZ.xy, toMinXYZ.z);

		t = smoothstep(0.0, 1.0, t);
		float outVal = lerp(lerp(lerp(dot(minXYZ_V, toMinXYZ),
									  dot(maxXminYZ_V, toMaxXminYZ),
									  t.x),
								 lerp(dot(minXmaxYminZ_V, toMinXmaxYminZ),
									  dot(maxXYminZ_V, toMaxXYminZ),
									  t.x),
								 t.y),
							lerp(lerp(dot(minXYmaxZ_V, toMinXYmaxZ),
									  dot(maxXminYmaxZ_V, toMaxXminYmaxZ),
									  t.x),
								 lerp(dot(minXmaxYZ_V, toMinXmaxYZ),
									  dot(maxXYZ_V, toMaxXYZ),
									  t.x),
								 t.y),
							t.z);
		return 0.5 + (0.5 * outVal);
	}

	float WorleyNoise1(float f, float cellVariance)
	{
		float cellThis = floor(f),
			  cellLess = cellThis - 1.0,
			  cellMore = cellThis + 1.0;

	#define VAL(var) abs((var + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashTo1(var))) - f)
		return min(VAL(cellThis), min(VAL(cellLess), VAL(cellMore)));
	#undef VAL
	}
	float WorleyNoise1_Wrap(float f, float cellVariance, float valMax)
	{
		float cellThis = floor(f),
			  cellLess = cellThis - 1.0,
			  cellMore = cellThis + 1.0;

	#define VAL(var) distance(f, (var + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashTo1(WRAP(var)))))
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
	#define VAL(var) distance(f, var + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashTo2(var)))
	#define MIN3(a, b, c) min(a, min(b, c))
	return MIN3(MIN3(VAL(cellMinXY),    VAL(cellMidXMinY), VAL(cellMaxXMinY)),
				MIN3(VAL(cellMinXMidY), VAL(cellMidXY),    VAL(cellMaxXMidY)),
				MIN3(VAL(cellMinXMaxY), VAL(cellMidXMaxY), VAL(cellMaxXY)));
	#undef VAL
	#undef MIN3
	}
	float WorleyNoise2_Wrap(float2 f, float2 cellVariance, float2 valMax)
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
	#define VAL(var) distance(f, var + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashTo2(WRAP(var))))
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
	#define VAL(swizzle) distance(f, cell##swizzle + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashTo3(cell##swizzle)))
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
	}
	float WorleyNoise3_Wrap(float3 f, float3 cellVariance, float3 valMax)
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
	#define VAL(swizzle) distance(f, cell##swizzle + lerp(0.5 - cellVariance, 0.5 + cellVariance, hashTo3(WRAP(cell##swizzle))))
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