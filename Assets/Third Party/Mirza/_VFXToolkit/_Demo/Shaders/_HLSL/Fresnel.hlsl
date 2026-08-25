
#ifndef VFXTOOLKIT_DEMO_FRESNEL_HLSL
#define VFXTOOLKIT_DEMO_FRESNEL_HLSL

// Fresnel.

float Fresnel(float3 viewDirection, float3 normal)
{
    float VdotN = dot(viewDirection, normal);
    float fresnel = saturate(VdotN);
    
    return fresnel;
}
float Fresnel(float3 viewDirection, float3 normal, float power)
{
    return pow(Fresnel(viewDirection, normal), power);
}

float InvertedFresnel(float3 viewDirection, float3 normal)
{
    return 1.0 - Fresnel(viewDirection, normal);
}
float InvertedFresnel(float3 viewDirection, float3 normal, float power)
{
    return pow(InvertedFresnel(viewDirection, normal), power);
}

#endif