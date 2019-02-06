//A RNG system that behaves more like a CPU RNG library,
//    with global state that is modified each time some numbers are generated.
//This isn't actually used by GPUGraph, but is provided as a utility for others.
//The initial state is set by the float parameter "_Seed".

//Based on: https://www.shadertoy.com/view/4djSRW
//TODO: Test integer XORshift performance as an alternative to this float-hashing.

//These values work best when the seed values are small, e.x. UVs.
#define HASH_CONSTS float4(443.897, 441.423, 437.195, 444.129)
#define HASH_OFFSET 19.19
//These values work best when the seed values are large, e.x. world or pixel coordinates.
/*
#define HASH_CONSTS float4(1.031, .1030, .0973, .1099)
#define HASH_OFFSET 19.19
*/


uniform float _Seed;
static float Seed;

void initRNG() { Seed = _Seed; }

//The following functions incorporate multiple given floats
//    into the RNG system.
//The floats are assumed to be small values (usually [0, 1]).
//Values outside that range are fine, but may yield worse results.
//Note that the current value of "Seed" still affects the result;
//    to completely reset the seed, just set "Seed" directly.
void addRandSeed(float f)
{
    float2 p2 = frac(float2(f, Seed) * HASH_CONSTS.x);
    p2 += dot(p2, p2.yx + HASH_OFFSET);
    Seed = frac((p2.x + p2.y) * p2.x);
}
void addRandSeed(float2 f)
{
    float3 p3 = frac(float3(f, Seed) * HASH_CONSTS.x);
    p3 += dot(p3, p3.yzx + HASH_OFFSET);
    Seed = frac((p3.x + p3.y) * p3.z);
}
void addRandSeed(float3 f)
{
    float4 p4 = frac(float4(f, Seed) * HASH_CONSTS.x);
    p4 += dot(p4, p4.wzyx + HASH_OFFSET);
    Seed = frac((p4.x + p4.y + p4.z) * p4.w);
}


//The following functions generate some amount of uniform random numbers between 0 and 1.
float rand1()
{
    float f = frac(Seed * HASH_CONSTS.x);
    f += f * (f + HASH_OFFSET);
    Seed = frac((f + f) * f);
    return Seed;
}
float2 rand2()
{
    float3 f = frac(Seed * HASH_CONSTS.xyz);
    f += dot(f, f.yzx + HASH_OFFSET);
    float2 val = frac((f.xx + f.yz) * f.zy);
    Seed = val.x;
    return val;
}
float3 rand3()
{
    float3 f = frac(Seed * HASH_CONSTS.xyz);
    f += dot(f, f.yzx + HASH_OFFSET);
    f = frac((f.xxy + f.yzz) * f.zyx);
    Seed = f.y;
    return f;
}
float4 rand4()
{
    float4 f = frac(Seed * HASH_CONSTS);
    f += dot(f, f.wzxy + HASH_OFFSET);
    f = frac((f.xxyz + f.yzzw) * f.zywx);
    Seed = f.z;
    return f;
}

//The following functions generate 2 gaussian random numbers
//    using 2 uniform random numbers in the range [0, 1].
float2 randGaussian2(float2 randUniform2)
{
    //Box-Muller method.

    const float epsilon = 0.000001,
                twoPi = (3.14159265359 * 2.0);

    float d1 = sqrt(-2.0 * log(randUniform2.x + epsilon)),
          d2 = twoPi * randUniform2.y;

    return float2(d1 * cos(d2), d1 * sin(d2));
}
float2 randGaussian2(float2 randUniform2, float mean, float deviation)
{
    return mean + (deviation * randGaussian2(randUniform2));
}
float2 randGaussian2(float mean, float deviation)
{
    return randGaussian2(rand2(), mean, deviation);
}
float2 randGaussian2()
{
    return randGaussian2(rand2());
}

//The following functions generate 1 gaussian random number
//    using 3 uniform random numbers in the range [0, 1].
float randGaussian1(float3 randUniform3)
{
    //Box-Muller method.

    const float epsilon = 0.000001,
                twoPi = (3.14159265359 * 2.0);

    float d1 = sqrt(-2.0 * log(randUniform3.x + epsilon)),
          d2 = twoPi * randUniform3.y;

    return (randUniform3.z > 0.5) ?
               (d1 * cos(d2)) :
               (d1 * sin(d2));
}
float randGaussian1(float3 randUniform3, float mean, float deviation)
{
    return mean + (deviation * randGaussian1(randUniform3));
}
float randGaussian1(float mean, float deviation)
{
    return randGaussian1(rand3(), mean, deviation);
}
float1 randGaussian1()
{
    return randGaussian1(rand3());
}


//Generating a position on/in a sphere using uniform random numbers.
//https://math.stackexchange.com/questions/87230/picking-random-points-in-the-volume-of-sphere-with-uniform-probability
float3 posOnUnitSphere(float yPos01, float angle01)
{
    float yPos = -1.0 + (2.0 * yPos01),
          angle = 3.14159265359 * 2.0 * angle01;
    float horizontalness = sqrt(1.0 - (yPos * yPos));
    return float3(horizontalness * cos(angle),
				  yPos,
				  horizontalness * sin(angle));
}
float3 randomPosOnUnitSphere()
{
	float2 r2 = rand2();
    return posOnUnitSphere(r2.x, r2.y);
}
float3 randomPosInUnitSphere()
{
	//Note that a uniform random radius actually yields a non-uniform distribution.
	//To get a uniform distribution, we must take the cube root of a uniform value.
	float3 r3 = rand3();
	return posOnUnitSphere(r3.x, r3.y) * pow(r3.z, 1.0/3.0);
}