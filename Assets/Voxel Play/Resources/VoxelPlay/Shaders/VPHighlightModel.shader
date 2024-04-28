Shader "Voxel Play/Misc/Highlight Model"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
		_Color ("Color", Color) = (1,0,0,0.5)
	}
	SubShader
	{
		Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -2, -2

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #include "VPCommon.cginc"

			fixed4 _Color;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float3 uv     : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos     : SV_POSITION;
				fixed4 color   : COLOR;
				float3 uv      : TEXCOORD0;
				float3 wpos    : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			v2f vert (appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.wpos   = wpos;
				VOXELPLAY_MODIFY_VERTEX(v.vertex, o.wpos)
				o.pos    = UnityObjectToClipPos(v.vertex);
				o.uv     = v.uv;

				float3 worldNormal = UnityObjectToWorldNormal(v.normal);
			    o.uv.xy = wpos.xz * worldNormal.y + wpos.xy * float2(-worldNormal.z, abs(worldNormal.z)) + wpos.zy * float2(worldNormal.x, abs(worldNormal.x));

				o.color  = fixed4(_Color.rgb, (sin(_Time.w * 4.0) + 1.0) * 0.25 + 0.25 );
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float3 grd = abs(frac(i.wpos + 0.5) - 0.5);
				grd /= fwidth(i.wpos);
				float  lin = min(grd.x, min(grd.y, grd.z));
				float4 col = float4(min(lin.xxx * 0.2, 1.0), 1.0);
				col.a = 1.0 - col.r;
				fixed4 color =  UNITY_SAMPLE_TEX2DARRAY_SAMPLER(_MainTex, _Point_Repeat, i.uv);
				color *= i.color;
				color.rgb += col.rgb * col.a;
				return color;
			}
			ENDCG
		}
	}
}
