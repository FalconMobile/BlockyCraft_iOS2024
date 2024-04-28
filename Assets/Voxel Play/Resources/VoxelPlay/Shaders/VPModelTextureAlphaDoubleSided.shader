Shader "Voxel Play/Models/Texture/Alpha Double Sided"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
        _CustomDaylightShadowAtten ("Daylight Shadow Atten", Range(0,1)) = 0.65
		 [HideInInspector] _TintColor ("Tint Color", Color) = (1,1,1,1)
		_VoxelLight ("Voxel Light", Range(0,15)) = 15
		[Toggle(_EMISSION)]
		_UseEmission ("Use Emission", Float) = 0
        _EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
	}

	SubShader {

		Tags { "Queue" = "Transparent+1" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"  }
		Pass {
			Tags { "LightMode" = "UniversalForward" }
			Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
			Cull Off
			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex   vert
			#pragma fragment frag
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
            #pragma multi_compile _ VOXELPLAY_GPU_INSTANCING
			#define SUBTLE_SELF_SHADOWS
			#define USE_TEXTURE
			#define NON_ARRAY_TEXTURE
			#pragma shader_feature _EMISSION
            #include "VPCommonURP.cginc"
            #include "VPCommonCore.cginc"
			#include "VPModel.cginc"
			ENDHLSL
		}

	}

	SubShader {

		Tags { "Queue" = "Transparent+1" "RenderType" = "Transparent" }
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex   vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
            #pragma multi_compile _ VOXELPLAY_GPU_INSTANCING
			#pragma multi_compile_instancing nolightprobe nolodfade
			#define SUBTLE_SELF_SHADOWS
			#define USE_TEXTURE
			#define NON_ARRAY_TEXTURE
			#pragma shader_feature _EMISSION
            #include "VPCommon.cginc"
			#include "VPModel.cginc"
			ENDCG
		}
	}

	Fallback Off
}