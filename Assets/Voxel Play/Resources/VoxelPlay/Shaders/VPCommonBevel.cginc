#ifndef VOXELPLAY_COMMON_BEVEL
#define VOXELPLAY_COMMON_BEVEL

#if VOXELPLAY_PIXEL_LIGHTS && defined(USES_BEVEL)

	#define BEVEL_WIDTH 0
	float ApplyBevel(int bevel, float3 wpos, float3 norm) {
		float2 fuv = frac(wpos.xz) * 2.0 - 1;
		float3 orientedNormal = norm;
		orientedNormal.xz += fuv;

		if (orientedNormal.x < 0) {
			orientedNormal.x *= bevel & 1;
		} else {
			orientedNormal.x *= (bevel & 2) >> 1;
		}
		if (orientedNormal.z > 0) {
			orientedNormal.z *= (bevel & 4) >> 2;
		} else {
			orientedNormal.z *= bevel >> 3;
		}
		float3 absNorm = abs(orientedNormal);
		orientedNormal.xz *= absNorm.xz > absNorm.zx;
		orientedNormal.y = 0;

		float t = max(absNorm.x, absNorm.z);
		t = saturate( (t - BEVEL_WIDTH) / (1.0 - BEVEL_WIDTH) );
		
		norm = lerp(norm, orientedNormal, t);
		return GetPerVoxelNdotL(normalize(norm));
	}

	#define VOXELPLAY_COMPUTE_BEVEL(i) int iuvz = round(i.uv.z); int bevel = iuvz >> 14; i.uv.z = iuvz & ((1<<14)-1);
	#define VOXELPLAY_APPLY_BEVEL(i) if (bevel>0) i.light.x = ApplyBevel(bevel, i.wpos, i.norm);
#else
	#define VOXELPLAY_COMPUTE_BEVEL(i)
	#define VOXELPLAY_APPLY_BEVEL(i)
#endif

#endif // VOXELPLAY_COMMON_BEVEL

