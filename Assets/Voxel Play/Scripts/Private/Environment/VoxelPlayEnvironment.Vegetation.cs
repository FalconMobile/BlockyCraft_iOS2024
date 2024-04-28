using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		struct VegetationRequest {
			public Vector3d position;
			public VoxelDefinition vd;
		}

		const int VEGETATION_CREATION_BUFFER_SIZE = 20000;

		VegetationRequest[] vegetationRequests;
		int vegetationRequestLast, vegetationRequestFirst;

		void InitVegetation () {
			if (vegetationRequests == null || vegetationRequests.Length != VEGETATION_CREATION_BUFFER_SIZE) {
				vegetationRequests = new VegetationRequest[VEGETATION_CREATION_BUFFER_SIZE];
			}
			vegetationRequestLast = -1;
			vegetationRequestFirst = -1;
		}

		/// <summary>
		/// Requests the vegetation creation.
		/// </summary>
		public void RequestVegetationCreation (Vector3d position, VoxelDefinition vd) { 
			if (!enableVegetation) {
				return;
			}

			vegetationRequestLast++;
			if (vegetationRequestLast >= vegetationRequests.Length) {
				vegetationRequestLast = 0;
			}
			if (vegetationRequestLast != vegetationRequestFirst) {
				vegetationRequests[vegetationRequestLast].position = position;
				vegetationRequests [vegetationRequestLast].vd = vd;
				vegetationInCreationQueueCount++;
			}
		}

		/// <summary>
		/// Monitors queue of new vegetations requests. This function calls Createvegetation to create the vegetation data and pushes a chunk refresh.
		/// </summary>
		void CheckVegetationRequests (long endTime) {
			int max = maxBushesPerFrame > 0 ? maxBushesPerFrame : 10000;
			for (int k = 0; k < max; k++) {
				if (vegetationRequestFirst == vegetationRequestLast)
					return;
				vegetationRequestFirst++;
				if (vegetationRequestFirst >= vegetationRequests.Length) {
					vegetationRequestFirst = 0;
				}
				vegetationInCreationQueueCount--;

				if (GetVoxelIndex(vegetationRequests[vegetationRequestFirst].position, out VoxelChunk chunk, out int voxelIndex)) {
					if (!chunk.modified && chunk.voxels[voxelIndex].opaque < FULL_OPAQUE) {
						CreateVegetation(chunk, voxelIndex, vegetationRequests[vegetationRequestFirst].vd);
					}
				}
				long elapsed = stopWatch.ElapsedMilliseconds;
				if (elapsed >= endTime)
					break;
			}
		}

		/// <summary>
		/// Gets the vegetation voxel based on position, biome and a random value
		/// </summary>
		/// <returns>The vegetation.</returns>
		/// <param name="biome">Biome.</param>
		/// <param name="random">Random.</param>
		public VoxelDefinition GetVegetation (BiomeDefinition biome, float random) {
			float acumProb = 0;
			int index = 0;
			for (int t = 0; t < biome.vegetation.Length; t++) {
				acumProb += biome.vegetation [t].probability;
				if (random < acumProb) {
					index = t;
					break;
				}
			}
			return biome.vegetation [index].vegetation;
		}


		/// <summary>
		/// Gets the underwater vegetation voxel based on position, biome and a random value
		/// </summary>
		/// <returns>The vegetation.</returns>
		/// <param name="biome">Biome.</param>
		/// <param name="random">Random.</param>
		public VoxelDefinition GetUnderwaterVegetation (BiomeDefinition biome, float random)
		{
			float acumProb = 0;
			int index = 0;
			for (int t = 0; t < biome.underwaterVegetation.Length; t++) {
				acumProb += biome.underwaterVegetation [t].probability;
				if (random < acumProb) {
					index = t;
					break;
				}
			}
			return biome.underwaterVegetation [index].vegetation;
		}

		/// <summary>
		/// Creates the vegetation.
		/// </summary>
		void CreateVegetation (VoxelChunk chunk, int voxelIndex, VoxelDefinition vd) {

			if ((object)chunk != null) {
				// Updates current chunk
				if (chunk.allowTrees && chunk.voxels [voxelIndex].opaque < 15) {
					chunk.voxels [voxelIndex].Set (vd);
					vegetationCreated++;
					ChunkRequestRefresh(chunk, false, true);
				}
			}
		}


	}



}
