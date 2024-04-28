using System.Collections.Generic;
using UnityEngine;


namespace VoxelPlay.GPULighting {

	public class VoxelPlayLightManager : MonoBehaviour {

		const int MAX_LIGHTS = 32; // also given by shader buffer length
		int lastX, lastY, lastZ;
		List<Light> lights;
		Vector3 camPos;
		Vector4[] lightPosBuffer;
		Vector4[] lightColorBuffer;
		bool rebuildBuffer;
		VoxelPlayEnvironment env;

		static class ShaderParams
        {
			public static int GlobalLightPositionsArray = Shader.PropertyToID ("_VPPointLightPosition");
			public static int GlobalLightColorsArray = Shader.PropertyToID ("_VPPointLightColor");
			public static int GlobalLightCount = Shader.PropertyToID ("_VPPointLightCount");
        }


        void OnEnable () {
            if (lights == null) {
                lights = new List<Light> ();
            }
            if (lightPosBuffer == null || lightPosBuffer.Length < MAX_LIGHTS) {
                lightPosBuffer = new Vector4 [MAX_LIGHTS];
            }
            if (lightColorBuffer == null || lightColorBuffer.Length < MAX_LIGHTS) {
                lightColorBuffer = new Vector4 [MAX_LIGHTS];
            }
        }

        private void Start () { 
            env = VoxelPlayEnvironment.instance;
            if (env != null) {
                if (!VoxelPlayEnvironment.supportsBrightPointLights) {
                    DestroyImmediate (this);
                    return;
                }
                env.OnTorchAttached += (chunk, lightSource) => rebuildBuffer = true;
                env.OnTorchDetached += (chunk, lightSource) => rebuildBuffer = true;
                env.OnChunkRender += (chunk) => rebuildBuffer = true;
                env.OnLightRefreshRequest += () => rebuildBuffer = true;
            }
		}

        void OnPreRender() {
            camPos = env.currentAnchorPosWS;
            int x, y, z;
            FastMath.FloorToInt(camPos.x, camPos.y, camPos.z, out x, out y, out z);
			x >>= 3;
			y >>= 3;
			z >>= 3;
			if (lastX == x && lastY == y && lastZ == z)
				return;
			lastX = x;
			lastY = y;
			lastZ = z;
			rebuildBuffer = true;
		}

		void LateUpdate() {
			if (rebuildBuffer) {
				FetchLights ();
			}
			UpdateLights ();
		}

		void FetchLights() {
			rebuildBuffer = false;
			lights.Clear ();
			Light[] sceneLights = FindObjectsOfType<Light> ();
			for (int k = 0; k < sceneLights.Length; k++) {
				if (sceneLights [k].isActiveAndEnabled && sceneLights [k].type == LightType.Point) {
					lights.Add (sceneLights [k]);
				}
			}
			lights.Sort (distanceComparer);
		}

		void UpdateLights() {
			int lightCount = lights.Count;
			float worldLightIntensity = Mathf.Max(env.world.lightIntensityMultiplier, 0);
			float worldLightScattering = Mathf.Max(env.world.lightScattering, 0);
			for (int k = 0; k < MAX_LIGHTS; k++) {
				if (k < lightCount) {
					Light light = lights [k];
					if (light != null) {
						Vector3 lightPos = light.transform.position;
						lightPosBuffer [k].x = lightPos.x;
						lightPosBuffer [k].y = lightPos.y;
						lightPosBuffer [k].z = lightPos.z;
						lightPosBuffer [k].w = 0.0001f + light.range * worldLightScattering;
						Color color = light.color;
						float intensity = light.intensity * worldLightIntensity;
						lightColorBuffer [k].x = color.r * intensity;
						lightColorBuffer [k].y = color.g * intensity;
						lightColorBuffer [k].z = color.b * intensity;
						lightColorBuffer [k].w = color.a;
						continue;
					}
				}
				lightPosBuffer [k].x = float.MaxValue;
				lightPosBuffer [k].y = float.MaxValue;
				lightPosBuffer [k].z = float.MaxValue;
				lightPosBuffer [k].w = 1.0f;
				lightColorBuffer [k].x = 0;
				lightColorBuffer [k].y = 0;
				lightColorBuffer [k].z = 0;
				lightColorBuffer [k].w = 0;
			}
			Shader.SetGlobalVectorArray (ShaderParams.GlobalLightPositionsArray, lightPosBuffer);
			Shader.SetGlobalVectorArray (ShaderParams.GlobalLightColorsArray, lightColorBuffer);
			Shader.SetGlobalInt (ShaderParams.GlobalLightCount, lightCount);
		}


		int distanceComparer(Light a, Light b) {
			Vector3 posA = a.transform.position;
			Vector3 posB = b.transform.position;
			float distA = FastVector.SqrDistance (ref camPos, ref posA);
			float distB = FastVector.SqrDistance (ref camPos, ref posB);
			if (distA < distB)
				return -1;
			if (distA > distB)
				return 1;
			return 0;
		}

	}

}
