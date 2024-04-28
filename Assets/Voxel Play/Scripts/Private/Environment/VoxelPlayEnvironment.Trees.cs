using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

    public delegate bool TreeBeforeCreateEvent(Vector3d position);
    public delegate void TreeAfterCreateEvent(List<VoxelIndex> indices);


    public partial class VoxelPlayEnvironment : MonoBehaviour {

        struct TreeRequest {
            public VoxelChunk chunk;
            public Vector3d chunkOriginalPosition;
            public Vector3d rootPosition;
            public ModelDefinition tree;
        }

        const int TREES_CREATION_BUFFER_SIZE = 20000;

        TreeRequest[] treeRequests;
        int treeRequestLast, treeRequestFirst;
        List<VoxelChunk> treeChunkRefreshRequests;

        void InitTrees() {
            if (treeRequests == null || treeRequests.Length != TREES_CREATION_BUFFER_SIZE) {
                treeRequests = new TreeRequest[TREES_CREATION_BUFFER_SIZE];
            }
            treeRequestLast = -1;
            treeRequestFirst = -1;
            if (treeChunkRefreshRequests == null) {
				treeChunkRefreshRequests = new List<VoxelChunk> ();
            } else {
                treeChunkRefreshRequests.Clear();
            }
        }


        public ModelDefinition GetTree(BiomeTree[] trees, float random) {
            float acumProb = 0;
            int index = 0;
            for (int t = 0; t < trees.Length; t++) {
                acumProb += trees[t].probability;
                if (random < acumProb) {
                    index = t;
					break;
				}
			}
			return trees [index].tree;
        }

        /// <summary>
        /// Requests the tree creation.
        /// </summary>
        /// <param name="position">Position.</param>
        public void RequestTreeCreation(VoxelChunk chunk, Vector3d position, ModelDefinition treeModel) {
            if (treeModel == null || !enableTrees)
                return;

            treeRequestLast++;
            if (treeRequestLast >= treeRequests.Length) {
                treeRequestLast = 0;
			}
			if (treeRequestLast != treeRequestFirst) {
				treeRequests [treeRequestLast].chunk = chunk;
				treeRequests [treeRequestLast].chunkOriginalPosition = chunk.position;
				treeRequests [treeRequestLast].rootPosition = position;
				treeRequests [treeRequestLast].tree = treeModel; // trees[index].tree;
				treesInCreationQueueCount++;
			} else {
				ShowMessage ("New trees request buffer exhausted.");
            }
        }

        /// <summary>
        /// Monitors queue of new trees requests. This function calls CreateTree to create the tree data and pushes a chunk refresh.
        /// </summary>
        void CheckTreeRequests(float endTime) {
            int max = maxTreesPerFrame > 0 ? maxTreesPerFrame : 10000;
            for (int k = 0; k < max; k++) {
                if (treeRequestFirst == treeRequestLast)
                    return;
                treeRequestFirst++;
                if (treeRequestFirst >= treeRequests.Length) {
					treeRequestFirst = 0;
				}
				treesInCreationQueueCount--;
				VoxelChunk chunk = treeRequests [treeRequestFirst].chunk;
				if ((object)chunk != null && chunk.allowTrees && chunk.position == treeRequests [treeRequestFirst].chunkOriginalPosition) {
					CreateTree (treeRequests [treeRequestFirst].rootPosition, treeRequests [treeRequestFirst].tree);
				}
                long elapsed = stopWatch.ElapsedMilliseconds;
                if (elapsed >= endTime)
                    break;
            }
        }

        void CreateTree(Vector3d position, ModelDefinition tree) {

            if ((object)tree == null) {
                return;
			}
			if (OnTreeBeforeCreate != null) {
				if (!OnTreeBeforeCreate (position)) {
					return;
				}
			}

			int rotation = WorldRand.Range (0, 4, position);
			Vector3d pos;
			treeChunkRefreshRequests.Clear ();
			VoxelChunk lastChunk = null;
			int modelOneYRow = tree.sizeZ * tree.sizeX;
			int modelOneZRow = tree.sizeX;
			int halfSizeX = tree.sizeX / 2;
			int halfSizeZ = tree.sizeZ / 2;

			VoxelIndex index = new VoxelIndex ();
			bool informIndices = false;
			List<VoxelIndex> tempVoxelIndices = BufferPool<VoxelIndex>.Get ();
			if (OnTreeAfterCreate != null) {
				informIndices = true;
			}

			float rotationDegrees = Voxel.GetTextureRotationDegrees(rotation);
			Vector3 zeroPos = Quaternion.Euler(0, rotationDegrees, 0) * new Vector3(-halfSizeX, 0, -halfSizeZ);

			for (int b = 0; b < tree.bits.Length; b++) {

				int bitIndex = tree.bits [b].voxelIndex;
				int py = bitIndex / modelOneYRow;
				int remy = bitIndex - py * modelOneYRow;
				int pz = remy / modelOneZRow;
				int px = remy - pz * modelOneZRow;
				float wx = zeroPos.x, wz = zeroPos.z;

				// Random rotation
				switch (rotation) {
					case 1:
						wx += pz;
						wz -= px;
						break;
					case 2:
						wx -= px;
						wz -= pz;
						break;
					case 3:
						wx -= pz;
						wz += px;
						break;
					default:
						wx += px;
						wz += pz;
						break;
				}

				pos.x = position.x + tree.offsetX + wx;
				pos.y = position.y + tree.offsetY + py;
				pos.z = position.z + tree.offsetZ + wz;

                if (GetVoxelIndex (pos, out VoxelChunk chunk, out int voxelIndex)) {
					if (!chunk.modified && (chunk.voxels [voxelIndex].opaque < FULL_OPAQUE || voxelDefinitions [chunk.voxels [voxelIndex].typeIndex].renderType == RenderType.CutoutCross)) {
						VoxelDefinition treeVoxel = tree.bits [b].voxelDefinition;
						if (treeVoxel == null) {
							treeVoxel = defaultVoxel;
						}
						chunk.voxels [voxelIndex].Set (treeVoxel, tree.bits [b].finalColor);
						if (informIndices) {
							index.chunk = chunk;
							index.voxelIndex = voxelIndex;
							index.position = pos;
                            tempVoxelIndices.Add(index);
                        }
                        if (py == 0) {
                            if (tree.fitToTerrain) {
                                Vector3d under = pos;
                                under.y -= 1;
                                float terrainAltitude = GetTerrainHeight(under);
                                for (int k = 0; k < 100; k++, under.y--) {
                                    GetVoxelIndex(under, out VoxelChunk bottomChunk, out int vindex);
                                    if (under.y < terrainAltitude || (object)bottomChunk == null || bottomChunk.voxels[vindex].opaque == 15) break;

                                    bottomChunk.voxels[vindex].Set(treeVoxel, tree.bits[b].finalColor);
                                    if (informIndices) {
                                        index.chunk = bottomChunk;
                                        index.voxelIndex = vindex;
                                        index.position = pos;
                                        index.position.y--;
                                        tempVoxelIndices.Add(index);
                                    }
                                    if (!treeChunkRefreshRequests.Contains(bottomChunk)) {
                                        treeChunkRefreshRequests.Add(bottomChunk);
                                    }
                                }
                            } else {
                                // fills one voxel beneath with tree voxel to avoid the issue of having some trees floating on some edges/corners
                                if (voxelIndex >= ONE_Y_ROW) {
								if (chunk.voxels [voxelIndex - ONE_Y_ROW].hasContent != 1) {
									chunk.voxels [voxelIndex - ONE_Y_ROW].Set (treeVoxel, tree.bits [b].finalColor);
									if (informIndices) {
										index.chunk = chunk;
										index.voxelIndex = voxelIndex - ONE_Y_ROW;
										index.position = pos;
										index.position.y--;
										tempVoxelIndices.Add (index);
                                        }
                                    }
                                } else {
                                    VoxelChunk bottomChunk = chunk.bottom;
                                    if ((object)bottomChunk != null && !bottomChunk.modified) {
                                        int bottomIndex = voxelIndex + (CHUNK_SIZE - 1) * ONE_Y_ROW;
                                        if (bottomChunk.voxels[bottomIndex].hasContent != 1) {
                                            chunk.voxels[bottomIndex].Set(treeVoxel, tree.bits[b].finalColor);
                                            if (informIndices) {
                                                index.chunk = bottomChunk;
                                                index.voxelIndex = bottomIndex;
                                                index.position = pos;
                                                index.position.y--;
                                                tempVoxelIndices.Add(index);
                                            }
                                            if (!treeChunkRefreshRequests.Contains(bottomChunk)) {
                                                treeChunkRefreshRequests.Add(bottomChunk);
                                            }
                                        }
                                    }
                                }
                            }
                        }
						if (chunk != lastChunk) {
							lastChunk = chunk;
							if (tree.exclusiveTree) chunk.allowTrees = false;
							if (!chunk.inqueue && !treeChunkRefreshRequests.Contains (chunk)) {
								treeChunkRefreshRequests.Add (chunk);
							}
						}
					}
				}
			}
			treesCreated++;

			ModelPlaceTorches(position, tree, rotation);

			if (informIndices) {
				OnTreeAfterCreate (tempVoxelIndices);
			}

			BufferPool<VoxelIndex>.Release (tempVoxelIndices);

			int refreshChunksCount = treeChunkRefreshRequests.Count;
			for (int k = 0; k < refreshChunksCount; k++) {
				ChunkRequestRefresh (treeChunkRefreshRequests [k], false, true);
			}
		}

	}



}
