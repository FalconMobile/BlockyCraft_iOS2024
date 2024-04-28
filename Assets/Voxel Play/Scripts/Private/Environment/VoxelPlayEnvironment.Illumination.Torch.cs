using UnityEngine;


namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        void ComputeTorchLightmap (VoxelChunk chunk)
        {
            Voxel [] voxels = chunk.voxels;
            if (voxels == null)
                return;

            int lightAtten = world.lightTorchAttenuation;

            // Get top chunk but only if it has been rendered at least once.
            // means that the top chunk is not available which in the case of surface will switch to the heuristic of heightmap (see else below)
            VoxelChunk topChunk = chunk.top;
            bool topChunkIsAccesible = (object)topChunk != null && topChunk.isPopulated;
            if (topChunkIsAccesible) {
                int top = CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW;
                for (int bottom = 0; bottom < CHUNK_SIZE * CHUNK_SIZE; bottom++, top++) {
                    byte torchLight = topChunk.voxels [bottom].torchLight;
                    if (torchLight - voxels [top].opaque - lightAtten > voxels [top].torchLight) {
                        torchLightmapSpreadQueue.Add (new LightmapAddNode { chunk = topChunk, voxelIndex = bottom });
                    }
                }
            }

            // Check bottom chunk
            VoxelChunk bottomChunk = chunk.bottom;
            bool bottomChunkIsAccesible = (object)bottomChunk != null && bottomChunk.isPopulated;
            if (bottomChunkIsAccesible) {
                int top = CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW;
                for (int bottom = 0; bottom < CHUNK_SIZE * CHUNK_SIZE; bottom++, top++) {
                    byte torchLight = bottomChunk.voxels [top].torchLight;
                    if (torchLight - voxels [bottom].opaque - lightAtten > voxels [bottom].torchLight) {
                        torchLightmapSpreadQueue.Add (new LightmapAddNode { chunk = bottomChunk, voxelIndex = top });
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
                    byte torchLight = leftChunk.voxels [right].torchLight;
                    if (torchLight - voxels [left].opaque - lightAtten > voxels [left].torchLight) {
                        torchLightmapSpreadQueue.Add (new LightmapAddNode { chunk = leftChunk, voxelIndex = right });
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
                    byte torchLight = rightChunk.voxels [left].torchLight;
                    if (torchLight - voxels [right].opaque - lightAtten > voxels [right].torchLight) {
                        torchLightmapSpreadQueue.Add (new LightmapAddNode { chunk = rightChunk, voxelIndex = left });
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
                        byte torchLight = forwardChunk.voxels [back].torchLight;
                        if (torchLight - voxels [forward].opaque - lightAtten > voxels [forward].torchLight) {
                            torchLightmapSpreadQueue.Add (new LightmapAddNode { chunk = forwardChunk, voxelIndex = back });
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
                        byte torchLight = backChunk.voxels [forward].torchLight;
                        if (torchLight - voxels [back].opaque - lightAtten > voxels [back].torchLight) {
                            torchLightmapSpreadQueue.Add (new LightmapAddNode { chunk = backChunk, voxelIndex = forward });
                        }
                    }
                }
            }
        }
    }

}
