#include "VPCommonVertexModifier.cginc"
#include "VPCommonIndirect.cginc"


struct appdata {
	float4 vertex   : POSITION;
    #if defined(VP_CUTOUT)
        float4 uv       : TEXCOORD0;
    #endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct v2f {
	float4 pos    : SV_POSITION;
    #if defined(VP_CUTOUT)
    	float4 uv     : TEXCOORD0;
    #endif
	UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert (appdata v, uint instanceID : SV_InstanceID) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	#if SHADER_TARGET >= 45
       	float4 position = _Positions[instanceID];
       	v.vertex.xyz *= position.w;
       	#if VOXELPLAY_USE_ROTATION
	    	float4 rotationQuaternion = _Rotations[instanceID];
       		VOXELPLAY_COMPUTE_WORLD_MATRIX(position, rotationQuaternion)
        	float3 wpos = mul(unity_ObjectToWorld_2, v.vertex).xyz;
        #else
        	float3 wpos = position.xyz + v.vertex.xyz;
        #endif
    #else
	    float3 wpos = v.vertex.xyz;
    #endif

	VOXELPLAY_MODIFY_WPOS(wpos)
	o.pos    = mul(UNITY_MATRIX_VP, float4(wpos, 1.0f));
    #if defined(VP_CUTOUT)
        o.uv     = v.uv;
    #endif

    return o;
}

fixed4 frag (v2f i) : SV_Target {
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    #if defined(VP_CUTOUT)
        fixed4 color = VOXELPLAY_GET_TEXEL_2D(i.uv);
        clip(color.a - 0.5);
    #endif
	return 0;
}

