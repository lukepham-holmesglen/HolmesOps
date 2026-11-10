Shader "Ata Khani/BRP_MaskMap_Transparent"
{
    Properties
    {
        [KeywordEnum(Off, Back, Front)] _Cull ("Cull", int) = 2

        [Space(25)]

        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.0
        
        [Space(25)]
        
        [Toggle(USE_NORMAL_MAP)] _UseNormalMap ("Use Normal Map", Float) = 1
        _NormalMap ("Normal Map", 2D) = "bump" {}

        [Space(25)]

        [Toggle(USE_MASK_MAP)] _UseMaskMap ("Use Mask Map", Float) = 1
        _MaskMap ("Mask Map", 2D) = "white" {}
        _MetallicStrength ("Metallic Strength", Range(0, 1)) = 1
        _SmoothnessStrength ("Smoothness Strength", Range(0, 1)) = 1
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1

        [Space(25)]

        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Occlusion("Occlusion", Range(0, 1)) = 1.0

        [Space(25)]

        [Toggle(USE_EMISSION)] _UseEmission ("Use Emission", Float) = 0
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 0)
        _EmissionMap ("Emission Map", 2D) = "white" {}

        [HideInInspector]  _Mode ("__mode", Float) = 0.000000
        [HideInInspector]  _SrcBlend ("__src", Float) = 1.000000
        [HideInInspector]  _DstBlend ("__dst", Float) = 0.000000
        [HideInInspector]  _ZWrite ("__zw", Float) = 1.000000
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        
        Cull [_Cull]
        ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows alpha
        #pragma target 3.0

        #pragma shader_feature USE_MASK_MAP
        #pragma shader_feature USE_NORMAL_MAP
        #pragma shader_feature USE_EMISSION
        
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _MaskMap;
        sampler2D _EmissionMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_MaskMap;
            float2 uv_EmissionMap;
        };

        half _Cutoff;

        half _MetallicStrength;
        half _SmoothnessStrength;
        half _OcclusionStrength;

        half _Glossiness;
        half _Metallic;
        half _Occlusion;

        fixed4 _Color;
        fixed4 _EmissionColor;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            
            #ifdef USE_NORMAL_MAP
            o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            #endif

            #ifdef USE_MASK_MAP
            fixed4 m = tex2D(_MaskMap, IN.uv_MaskMap);
            o.Metallic = m.r * _MetallicStrength;
            o.Occlusion = lerp(1.0, m.g, _OcclusionStrength);
            o.Smoothness = m.a * _SmoothnessStrength;
            #else
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Occlusion = _Occlusion;
            #endif

            #ifdef USE_EMISSION
            o.Emission = tex2D(_EmissionMap, IN.uv_EmissionMap) * _EmissionColor;
            #endif
            
            clip(c.a - _Cutoff);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
