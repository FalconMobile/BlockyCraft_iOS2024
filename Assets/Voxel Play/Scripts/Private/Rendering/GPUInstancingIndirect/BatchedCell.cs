using UnityEngine;


namespace VoxelPlay.GPURendering.InstancingIndirect {

	public class BatchedCell {
		public bool rebuild;
		public int lastRebuildFrame;
		public Vector3 boundsMin, boundsMax;

		internal FastList<InstancedChunk> instancedChunks;
		internal FastIndexedList<VoxelDefinition, BatchedMesh> batchedMeshes;

		public BatchedCell (Vector3 cellPosition, float cellSize) {
			cellPosition *= cellSize;
			boundsMin = cellPosition;
			boundsMax = cellPosition + new Vector3 (cellSize, cellSize, cellSize);
			batchedMeshes = new FastIndexedList<VoxelDefinition, BatchedMesh> ();
			instancedChunks = new FastList<InstancedChunk> ();
		}

		public void ClearBatches () {
			for (int k = 0; k <= batchedMeshes.lastIndex; k++) {
				BatchedMesh batchedMesh = batchedMeshes.values [k];
				if (batchedMesh != null) {
					batchedMesh.batches.Clear ();
					batchedMesh.lastBatch = null;
				}
			}
		}

		public void DisposeBuffers() {
			if (batchedMeshes == null)
				return;
			for (int k = 0; k <= batchedMeshes.lastIndex; k++) {
				BatchedMesh batchedMesh = batchedMeshes.values [k];
				if (batchedMesh != null) {
					batchedMesh.DisposeBuffers ();
				}
			}

		}

	}
}
