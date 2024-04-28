
struct appdata {
	float4 vertex   : POSITION;
	float3 normal   : NORMAL;
	float4 color    : COLOR;
	float2 uv       : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct v2f {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 pos    : SV_POSITION;
	VOXELPLAY_LIGHT_DATA(0,1)
	SHADOW_COORDS(2)
    float4 uv     : TEXCOORD3;
	#if defined(USE_TRIPLANAR) 
	    float3 wpos    : TEXCOORD4;
	    float3 norm : TEXCOORD5;
    #endif
    VOXELPLAY_BUMPMAP_DATA(4)
	fixed4 color  : COLOR;
	VOXELPLAY_FOG_DATA(6)
	UNITY_VERTEX_OUTPUT_STEREO
};


fixed4 _Color;
fixed _DiffuseWrap;
int _VoxelLight;

// Emission =====================================================================

#if _EMISSION
	fixed3 _EmissionColor;
	UNITY_DECLARE_TEX2D_NOSAMPLER(_EmissionMap);
	#define VOXELPLAY_APPLY_EMISSION(color, i) color.rgb += _EmissionMap.Sample(SAMPLER_NAME, i.uv.xy).rgb * _EmissionColor;
#else
	#define VOXELPLAY_APPLY_EMISSION(color, i);
#endif

UNITY_INSTANCING_BUFFER_START(Props)
	UNITY_DEFINE_INSTANCED_PROP(fixed4, _TintColor)
#define _TintColor_arr Props
UNITY_INSTANCING_BUFFER_END(Props)


v2f vert (appdata v) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float3 wpos = UnityObjectToWorldPos(v.vertex);
	VOXELPLAY_MODIFY_VERTEX(v.vertex, wpos)
	o.pos    = UnityObjectToClipPos(v.vertex);

	fixed4 color = v.color;
    color.a = 1.0;
    color *= _Color;

    // By default, dynamic models (models that can move) receive lighting using VoxelPlayBehaviour through the _VoxelLight property
    int packedLight = _VoxelLight;

    if (v.color.a<1.0) {
        // For static models placed by VP, the lighting is encoded in the mesh colors alpha channel.
        // Sun light + torch light is encoded in mesh color alpha channel; we pass it to uv.w where lighting is stored for all voxel play shaders
        packedLight = (int)(v.color.a * 255);
        packedLight = ((packedLight & 0xF0) << 8) + (packedLight & 0xF);
    }
	
    #ifdef UNITY_INSTANCING_ENABLED
        float4 tintColor = UNITY_ACCESS_INSTANCED_PROP(_TintColor_arr, _TintColor);
        color.rgb *= tintColor.rgb;
        #if VOXELPLAY_GPU_INSTANCING
            packedLight = (int)tintColor.a; // voxel light now is encoded into tintColor.a
        #endif
    #endif
	o.color = color;
	
	float3 worldNormal = UnityObjectToWorldNormal(v.normal);
    float2 scaledUV = TRANSFORM_TEX(v.uv.xy, _MainTex);
    float4 uv = float4(scaledUV, 0, packedLight);
    
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(uv, wpos, worldNormal);
	VOXELPLAY_SET_LIGHT(o, wpos, worldNormal);
    VOXELPLAY_OUTPUT_UV(uv, o);

    #if defined(USE_TRIPLANAR)
        o.wpos    = wpos;
        o.norm = worldNormal;
    #endif

	VOXELPLAY_SET_TANGENT_SPACE(tang, v.normal)

	VOXELPLAY_OUTPUT_BUMPMAP_DATA(uv, o)

	TRANSFER_SHADOW(o);
	return o;
}

fixed4 frag (v2f i) : SV_Target {
//	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	// Diffuse
	#if defined(USE_TEXTURE)
		fixed4 color = VOXELPLAY_GET_TEXEL_2D(i.uv) * i.color;
	#elif defined(USE_TRIPLANAR) // from bgolus' https://github.com/bgolus/Normal-Mapping-for-a-Triplanar-Shader/blob/master/TriplanarSwizzle.shader
		// triplanar blend
        half3 triblend = saturate(pow(i.norm, 4));
        triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);

        // triplanar uvs
        float2 uvX = i.wpos.zy * _MainTex_ST.xy + _MainTex_ST.zw;
        float2 uvY = i.wpos.xz * _MainTex_ST.xy + _MainTex_ST.zw;
        float2 uvZ = i.wpos.xy * _MainTex_ST.xy + _MainTex_ST.zw;

        // albedo textures
        fixed4 colX = VOXELPLAY_GET_TEXEL_2D(uvX);
        fixed4 colY = VOXELPLAY_GET_TEXEL_2D(uvY);
        fixed4 colZ = VOXELPLAY_GET_TEXEL_2D(uvZ);
        fixed4 color = colX * triblend.x + colY * triblend.y + colZ * triblend.z;

        half3 axisSign = i.norm < 0 ? -1 : 1;

        // tangent space normal maps
        half3 tnormalX = UnpackNormal(_BumpMap.Sample(sampler_Point_Repeat, uvX));
        half3 tnormalY = UnpackNormal(_BumpMap.Sample(sampler_Point_Repeat, uvY));
        half3 tnormalZ = UnpackNormal(_BumpMap.Sample(sampler_Point_Repeat, uvZ));

        // flip normal maps' z axis to account for world surface normal facing
        tnormalX.z *= axisSign.x;
        tnormalY.z *= axisSign.y;
        tnormalZ.z *= axisSign.z;

        // swizzle tangent normals to match world orientation and blend together
        half3 worldNormal = normalize(tnormalX.zyx * triblend.x + tnormalY.xzy * triblend.y + tnormalZ.xyz * triblend.z);
        half ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
        ndotl = lerp(ndotl, ndotl * 0.5 + 0.5, _DiffuseWrap);
        color *= ndotl + _VPAmbientLight;
        color *= i.color;
	#else
		fixed4 color = i.color;
	#endif

    #if defined(VP_CUTOUT)
        clip(color.a - 0.5);
    #endif

    #if defined(USE_CUSTOM_BUMP_MAP)
       float4 nrm = _BumpMap.Sample(sampler_Linear_Repeat, i.uv.xy);
       VOXELPLAY_APPLY_CUSTOM_BUMPMAP(i, nrm);
    #endif
	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);
    VOXELPLAY_APPLY_EMISSION(color, i);
	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}

