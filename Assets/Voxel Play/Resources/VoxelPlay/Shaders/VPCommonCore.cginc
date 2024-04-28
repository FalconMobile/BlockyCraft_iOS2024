#ifndef VOXELPLAY_COMMON_CORE
#define VOXELPLAY_COMMON_CORE

// disables warning of pow(f,e) with negative values
#pragma warning (disable : 3571)

#include "VPCommonOptions.cginc"
#include "VPCommonVertexModifier.cginc"
#include "VPCommonSeeThrough.cginc"
#include "VPCommonMainTex.cginc"
#include "VPCommonSky.cginc"

/* cube coords
   
  7+------+6
  /.   3 /|
2+------+ |
 |4.....|.+5
 |/     |/
0+------+1

*/

// Samplers  =====================================================================

SamplerState sampler_Point_Repeat;
SamplerState sampler_Linear_Repeat;
SamplerState sampler_Trilinear_Repeat;

#if FILTER_MODE == TRILINEAR_FILTER
	#define SAMPLER_NAME sampler_Trilinear_Repeat
	#define SAMPLER_MODE _Trilinear_Repeat
#elif FILTER_MODE == BILINEAR_FILTER
	#define SAMPLER_NAME sampler_Linear_Repeat
	#define SAMPLER_MODE _Linear_Repeat
#else
	#define SAMPLER_NAME sampler_Point_Repeat
	#define SAMPLER_MODE _Point_Repeat
#endif


// Globals  =====================================================================

#if defined(NO_AMBIENT)
    #define _VPAmbientLight 0
#else
    fixed _VPAmbientLight;
#endif

fixed _VPGrassWindSpeed, _VPTreeWindSpeed;


// Tinting =====================================================================

#if defined(USES_TINTING)
	#define VOXELPLAY_TINTCOLOR_DATA fixed4 color : COLOR;
	#define VOXELPLAY_SET_TINTCOLOR(color, i) i.color = color;
	#define VOXELPLAY_OUTPUT_TINTCOLOR(o) o.color = v.color;
	#if defined(VP_CUTOUT)
		#define VOXELPLAY_APPLY_TINTCOLOR(color, i) color.rgb *= i.color.rgb;
	#else
		#define VOXELPLAY_APPLY_TINTCOLOR(color, i) color *= i.color;
	#endif
#else
	#define VOXELPLAY_TINTCOLOR_DATA
	#define VOXELPLAY_SET_TINTCOLOR(color, i)
	#define VOXELPLAY_OUTPUT_TINTCOLOR(o)
	#define VOXELPLAY_APPLY_TINTCOLOR(color, i)
#endif


// Sampling =====================================================================


#if VOXELPLAY_USE_AA
	#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_METAL)
		#define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) tex.SampleGrad (sampler##tex,coord,dx,dy)
	#elif defined(UNITY_COMPILER_HLSL2GLSL) || defined(SHADER_TARGET_SURFACE_ANALYSIS)
		#define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) tex2DArray(tex,coord,dx,dy)
	#else
		#define UNITY_SAMPLE_TEX2DARRAY_GRAD(tex,coord,dx,dy) UNITY_SAMPLE_TEX2DARRAY(tex,coord)
	#endif

    #if defined(NON_ARRAY_TEXTURE)
        inline fixed4 ReadSmoothTexel2D(sampler tex, float2 textureSize, float2 uv) {
            float2 ruv = uv.xy * textureSize - 0.5;
            float2 f = fwidth(ruv);
            uv.xy = (floor(ruv) + 0.5 + saturate( (frac(ruv) - 0.5 + f ) / f)) / textureSize; 
            return tex2D(tex, uv);
        }
        #define VOXELPLAY_GET_TEXEL(uv) ReadSmoothTexel2D(_MainTex, _MainTex_TexelSize.zw, uv.xy)
        #define VOXELPLAY_GET_TEXEL_DD(uv) ReadSmoothTexel2D(_MainTex, _MainTex_TexelSize.zw, uv.xy)
        #define VOXELPLAY_GET_TEXEL_DD_X(tex, textureSize, uv) ReadSmoothTexel2D(tex, textureSize, uv.xy)
    #else
        inline fixed4 ReadSmoothTexel(float3 uv) {
			#if FILTER_MODE != POINT_FILTER
				return UNITY_SAMPLE_TEX2DARRAY_SAMPLER(_MainTex, SAMPLER_MODE, uv);
			#else
				float2 ruv = uv.xy * _MainTex_TexelSize.zw - 0.5;
				float2 f = fwidth(ruv);
				uv.xy = (floor(ruv) + 0.5 + saturate( (frac(ruv) - 0.5 + f ) / f)) / _MainTex_TexelSize.zw; 
				return UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv);
			#endif
        }
        inline fixed4 ReadSmoothTexelWithDerivatives(float3 uv) {
			#if FILTER_MODE != POINT_FILTER
				return UNITY_SAMPLE_TEX2DARRAY_SAMPLER(_MainTex, SAMPLER_MODE, uv);
			#else
	            float2 ruv = frac(uv.xy) * _MainTex_TexelSize.zw - 0.5;
		        float2 f = fwidth(ruv);
			    float2 nuv = (floor(ruv) + 0.5 + saturate( (frac(ruv) - 0.5 + f ) / f)) / _MainTex_TexelSize.zw;    
				return UNITY_SAMPLE_TEX2DARRAY_GRAD(_MainTex, float3(nuv, uv.z), ddx(uv.xy), ddy(uv.xy));
			#endif
        }
	    #define VOXELPLAY_GET_TEXEL(uv) ReadSmoothTexel(uv)
	    #define VOXELPLAY_GET_TEXEL_DD(uv) ReadSmoothTexelWithDerivatives(uv)
    #endif
    #define VOXELPLAY_GET_TEXEL_2D(uv) ReadSmoothTexel2D(uv)

#else // no AA pixels

    #if defined(NON_ARRAY_TEXTURE)
        #define VOXELPLAY_GET_TEXEL(uv) tex2D(_MainTex, uv.xy)
        #define VOXELPLAY_GET_TEXEL_DD(uv) tex2D(_MainTex, uv.xy)
        #define VOXELPLAY_GET_TEXEL_DD_X(sampler, textureSize, uv) tex2D(sampler, uv.xy)
    #else
    	#define VOXELPLAY_GET_TEXEL(uv) UNITY_SAMPLE_TEX2DARRAY(_MainTex, uv.xyz)
   	    #define VOXELPLAY_GET_TEXEL_DD(uv) UNITY_SAMPLE_TEX2DARRAY_SAMPLER(_MainTex, SAMPLER_MODE, uv)
    #endif
    #define VOXELPLAY_GET_TEXEL_2D(uv) tex2D(_MainTex, uv.xy)

#endif

// Outline  =====================================================================

fixed4 _OutlineColor;
fixed _OutlineThreshold;

inline void ApplyOutlineSimple(inout fixed4 color, float2 uv) {
	float2 grd = abs(frac(uv + 0.5) - 0.5);
	grd /= fwidth(uv) * _OutlineThreshold;
	float  lin = 1.0 - saturate(min(grd.x, grd.y));
	color.rgb = lerp(color.rgb, _OutlineColor.rgb, lin * _OutlineColor.a);
}

// Bump Map =====================================================================

#define VOXELPLAY_NEEDS_TANGENT_SPACE VOXELPLAY_USE_PARALLAX || VOXELPLAY_USE_NORMAL || defined(USE_CUSTOM_BUMP_MAP)
#if VOXELPLAY_NEEDS_TANGENT_SPACE
	float3x3 objectToTangent;
	#define VOXELPLAY_SET_TANGENT_SPACE(tang,norm) float3 tang = float3( dot(float2(1,-1), norm.yz), 0, norm.x ); objectToTangent = float3x3( tang, cross(tang, norm), norm );
#else
	#define VOXELPLAY_SET_TANGENT_SPACE(tang,norm)
#endif

UNITY_DECLARE_TEX2D_NOSAMPLER(_BumpMap);

#if defined(USE_CUSTOM_BUMP_MAP)
	// normal map is provided in the material

	inline fixed GetPerVoxelNdotL(float3 normal) {
		return saturate(1.0 + _WorldSpaceLightPos0.y * 2.0);
	}

	#define VOXELPLAY_BUMPMAP_DATA(idx1) float3 tlightDir : TEXCOORD##idx1;
	#define VOXELPLAY_OUTPUT_BUMPMAP_DATA(uv, i) i.tlightDir = mul(objectToTangent, _WorldSpaceLightPos0.xyz);

	fixed GetPerPixelNdotL(float3 tlightDir, float4 rawNorm) {
		float3 nrm  = UnpackNormal(rawNorm);
		nrm.y *= -1.0;
		return saturate(dot(nrm, tlightDir));
	}

	#define VOXELPLAY_APPLY_BUMPMAP(i) i.light.x *= GetPerPixelNdotL(i.tlightDir, _BumpMap.Sample(SAMPLER_NAME, i.uv.xy));
	#define VOXELPLAY_APPLY_CUSTOM_BUMPMAP(i, rawNorm) i.light.x *= GetPerPixelNdotL(i.tlightDir, rawNorm);


#elif VOXELPLAY_USE_NORMAL
	// normal map is included in the texture array

	inline fixed GetPerVoxelNdotL(float3 normal) {
		return saturate(1.0 + _WorldSpaceLightPos0.y * 2.0);
	}

	#define VOXELPLAY_BUMPMAP_DATA(idx1) float3 tlightDir : TEXCOORD##idx1;
	#define VOXELPLAY_OUTPUT_BUMPMAP_DATA(uv, i) i.tlightDir = mul(objectToTangent, _WorldSpaceLightPos0.xyz);

	float GetPerPixelNdotL(float3 tlightDir, float3 uv) {
		float3 nrm  = UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, uv, 0).xyz;
		nrm = nrm * 2.0 - 1.0;
		nrm.y *= -1.0;
		// can apply optional diffuse wrap; disabled to make bumpmap more visible
		//return max(0, dot(nrm, tlightDir) * 0.5 + 0.5);
		return max(0, dot(nrm, tlightDir));
	}
	// frac is necessary because uv are in world space
	#define VOXELPLAY_APPLY_BUMPMAP(i) i.light.x *= GetPerPixelNdotL(i.tlightDir, float3(frac(i.uv.xy), i.uv.z+1));
	#define VOXELPLAY_APPLY_CUSTOM_BUMPMAP(i, rawNorm)
#else
	// no normal mapping

	inline fixed GetPerVoxelNdotL(float3 normal) {
        #if defined(SUN_SCATTERING)
            const float NdotL = 0.7;
        #else
//		    float NdotL = saturate(dot(_WorldSpaceLightPos0.xyz, normal) * 0.5 + 0.5); // commented out to avoid shadow acne

		    float NdotL = saturate(dot(_WorldSpaceLightPos0.xyz, normal));
        #endif
		return NdotL * saturate(1.0 + _WorldSpaceLightPos0.y * 2.0);
	}

	#define VOXELPLAY_BUMPMAP_DATA(idx1)
	#define VOXELPLAY_OUTPUT_BUMPMAP_DATA(uv, i)
	#define VOXELPLAY_APPLY_BUMPMAP(i)
	#define VOXELPLAY_APPLY_CUSTOM_BUMPMAP(i, rawNorm)
#endif

#if VOXELPLAY_PIXEL_LIGHTS || defined(USE_NORMAL)
	#define VOXELPLAY_NORMAL_DATA float3 norm: NORMAL;
#else
	#define VOXELPLAY_NORMAL_DATA
#endif

// Point Lights =====================================================================

CBUFFER_START(VoxelPlayLightBuffers)
float4 _VPPointLightPosition[MAX_LIGHTS];
float4 _VPPointLightColor[MAX_LIGHTS];
int _VPPointLightCount;
CBUFFER_END

float3 ShadePointLights(float3 worldPos, float3 normal, float specularAtten) {
	float3 color = 0;

    #if defined(USES_BRIGHT_POINT_LIGHTS)
	for (int k=0;k<MAX_LIGHTS;k++) {
		if (k<_VPPointLightCount) {
			float3 toLight = _VPPointLightPosition[k].xyz - worldPos;
			float dist = dot(toLight, toLight);
			toLight /= dist + 0.0001;
			float lightAtten = dist / _VPPointLightPosition[k].w;
			float NdL = saturate((dot(normal, toLight) - 1.0) * _VPPointLightColor[k].a + 1.0);
			color += _VPPointLightColor[k].rgb * (NdL / (1.0 + lightAtten));
		}
	}
    #endif

	#if defined(USE_SPECULAR)
		float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos);
		float3 h = normalize (_WorldSpaceLightPos0.xyz + viewDir);
		h *= (sign(_WorldSpaceLightPos0.y) + 1.0) * 0.5; // avoid specular under the horizon
	    float nh = max (0, dot (normal, h));
	    float spec = pow (nh, 32.0);
	    color.rgb += (specularAtten * spec) * _LightColor0.rgb;
	#endif

	return color;
}


float3 ShadePointLights(float3 worldPos, float3 normal) {
	return ShadePointLights(worldPos, normal, 0.0);
}

float3 ShadePointLightsWithoutNormal(float3 worldPos) {
    fixed3 color = 0;
    #if defined(USES_BRIGHT_POINT_LIGHTS)
    for (int k=0;k<32;k++) {
        if (k<_VPPointLightCount) {
            float3 toLight = _VPPointLightPosition[k].xyz - worldPos;
            float dist = dot(toLight, toLight);
            toLight /= dist + 0.0001;
            float atten = dist / _VPPointLightPosition[k].w;
            float NdL = max(abs(toLight.x), abs(toLight.z));
            color += _VPPointLightColor[k].rgb * (NdL / (1.0 + atten));
        }
    }
    #endif
    return color;
}

// Lighting setup  =====================================================================

#if VOXELPLAY_GLOBAL_USE_FOG
    #define VOXELPLAY_FOG_DATA(idx1) fixed4 skyColor: TEXCOORD##idx1;
    #define VOXELPLAY_APPLY_FOG(color, i) color.rgb = lerp(color.rgb, i.skyColor.rgb, i.skyColor.a);
	#if defined(IS_CLOUD)
		#define CLOUD_FOG_DIST_MULTIPLIER 0.25
	#else
		#define CLOUD_FOG_DIST_MULTIPLIER 1.0
	#endif

    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(uv, worldPos, normal) float3 viewDir = worldPos - _WorldSpaceCameraPos; float3 nviewDir = normalize(viewDir); o.skyColor = fixed4(getSkyColor(nviewDir), saturate( (dot(viewDir * CLOUD_FOG_DIST_MULTIPLIER, viewDir * CLOUD_FOG_DIST_MULTIPLIER) - _VPFogData.x) / _VPFogData.y)); o.light = fixed2(GetPerVoxelNdotL(normal), uv.w/(4096.0*15.0)); uv.w = ((int)uv.w & 0x1FF) / 15.0;
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL_NO_GI(worldPos, normal) float3 viewDir = worldPos - _WorldSpaceCameraPos; float3 nviewDir = normalize(viewDir); o.skyColor = fixed4(getSkyColor(nviewDir), saturate( (dot(viewDir * CLOUD_FOG_DIST_MULTIPLIER, viewDir * CLOUD_FOG_DIST_MULTIPLIER) - _VPFogData.x) / _VPFogData.y)); o.light = fixed2(GetPerVoxelNdotL(normal), 0);
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG(uv, worldPos) float3 viewDir = worldPos - _WorldSpaceCameraPos; float3 nviewDir = normalize(viewDir); float3 normal = -nviewDir; o.skyColor = fixed4(getSkyColor(nviewDir), saturate((dot(viewDir * CLOUD_FOG_DIST_MULTIPLIER, viewDir * CLOUD_FOG_DIST_MULTIPLIER) - _VPFogData.x) / _VPFogData.y)); o.light = fixed2(GetPerVoxelNdotL(normal), uv.w/(4096.0*15.0)); uv.w = ((int)uv.w & 0x1FF) / 15.0;

#else // fallbacks when fog is disabled

    #define VOXELPLAY_FOG_DATA(idx1)
    #define VOXELPLAY_APPLY_FOG(color, i)
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(uv, worldPos, normal) o.light = fixed2(GetPerVoxelNdotL(normal), uv.w/(4096.0*15.0)); uv.w = ((int)uv.w & 0x1FF) / 15.0;
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL_NO_GI(worldPos, normal) o.light = fixed2(GetPerVoxelNdotL(normal), 0);
    #define VOXELPLAY_INITIALIZE_LIGHT_AND_FOG(uv, worldPos) float3 viewDir = _WorldSpaceCameraPos - worldPos; float3 normal = normalize(viewDir); o.light = fixed2(GetPerVoxelNdotL(normal), uv.w/(4096.0*15.0)); uv.w = ((int)uv.w & 0x1FF) / 15.0;

#endif // VOXELPLAY_GLOBAL_USE_FOG

#define TORCH_LIGHT_CONTRIBUTION i.light.yyy

#if VOXELPLAY_PIXEL_LIGHTS
	#define VOXELPLAY_LIGHT_DATA(idx1,idx2) fixed2 light: TEXCOORD##idx1; float3 wpos: TEXCOORD##idx2;
	#if VOXELPLAY_USE_AO || defined(USE_SPECULAR)
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal) i.wpos = worldPos; i.norm = normal;
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos) i.wpos = worldPos;
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal)
	#else
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal)
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos)
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal) i.wpos = worldPos; i.norm = normal;
	#endif
	#define VOXELPLAY_SET_LIGHT(i, worldPos, normal) i.wpos = worldPos; i.norm = normal;
	#define VOXELPLAY_SET_LIGHT_WITHOUT_NORMAL(i, worldPos) i.wpos = worldPos;
	#define VOXELPLAY_VERTEX_LIGHT_COLOR(specularAtten) ShadePointLights(i.wpos, i.norm, specularAtten)
#else
	#define VOXELPLAY_LIGHT_DATA(idx1,idx2) fixed2 light: TEXCOORD##idx1; fixed3 vertexLightColor: TEXCOORD##idx2;
	#if VOXELPLAY_USE_AO
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal) i.vertexLightColor = ShadePointLights(worldPos, normal);
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos) i.vertexLightColor = ShadePointLights(worldPos, normal);
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal)
	#else
		#define VOXELPLAY_SET_VERTEX_LIGHT(i, worldPos, normal)
		#define VOXELPLAY_SET_VERTEX_LIGHT_WITHOUT_NORMAL(i, worldPos)
		#define VOXELPLAY_SET_FACE_LIGHT(i, worldPos, normal) i.vertexLightColor = ShadePointLights(worldPos, normal);
	#endif
	#define VOXELPLAY_SET_LIGHT(i, worldPos, normal) i.vertexLightColor = ShadePointLights(worldPos, normal);
	#define VOXELPLAY_SET_LIGHT_WITHOUT_NORMAL(i, worldPos) i.vertexLightColor = ShadePointLightsWithoutNormal(worldPos);
	#define VOXELPLAY_VERTEX_LIGHT_COLOR(atten) i.vertexLightColor
#endif

// Fresnel  =====================================================================

#if defined(USES_FRESNEL) && (VOXELPLAY_PIXEL_LIGHTS || defined(FRESNEL_USES_CUSTOM_NORMAL))
	float _FresnelExponent;
	fixed3 _FresnelColor;
	fixed3 ComputeFresnel(float3 norm, float3 wpos) { // diffuse fresnel
		float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - wpos);
		float fresnel = 1.0 - max(0, dot(norm, viewDir));
		fresnel = pow(fresnel, _FresnelExponent);
		return fresnel * _FresnelColor;
	}
	#define VOXELPLAY_FRESNEL_DATA(idx1) float3 wpos : TEXCOORD##idx1; 
	#define VOXELPLAY_OUTPUT_FRESNEL_DATA(o, worldPos) o.wpos = worldPos;
	#define VOXELPLAY_APPLY_FRESNEL(color, normal, worldPos) color.rgb += ComputeFresnel(normal, worldPos);
#else
	#define VOXELPLAY_FRESNEL_DATA(idx1) 
	#define VOXELPLAY_OUTPUT_FRESNEL_DATA(o, worldPos)
	#define VOXELPLAY_APPLY_FRESNEL(color, normal, worldPos)
#endif


#if defined(SUBTLE_SELF_SHADOWS)
    fixed _CustomDaylightShadowAtten;
    #define _VPDaylightShadowAtten _CustomDaylightShadowAtten
#else
    fixed _VPDaylightShadowAtten;
#endif

#define VOXELPLAY_APPLY_LIGHTING(color,i) fixed atten = VOXELPLAY_LIGHT_ATTENUATION(i); color.rgb *= min(atten * _LightColor0.rgb + TORCH_LIGHT_CONTRIBUTION, 1.2) + VOXELPLAY_VERTEX_LIGHT_COLOR(atten * UNITY_SHADOW_ATTEN(i) );
#define VOXELPLAY_APPLY_LIGHTING_AO_AND_GI(color,i) fixed atten = VOXELPLAY_LIGHT_ATTENUATION(i); float ao = i.uv.w; AO_FUNCTION; color.rgb *= min((atten * ao) * _LightColor0.rgb + TORCH_LIGHT_CONTRIBUTION, 1.2) + VOXELPLAY_VERTEX_LIGHT_COLOR(i.uv.w * UNITY_SHADOW_ATTEN(i) );
#define VOXELPLAY_APPLY_LIGHTING_AND_GI(color,i) fixed atten = VOXELPLAY_LIGHT_ATTENUATION(i); float ao = i.uv.w; AO_FUNCTION; color.rgb *= min((atten * ao) * _LightColor0.rgb + TORCH_LIGHT_CONTRIBUTION, 1.2) + VOXELPLAY_VERTEX_LIGHT_COLOR(i.uv.w * UNITY_SHADOW_ATTEN(i) );

// Integrated emission with animation =============================================

#if defined(USE_EMISSION)
    fixed _VPEmissionIntensity;
    #define VOXELPLAY_COMPUTE_EMISSION(color) fixed3 emissionColor = color.rgb * ( _VPEmissionIntensity * (1.0 - color.a) );
    #define VOXELPLAY_ADD_EMISSION(color) color.rgb += emissionColor;
#else
    #define VOXELPLAY_COMPUTE_EMISSION(color)
    #define VOXELPLAY_ADD_EMISSION(color)
#endif // EMISSION


#define VOXELPLAY_OUTPUT_UV(x, o) o.uv = (x); 



#if VOXELPLAY_USE_PARALLAX

	float _VPParallaxStrength, _VPParallaxMaxDistanceSqr;
	int _VPParallaxIterations, _VPParallaxIterationsBinarySearch;

	float GetParallaxHeight (float3 uv, float2 uvOffset) {
		return UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, float3(uv.xy + uvOffset, uv.z), 0).a;
	}

	void ApplyParallax(float4 tangentViewDir, inout float3 uv) {

		if (tangentViewDir.w == 0) return; // do not apply parallax to remote pixels

		float3 tviewDir = normalize(tangentViewDir.xyz);
		float2 uvDir = tviewDir.xy / (tviewDir.z + 0.42);
		float stepSize = 1.0 / _VPParallaxIterations;
		float2 uvInc = uvDir * (stepSize * _VPParallaxStrength);

		float2 uvOffset = 0;

		float stepHeight = 1;

		// get the texture index for displacement map
		uv.z ++;
		float surfaceHeight = UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, uv, 0).a;

		float2 prevUVOffset = uvOffset;
		float prevStepHeight = stepHeight;
		float prevSurfaceHeight = surfaceHeight;

		for (int i1 = 1; i1 < _VPParallaxIterations && stepHeight > surfaceHeight; i1++) {
			prevUVOffset = uvOffset;
			prevStepHeight = stepHeight;
			prevSurfaceHeight = surfaceHeight;
			uvOffset -= uvInc;
			stepHeight -= stepSize;
			surfaceHeight = GetParallaxHeight(uv, uvOffset);
		}

		for (int i2 = 0; i2 < _VPParallaxIterationsBinarySearch; i2++) {
			uvInc *= 0.5;
			stepSize *= 0.5;

			if (stepHeight < surfaceHeight) {
				uvOffset += uvInc;
				stepHeight += stepSize;
			} else {
				uvOffset -= uvInc;
				stepHeight -= stepSize;
			}
			surfaceHeight = GetParallaxHeight(uv, uvOffset);
		}

		uv.xy += uvOffset;
		uv.z --;
	}

	#define VOXELPLAY_PARALLAX_DATA(idx1) float4 tviewDir : TEXCOORD##idx1; 
	#define VOXELPLAY_OUTPUT_PARALLAX_DATA(v, uv, i) float pdistSqr = dot(wpos - _WorldSpaceCameraPos.xyz, wpos - _WorldSpaceCameraPos.xyz); float3 invViewDir = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz - v.vertex.xyz; i.tviewDir = float4(mul(objectToTangent, invViewDir), step(pdistSqr, _VPParallaxMaxDistanceSqr));
	#define VOXELPLAY_APPLY_PARALLAX(i) ApplyParallax(i.tviewDir, i.uv.xyz);
#else
	#define VOXELPLAY_PARALLAX_DATA(idx1) 
	#define VOXELPLAY_OUTPUT_PARALLAX_DATA(worldPos, uv, i) 
	#define VOXELPLAY_APPLY_PARALLAX(i)
#endif // VOXELPLAY_USE_PARALLAX

#if VOXELPLAY_USE_OUTLINE
	#define VOXELPLAY_APPLY_OUTLINE_SIMPLE(color, i) ApplyOutlineSimple(color, i.uv.xy);
#else
	#define VOXELPLAY_APPLY_OUTLINE_SIMPLE(color, i)
#endif

#endif // VOXELPLAY_COMMON_CORE

