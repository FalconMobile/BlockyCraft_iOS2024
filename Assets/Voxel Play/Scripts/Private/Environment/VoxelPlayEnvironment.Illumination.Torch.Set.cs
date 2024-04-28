using UnityEngine;


namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        FastList<LightmapAddNode> torchLightmapSpreadQueue;

        /// <summary>
        /// Use when placing a torch light
        /// </summary>
        public void SetTorchLightmap (VoxelChunk chunk, int voxelIndex, int light)
        {
            // Set torch's light
            chunk.voxels [voxelIndex].torchLight = (byte)light;
            ChunkRequestRefresh (chunk, false, true);
            RebuildNeighboursIfNeeded (chunk, voxelIndex);

            // Add to spread queue
            torchLightmapSpreadQueue.Add (new LightmapAddNode { chunk = chunk, voxelIndex = voxelIndex });
        }


        /// <summary>
        /// Used when destroying a voxel and surrounding light needs to spread
        /// </summary>
        void SpreadTorchLightmapAroundVoxel (VoxelChunk chunk, int voxelIndex)
        {
            // Spread on neighbours
            int px, py, pz;
            VoxelChunk nchunk;
            int nindex;
            GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);

            int lightAtten = world.lightTorchAttenuation;

            // left voxel
            if (px > 0) {
                nchunk = chunk; nindex = voxelIndex - 1;
            } else {
                nchunk = chunk.left; nindex = voxelIndex + CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadTorchLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].torchLight, lightAtten);
            }

            // right voxel
            if (px < CHUNK_SIZE_MINUS_ONE) {
                nchunk = chunk; nindex = voxelIndex + 1;
            } else {
                nchunk = chunk.right; nindex = voxelIndex - CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadTorchLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].torchLight, lightAtten);
            }

            // back voxel
            if (pz > 0) {
                nchunk = chunk; nindex = voxelIndex - ONE_Z_ROW;
            } else {
                nchunk = chunk.back; nindex = voxelIndex + ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadTorchLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].torchLight, lightAtten);
            }

            // forward voxel
            if (pz < CHUNK_SIZE_MINUS_ONE) {
                nchunk = chunk; nindex = voxelIndex + ONE_Z_ROW;
            } else {
                nchunk = chunk.forward; nindex = voxelIndex - ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadTorchLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].torchLight, lightAtten);
            }

            // bottom voxel
            if (py > 0) {
                nchunk = chunk; nindex = voxelIndex - ONE_Y_ROW;
            } else {
                nchunk = chunk.bottom; nindex = voxelIndex + ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadTorchLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].torchLight, lightAtten);
            }

            // top voxel
            if (py < CHUNK_SIZE_MINUS_ONE) {
                nchunk = chunk; nindex = voxelIndex + ONE_Y_ROW;
            } else {
                nchunk = chunk.top; nindex = voxelIndex - ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadTorchLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].torchLight, lightAtten);
            }
        }


        void SpreadTorchLightToNeighbourVoxel (VoxelChunk nchunk, int nindex, int light, int decrement)
        {
            light -= decrement + nchunk.voxels [nindex].opaque;
            if (nchunk.voxels [nindex].torchLight < light) {
                nchunk.voxels [nindex].torchLight = (byte)light;
                ChunkRequestRefresh (nchunk, false, true);
                RebuildNeighboursIfNeeded (nchunk, nindex);
                if (light > decrement) {
                    torchLightmapSpreadQueue.Add (new LightmapAddNode { chunk = nchunk, voxelIndex = nindex });
                }
            }
        }

        void ProcessTorchLightmapSpread ()
        {
            int lightAtten = world.lightTorchAttenuation;

            for (int k = 0; k < torchLightmapSpreadQueue.count; k++) {
                VoxelChunk chunk = torchLightmapSpreadQueue.values [k].chunk;
                int voxelIndex = torchLightmapSpreadQueue.values [k].voxelIndex;
                int light = chunk.voxels [voxelIndex].torchLight;

                if (light <= lightAtten) continue;

                // Spread on neighbours
                VoxelChunk nchunk;
                int nindex;
                int bx = voxelIndex & VOXELINDEX_X_EDGE_BITWISE;

                // left voxel
                if (bx == 0) {
                    nchunk = chunk.left; nindex = voxelIndex + CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex - 1;
                }
                if ((object)nchunk != null) {
                    SpreadTorchLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                }

                // right voxel
                if (bx == VOXELINDEX_X_EDGE_BITWISE) {
                    nchunk = chunk.right; nindex = voxelIndex - CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex + 1;
                }
                if ((object)nchunk != null) {
                    SpreadTorchLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                }

                int bz = voxelIndex & VOXELINDEX_Z_EDGE_BITWISE;
                // back voxel
                if (bz == 0) {
                    nchunk = chunk.back; nindex = voxelIndex + ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex - ONE_Z_ROW;
                }
                if ((object)nchunk != null) {
                    SpreadTorchLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                }

                // forward voxel
                if (bz == VOXELINDEX_Z_EDGE_BITWISE) {
                    nchunk = chunk.forward; nindex = voxelIndex - ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex + ONE_Z_ROW;
                }
                if ((object)nchunk != null) {
                    SpreadTorchLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                }

                // bottom voxel
                if (voxelIndex < ONE_Y_ROW) {
                    nchunk = chunk.bottom; nindex = voxelIndex + ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex - ONE_Y_ROW;
                }
                if ((object)nchunk != null) {
                    SpreadTorchLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                }

                // top voxel
                if (voxelIndex < CHUNK_VOXEL_COUNT - ONE_Y_ROW) {
                    nchunk = chunk; nindex = voxelIndex + ONE_Y_ROW;
                } else {
                    nchunk = chunk.top; nindex = voxelIndex - ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
                }
                if ((object)nchunk != null) {
                    SpreadTorchLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                }
            }
            torchLightmapSpreadQueue.Clear ();
        }


    }



}
