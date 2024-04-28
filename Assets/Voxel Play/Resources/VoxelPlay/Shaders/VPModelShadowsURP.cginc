
#include "VPCommonURP.cginc"
#include "VPCommonCore.cginc"
#include "VPCommonVertexModifier.cginc"

float3 _LightDirection;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    #if defined(VP_CUTOUT)
        float2 uv       : TEXCOORD1;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    #if defined(VP_CUTOUT)
        float2 uv       : TEXCOORD0;
    #endif
	UNITY_VERTEX_OUTPUT_STEREO
};

float4 GetShadowPositionHClip(Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}

Varyings vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	VOXELPLAY_MODIFY_VERTEX_NO_WPOS(input.positionOS)

    output.positionCS = GetShadowPositionHClip(input);

    #if defined(VP_CUTOUT)
        output.uv = input.uv;
    #endif

    return output;
}

half4 frag(Varyings input) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    #if defined(VP_CUTOUT)
        fixed4 color = VOXELPLAY_GET_TEXEL_2D(input.uv);
        clip(color.a - 0.5);
    #endif
    return 0;
}



