Shader "Voxel Play/FX/Curved/Unlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
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
            #define NON_ARRAY_TEXTURE

            #include "VPCommonURP.cginc"
            #include "VPCurvedUnlitPass.cginc"
			ENDHLSL
		}
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue" = "Geometry" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #define NON_ARRAY_TEXTURE

            #include "VPCommon.cginc"
            #include "VPCurvedUnlitPass.cginc"

			ENDCG
		}
	}
}
