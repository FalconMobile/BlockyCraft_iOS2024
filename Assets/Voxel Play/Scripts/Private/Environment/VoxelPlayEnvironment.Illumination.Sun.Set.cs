using System;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        FastList<LightmapAddNode> sunLightmapSpreadQueue;


        /// <summary>
        /// Used when destroying a voxel and surrounding light needs to spread
        /// </summary>
        void SpreadSunLightmapAroundVoxel (VoxelChunk chunk, int voxelIndex)
        {
            // Spread on neighbours
            VoxelChunk nchunk;
            int nindex;
            GetVoxelChunkCoordinates (voxelIndex, out int px, out int py, out int pz);

            int lightAtten = world.lightSunAttenuation;

            // left voxel
            if (px > 0) {
                nchunk = chunk; nindex = voxelIndex - 1;
            } else {
                nchunk = chunk.left; nindex = voxelIndex + CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadSunLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].light, lightAtten);
            }

            // right voxel
            if (px < CHUNK_SIZE_MINUS_ONE) {
                nchunk = chunk; nindex = voxelIndex + 1;
            } else {
                nchunk = chunk.right; nindex = voxelIndex - CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadSunLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].light, lightAtten);
            }

            // back voxel
            if (pz > 0) {
                nchunk = chunk; nindex = voxelIndex - ONE_Z_ROW;
            } else {
                nchunk = chunk.back; nindex = voxelIndex + ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadSunLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].light, lightAtten);
            }

            // forward voxel
            if (pz < CHUNK_SIZE_MINUS_ONE) {
                nchunk = chunk; nindex = voxelIndex + ONE_Z_ROW;
            } else {
                nchunk = chunk.forward; nindex = voxelIndex - ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadSunLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].light, lightAtten);
            }

            // bottom voxel
            if (py > 0) {
                nchunk = chunk; nindex = voxelIndex - ONE_Y_ROW;
            } else {
                nchunk = chunk.bottom; nindex = voxelIndex + ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadSunLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].light, lightAtten);
            }

            // top voxel
            if (py < CHUNK_SIZE_MINUS_ONE) {
                nchunk = chunk; nindex = voxelIndex + ONE_Y_ROW;
            } else {
                nchunk = chunk.top; nindex = voxelIndex - ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
            }
            if ((object)nchunk != null) {
                SpreadSunLightToNeighbourVoxel (chunk, voxelIndex, nchunk.voxels [nindex].light, lightAtten);
            }
        }

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        void SpreadSunLightToNeighbourVoxel (VoxelChunk nchunk, int nindex, int light, int decrement)
        {
            light -= decrement + nchunk.voxels [nindex].opaque;
            if (nchunk.voxels [nindex].light < light) {
                nchunk.voxels [nindex].light = (byte)light;
                if (!nchunk.inqueue) {
                    ChunkRequestRefresh (nchunk, false, true);
                }
                RebuildNeighboursIfNeeded (nchunk, nindex);
                sunLightmapSpreadQueue.Add (new LightmapAddNode { chunk = nchunk, voxelIndex = nindex });
            }
        }

        void ProcessSunLightmapSpread ()
        {
            int lightAtten = world.lightSunAttenuation;

            for (int k = 0; k < sunLightmapSpreadQueue.count; k++) {
                VoxelChunk chunk = sunLightmapSpreadQueue.values [k].chunk;
                int voxelIndex = sunLightmapSpreadQueue.values [k].voxelIndex;
                int light = chunk.voxels [voxelIndex].light;

                // bottom voxel
                if (voxelIndex >= ONE_Y_ROW) {
                    SpreadSunLightToNeighbourVoxel (chunk, voxelIndex - ONE_Y_ROW, light, chunk.isAboveSurface ? 0 : lightAtten);
                } else {
                    VoxelChunk nchunk = chunk.bottom;
                    if ((object)nchunk != null) {
                        int nindex = voxelIndex + ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
                        SpreadSunLightToNeighbourVoxel (nchunk, nindex, light, chunk.isAboveSurface ? 0 : lightAtten);
                    }
                }

                if (light <= lightAtten) continue;

                // Spread on neighbours
                int bx = voxelIndex & VOXELINDEX_X_EDGE_BITWISE;

                // left voxel
                if (bx == 0) {
                    VoxelChunk nchunk = chunk.left;
                    if ((object)nchunk != null) {
                        int nindex = voxelIndex + CHUNK_SIZE_MINUS_ONE;
                        SpreadSunLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                    }
                } else {
                    SpreadSunLightToNeighbourVoxel (chunk, voxelIndex - 1, light, lightAtten);
                }

                // right voxel
                if (bx == VOXELINDEX_X_EDGE_BITWISE) {
                    VoxelChunk nchunk = chunk.right;
                    if ((object)nchunk != null) {
                        int nindex = voxelIndex - CHUNK_SIZE_MINUS_ONE;
                        SpreadSunLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                    }
                } else {
                    SpreadSunLightToNeighbourVoxel (chunk, voxelIndex + 1, light, lightAtten);
                }

                int bz = voxelIndex & VOXELINDEX_Z_EDGE_BITWISE;

                // back voxel
                if (bz == 0) {
                    VoxelChunk nchunk = chunk.back;
                    if ((object)nchunk != null) {
                        int nindex = voxelIndex + ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
                        SpreadSunLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                    }
                } else {
                    SpreadSunLightToNeighbourVoxel (chunk, voxelIndex - ONE_Z_ROW, light, lightAtten);
                }

                // forward voxel
                if (bz == VOXELINDEX_Z_EDGE_BITWISE) {
                    VoxelChunk nchunk = chunk.forward;
                    if ((object)nchunk != null) {
                        int nindex = voxelIndex - ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
                        SpreadSunLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                    }
                } else {
                    SpreadSunLightToNeighbourVoxel (chunk, voxelIndex + ONE_Z_ROW, light, lightAtten);
                }

                // top voxel
                if (voxelIndex < CHUNK_VOXEL_COUNT - ONE_Y_ROW) {
                    SpreadSunLightToNeighbourVoxel (chunk, voxelIndex + ONE_Y_ROW, light, lightAtten);
                } else {
                    VoxelChunk nchunk = chunk.top;
                    if ((object)nchunk != null) {
                        int nindex = voxelIndex - ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
                        SpreadSunLightToNeighbourVoxel (nchunk, nindex, light, lightAtten);
                    }
                }
            }
            sunLightmapSpreadQueue.Clear ();
        }


    }



}
