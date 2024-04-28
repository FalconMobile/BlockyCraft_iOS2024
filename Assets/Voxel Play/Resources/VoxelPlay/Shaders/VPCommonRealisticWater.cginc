#ifndef VOXELPLAY_REALISTIC_WATER
#define VOXELPLAY_REALISTIC_WATER

#define FOAM_SIZE 0.4

float _WaveScale, _WaveSpeed, _WaveAmplitude, _RefractionDistortion, _Fresnel;
half _SpecularPower, _SpecularIntensity, _NormalStrength;
half4 _WaterColor, _UnderWaterFogColor;
half3 _FoamColor;
half3 _OceanWave;
sampler2D _WaterBackgroundTexture;
sampler2D _ReflectiveColor;
sampler2D _FoamTex;
sampler2D _FoamGradient;

inline half3 GetWaterNormal(float2 uv) {
    return UnpackNormal(_BumpMap.Sample(SAMPLER_NAME, uv));
    //return UnpackNormal(_BumpMap.Sample(sampler_Trilinear_Repeat, uv));
}


#endif // VOXELPLAY_REALISTIC_WATER

