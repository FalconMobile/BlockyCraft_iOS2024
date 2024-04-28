Shader "Voxel Play/FX/DamageParticle"
{
    Properties
    {
    	_MainTex ("Particle Texture Top", 2D) = "white" {}
    	_TexSides ("Particle Texture Sides", 2D) = "white" {}
    	_TexBottom ("Particle Texture Bottom", 2D) = "white" {}
    	_VoxelLight ("Voxel Ambient Light", Float) = 1
    	_FlashDelay("Flash Delay", Float) = 0
    	_Color ("Tint Color", Color) = (1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
		Pass {
			Tags { "LightMode" = "UniversalForward" }
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile _ VERTEXLIGHT_ON
            #define NON_ARRAY_TEXTURE
            #include "VPCommonURP.cginc"
            #include "VPDamageParticlePass.cginc"
            ENDHLSL
        }
    }

    SubShader
    {
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ VERTEXLIGHT_ON
			#define NON_ARRAY_TEXTURE
            #include "UnityLightingCommon.cginc"
			#include "VPCommon.cginc"
            #include "VPDamageParticlePass.cginc"
            ENDCG
        }
    }
}