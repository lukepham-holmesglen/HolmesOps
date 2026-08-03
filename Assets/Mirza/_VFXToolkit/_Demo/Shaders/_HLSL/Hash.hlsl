
#ifndef VFXTOOLKIT_DEMO_HASH_HLSL
#define VFXTOOLKIT_DEMO_HASH_HLSL

// --------------------------------------------------

// Hash function(s), by David Hoskins:
// > https://www.shadertoy.com/view/4djSRW

// 1 in, 1 out.

float Random1DFrom1D(float p)
{
    p = frac(p * 0.1031);
    
    p *= p + 33.33;
    p *= p + p;
    
    return frac(p);
}

// 1 in, 2 out.

float2 Random2DFrom1D(float p)
{
    float3 p3 = frac(p * float3(0.1031, 0.1030, 0.0973));
    
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);

}

// 2 in, 1 out.

float Random1DFrom2D(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

//  2 in, 2 out.

float2 Random2DFrom2D(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);

}

// 3 in, 2 out.

float2 Random2DFrom3D(float3 p3)
{
    p3 = frac(p3 * float3(0.1031, 0.1030, 0.0973));
    
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

// --------------------------------------------------

#endif