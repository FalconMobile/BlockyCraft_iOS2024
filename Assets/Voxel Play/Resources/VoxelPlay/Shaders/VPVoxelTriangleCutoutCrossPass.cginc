
struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
	VOXELPLAY_TINTCOLOR_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct v2f {
	float4 pos    : SV_POSITION;
	float4 uv     : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	SHADOW_COORDS(4)
	VOXELPLAY_FRESNEL_DATA(5)
	VOXELPLAY_TINTCOLOR_DATA
	UNITY_VERTEX_OUTPUT_STEREO
};



v2f vert (appdata v) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float3 wpos = UnityObjectToWorldPos(v.vertex);
	VOXELPLAY_MODIFY_VERTEX(v.vertex, wpos)
    int iuvz = (int)v.uv.z;
	float disp = (iuvz>>16) * sin(wpos.x + _Time.w) * _VPGrassWindSpeed;
	v.vertex.x += disp * v.uv.y;

	o.pos    = UnityObjectToClipPos(v.vertex);
	float4 uv = v.uv;
    uv.z = iuvz & 65535; // remove wind animation flag

	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG(uv, wpos);
	VOXELPLAY_SET_LIGHT_WITHOUT_NORMAL(o, wpos);
    VOXELPLAY_OUTPUT_UV(uv, o);
	VOXELPLAY_OUTPUT_TINTCOLOR(o);
	VOXELPLAY_OUTPUT_FRESNEL_DATA(o, wpos);
	TRANSFER_SHADOW(o);
	return o;
}


fixed4 frag (v2f i) : SV_Target {

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	// Diffuse
	fixed4 color   = VOXELPLAY_GET_TEXEL(i.uv.xyz);
	clip(color.a - 0.25);

	VOXELPLAY_APPLY_FRESNEL(color, float3(0,1,0), i.wpos);
	
	VOXELPLAY_APPLY_TINTCOLOR(color, i);

	// AO
	color.rgb *= saturate((abs(i.uv.x - 0.5) + 0.33) * 2.0);

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}

