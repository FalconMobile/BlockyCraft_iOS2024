#ifndef VOXELPLAY_COMMON_URP
#define VOXELPLAY_COMMON_URP

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// translate common cginc syntax elements to Universal syntax
SAMPLER(sampler_MainTex);
SAMPLER(_Point_Repeat);
SAMPLER(_Linear_Repeat);
SAMPLER(_Trilinear_Repeat);

#define fixed half
#define fixed2 half2
#define fixed3 half3
#define fixed4 half4

#define UNITY_DECLARE_TEX2DARRAY TEXTURE2D_ARRAY
#define UNITY_DECLARE_TEX2D_NOSAMPLER TEXTURE2D
#define UNITY_SAMPLE_TEX2DARRAY(tex, uv) SAMPLE_TEXTURE2D_ARRAY(tex, sampler_MainTex, uv.xy, uv.z)
#define UNITY_SAMPLE_TEX2DARRAY_SAMPLER(tex, sampler, uv) SAMPLE_TEXTURE2D_ARRAY(tex, sampler, uv.xy, uv.z)
#define UNITY_SAMPLE_TEX2DARRAY_LOD(tex, uv, lod) SAMPLE_TEXTURE2D_ARRAY_LOD(tex, sampler_MainTex, uv.xy, uv.z, 0)

#define _WorldSpaceLightPos0 _MainLightPosition
#define _LightColor0 _MainLightColor

#define UNITY_INITIALIZE_OUTPUT(t,v) v=(t)0;
#define UnityObjectToClipPos(x) TransformObjectToHClip(x.xyz)
#define UnityObjectToWorldNormal(x) TransformObjectToWorldNormal(x)

#define UnityObjectToWorldPos(v) TransformObjectToWorld(v.xyz)
#define ComputeGrabScreenPos(x) ComputeScreenPos(x)

#define LinearEyeDepthX(x) LinearEyeDepth(x, _ZBufferParams)
#define SampleSceneDepthProj(grabPos) SampleSceneDepth(grabPos.xy/grabPos.w);

#undef VERTEXLIGHT_ON

inline float3 WorldSpaceViewDir( float4 vertex ) {
    float3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;
    return _WorldSpaceCameraPos - worldPos;
}

half3 BlendNormals(half3 n1, half3 n2) {
    return normalize(half3(n1.xy + n2.xy, n1.z*n2.z));
}

float GetLightAttenuation(float3 wpos) {
    float4 shadowCoord = TransformWorldToShadowCoord(wpos);
	float atten = MainLightRealtimeShadow(shadowCoord);
    return atten;
}

#if defined(NO_SHADOWS)
        #define SHADOW_COORDS(x)
        #define TRANSFER_SHADOW(x)
        #define SHADOW_ATTENUATION(i) 1.0
#else
    #if VOXELPLAY_PIXEL_LIGHTS
        #define SHADOW_COORDS(x)
        #define TRANSFER_SHADOW(x)
        #define SHADOW_ATTENUATION(i) GetLightAttenuation(i.wpos)
    #else
        #define SHADOW_COORDS(idx) float3 shadowCoordsWS: TEXCOORD##idx;
        #define TRANSFER_SHADOW(x) x.shadowCoordsWS = wpos;
        #define SHADOW_ATTENUATION(i) GetLightAttenuation(i.shadowCoordsWS)
    #endif
#endif

// note: _VPAmbientLight could be left outside of saturate() function. In that case AO will be affected (diminished due to atten * ao calc, see VOXELPLAY_APPLY_LIGHTING_AO_AND_GI function below) so we leave it inside.
#if defined(NO_SELF_SHADOWS)
    #define VOXELPLAY_LIGHT_ATTENUATION(i) max(0, (1.0 + _MainLightPosition.y * _VPDaylightShadowAtten) * i.light.x + _VPAmbientLight)
    #define UNITY_SHADOW_ATTEN(i) 1.0
#else
    #if defined(USE_SOFT_SHADOWS)
        #define VOXELPLAY_SHADOW_ATTENUATION(i) min(1, SHADOW_ATTENUATION(i) + 0.25)
    #else
        #define VOXELPLAY_SHADOW_ATTENUATION(i) min(1, SHADOW_ATTENUATION(i) )
    #endif
    #define VOXELPLAY_LIGHT_ATTENUATION(i) saturate( (VOXELPLAY_SHADOW_ATTENUATION(i) * i.light.x + _MainLightPosition.y * _VPDaylightShadowAtten) + _VPAmbientLight)
    #define UNITY_SHADOW_ATTEN(i) SHADOW_ATTENUATION(i)
#endif

#endif // VOXELPLAY_COMMON_URP

