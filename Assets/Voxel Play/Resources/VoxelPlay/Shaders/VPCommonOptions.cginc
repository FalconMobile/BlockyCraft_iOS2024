#ifndef VOXELPLAY_COMMON_OPTIONS
#define VOXELPLAY_COMMON_OPTIONS

//#define USES_TINTING

//#define USES_SEE_THROUGH

//#define USES_BRIGHT_POINT_LIGHTS

float _VPObscuranceIntensity;
#define AO_FUNCTION ao = 1.05-(1.0-ao)*(1.0-ao)

//#define USES_FRESNEL

//#define USES_BEVEL

#define POINT_FILTER 1
#define BILINEAR_FILTER 2
#define TRILINEAR_FILTER 3
#define FILTER_MODE 1

#define MAX_LIGHTS 32

#endif // VOXELPLAY_COMMON_OPTIONS

