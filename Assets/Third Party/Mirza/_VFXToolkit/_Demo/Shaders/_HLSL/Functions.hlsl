
#ifndef VFXTOOLKIT_DEMO_FUNCTIONS_HLSL
#define VFXTOOLKIT_DEMO_FUNCTIONS_HLSL

#include "Hash.hlsl"

// Shared and common functions for VFX Tookit demos.
// > for free assets, to avoid including the entire toolkit.

#define TAU PI * 2.0

// Angle in radians.

float2 PointOnUnitCircle(float angle)
{
    return float2(cos(angle), sin(angle));
}

// Automatic (correct) aspect ratio from texture.
// Mirza: https://www.shadertoy.com/view/wXyfD1.

void GetTextureAspectRatio_float(Texture2D tex, out float output1D, out float2 output2D)
{
    // Get texture dimensions.
    
    uint width;
    uint height;
    
    tex.GetDimensions(width, height);
    
    float height_f = height;
    
    output1D = 1.0;
    output2D = 1.0;
    
    if (width < height)
    {
        output1D = width / height_f;
        output2D.y = output1D;
    }
    else if (height < width)
    {
        output1D = height_f / width;
        output2D.x = output1D;
    }
}

// --------------------------------------------------

// Function wrapper for interleaved gradient noise.
// UV is in 'pixel space', which is *= texture resolution.

float InterleavedGradientNoise(float2 uvPS)
{
    return InterleavedGradientNoise(uvPS, (_Time.y * 60.0) % 60);
}

// -- for Shader Graph.

void InterleavedGradientNoise_float(float2 uvPS, out float output)
{
    output = InterleavedGradientNoise(uvPS);
}
void InterleavedGradientNoise_float(float2 uvPS, uint frame, out float output)
{
    output = InterleavedGradientNoise(uvPS, frame);
}

// Basic box blur for Unity. 

// If texelSize is correct, radius is in pixels, 
// otherwise it's in UV space (relative, scale).

// -- pixel [or texel] space vs. uv, normalized space.

// Relative is better, because it will automatically be correct across resolutions.

// For relative radius scaling, pass (1.0, 1.0) for texelSize (you can also set x or y to be aspect ratio-corrected).
// If you don't apply aspect ratio correction to texelSize, you can also apply it to the 2D radius before passing it in.

void BoxBlur_float(Texture2D tex, float2 uv, float2 texelSize, SamplerState samplerState, int quality, float2 radius, bool clip, bool radial, float noise, out float4 output)
{
    // Initialize.
    
    output = 0.0;
    
    // If no blur, return current pixel as-is.
        
    if (radius.x <= 0.0 && radius.y <= 0.0)
    {
        output = SAMPLE_TEXTURE2D(tex, samplerState, uv);
        return;
    }
    
	// Pre-calculations, setup.
    
    float totalWeight = 0.0;        
    radius *= texelSize / quality;
    
    // Noise.
    
    float2 textureResolution;
    
    tex.GetDimensions(textureResolution.x, textureResolution.y);
    
    float2 pixelCoordinates;    
    bool applyNoise = noise != 0.0;
    
    if (applyNoise)
    {
        pixelCoordinates = uv * textureResolution;
    }
    
    // Blur (average) the pixels around current pixel.
    // Force loop required for when radius == 0.0 (used as a bool check).
    
    [loop]
    for (int y = -quality; y <= quality; y++)
    {
        [loop]
        for (int x = -quality; x <= quality; x++)
        {            
            // Offset from current pixel.
            
            float2 pixelOffset = float2(x, y);
            
            if (applyNoise)
            {
                // Random value between [0.0, 1.0].
                // Time-variable, via 'interleavedGradientNoise'.
          
                float3 randomInput = float3(pixelCoordinates + pixelOffset, noise);
            
                // -0.5 to center.
            
                float2 randomPointInUnitSquare = Random2DFrom3D(randomInput) - 0.5;
                        
                // Add centered noise to pixel offset.
            
                pixelOffset += randomPointInUnitSquare;
            }
            
            // Ignore pixels outside radius from current pixel.
            
            // Could also just > x, rather than >= x...
            
            // >= cuts off outliers, which is *maybe* better.
            // >  provides a more circular clip.
            
            // It's a preference...
            
            float distanceFromCenter = length(pixelOffset);
            
            if (clip && distanceFromCenter > quality)
            {
                continue;
            }
            
            // Use either a constant of 1.0, or normalized radial distance weighting.
            
            float weight;
            
            if (radial)
            {                
                float normalizedDistance = distanceFromCenter / quality;
                float inverseNormalizedDistance = 1.0 - normalizedDistance;
                
                // Saturate, in case of NOT using clip. Otherwise: when using radial without clip, 
                // it will not be in range because it's a square sampling area.
                
                // That being said, there's no reason to not use clip when using radial.
                // Because the output will be identical, yet slower from more samples.
                
                if (!clip)
                {
                    inverseNormalizedDistance = saturate(inverseNormalizedDistance);
                }

                weight = inverseNormalizedDistance;
            }
            else
            {
                weight = 1.0;
            }
            
            // Convert from pixel coordinate space to normalized UV space.
            // 'texel' = "texture element" or "texture pixel".
            
            float2 uvOffset = pixelOffset * radius;
            
            // Sample and accumulate colour from current pixel, with weight.
            
            output += SAMPLE_TEXTURE2D(tex, samplerState, uv + uvOffset) * weight;
            
            // Box blur weight is always a constant 1.0.

            //totalWeight++;
            
            // Or, if you're using a custom weight (Gauss, Radial)...
            
            totalWeight += weight;
        }
    }

	// Normalize accumulated blur colour by iterations and return.

    output /= totalWeight;
}

// Convenience-- includes noise as a bool/toggle.

void BoxBlur_float(Texture2D tex, float2 uv, float2 texelSize, SamplerState samplerState, int quality, float2 radius, bool clip, bool radial, bool noise, out float4 output)
{
    if (noise)
    {
        float2 textureResolution;
        tex.GetDimensions(textureResolution.x, textureResolution.y);
        
        float2 uvPS = uv * textureResolution;
        
        float interleavedGradientNoise = InterleavedGradientNoise(uvPS);
        
        BoxBlur_float(tex, uv, texelSize, samplerState, quality, radius, clip, radial, interleavedGradientNoise, output);
    }
    else
    {
        BoxBlur_float(tex, uv, texelSize, samplerState, quality, radius, clip, radial, 0.0, output);
    }
}

void BoxBlur_DepthAware_float(Texture2D tex, Texture2D depthTex, Texture2D customDepthTex, float2 uv, float2 texelSize, SamplerState samplerState, int quality, float2 radius, bool clip, bool radial, float noise, out float4 output)
{
    // Initialize.
    
    output = 0.0;
    
    // If no blur, return current pixel as-is.
        
    if (radius.x <= 0.0 && radius.y <= 0.0)
    {
        output = SAMPLE_TEXTURE2D(tex, samplerState, uv);
        return;
    }
    
	// Pre-calculations, setup.
    
    float totalWeight = 0.0;
    radius *= texelSize / quality;
    
    // Noise.
    
    float2 textureResolution;
    
    tex.GetDimensions(textureResolution.x, textureResolution.y);
    
    float2 pixelCoordinates;
    bool applyNoise = noise != 0.0;
    
    if (applyNoise)
    {
        pixelCoordinates = uv * textureResolution;
    }
    
    float centerDepth = SAMPLE_DEPTH_TEXTURE(depthTex, samplerState, uv);
    
    // Blur (average) the pixels around current pixel.
    // Force loop required for when radius == 0.0 (used as a bool check).
    
    [loop]
    for (int y = -quality; y <= quality; y++)
    {
        [loop]
        for (int x = -quality; x <= quality; x++)
        {
            // Offset from current pixel.
            
            float2 pixelOffset = float2(x, y);
            
            if (applyNoise)
            {
                // Random value between [0.0, 1.0].
                // Time-variable, via 'interleavedGradientNoise'.
          
                float3 randomInput = float3(pixelCoordinates + pixelOffset, noise);
            
                // -0.5 to center.
            
                float2 randomPointInUnitSquare = Random2DFrom3D(randomInput) - 0.5;
                        
                // Add centered noise to pixel offset.
            
                pixelOffset += randomPointInUnitSquare;
            }
            
            // Ignore pixels outside radius from current pixel.
            
            // Could also just > x, rather than >= x...
            
            // >= cuts off outliers, which is *maybe* better.
            // >  provides a more circular clip.
            
            // It's a preference...
            
            float distanceFromCenter = length(pixelOffset);
            
            if (clip && distanceFromCenter > quality)
            {
                continue;
            }
            
            // Convert from pixel coordinate space to normalized UV space.
            // 'texel' = "texture element" or "texture pixel".
            
            float2 uvOffset = pixelOffset * radius;
            float2 sampleUV = uv + uvOffset;
            
            float depth = SAMPLE_DEPTH_TEXTURE(depthTex, samplerState, sampleUV);
            float customDepth = SAMPLE_DEPTH_TEXTURE(customDepthTex, samplerState, sampleUV);
            
            //float3 depthPositionWS = ComputeWorldSpacePosition(sampleUV, depth, UNITY_MATRIX_I_VP);            
            //float distanceFromCameraToDepthPositionWS = length(_WorldSpaceCameraPos - depthPositionWS);
            
            // If depth closer to camera.
            
            if (customDepth < depth) // lol, it worked.
            {
                output = SAMPLE_TEXTURE2D(tex, samplerState, uv);
                return;
            }
            
            // Use either a constant of 1.0, or normalized radial distance weighting.
            
            float weight;
            
            if (radial)
            {
                float normalizedDistance = distanceFromCenter / quality;
                float inverseNormalizedDistance = 1.0 - normalizedDistance;
                
                // Saturate, in case of NOT using clip. Otherwise: when using radial without clip, 
                // it will not be in range because it's a square sampling area.
                
                // That being said, there's no reason to not use clip when using radial.
                // Because the output will be identical, yet slower from more samples.
                
                if (!clip)
                {
                    inverseNormalizedDistance = saturate(inverseNormalizedDistance);
                }

                weight = inverseNormalizedDistance;
            }
            else
            {
                weight = 1.0;
            }
            
            // Sample and accumulate colour from current pixel, with weight.
            
            output += SAMPLE_TEXTURE2D(tex, samplerState, sampleUV) * weight;
            
            // Box blur weight is always a constant 1.0.

            //totalWeight++;
            
            // Or, if you're using a custom weight (Gauss, Radial)...
            
            totalWeight += weight;
        }
    }
    
	// Normalize accumulated blur colour by iterations and return.

    output /= totalWeight;
}

// Box blur with clipping and smoothing, +animated noise to hide lower sample iteration artifacts.
// Faster and uglier (more noisy) than the double-loop box blur. May want to use a blue noise texture later.

void NoiseBlur_float(Texture2D tex, float2 uv, float2 texelSize, SamplerState samplerState, int quality, float2 radius, bool clip, bool radial, out float4 output)
{
    // Initialize.
    
    output = 0.0;
    
    // If no blur, return current pixel as-is.
        
    if (radius.x <= 0.0 && radius.y <= 0.0)
    {
        output = SAMPLE_TEXTURE2D(tex, samplerState, uv);
        return;
    }
    
	// Pre-calculations, setup.    
    
    // Need non-zero for division later, 
    // as low sample counts may result in zero total weight.
    
    float totalWeight = 0.0001; 
    radius *= texelSize * 2.0;
            
    // Noise. 
    
    float2 textureResolution;
    
    tex.GetDimensions(textureResolution.x, textureResolution.y);
    
    float2 pixelCoordinates = uv * textureResolution;
    float interleavedGradientNoise = InterleavedGradientNoise(pixelCoordinates, (_Time.y * 60.0) % 60);
            
    // Accumulation loop, for blur.
    
    [loop]
    for (int i = 0; i < quality; i++)
    {
        // Randomized per fragment (pixel), and per-iteration.
        // Noise (blue, IGN...) fixes directionality bias, skew.
        
        float2 randomInput = (pixelCoordinates + i) + interleavedGradientNoise;
        
        // -0.5 to center.
        
        float2 randomPointInUnitSquare = Random2DFrom2D(randomInput) - 0.5;
        
        // Radial clipping.
        
        float distanceToCenter = length(randomPointInUnitSquare);
        
        if (clip && distanceToCenter > 0.5)
        {
            continue;
        }
        
        // Radial weighting, fading out towards the edges.
        
        float weight;
        
        if (radial)
        {            
            weight = (0.5 - distanceToCenter) * 2.0;
            
            // Saturate, in case of NOT using clip.
            
            if (!clip)
            {
                weight = saturate(weight);
            }
        }
        else
        {
            weight = 1.0;
        }
        
        float2 uvOffset = randomPointInUnitSquare * radius;
                
        // Accumulate, sum.
        
        output += SAMPLE_TEXTURE2D(tex, samplerState, uv + uvOffset) * weight;
        
        totalWeight += weight;
    }

    // Average, 'normalize'.
    
    output /= totalWeight;
}

// Box blur with clipping and smoothing, +animated noise to hide lower sample iteration artifacts.
// Faster and uglier (more noisy) than the double-loop box blur. May want to use a blue noise texture later.

void NoiseCircleBlur_float(Texture2D tex, float2 uv, float2 texelSize, SamplerState samplerState, int quality, float2 radius, out float4 output)
{
    // Initialize.
    
    output = 0.0;
    
    // If no blur, return current pixel as-is.
        
    if (radius.x <= 0.0 && radius.y <= 0.0)
    {
        output = SAMPLE_TEXTURE2D(tex, samplerState, uv);
        return;
    }
    
	// Pre-calculations, setup.    
        
    radius *= texelSize;    
    radius /= 2.0;
            
    // Noise. 
    
    float2 textureResolution;
    
    tex.GetDimensions(textureResolution.x, textureResolution.y);
    
    float2 pixelCoordinates = uv * textureResolution;
    
    float frame = (_Time.y * 60.0) % 60;
    float interleavedGradientNoise = InterleavedGradientNoise(pixelCoordinates, frame);
    
    float stepSize = 1.0 / float(quality);
        
    // Accumulation loop, for blur.
    
    for (int i = 0; i < quality; i++)
    {
        // Loop progress.
        
        float t = i * stepSize;
        
        // Noise value -> disk/circle offset.
        
        float angle = interleavedGradientNoise + t;                
        float2 randomPointOnUnitCircle = PointOnUnitCircle(angle * TAU);
                
        float2 randomPointInUnitSquare = Random2DFrom3D(
            float3(pixelCoordinates, interleavedGradientNoise + i));
        
        // -0.5 to center.
        
        randomPointInUnitSquare -= 0.5;
        
        // Combine circle and square noise.
        
        float2 noise = randomPointOnUnitCircle + randomPointInUnitSquare;
                
        // Scale to blur radius.
                
        float2 uvOffset = noise * radius;
        
        // Gather, sum.

        output += SAMPLE_TEXTURE2D(tex, samplerState, uv + uvOffset);
    }

    // Average, 'normalize'.
    
    output /= quality;
}

// LOD-based blur.
// TO-DO: add LOD mixing/blending as a separate stand-alone function?

void LODBlur_float(

    Texture2D lodTexture, Texture2D rawTexture,
    float2 uvSS, float2 texelSize, float2 renderResolution,

    SamplerState samplerState,

    int blurQuality, float blurRadius, float blurDilateRadius,

    float lodPower, float noise,
    
    out float4 output)
{    
    // Calculate LOD from radius, with power shaping for control.
    
    float lodRaw = log2(max(blurRadius * renderResolution.y, 1e-5));
    
    if (lodRaw > 0.0)
    {
        lodRaw = pow(lodRaw, lodPower);
    }
    
    int lodA = floor(lodRaw);
    
    // I noticed 'snapping' (of apparent LOD) under normal texture distortion when 0.0 blur, 
    // which would trigger instant return of texture sample as-is.
    
    // Hence, I moved this from start of the the function, to here, immediately after I have the LOD.
    // I can return the LOD'd sample instead of a 'raw' sample.
    
    if (blurRadius == 0.0)
    {
        output = SAMPLE_TEXTURE2D_LOD(lodTexture, samplerState, uvSS, lodA); return;
    }
    
    int lodB = lodA + 1;
    
    float lodBlend = frac(lodRaw);
    
    float lodBlendRaw = saturate(lodRaw);
    bool blendRaw = lodRaw < 1.0;
    
    blendRaw = false;
    
    // Scale radius by output render resolution and input mip texture resolution.
    
    float2 textureResolution;
    
    lodTexture.GetDimensions(textureResolution.x, textureResolution.y);
    float renderResolutionOverTextureResolution = renderResolution / textureResolution;
    
    blurRadius *= renderResolutionOverTextureResolution * (2.0 + blurDilateRadius);
    
    // Blur.
    
    output = 0.0;
    
    float step = 1.0 / blurQuality;
    //float step = 1.0 / max(1.0, (blurQuality - (noise != 0.0 ? 0 : 1)));
    
    // TO-DO: Adjust LOD UV offset by the appropriate LOD texel size, so it's always centered.
    // Else, it tends to move top-right with higher LODs...
    
    [loop]
    for (int i = 0; i < blurQuality; i++)
    {
        float2 sampleUV = uvSS;
        
        if (noise != 0.0)
        {
            float t = i * step;
            t += step * noise;
    
            float angle = t * TAU;

            float2 randomPoint = PointOnUnitCircle(angle);
            randomPoint *= texelSize;

            float2 uvOffset = randomPoint * blurRadius;
            
            sampleUV += uvOffset;
        }
                
        float4 sampleB = SAMPLE_TEXTURE2D_LOD(lodTexture, samplerState, sampleUV, lodB);

        float4 sample;
        
        if (!blendRaw)
        {
            float4 sampleA = SAMPLE_TEXTURE2D_LOD(lodTexture, samplerState, sampleUV, lodA);
            sample = lerp(sampleA, sampleB, lodBlend);
        }
        else
        {
            float4 sampleRaw = SAMPLE_TEXTURE2D(rawTexture, samplerState, sampleUV);
            sample = lerp(sampleRaw, sampleB, lodBlendRaw);
        }
        
        output += sample;
    }

    output /= blurQuality;
}

void LODBlur_DepthAware_float(

    Texture2D lodTexture, Texture2D rawTexture, 
    Texture2D depthTexure, Texture2D customDepthTexure,

    float2 uvSS, float2 texelSize, float2 renderResolution,

    SamplerState samplerState,

    int blurQuality, float blurRadius, float blurDilateRadius,

    float lodPower, float noise,
    
    out float4 output)
{
    // Calculate LOD from radius, with power shaping for control.
    
    float lodRaw = log2(max(blurRadius * renderResolution.y, 1e-5));
    
    if (lodRaw > 0.0)
    {
        lodRaw = pow(lodRaw, lodPower);
    }
    
    int lodA = floor(lodRaw);
    
    // I noticed 'snapping' (of apparent LOD) under normal texture distortion when 0.0 blur, 
    // which would trigger instant return of texture sample as-is.
    
    // Hence, I moved this from start of the the function, to here, immediately after I have the LOD.
    // I can return the LOD'd sample instead of a 'raw' sample.
    
    if (blurRadius == 0.0)
    {
        output = SAMPLE_TEXTURE2D_LOD(lodTexture, samplerState, uvSS, lodA);
        return;
    }
    
    int lodB = lodA + 1;
    
    float lodBlend = frac(lodRaw);
    
    float lodBlendRaw = saturate(lodRaw);
    bool blendRaw = lodRaw < 1.0;
    
    blendRaw = false;
    
    // Scale radius by output render resolution and input mip texture resolution.
    
    float2 textureResolution;
    
    lodTexture.GetDimensions(textureResolution.x, textureResolution.y);
    float renderResolutionOverTextureResolution = renderResolution / textureResolution;
    
    blurRadius *= renderResolutionOverTextureResolution * (2.0 + blurDilateRadius);
    
    // Blur.
    
    output = 0.0;
    
    float step = 1.0 / blurQuality;
    
    [loop]
    for (int i = 0; i < blurQuality; i++)
    {        
        float2 sampleUV = uvSS;
        
        if (noise != 0.0)
        {
            float t = i * step;
            t += step * noise;
    
            float angle = t * TAU;

            float2 randomPoint = PointOnUnitCircle(angle);
            randomPoint *= texelSize;

            float2 uvOffset = randomPoint * blurRadius;
            
            sampleUV += uvOffset;
        }
        
        // Depth test.
            
        float depth = SAMPLE_DEPTH_TEXTURE(depthTexure, samplerState, sampleUV);
        float customDepth = SAMPLE_DEPTH_TEXTURE(customDepthTexure, samplerState, sampleUV);
        
        // If depth closer to camera.
        
        if (customDepth < depth)
        {
            output = SAMPLE_TEXTURE2D(rawTexture, samplerState, uvSS);
            return;
        }
        
        float4 sampleB = SAMPLE_TEXTURE2D_LOD(lodTexture, samplerState, sampleUV, lodB);

        float4 sample;
        
        if (!blendRaw)
        {
            float4 sampleA = SAMPLE_TEXTURE2D_LOD(lodTexture, samplerState, sampleUV, lodA);
            sample = lerp(sampleA, sampleB, lodBlend);
        }
        else
        {
            float4 sampleRaw = SAMPLE_TEXTURE2D(rawTexture, samplerState, sampleUV);
            sample = lerp(sampleRaw, sampleB, lodBlendRaw);
        }
        
        output += sample;
    }

    output /= blurQuality;
}

#endif