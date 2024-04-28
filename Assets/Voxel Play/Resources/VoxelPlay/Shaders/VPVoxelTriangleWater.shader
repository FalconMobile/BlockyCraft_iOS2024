Shader "Voxel Play/Voxels/Triangle/Water"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
	}
	SubShader {

		Tags { "Queue" = "Geometry+100" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

		Pass {
			Tags { "LightMode" = "UniversalForward" }
			ZWrite Off
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_AA VOXELPLAY_USE_PARALLAX
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
			#define USE_SHADOWS
            #define USE_SOFT_SHADOWS
			#define USE_SPECULAR
			#include "VPVoxelTriangleWaterPass.cginc"
			ENDHLSL
		}
	}

	SubShader {

		Tags { "Queue" = "Geometry+100" "RenderType" = "Opaque" }

		GrabPass { "_WaterBackgroundTexture" }

		Pass {
			Tags { "LightMode" = "ForwardBase" }
			ZWrite Off
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_NORMAL
			#pragma multi_compile _ VOXELPLAY_USE_AA VOXELPLAY_USE_PARALLAX
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
			#define USE_SHADOWS
            #define USE_SOFT_SHADOWS
			#define USE_SPECULAR
			#include "VPVoxelTriangleWaterPass.cginc"
			ENDCG
		}

	}
	Fallback Off
}