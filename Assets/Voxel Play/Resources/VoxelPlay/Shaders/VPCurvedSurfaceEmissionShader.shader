Shader "Voxel Play/FX/Curved/Surface Emission" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
	    Pass {
			Tags { "LightMode" = "UniversalForward" }
			HLSLPROGRAM
			#pragma target 2.0
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment frag
            #define USE_EMISSION
            #define NON_ARRAY_TEXTURE

            #include "VPCommonURP.cginc"
            #include "VPCurvedUnlitPass.cginc"
			ENDHLSL
		}
	}


	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard addshadow fullforwardshadows vertex:disp nolightmap
		#pragma target 3.0
        #define NON_ARRAY_TEXTURE
		#define SURFACE_SHADER

		#include "VPCommon.cginc"

		sampler2D _EmissionMap;

		struct appdata {
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID
        };

		void disp(inout appdata v) {
			VOXELPLAY_MODIFY_VERTEX_NO_WPOS(v.vertex)
		}


		struct Input {
			float2 uv_MainTex;
			float2 uv_EmissionMap;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color, _EmissionColor;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Emission = tex2D(_EmissionMap, IN.uv_EmissionMap) * _EmissionColor;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
