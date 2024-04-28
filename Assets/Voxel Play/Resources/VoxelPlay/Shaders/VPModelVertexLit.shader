Shader "Voxel Play/Models/VertexLit"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
        _CustomDaylightShadowAtten ("Daylight Shadow Atten", Range(0,1)) = 0.65
		 [HideInInspector] _TintColor ("Per Instance Tint Color", Color) = (1,1,1,1)
		_VoxelLight ("Voxel Light", Range(0,15)) = 15
	}
	SubShader {

		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"  }
		Pass {
			Tags { "LightMode" = "UniversalForward" }
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
            #pragma multi_compile _ VOXELPLAY_GPU_INSTANCING
			#pragma multi_compile_instancing nolightprobe nolodfade
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
			#define SUBTLE_SELF_SHADOWS
			#define NON_ARRAY_TEXTURE
            #include "VPCommonURP.cginc"
            #include "VPCommonCore.cginc"
			#include "VPModel.cginc"
			ENDHLSL
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
		    #pragma multi_compile_instancing
			#include "VPModelShadowsURP.cginc"
			ENDHLSL
		}
	}

	SubShader {

		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
            #pragma multi_compile _ VOXELPLAY_GPU_INSTANCING
			#pragma multi_compile_instancing nolightprobe nolodfade
			#define SUBTLE_SELF_SHADOWS
			#define NON_ARRAY_TEXTURE
            #include "VPCommon.cginc"
			#include "VPModel.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "VPModelShadows.cginc"
			ENDCG
		}

	}
	Fallback Off
}