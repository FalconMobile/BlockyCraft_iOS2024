using UnityEngine;


namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        FastList<LightmapRemovalNode> sunLightmapRemovalQueue;


        void ClearSunLightmap (VoxelChunk chunk, int voxelIndex)
        {
            int light = chunk.voxels [voxelIndex].light;
            if (light == 0) return;

            chunk.voxels [voxelIndex].light = 0;

            ChunkRequestRefresh (chunk, false, true);
            RebuildNeighboursIfNeeded (chunk, voxelIndex);

            sunLightmapRemovalQueue.Add (new LightmapRemovalNode { chunk = chunk, voxelIndex = voxelIndex, light = light });
        }

        void RemoveSunLightFromNeighbourVoxel (VoxelChunk nchunk, int nindex, int light, int decrement)
        {
            if ((object)nchunk == null) return;

            int nlight = nchunk.voxels [nindex].light;
            if (nlight <= 0) return;

            light -= decrement + nchunk.voxels [nindex].opaque;

            if (nlight <= light) {
                nchunk.voxels [nindex].light = 0;
                ChunkRequestRefresh (nchunk, false, true);
                RebuildNeighboursIfNeeded (nchunk, nindex);
                sunLightmapRemovalQueue.Add (new LightmapRemovalNode { chunk = nchunk, voxelIndex = nindex, light = nlight });
            } else {
                sunLightmapSpreadQueue.Add (new LightmapAddNode { chunk = nchunk, voxelIndex = nindex });
            }
        }

        void ProcessSunLightmapRemoval ()
        {
            int lightAtten = world.lightSunAttenuation;

            for (int k = 0; k < sunLightmapRemovalQueue.count; k++) {
                VoxelChunk chunk = sunLightmapRemovalQueue.values [k].chunk;
                int voxelIndex = sunLightmapRemovalQueue.values [k].voxelIndex;
                int light = sunLightmapRemovalQueue.values [k].light;

                // Spread on neighbours
                VoxelChunk nchunk;
                int nindex;
                int bx = voxelIndex & VOXELINDEX_X_EDGE_BITWISE;
                int by = voxelIndex & VOXELINDEX_Y_EDGE_BITWISE;
                int bz = voxelIndex & VOXELINDEX_Z_EDGE_BITWISE;

                // left voxel
                if (bx == 0) {
                    nchunk = chunk.left; nindex = voxelIndex + CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex - 1;
                }
                RemoveSunLightFromNeighbourVoxel (nchunk, nindex, light, lightAtten);

                // right voxel
                if (bx == VOXELINDEX_X_EDGE_BITWISE) {
                    nchunk = chunk.right; nindex = voxelIndex - CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex + 1;
                }
                RemoveSunLightFromNeighbourVoxel (nchunk, nindex, light, lightAtten);

                // back voxel
                if (bz == 0) {
                    nchunk = chunk.back; nindex = voxelIndex + ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex - ONE_Z_ROW;
                }
                RemoveSunLightFromNeighbourVoxel (nchunk, nindex, light, lightAtten);

                // forward voxel
                if (bz == VOXELINDEX_Z_EDGE_BITWISE) {
                    nchunk = chunk.forward; nindex = voxelIndex - ONE_Z_ROW * CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex + ONE_Z_ROW;
                }
                RemoveSunLightFromNeighbourVoxel (nchunk, nindex, light, lightAtten);

                // bottom voxel
                if (by == 0) {
                    nchunk = chunk.bottom; nindex = voxelIndex + ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex - ONE_Y_ROW;
                }
                RemoveSunLightFromNeighbourVoxel (nchunk, nindex, light, chunk.isAboveSurface ? 0: lightAtten);

                // top voxel
                if (by == VOXELINDEX_Y_EDGE_BITWISE) {
                    nchunk = chunk.top; nindex = voxelIndex - ONE_Y_ROW * CHUNK_SIZE_MINUS_ONE;
                } else {
                    nchunk = chunk; nindex = voxelIndex + ONE_Y_ROW;
                }
                RemoveSunLightFromNeighbourVoxel (nchunk, nindex, light, lightAtten);
            }
            sunLightmapRemovalQueue.Clear ();
        }



    }



}
