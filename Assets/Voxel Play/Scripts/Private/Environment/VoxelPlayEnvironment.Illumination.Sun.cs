using UnityEngine;


namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        void ComputeSunLightmap (VoxelChunk chunk)
        {
            Voxel [] voxels = chunk.voxels;
            if (voxels == null)
                return;

            bool isAboveSurface = chunk.isAboveSurface;

            // Get top chunk but only if it has been rendered at least once.
            // means that the top chunk is not available which in the case of surface will switch to the heuristic of heightmap (see else below)
            VoxelChunk topChunk = chunk.top;
            bool topChunkIsAccesible = (object)topChunk != null && topChunk.isPopulated;
            if (topChunkIsAccesible) {
                int top = CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW;
                for (int bottom = 0; bottom < CHUNK_SIZE * CHUNK_SIZE; bottom++, top++) {
                    byte light = topChunk.voxels [bottom].light;
                    if (light - voxels [top].opaque > voxels [top].light) {
                        sunLightmapSpreadQueue.Add (new LightmapAddNode { chunk = topChunk, voxelIndex = bottom });
                    }
                }
            } else if (isAboveSurface) {
                for (int top = CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW; top < CHUNK_VOXEL_COUNT; top++) {
                    if (voxels [top].light < FULL_LIGHT - voxels [top].opaque) {
                        voxels [top].light = (byte)(FULL_LIGHT - voxels [top].opaque);
                        sunLightmapSpreadQueue.Add (new LightmapAddNode { chunk = chunk, voxelIndex = top });
                    }
                }
            }

            int lightAtten = world.lightSunAttenuation;

            // Check bottom chunk
            VoxelChunk bottomChunk = chunk.bottom;
            bool bottomChunkIsAccesible = (object)bottomChunk != null && bottomChunk.isPopulated;
            if (bottomChunkIsAccesible) {
                int top = CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW;
                for (int bottom = 0; bottom < CHUNK_SIZE * CHUNK_SIZE; bottom++, top++) {
                    byte light = bottomChunk.voxels [top].light;
                    if (light - voxels [bottom].opaque - lightAtten > voxels [bottom].light) {
                        sunLightmapSpreadQueue.Add (new LightmapAddNode { chunk = bottomChunk, voxelIndex = top });
                    }
                }
            }

            // Check left face
            VoxelChunk leftChunk = chunk.left;
            bool leftChunkIsAccesible = (object)leftChunk != null && leftChunk.isPopulated;
            if (leftChunkIsAccesible) {
                int left = CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW + CHUNK_SIZE_MINUS_ONE * ONE_Z_ROW;
                int right = left + CHUNK_SIZE_MINUS_ONE;
                for (int z = 0; z < CHUNK_SIZE * CHUNK_SIZE; z++, right -= ONE_Z_ROW, left -= ONE_Z_ROW) {
                    byte light = leftChunk.voxels [right].light;
                    if (light - voxels [left].opaque - lightAtten > voxels [left].light) {
                        sunLightmapSpreadQueue.Add (new LightmapAddNode { chunk = leftChunk, voxelIndex = right });
                    }
                }
            }


            // Check right face
            VoxelChunk rightChunk = chunk.right;
            bool rightChunkIsAccesible = (object)rightChunk != null && rightChunk.isPopulated;
            if (rightChunkIsAccesible) {
                int left = CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW + CHUNK_SIZE_MINUS_ONE * ONE_Z_ROW;
                int right = left + CHUNK_SIZE_MINUS_ONE;
                for (int z = 0; z < CHUNK_SIZE * CHUNK_SIZE; z++, right -= ONE_Z_ROW, left -= ONE_Z_ROW) {
                    byte light = rightChunk.voxels [left].light;
                    if (light - voxels [right].opaque - lightAtten > voxels [right].light) {
                        sunLightmapSpreadQueue.Add (new LightmapAddNode { chunk = rightChunk, voxelIndex = left });
                    }
                }
            }

            // Check forward face
            VoxelChunk forwardChunk = chunk.forward;
            bool forwardChunkIsAccesible = (object)forwardChunk != null && forwardChunk.isPopulated;
            if (forwardChunkIsAccesible) {
                for (int y = CHUNK_SIZE_MINUS_ONE; y >= 0; y--) {
                    int back = y * ONE_Y_ROW;
                    int forward = back + CHUNK_SIZE_MINUS_ONE * ONE_Z_ROW;
                    for (int x = 0; x <= CHUNK_SIZE_MINUS_ONE; x++, back++, forward++) {
                        byte light = forwardChunk.voxels [back].light;
                        if (light - voxels [forward].opaque - lightAtten > voxels [forward].light) {
                            sunLightmapSpreadQueue.Add (new LightmapAddNode { chunk = forwardChunk, voxelIndex = back });
                        }
                    }
                }
            }

            // Check back face
            VoxelChunk backChunk = chunk.back;
            bool backChunkIsAccesible = (object)backChunk != null && backChunk.isPopulated;
            if (backChunkIsAccesible) {
                for (int y = CHUNK_SIZE_MINUS_ONE; y >= 0; y--) {
                    int back = y * ONE_Y_ROW;
                    int forward = back + CHUNK_SIZE_MINUS_ONE * ONE_Z_ROW;
                    for (int x = 0; x <= CHUNK_SIZE_MINUS_ONE; x++, back++, forward++) {
                        byte light = backChunk.voxels [forward].light;
                        if (light - voxels [back].opaque - lightAtten > voxels [back].light) {
                            sunLightmapSpreadQueue.Add (new LightmapAddNode { chunk = backChunk, voxelIndex = forward });
                        }
                    }
                }
            }
        }
    }



}
