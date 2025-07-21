void ToonShading_float(in float3 Normal, in float ToonRampSmoothness, in float4 ClipSpacePos, in float3 WorldPos, in float3 ToonRampTinting,
in float ToonRampOffset, in float ToonRampOffsetPoint, in float Ambient, out float3 ToonRampOutput, out float3 Direction)
{

	// set the shader graph node previews
	#ifdef SHADERGRAPH_PREVIEW
		ToonRampOutput = float3(0.5,0.5,0);
		Direction = float3(0.5,0.5,0);
	#else

		// grab the shadow coordinates
		#if SHADOWS_SCREEN
			half4 shadowCoord = ComputeScreenPos(ClipSpacePos);
		#else
			half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
		#endif 
		// grab the main light
		#if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
			Light light = GetMainLight(shadowCoord);
		#else
			Light light = GetMainLight();
		#endif

		// dot product for toonramp
		half d = dot(Normal, light.direction) * 0.5 + 0.5;
		
		
		// toonramp in a smoothstep
		half toonRamp = smoothstep(ToonRampOffset, ToonRampOffset+ ToonRampSmoothness, d );
		
		

		float3 extraLights = float3(0,0,0);
		// get the number of point/spot lights

		// create inputdata struct to use in LIGHT_LOOP
   		InputData inputData = (InputData)0;
        inputData.positionWS = WorldPos;
        inputData.normalWS = Normal;
        inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(WorldPos);
		//light disappearing outside of range fix thanks to  rsofia on github https://github.com/rsofia/CustomLightingForwardPlus
  		float4 screenPos = float4(ClipSpacePos.x, (_ScaledScreenParams.y - ClipSpacePos.y), 0, 0);
        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(screenPos);

		// forward and forward+ lights loop
		uint lightsCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(lightsCount)
            
            Light aLight = GetAdditionalLight(lightIndex, WorldPos, half4(1,1,1,1));
			half d = dot(Normal, aLight.direction) * 0.5 + 0.5;
			float3 attenuatedLightColor = aLight.color * (aLight.distanceAttenuation * aLight.shadowAttenuation);
			half toonRampExtra = smoothstep(ToonRampOffsetPoint, ToonRampOffsetPoint+ ToonRampSmoothness, d );
			extraLights += ( toonRampExtra * attenuatedLightColor);
					
        LIGHT_LOOP_END
		
		// multiply with shadows;
		toonRamp *= light.shadowAttenuation;

		// add in lights and extra tinting
		ToonRampOutput = light.color * (toonRamp + ToonRampTinting)  + Ambient;

		// also add in point/spot lights
		ToonRampOutput += extraLights;
		// output direction for rimlight
		Direction = normalize(light.direction);
		

	#endif
}