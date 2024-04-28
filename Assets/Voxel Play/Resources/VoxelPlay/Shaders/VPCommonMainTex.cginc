#ifndef VOXELPLAY_COMMON_MAINTEX
#define VOXELPLAY_COMMON_MAINTEX

#ifndef NON_ARRAY_TEXTURE
    UNITY_DECLARE_TEX2DARRAY(_MainTex); 
#else
    sampler _MainTex;
#endif

#ifndef SURFACE_SHADER
    float4 _MainTex_ST;
#endif

float4 _MainTex_TexelSize;

#endif // VOXELPLAY_COMMON_MAINTEX

