using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public class VoxelPlayGreedyMesherLitAO
    {
        VoxelPlayGreedySliceLitAO [] slicesFull;

        public VoxelPlayGreedyMesherLitAO ()
        {
            slicesFull = new VoxelPlayGreedySliceLitAO [VoxelPlayEnvironment.CHUNK_SIZE * 6];
            for (int k = 0; k < slicesFull.Length; k++) {
                slicesFull [k] = new VoxelPlayGreedySliceLitAO ();
            }
        }


        public void AddQuad (FaceDirection direction, int x, int y, int slice, Color32 color, float l0, float l1, float l2, float l3, int textureIndex)
        {
            int index = (int)direction * VoxelPlayEnvironment.CHUNK_SIZE + slice;
            slicesFull [index].AddQuad (x, y, color, l0, l1, l2, l3, textureIndex);
        }


        public void FlushTriangles (List<Vector3> vertices, List<int> indices, List<Vector4> uv0, List<Vector3> normals, List<Color32> colors)
        {
            for (int d = 0; d < 6; d++) {
                for (int s = 0; s < VoxelPlayEnvironment.CHUNK_SIZE; s++) {
                    slicesFull [d * VoxelPlayEnvironment.CHUNK_SIZE + s].FlushTriangles ((FaceDirection)d, s, vertices, indices, uv0, normals, colors);
                }
            }
        }


        public void Clear ()
        {
            for (int k = 0; k < slicesFull.Length; k++) {
                slicesFull [k].Clear ();
            }
        }

    }
}