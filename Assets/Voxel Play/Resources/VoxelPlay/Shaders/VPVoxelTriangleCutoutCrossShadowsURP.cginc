
#include "VPCommonURP.cginc"
#include "VPCommonCore.cginc"

float3 _LightDirection;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
	float3 uv       : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
	float3 uv     : TEXCOORD0;

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

	float3 wpos = UnityObjectToWorldPos(input.positionOS);
	VOXELPLAY_MODIFY_VERTEX(input.positionOS, wpos)

    int iuvz = (int)input.uv.z;
    float disp = (iuvz>>16) * sin(wpos.x + _Time.w) * 0.01;
    input.positionOS.x += disp * input.uv.y;

    output.positionCS = GetShadowPositionHClip(input);

    float3 uv = input.uv;
    uv.z = iuvz & 65535; // remove wind animation flag
    output.uv     = uv;

    return output;
}

half4 frag(Varyings input) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	fixed4 color   = UNITY_SAMPLE_TEX2DARRAY(_MainTex, input.uv.xyz);
	clip(color.a - 0.25);

    return 0;
}



