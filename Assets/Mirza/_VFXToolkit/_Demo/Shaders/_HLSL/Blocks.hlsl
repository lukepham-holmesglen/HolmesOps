
#ifndef VFXTOOLKIT_DEMO_BLOCKS_HLSL
#define VFXTOOLKIT_DEMO_BLOCKS_HLSL

// ...

float2 Rotate2D(float2 position, float angle)
{
    float cosAngle = cos(angle);
    float sinAngle = sin(angle);
    
    return float2(
        position.x * cosAngle - position.y * sinAngle,
        position.x * sinAngle + position.y * cosAngle
    );
}

// ...

void TextureScrolling_float(Texture2D tex, SamplerState samplerState, float2 uv, float scale, float2 tiling, float2 offset, float2 animation, float uvRotation, float animationRotation, out float4 output)
{
    //uv -= 0.5;
    uvRotation = radians(uvRotation);
    uv = Rotate2D(uv, uvRotation);
    //uv += 0.5;
    
    uv *= scale;
    uv *= tiling;
    
    uv -= offset;
    
    animationRotation = -radians(animationRotation);
    animation = Rotate2D(animation, animationRotation);
    
    uv -= animation * _Time.y;
    
    output = SAMPLE_TEXTURE2D(tex, samplerState, uv);
}

#endif