#ifndef VOXELPLAY_COMMON_SKY
#define VOXELPLAY_COMMON_SKY

float _VPFogAmount;
float3 _VPFogData;
half3 _VPSkyTint;
half _VPExposure;

fixed3 getSkyColor(float3 ray) {
	float3 delta  = _WorldSpaceLightPos0.xyz - ray;
	float dist    = dot(delta, delta);
	float y = abs(ray.y);

	// sky base color
	half3 skyColor = _VPSkyTint;

	// fog
	half fog = saturate(_VPFogAmount - y) / (1.0001 - _VPFogAmount);
	skyColor = lerp(skyColor, 1.0.xxx, fog);

	// sky tint
	float hy = abs(_WorldSpaceLightPos0.y) + y;
	half t = saturate( (0.4 - hy) * 2.2) / (1.0 + dist * 0.8);
	skyColor.r = lerp(skyColor.r, 1.0, t);
	skyColor.b = lerp(skyColor.b, 0.0, t);

	// daylight + obscure opposite side of sky
	fixed dayLightDir = 1.0 + _WorldSpaceLightPos0.y * 2.0;
	half daylight = saturate(dayLightDir - dist * 0.03);
	skyColor *= daylight;

	// exposure
	skyColor *= _VPExposure * _LightColor0.rgb;

	// gamma
	#if defined(UNITY_COLORSPACE_GAMMA) && !defined(SHADER_API_MOBILE)
	    skyColor = sqrt(skyColor);
	#endif

	return skyColor;
}

#endif // VOXELPLAY_COMMON_CORE

