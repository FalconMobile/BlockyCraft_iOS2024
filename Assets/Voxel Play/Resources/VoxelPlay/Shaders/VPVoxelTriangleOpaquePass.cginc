
#include "VPCommonBevel.cginc"

struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
	float3 normal   : NORMAL;
	VOXELPLAY_TINTCOLOR_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct v2f {
	float4 pos     : SV_POSITION;
	float4 uv      : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	SHADOW_COORDS(4)
	VOXELPLAY_TINTCOLOR_DATA
	VOXELPLAY_BUMPMAP_DATA(5)
	VOXELPLAY_PARALLAX_DATA(6)
	VOXELPLAY_NORMAL_DATA
	UNITY_VERTEX_OUTPUT_STEREO
};

struct vertexInfo {
	float4 vertex;
};

int _VoxelLight;
fixed _CutOff;

v2f vert (appdata v) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    #if defined(IS_CLOUD)
        v.vertex.xyz *= float3(4, 2, 4);
    #endif
	float3 wpos = UnityObjectToWorldPos(v.vertex);
	VOXELPLAY_MODIFY_VERTEX(v.vertex, wpos)

	float4 uv = v.uv;
	#if defined(VP_CUTOUT)
		int iuvz = (int)uv.z;
		float disp = (iuvz>>16) * sin(wpos.x + wpos.y + _Time.w) * _VPTreeWindSpeed;
		v.vertex.xy += disp;
		uv.z = iuvz & 65535; // remove wind animation flag
    #elif defined(USE_ANIMATION)
		int iuvz = (int)uv.z;
        int frameCount = (iuvz>>14) & 0xF;
        float speed = (iuvz>>18);
        speed = speed * speed / 8.0;
        #if defined (VOXELPLAY_USE_NORMAL) || defined (VOXELPLAY_USE_PARALLAX)
			uv.z = (iuvz & 16383) + ((uint)(_Time.y * speed) % frameCount) * 2;
		#else
			uv.z = (iuvz & 16383) + ((uint)(_Time.y * speed) % frameCount);
		#endif
	#endif

	#if defined(USE_PACKED_LIGHT)
		uv.w = _VoxelLight;
	#endif

	o.pos    = UnityObjectToClipPos(v.vertex);

    #if defined(USE_WORLD_SPACE_NORMAL)
        v.normal = UnityObjectToWorldNormal(v.normal);
    #endif

	VOXELPLAY_OUTPUT_TINTCOLOR(o);
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(uv, wpos, v.normal);
	VOXELPLAY_SET_LIGHT(o, wpos, v.normal);
	TRANSFER_SHADOW(o);

	VOXELPLAY_SET_TANGENT_SPACE(tang, v.normal)

    #if defined(USE_WORLD_SPACE_UV)
	    uv.xy = wpos.xz * v.normal.y + wpos.xy * float2(-v.normal.z, abs(v.normal.z)) + wpos.zy * float2(v.normal.x, abs(v.normal.x));
	    uv.xy = TRANSFORM_TEX(uv.xy, _MainTex);
    #endif

	VOXELPLAY_OUTPUT_PARALLAX_DATA(v, uv, o)
	VOXELPLAY_OUTPUT_BUMPMAP_DATA(uv, o)
	VOXELPLAY_OUTPUT_UV(uv, o)

	return o;
}


fixed4 frag (v2f i) : SV_Target {

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	VOXELPLAY_APPLY_PARALLAX(i);

	VOXELPLAY_COMPUTE_BEVEL(i);

	// Diffuse
	fixed4 color   = VOXELPLAY_GET_TEXEL_DD(i.uv.xyz);

	#if defined(VP_CUTOUT)
	    clip(color.a - _CutOff);
	#endif

	VOXELPLAY_APPLY_BEVEL(i);

	VOXELPLAY_APPLY_FRESNEL(color, i.norm, i.wpos);

	VOXELPLAY_COMPUTE_EMISSION(color)

	VOXELPLAY_APPLY_BUMPMAP(i);

	VOXELPLAY_APPLY_TINTCOLOR(color, i);

	VOXELPLAY_APPLY_OUTLINE_SIMPLE(color, i);

    #if defined(IS_CLOUD)
        VOXELPLAY_APPLY_LIGHTING(color, i);
    #else
	    VOXELPLAY_APPLY_LIGHTING_AO_AND_GI(color, i);
    #endif

	VOXELPLAY_ADD_EMISSION(color)

	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}

