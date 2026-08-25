
// Mirza: https://www.shadertoy.com/view/Dl3fRl

#define FLAT_TOP_HEXAGON

// MB: This special right-angle triangle (30-60-90°) is just half an equilateral ('regular') triangle (all sides = 60°).

// https://en.wikipedia.org/wiki/Equilateral_triangle

#ifdef FLAT_TOP_HEXAGON
    #define tsnTriangleRatio float2(sqrt(3.0), 1.0)
#else
    #define tsnTriangleRatio float2(1.0, sqrt(3.0))
#endif

void Hex_float(float2 p, out float output)
{
    p = abs(p);
    
#ifdef FLAT_TOP_HEXAGON
    output = max(dot(p, tsnTriangleRatio * 0.5), p.y);
#else
    output = max(dot(p, tsnTriangleRatio * 0.5), p.x);
#endif  
}

// xy = 2D distance in each cell. Use with Hex() to get hexagon shape.
// zw = hexagonal-quantized UV coordinates (cell ID).

// MB: See, https://andrewhungblog.wordpress.com/2018/07/28/shader-art-tutorial-hexagonal-grids/

// Essentially rows of points, where every other row is shifted.
// https://blender.stackexchange.com/questions/161701/how-to-do-uv-indexing-in-hexagonal-pattern

void HexLattice_float(float2 uv, out float2 cellDistance, out float2 cellUV, out float cell)
{
    // MB: Using round (vs. floor), removing need for + 0.5.
    
#ifdef FLAT_TOP_HEXAGON
    float4 hexCenter = round(float4(uv, uv - float2(1.0, 0.5)) / tsnTriangleRatio.xyxy);
#else
    float4 hexCenter = round(float4(uv, uv - float2(0.5, 1.0)) / tsnTriangleRatio.xyxy);
#endif    
    
    float4 offset = float4(uv - (hexCenter.xy * tsnTriangleRatio), uv - ((hexCenter.zw + 0.5) * tsnTriangleRatio));
     
    if (dot(offset.xy, offset.xy) < dot(offset.zw, offset.zw))
    {
        cellDistance = offset.xy;
        cellUV = hexCenter.xy;
    }
    else
    {
        cellDistance = offset.zw;
        cellUV = hexCenter.zw + 0.5;
    }
    
    Hex_float(cellDistance, cell);
}