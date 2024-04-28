using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[CreateAssetMenu(menuName = "Voxel Play/Detail Generators/Prefab Spawner", fileName = "PrefabSpawner", order = 103)]
	public class PrefabSpawner : VoxelPlayDetailGenerator {

		public float seed = 1;

        [Range(0,1f)]
		public float spawnProbability = 0.02f;
		public BiomeDefinition[] allowedBiomes;

		public GameObject[] prefabs;

		public bool optimizeMaterial = true;

		VoxelPlayEnvironment env;
		Shader vpShader;

		/// <summary>
		/// Initialization method. Called by Voxel Play at startup.
		/// </summary>
		public override void Init() {
			env = VoxelPlayEnvironment.instance;
			vpShader = Shader.Find("Voxel Play/Models/Texture/Opaque");
		}


		/// <summary>
		/// Fills the given chunk with detail. Filled voxels won't be replaced by the terrain generator.
		/// Use Voxel.Empty to fill with void.
		/// </summary>
		/// <param name="chunk">Chunk.</param>
		public override void AddDetail(VoxelChunk chunk) {

			if (prefabs == null || prefabs.Length == 0) return;
			Vector3d position = chunk.position;
			Vector3d rndPos = position;
			rndPos.x += seed;
			if (WorldRand.GetValue(rndPos) > spawnProbability) return;

			BiomeDefinition biome = env.GetBiome(position);
			if (allowedBiomes != null) {
				for (int k = 0; k < allowedBiomes.Length; k++) {
					if (allowedBiomes[k] == biome) {
						SpawnPrefab(position);
						return;
					}
				}
			}
		}

		void SpawnPrefab(Vector3d position) {

			int prefabIndex = WorldRand.Range(0, prefabs.Length);
			position.x += WorldRand.Range(0, VoxelPlayEnvironment.CHUNK_SIZE) - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
			position.z += WorldRand.Range(0, VoxelPlayEnvironment.CHUNK_SIZE) - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
			position.y = env.GetTerrainHeight(position);

			GameObject o = Instantiate(prefabs[prefabIndex]);

			if (optimizeMaterial) {
				Renderer r = o.GetComponentInChildren<Renderer>();
				if (r != null) {
					Material oldMat = r.sharedMaterial;
					if (oldMat != null && !oldMat.shader.name.Contains ("Voxel Play/Models")) {
						if (vpShader != null)
						{
							Material newMat = new Material(vpShader);
							newMat.mainTexture = oldMat.mainTexture;
							newMat.color = oldMat.color;
							r.sharedMaterial = newMat;
						}
					}
				}
            }
			o.transform.position = position;

			VoxelPlayBehaviour bh = o.GetComponentInChildren<VoxelPlayBehaviour> ();
			if (bh == null) {
				o.AddComponent<VoxelPlayBehaviour> ();
			}
			
		}

	}

}