
// ...

// Need #define Before Lighting.hlsl, Shadows.hlsl:

//#define _SURFACE_TYPE_TRANSPARENT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

// https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-shadows.html
// https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-additional-lights-fplus.html

//#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

// Light cookie support.

#pragma multi_compile_fragment _ _LIGHT_COOKIES

// Assign these via script.

// Can't use name _AdditionalLightsCount (with plural 'Lights')
// because it would be a redefinition of an existing variable that
// I can't use because it appears to be used by Unity...

uint _AdditionalLightCount;
float4 _AmbientLighting;

// ...

#pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

float3 SampleAPV(float3 positionWS, float3 viewDirectionWS, float2 uvPP)
{
    float3 bakedGI = 0.0;

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    
    // For fog, there is no surface normal.
    // Use consistent vector: -viewDirWS for camera-facing fog.
    
    float3 normalWS = -viewDirectionWS;
    uint renderingLayer = 0xFFFFFFFF;

    EvaluateAdaptiveProbeVolume(positionWS, normalWS, viewDirectionWS, uvPP, renderingLayer, bakedGI);
    
#endif

    return bakedGI;
}

// ...

// Henyey–Greenstein anisotropic phase function.
// Would optimization via pre-compute really matter?

// Compiler may take care of it...
// Else, I'll need to pass in pre-computed values for anisotropySqr, etc.

//float HenyeyGreensteinPhase(float VdotL, float anisotropy)
//{
//    float g = anisotropy;
//    float gSqr = g * g;

//    float denom = 1.0 + gSqr - ((2.0 * g) * VdotL);

//    return (1.0 - gSqr) / (4.0 * PI * denom * sqrt(denom));
//}
float HenyeyGreensteinPhase(float VdotL, float anisotropy)
{
    float g = anisotropy;
    //float g = clamp(anisotropy, -0.9999, 0.9999);
    
    float gSqr = g * g;
    float denom = 1.0 + gSqr - 2.0 * g * VdotL;
    
    return (1.0 - gSqr) / (denom * sqrt(denom)); // g = 0.0 → 1.0, energy-neutral.
}

// ...

float GetVolumetricFogDensity(float3 positionWS)
{
    // TO-DO: noise, detail, height, etc.
    
    return 1.0;
}

// Height fog.

float GetVolumetricFogDensity(float3 positionWS, float heightDistance, float heightOffset, float heightFalloff, float heightRemapMin, float heightRemapMax)
{    
    // Height of this sample above the fog base.

    float heightAboveBase = (positionWS.y - heightOffset) - heightDistance;
    
    // Remap height factor before exp shaping.

    float heightFactor = smoothstep(heightRemapMin, heightRemapMax, heightAboveBase);
    
    // Exponential falloff: full density at base, thinning with height.

    float heightDensity = exp(-heightFactor * heightFalloff);
    
    //float f = exp(-heightFalloff);
    //float heightDensity = saturate((exp(-heightFactor * heightFalloff) - f) / (1.0 - f));
    
    return heightDensity;
}

struct SampleData
{    
    float scale;
    
    float2 tiling;    
    float2 offset;
    
    float2 animation;
    
    float4 SampleTexture(Texture2D tex, float2 uv)
    {
        uv *= scale;
        uv *= tiling;
        
        uv -= offset;
        
        uv -= animation * _Time.y;
        
        // Using the LOD version is necessary for large-area fog.
        
        //return SAMPLE_TEXTURE2D(tex, sampler_LinearRepeat, uv);
        
        return SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearRepeat, uv, 0);
        //return SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearRepeat, frac(uv), 0);
    }
};

struct FogDensityData
{
    float density;

    bool enableHeightMask;

    float heightMaskBlend;
    
    float heightMaskLength;
    float heightMaskOffset;
    
    float heightMaskFalloff;
    
    float heightMaskRemapMin;
    float heightMaskRemapMax;

    bool enableHeightMaskTexture;
    
    float heightMaskTextureAmplitude;    
    float heightMaskTextureScale;
    
    float2 heightMaskTextureAnimation;
    
    float heightMaskTexturePower;

    // Density at position, before step-length multiply.

    float Evaluate(float3 positionWS, Texture2D heightMaskTexture)
    {
        float result = GetVolumetricFogDensity(positionWS);

        if (enableHeightMask)
        {
            float heightMaskOffsetComposite = heightMaskOffset;

            if (enableHeightMaskTexture)
            {
                SampleData heightMaskTextureSampleData;

                heightMaskTextureSampleData.scale = heightMaskTextureScale;

                heightMaskTextureSampleData.tiling = _Height_Mask_Texture_ST.xy;
                heightMaskTextureSampleData.offset = _Height_Mask_Texture_ST.zw;

                heightMaskTextureSampleData.animation = heightMaskTextureAnimation;

                float heightMaskTextureSample = heightMaskTextureSampleData.SampleTexture(heightMaskTexture, positionWS.xz).r;

                heightMaskTextureSample = pow(heightMaskTextureSample, heightMaskTexturePower);

                // Remap from [0.0, 1.0] to [-1.0, 1.0], and scale.

                heightMaskTextureSample = (heightMaskTextureSample * 2.0) - 1.0;
                heightMaskTextureSample *= heightMaskTextureAmplitude;
                                
                heightMaskOffsetComposite += heightMaskTextureSample;
            }

            float result_heightMask = GetVolumetricFogDensity(positionWS, heightMaskLength, heightMaskOffsetComposite, heightMaskFalloff, heightMaskRemapMin, heightMaskRemapMax);
            
            result = lerp(result, result_heightMask, heightMaskBlend);
        }

        return result;
    }
};

// Main light self-shadow.

// Marches from sample toward directional light, accumulating fog depth.
// Returns path transmittance: 1.0 = clear, approaching 0.0 = occluded by fog.

// Noise doesn't look good here. It just adds MORE banding.
// So- no noise. Which is fine; the main loop already dithers.

float GetLightSelfShadow(

    float3 positionWS,
    float3 lightDirectionWS,

    int steps,
    float dist, // Lol. Isn't 'distance' reserved? Rename this later, maybe.

    FogDensityData densityData,
    Texture2D heightMaskTexture)
{
    float stepLength = dist / steps;
    float opticalDepth = 0.0;

    [loop]
    for (int i = 0; i < steps; ++i)
    {
        float shadowSampleDistance = (i + 0.5) * stepLength;
        
        float3 shadowSamplePositionWS = positionWS + (lightDirectionWS * shadowSampleDistance);
        float shadowSampleDensity = densityData.Evaluate(shadowSamplePositionWS, heightMaskTexture);

        opticalDepth += (shadowSampleDensity * densityData.density) * stepLength;
    }

    return exp(-opticalDepth);
}

// Volumetric fog.
// WS = world space.

void VolumetricFog_float(

    // ...

    float3 positionWS, float3 normalWS, float2 uvSS,
    float4 colour, bool enableShadowColours, float4 shadowColour, float4 selfShadowColour,

    int steps, float density, float maxDistance,

    // ...

    bool enableAnisotropy,

    float anisotropy, 
    float anisotropyBlend,

    // ...

    bool enableBlurTexture,

    Texture2D blurTexture,

    float blurTextureBlend,

    float blurTextureRemapMin,
    float blurTextureRemapMax,

    // ...

    bool enableHeightMask,

    float heightMaskBlend,

    float heightMaskLength,
    float heightMaskOffset,

    float heightMaskFalloff,

    float heightMaskRemapMin,
    float heightMaskRemapMax,

    // ...

    bool enableHeightMaskTexture,

    Texture2D heightMaskTexture,

    float heightMaskTextureAmplitude,
    float heightMaskTextureScale,

    float2 heightMaskTextureAnimation,

    float heightMaskTexturePower,

    // ...

    bool enableHeightGradient,

    float heightGradientBlend,

    float4 heightGradientColourTop,
    float4 heightGradientColourBottom,

    float heightGradientLength,
    float heightGradientOffset,

    float heightGradientFalloff,

    float heightGradientRemapMin,
    float heightGradientRemapMax,

    // ...

    bool enableHeightGradientTexture,

    Texture2D heightGradientTexture,

    float heightGradientTextureAmplitude,
    float heightGradientTextureScale,

    float2 heightGradientTextureAnimation,

    float heightGradientTexturePower, 

    // ...

    bool enableHeightGradientLUT,

    Texture2D heightGradientLUT,
    float heightGradientLUTBlend,

    // ...

    float ambientLightScale,

    // ...    

    bool enableAdaptiveProbeVolumes,

    float adaptiveProbeVolumeScale,
    float adaptiveProbeVolumePower,

    // ...

    bool enableMainLightSelfShadow,
    
    int mainLightSelfShadowSteps,
    float mainLightSelfShadowDistance,

    // ...

    bool enableAdditionalLightSelfShadow,

    int additionalLightSelfShadowSteps,
    float additionalLightSelfShadowDistance,

    // ...

    bool enableSelfShadowCurves,

    float selfShadowPower,

    float selfShadowRemapMin,
    float selfShadowRemapMax,

    // Final fog composite/mix with the scene.
    // If I want the final fog, use this.

    out float4 composite, 
    
    // Output lighting and transmittance separately.
    // If I want to composite them separate, use these.

    out float3 lighting, out float transmittance)
{    
    // -- SETUP.
    
    float3 cameraPositionWS = GetCameraPositionWS();
    //float3 offsetToSurfaceWS = positionWS - cameraPositionWS;
    
    // Edit: need to calculate offset to surface manually.
    // > prevent issues from precision loss away from origin.
    
    float rawDepth = SampleSceneDepth(uvSS);
    
    // Unproject to view space manually.
    
    // ComputeViewSpacePosition() negates z (SRP core convention),
    // which mirrors ray through camera plane before I_V rotation.
    
    // -- do *not* use it here.
    
    // I_VP = I_V * I_P -> this is Shader Graph's old reconstruction,
    // split so camera's world translation never enters the equation.
    
    float4 positionCS = ComputeClipSpacePosition(uvSS, rawDepth);
    float4 positionVS = mul(UNITY_MATRIX_I_P, positionCS);
    
    // Perspective divide. 
    // > Orthographic, w == 1.0.
    
    positionVS.xyz = positionVS.xyz / positionVS.w;
    
    //float aspectRatio = _ScreenParams.x / _ScreenParams.y;
    
/*  // Distance and direction for perspective-only calculations.
    
    float3 offsetToSurfaceWS = mul((float3x3) UNITY_MATRIX_I_V, positionVS);
    
    // Distance between camera and surface (vertex or fragment).
    
    float distanceToSurfaceWS = length(offsetToSurfaceWS);
    
    // Direction from camera to surface == -(view direction).
    
    float3 directionToSurfaceWS = offsetToSurfaceWS / distanceToSurfaceWS;
    
*/
    
    // Setup support for both perspective and orthographic cameras/views (rendering).
    
    // Build ray in view space, where perspective/orthographic are cleanly differentiated.
    
    // Perspective: rays fan out into a frustum from camera origin (XYZ == 0.0).    
    // Orthographic: rays shoot directly parallel to each other, down [view] -Z.
    
    // In orthographic, each ray originates on its own point on the camera plane.
    
    float3 rayOriginVS;
    float3 directionToSurfaceVS;
    
    float distanceToSurfaceWS;
    
    if (unity_OrthoParams.w == 1.0)
    {
        rayOriginVS = float3(positionVS.xy, 0.0);
        directionToSurfaceVS = float3(0.0, 0.0, -1.0);
        
        distanceToSurfaceWS = abs(positionVS.z);
    }
    else
    {
        rayOriginVS = float3(0.0, 0.0, 0.0);
        directionToSurfaceVS = normalize(positionVS.xyz);
        
        distanceToSurfaceWS = length(positionVS.xyz);
    }
    
    //float aspectRatio = _ScreenParams.x / _ScreenParams.y;
    
    // Rotate into world orientation. (3x3 only, no translation -- preserves precision).
    // Origin offset is camera-relative, added to camera position as raymarch origin.
    
    float3 rayOriginWS = cameraPositionWS + mul((float3x3) UNITY_MATRIX_I_V, rayOriginVS);
    float3 directionToSurfaceWS = mul((float3x3) UNITY_MATRIX_I_V, directionToSurfaceVS);
    
    // Screen pixel coordinates.
    
    // '_ScaledScreenParams' better for scaled resolution consistency vs. '_ScreenParams'.
    // Example: like the renderer settings/asset scale slider.
    
    float2 uvPP = uvSS * _ScaledScreenParams.xy;
    
    // Noise.
     
    int frame = (_Time.y * 60.0) % 60;    
    float interleavedGradientNoise = InterleavedGradientNoise(uvPP, frame);
    
    // Distance.
    
    // Limiting this allows for higher resolution.
    // Same number of steps, covering a smaller distance.
    
    float raymarchDistanceWS = distanceToSurfaceWS;    
    raymarchDistanceWS = min(raymarchDistanceWS, maxDistance);
    
    // Lighting.
    
    lighting = 0.0;
    
    InputData inputData = (InputData) 0;
    
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = -directionToSurfaceWS;
    
    inputData.normalizedScreenSpaceUV = uvSS;
    
    // Get main light *now* -- it will not change during raymarch.
    
#ifdef _ENABLE_MAIN_LIGHT
    
    Light mainLight = GetMainLight();
    
    // Main colour doesn't change in raymarch loop.
    // Precision has practically no purpose being less than 0.001.
    
    // Note: consider making '0.001' a named constant like 'lightEpsilon',
    // and using it for all light (or otherwise?)... precision stuff.
        
    // I *think* the two max() calls are actually faster or less instructions than the tri-circuit '||' (or-logic) checks.
        
    //bool mainLightContributes = 
        
    //    mainLighting.r > 0.001 || 
    //    mainLighting.g > 0.001 || 
    //    mainLighting.b > 0.001;
    
    bool mainLightContributes = max(mainLight.color.r, max(mainLight.color.g, mainLight.color.b)) > 0.001;
    
#endif
    
    // -- RAYMARCH.
    
    float rayStepWS = raymarchDistanceWS / steps;
    float rayStepNoiseWS = rayStepWS * interleavedGradientNoise;
    
    float densityPerStepWS = density * rayStepWS;
        
    // Transmittance: How much light can pass through to the camera.
    // Starts at 1.0 (fully clear) and decays towards 0.0 (fully blocked).
    
    transmittance = 1.0;
    
    // Pack density settings once, for both ray- and self-shadow marching.
    
    FogDensityData densityData;

    densityData.density = density;

    densityData.enableHeightMask = enableHeightMask;

    densityData.heightMaskBlend = heightMaskBlend;
    
    densityData.heightMaskLength = heightMaskLength;    
    densityData.heightMaskOffset = heightMaskOffset;
    
    densityData.heightMaskFalloff = heightMaskFalloff;
    
    densityData.heightMaskRemapMin = heightMaskRemapMin;
    densityData.heightMaskRemapMax = heightMaskRemapMax;

    densityData.enableHeightMaskTexture = enableHeightMaskTexture;
    
    densityData.heightMaskTextureAmplitude = heightMaskTextureAmplitude;
    densityData.heightMaskTextureScale = heightMaskTextureScale;
    densityData.heightMaskTextureAnimation = heightMaskTextureAnimation;
    densityData.heightMaskTexturePower = heightMaskTexturePower;
    
    // Loop.
        
    [loop]
    for (int i = 0; i < steps; ++i)
    {
        // Calculate distance along ray for this step/iteration.
        
        float stepRayDistanceWS = i * rayStepWS;
                
        // Add up to one full step/iteration (noise is [0.0, 1.0] of IGNoise-based offset.
        
        stepRayDistanceWS += rayStepNoiseWS;
        
        // Depth test.
                
        if (stepRayDistanceWS > raymarchDistanceWS)
        {
            break;
        }
        
        // Calculate current position along ray in world space.
        
        // Note: replaced 'cameraPositionWS' with 'rayOriginWS'.
        // -- to support both perspective and orthographic rendering.
        
        float3 stepPositionWS = rayOriginWS + (directionToSurfaceWS * stepRayDistanceWS);
    
        // -- LIGHTING.
        // Setup lighting data for this position.
                    
        inputData.positionWS = stepPositionWS;
        
        // Sample density.
        // Can vary based on noise/height/etc.
        
        float stepDensity = densityData.Evaluate(stepPositionWS, heightMaskTexture);
                
        stepDensity *= densityPerStepWS;
                
        // Calculate transmittance for this specific step (Beer-Lambert Law).
        
        float stepTransmittance = exp(-stepDensity);
        
        // Calculate scattering probability.        
        // > Amount of light captured and scattered, this step.
        
        // Physically cannot exceed 1.0, unlike raw density.
        
        float stepScattering = 1.0 - stepTransmittance;
        
        // Combined lighting for this step...
        
        float3 stepLighting = 0.0;
        
//#define _ENABLE_MAIN_LIGHT
//#define _ENABLE_MAIN_LIGHT_SHADOWS
        
        // 1. Main light.
        
#ifdef _ENABLE_MAIN_LIGHT        
        
        if (mainLightContributes)
        {   
            // Default to fully lit (no geometry shadows), until calculated/derived otherwise.
        
            mainLight.shadowAttenuation = 1.0;
            float mainLightSelfShadowAttenuation = 1.0; 
                
            #ifdef _ENABLE_MAIN_LIGHT_SHADOWS
        
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);     
        
                // MainLightShadow() > MainLightRealtimeShadow().
                // > it includes the fade to remove hard clipping.
        
                //mainLight.shadowAttenuation = MainLightRealtimeShadow(inputData.shadowCoord);
        
                //// Fade necessary to remove hard clipping and related artifacts at shadow distance limit.
        
                //half fade = GetMainLightShadowFade(inputData.positionWS);
                //mainLight.shadowAttenuation = lerp(mainLight.shadowAttenuation, 1.0, fade);
                
                mainLight.shadowAttenuation = MainLightShadow(inputData.shadowCoord, inputData.positionWS, half4(1.0, 1.0, 1.0, 1.0), _MainLightOcclusionProbes);
                
                // Blend between fully lit (1.0) and occluded by geometry, 
                // based on shadow colour alpha (0.0 if shadows disabled, else > 0.0).
        
                if (enableShadowColours)
                {
                    mainLight.shadowAttenuation = lerp(1.0, mainLight.shadowAttenuation, shadowColour.a);
                }
        
            #endif
        
            // Light cookie: per-position projection mask.
            // Cookies vary with world position, so sample per step.
        
            float3 mainLightCookie = 1.0;
            float mainLightCookieMax = 1.0;
        
            // I thought about adding a dedicated bool to additionally gate 
            // lighting cookie functions and sampling, but I figure the cost
            // is relatively negligible to having another bool to track, and 
            // I can't think of a strong/coherent use-case for not also having
            // light cookies apply to the volumetric fog lighting.
        
            #if defined(_LIGHT_COOKIES)
        
                mainLightCookie = SampleMainLightCookie(inputData.positionWS);
                mainLightCookieMax = max(mainLightCookie.r, max(mainLightCookie.g, mainLightCookie.b));
        
            #endif
        
            // Self-shadow: attenuate by the fog between this sample and the light.
        
            // No contribution from light (absolutely blackened by light's shadow)? 
            // If geometry fully occluding light, there's nothing for fog to attenuate as shadow.
        
            // > Skip self-shadowing.
                      
            //bool mainLightReachesHere = mainLight.shadowAttenuation > 0.001;
            bool mainLightReachesHere = (mainLight.shadowAttenuation > 0.001) && (mainLightCookieMax > 0.001);
        
            if (enableMainLightSelfShadow && mainLightReachesHere)
            {
                mainLightSelfShadowAttenuation = GetLightSelfShadow(
            
                    inputData.positionWS,
                    mainLight.direction,

                    mainLightSelfShadowSteps,
                    mainLightSelfShadowDistance,
        
                    densityData,
                    heightMaskTexture
            
                );
        
                // Blend between fully lit (1.0) and self-shadowed by fog, based on current geometry occlusion.
                // If not occluded by geometry, shadowAttenuation == 1.0; Anything less is partial occlusion/shading.
        
                // If light is fully occluded by geometry, then there is no self-shadowing.
                // Self-shadowing requires lighting to reach where there will be a shadow.
        
                // Alpha only applies when shadow colours are on.
        
                // Do *not* pre-multiply into mainLight.shadowAttenuation,
                // as that is for geometry shadows, not self-shadows.
        
                float mainLightSelfShadowStrength = enableShadowColours ? selfShadowColour.a : 1.0;
        
                //mainLightSelfShadowAttenuation = lerp(1.0, mainLightSelfShadowAttenuation, mainLight.shadowAttenuation * mainLightSelfShadowStrength);
                mainLightSelfShadowAttenuation = lerp(1.0, mainLightSelfShadowAttenuation, (mainLight.shadowAttenuation * mainLightCookieMax) * mainLightSelfShadowStrength);
                
                if (enableSelfShadowCurves)
                {
                    mainLightSelfShadowAttenuation = smoothstep(selfShadowRemapMin, selfShadowRemapMax, mainLightSelfShadowAttenuation);
                    mainLightSelfShadowAttenuation = pow(mainLightSelfShadowAttenuation, selfShadowPower);
                }
            }
        
            // Shadow colouring. Blend shadowed regions toward custom colour.
            // RGB tints, alpha blends. If alpha == 0.0, no shadows...
            
            float3 mainLighting = mainLight.color;
            mainLighting *= mainLightCookie;
        
            // Anisotropy applies to direct light only, *before* shadow colouring.
        
            if (enableAnisotropy)
            {
                float mainLight_VdotL = dot(directionToSurfaceWS, mainLight.direction);
                float mainLight_phase = HenyeyGreensteinPhase(mainLight_VdotL, anisotropy);
        
                mainLight_phase = lerp(1.0, mainLight_phase, anisotropyBlend);
        
                mainLighting *= mainLight_phase;
            }
        
            if (!enableShadowColours)
            {
                mainLighting *= mainLight.shadowAttenuation;
                mainLighting *= mainLightSelfShadowAttenuation;
            }
            else
            {
                mainLighting = lerp(mainLighting, shadowColour.rgb, 1.0 - mainLight.shadowAttenuation);
                mainLighting = lerp(mainLighting, selfShadowColour.rgb, 1.0 - mainLightSelfShadowAttenuation);
            }
        
            // Add main light to this step's lighting.
        
            stepLighting += mainLighting;
        }
        
#endif
        
        // 2. Additional lights.
        
#ifdef _ENABLE_ADDITIONAL_LIGHTS   
        
        //int lightCount = GetAdditionalLightsCount(); // Doesn't seem to work for fullscreen effects.
                
        // _AdditionalLightsCount already exists as a global URP uniform (hence the redefinition error).
        // It is bound automatically every frame, and holds the post-cull additional light count in .x.
        
        [loop]
        LIGHT_LOOP_BEGIN(_AdditionalLightCount)
        {
            Light additionalLight = GetAdditionalPerObjectLight(lightIndex, inputData.positionWS); // This one works for post-processing.
            //Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);

            additionalLight.shadowAttenuation = 1.0;
            float additionalLightSelfShadowAttenuation = 1.0;
        
            #ifdef _ENABLE_ADDITIONAL_LIGHT_SHADOWS
        
                //additionalLight.shadowAttenuation = AdditionalLightRealtimeShadow(

                //    lightIndex,

                //    inputData.positionWS,
                //    additionalLight.direction,

                //    GetAdditionalLightShadowParams(lightIndex),
                //    GetAdditionalLightShadowSamplingData(lightIndex)
                //);
        
                //half fade = GetAdditionalLightShadowFade(inputData.positionWS);
                //additionalLight.shadowAttenuation = lerp(additionalLight.shadowAttenuation, 1.0, fade);
                        
                additionalLight.shadowAttenuation = AdditionalLightShadow(

                    lightIndex,

                    inputData.positionWS,
                    additionalLight.direction,

                    half4(1.0, 1.0, 1.0, 1.0),
                    _AdditionalLightsOcclusionProbes[lightIndex]
                );
        
                if (enableShadowColours)
                {
                    additionalLight.shadowAttenuation = lerp(1.0, additionalLight.shadowAttenuation, shadowColour.a);
                }
        
            #endif
                
            float3 additionalLightCookie = 1.0;
            float additionalLightCookieMax = 1.0;
        
            #if defined(_LIGHT_COOKIES)
        
                additionalLightCookie = SampleAdditionalLightCookie(lightIndex, inputData.positionWS);
                additionalLightCookieMax = max(additionalLightCookie.r, max(additionalLightCookie.g, additionalLightCookie.b));
        
            #endif
        
            // Self-shadow: attenuate by fog between *this* [sample] and light.
            // Check if light is dead here -- out of range/cone, or fully shadowed.
        
            //bool additionalLightReachesHere = 
        
            //    additionalLight.shadowAttenuation > 0.001 &&
            //    additionalLight.distanceAttenuation > 0.00001; // Needs to be more precise, hence additional decimal place(s).
        
            bool additionalLightReachesHere = 
        
                (additionalLight.shadowAttenuation > 0.001) &&
                (additionalLight.distanceAttenuation > 0.00001) && // Needs to be more precise, hence additional decimal place(s).
                (additionalLightCookieMax > 0.001);
        
            // ^ Without extra precision for distanceAttenuation, 
            // there's noticeable clipping of self-shading in some cases.

            // Reaches here bool saves a *lot* of FPS (tested and confirmed), 
            // > no expensive self-shadow calculations for non-contribution.
        
            if (enableAdditionalLightSelfShadow && additionalLightReachesHere)
            {
                // Point/spot lights are at finite position, so clamp march to
                // distance to light, else: it accumulates fog behind a near one.
        
                // -- (w = 1.0 for point/spot, 0.0 for a directional additional light.)

                float4 lightPositionWS = _AdditionalLightsPosition[lightIndex];
                float distanceToLight = length(lightPositionWS.xyz - inputData.positionWS);

                //float selfShadowDistance = min(additionalLightSelfShadowDistance, distanceToLight);
        
                // Handle distance clamping for additional directional lights.
        
                float clampDistance = lerp(additionalLightSelfShadowDistance, distanceToLight, lightPositionWS.w);
                float selfShadowDistance = min(additionalLightSelfShadowDistance, clampDistance);
        
                additionalLightSelfShadowAttenuation = GetLightSelfShadow(

                    inputData.positionWS,
                    additionalLight.direction,

                    additionalLightSelfShadowSteps,
                    selfShadowDistance,

                    densityData,
                    heightMaskTexture

                );
                
                float additionalLightSelfShadowStrength = enableShadowColours ? selfShadowColour.a : 1.0;
        
                //additionalLightSelfShadowAttenuation = lerp(1.0, additionalLightSelfShadowAttenuation, additionalLight.shadowAttenuation * additionalLightSelfShadowStrength);
                additionalLightSelfShadowAttenuation = lerp(1.0, additionalLightSelfShadowAttenuation, (additionalLight.shadowAttenuation * additionalLightCookieMax) * additionalLightSelfShadowStrength);
                
                if (enableSelfShadowCurves)
                {
                    additionalLightSelfShadowAttenuation = smoothstep(selfShadowRemapMin, selfShadowRemapMax, additionalLightSelfShadowAttenuation);
                    additionalLightSelfShadowAttenuation = pow(additionalLightSelfShadowAttenuation, selfShadowPower);
                }
            }
        
            float3 additionalLighting = additionalLight.color;
            additionalLighting *= additionalLightCookie;
        
            // Anisotropy applies to direct light only, *before* shadow colouring.
            
            if (enableAnisotropy)
            {
                float additionalLight_VdotL = dot(directionToSurfaceWS, additionalLight.direction);
                float additionalLight_phase = HenyeyGreensteinPhase(additionalLight_VdotL, anisotropy);
        
                additionalLight_phase = lerp(1.0, additionalLight_phase, anisotropyBlend);
        
                additionalLighting *= additionalLight_phase;        
            }
        
            if (!enableShadowColours)
            {
                additionalLighting *= additionalLight.shadowAttenuation;
                additionalLighting *= additionalLightSelfShadowAttenuation;
            }
            else
            {
                additionalLighting = lerp(additionalLighting, shadowColour.rgb, 1.0 - additionalLight.shadowAttenuation);
                additionalLighting = lerp(additionalLighting, selfShadowColour.rgb, 1.0 - additionalLightSelfShadowAttenuation);
            }
        
            // This goes *after* custom shadow colours.
        
            additionalLighting *= additionalLight.distanceAttenuation;
            
            // Add additional light to this step's lighting.
            
            stepLighting += additionalLighting;
        }
        LIGHT_LOOP_END
        
#endif
        
        // 3. Ambient light.
        
#ifdef _ENABLE_AMBIENT_LIGHT
                
        stepLighting += _AmbientLighting.rgb * ambientLightScale;        
        
#endif
        
        // 4. Adaptive probe volumes.
                
        if (enableAdaptiveProbeVolumes)
        {
            // Note: changed uvSS to uvPP -> removes sampling seams in some places (like the green cube on the right).
            // uvPP seems to be the more consistent/correct choice for sampling probe volumes in world space...
            
            float3 apvSample = SampleAPV(stepPositionWS, -directionToSurfaceWS, uvPP);
            
            // SH ringing can go negative. Negative irradiance is non-physical, and NaNs pow.
            
            // Don't allow negative values -> fixes issues with flickering bright spots/flashes.
            // -- (without needing additional anti-aliasing on the camera).
            
            apvSample = max(apvSample, 0.0);
            
            apvSample = pow(apvSample, adaptiveProbeVolumePower);
            stepLighting += apvSample * adaptiveProbeVolumeScale;
        } 
        
        // 5. Height gradient tint.
        
        if (enableHeightGradient)
        {
            float heightGradientOffsetComposite = heightGradientOffset;

            if (enableHeightGradientTexture)
            {
                SampleData heightGradientTextureSampleData;

                heightGradientTextureSampleData.scale = heightGradientTextureScale;

                heightGradientTextureSampleData.tiling = _Height_Gradient_Texture_ST.xy;
                heightGradientTextureSampleData.offset = _Height_Gradient_Texture_ST.zw;

                heightGradientTextureSampleData.animation = heightGradientTextureAnimation;

                float heightGradientTextureSample = heightGradientTextureSampleData.SampleTexture(heightGradientTexture, stepPositionWS.xz).r;

                heightGradientTextureSample = pow(heightGradientTextureSample, heightGradientTexturePower);

                // Remap from [0.0, 1.0] to [-1.0, 1.0], and scale.

                heightGradientTextureSample = (heightGradientTextureSample * 2.0) - 1.0;
                heightGradientTextureSample *= heightGradientTextureAmplitude;

                heightGradientOffsetComposite += heightGradientTextureSample;
            }

            // Normalized position of sample within gradient range.
            
            float heightGradientFactor = (stepPositionWS.y - heightGradientOffsetComposite) / heightGradientLength;
            
            // Window, then shape.
                    
            heightGradientFactor = smoothstep(heightGradientRemapMin, heightGradientRemapMax, heightGradientFactor);
            heightGradientFactor = 1.0 - exp(-heightGradientFactor * heightGradientFalloff);
            
            // Bottom-to-top colour by height.
            
            float3 heightGradientColourTopTint = lerp(1.0, heightGradientColourTop.rgb, heightGradientColourTop.a);
            float3 heightGradientColourBottomTint = lerp(1.0, heightGradientColourBottom.rgb, heightGradientColourBottom.a);
            
            float3 heightGradientColour = lerp(heightGradientColourBottomTint, heightGradientColourTopTint, heightGradientFactor);
            
            // Blend tint.
            
            heightGradientColour = lerp(1.0, heightGradientColour, heightGradientBlend);
            
            // Optional LUT for complex/artistic colour grading by height.
            
            if (enableHeightGradientLUT)
            {
                //float4 heightGradientLUTSample = SAMPLE_TEXTURE2D(heightGradientLUT, sampler_LinearClamp, float2(heightGradientFactor, 0.0));
                float4 heightGradientLUTSample = SAMPLE_TEXTURE2D_LOD(heightGradientLUT, sampler_LinearClamp, float2(heightGradientFactor, 0.0), 0);
                
                heightGradientColour *= lerp(1.0, heightGradientLUTSample.rgb, heightGradientLUTSample.a * heightGradientLUTBlend);
            }
            
            stepLighting *= heightGradientColour;
        }
                        
        // -- INTEGRATION.
        
        // Apply scattering.
        
        stepLighting *= stepScattering;
        
        // Accumulate light into final' buffer'.
        // Crucial: multiply by transmittance. 
        
        // Light deeper in the fog obscured by fog already stepped through.
        
        lighting += stepLighting * transmittance;
        
        // -- ABSORPTION.
        
        // Update transmittance for the NEXT step/iteration/cycle.
        // Beer-Lambert law: exp(-density).
        
        transmittance *= stepTransmittance;
        
        // Early exit if transmittance is nearly zero (opaque).
        
        if (transmittance < 0.001)
        {
            transmittance = 0.0f; break;
        }
    }
    
    // Tint accumulated light.
    
    lighting *= colour.rgb;
    
    // -- COMPOSITE (FINAL COLOUR).
    
    // Blend to blur based on scattering, attenuated by fog alpha.
    
    float4 sceneColour = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvSS);
    
    if (enableBlurTexture)
    {    
        // Blur.
    
        // [0.0, 1.0] depth = distance to surface (from camera, in units [meters]) / cameraFarPlane;
        
        //float linearDepth = distanceToSurfaceWS / _ProjectionParams.z;
        //float scattering = smoothstep(remapMin, remapMax, linearDepth);

        // UPDATE: Blur by fog 'density' (inverse transmittance).

        float blur = smoothstep(blurTextureRemapMin, blurTextureRemapMax, 1.0 - transmittance);
    
        blur *= blurTextureBlend;
        
        float4 sceneColour_blur = SAMPLE_TEXTURE2D(blurTexture, sampler_LinearClamp, uvSS);
        sceneColour = lerp(sceneColour, sceneColour_blur, blur * colour.a);
    }
    
    // Scene colour multiplied by remaining transmittance (whatever wasn't blocked).
    // Fog light is then added on top.
    
    composite.rgb = (sceneColour.rgb * transmittance) + lighting;
    composite.rgb = lerp(sceneColour.rgb, composite.rgb, colour.a);

    composite.a = sceneColour.a;
}