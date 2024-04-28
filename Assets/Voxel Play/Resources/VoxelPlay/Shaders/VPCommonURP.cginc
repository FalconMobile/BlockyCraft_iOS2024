#ifndef VOXELPLAY_COMMON_URP
#define VOXELPLAY_COMMON_URP

// Dummy defines to avoid compiler errors of SubShader for Universal Pipeline when using Standard

float3 _MainLightPosition;
half4 _MainLightColor;
#define TransformObjectToWorld
#define TransformWorldToHClip(x) 1.0.xxxx
#define TransformObjectToHClip(x) 1.0.xxxx
#define TransformObjectToWorldNormal
#define ApplyShadowBias(x,y,z) x

#include "VPCommon.cginc"


#endif // VOXELPLAY_COMMON_URP

