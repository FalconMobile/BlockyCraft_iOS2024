Shader "Voxel Play/Voxels/Triangle/Cloud"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
		_OutlineColor ("Outline Color", Color) = (1,1,1,0.5)
		_OutlineThreshold("Outline Threshold", Float) = 0.48
		[HideInInspector] _VPParallaxStrength("__Elev", Float) = 0.2
		[HideInInspector] _VPParallaxMaxDistanceSqr("__MaxDistSqr", Float) = 625
		[HideInInspector] _VPParallaxIterations("__Iterations", Float) = 10
		[HideInInspector] _VPParallaxIterationsBinarySearch("__IterationsBinarySearch", Float) = 6
	}

	SubShader {

		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "DisableBatching" = "True" "RenderPipeline" = "UniversalPipeline" }
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
			#pragma multi_compile _ VOXELPLAY_USE_AA
            #pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
            #define NO_SELF_SHADOWS
            #define SUN_SCATTERING
            #define NO_AMBIENT
            #define IS_CLOUD
            #include "VPCommonURP.cginc"
            #include "VPCommonCore.cginc"
			#include "VPVoxelTriangleOpaquePass.cginc"
			ENDHLSL
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" "DisableBatching" = "True" }
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
            #define IS_CLOUD
		    #pragma multi_compile_instancing
			#include "VPVoxelTriangleShadows.cginc"
			ENDCG
		}

	}

	SubShader {

		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "DisableBatching" = "True"  }
		Pass {
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase nolightmap nodynlightmap novertexlight nodirlightmap
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_AA
            #pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
            #define NO_SELF_SHADOWS
            #define SUN_SCATTERING
            #define NO_AMBIENT
            #define IS_CLOUD
            #include "VPCommon.cginc"
			#include "VPVoxelTriangleOpaquePass.cginc"
			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" "DisableBatching" = "True" }
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest
            #define IS_CLOUD
			#include "VPVoxelTriangleShadows.cginc"
			ENDCG
		}
	}

	Fallback Off
}