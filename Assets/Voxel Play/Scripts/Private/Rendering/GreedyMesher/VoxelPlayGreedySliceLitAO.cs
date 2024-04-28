//#define USES_TINTING
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public class QuadFullAO
    {
        public byte x, y, w, h;
#if USES_TINTING
		public Color32 color;
#endif
        public float l0, l1, l2, l3;
        public int textureIndex;
    }

    public class VoxelPlayGreedySliceLitAO
    {

        QuadFullAO [] qq;
        QuadFullAO lastQ;
        int qqCount;
        const byte USED = 255;

        public VoxelPlayGreedySliceLitAO ()
        {
            qq = new QuadFullAO [VoxelPlayEnvironment.CHUNK_SIZE * VoxelPlayEnvironment.CHUNK_SIZE];
            for (int k = 0; k < qq.Length; k++) {
                qq [k] = new QuadFullAO ();
            }
        }

        public void Clear ()
        {
            qqCount = 0;
        }



        public void AddQuad (int x, int y, Color32 color, float l0, float l1, float l2, float l3, int textureIndex)
        {
#if USES_TINTING
			if (qqCount > 0 && lastQ.y == y && lastQ.x + lastQ.w == x && lastQ.textureIndex == textureIndex && lastQ.l0 == l0 && lastQ.l1 == l1 && lastQ.l2 == l2 && lastQ.l3 == l3 && lastQ.color.r == color.r && lastQ.color.g == color.g && lastQ.color.b == color.b) {
#else
            if (qqCount > 0 && lastQ.y == y && lastQ.x + lastQ.w == x && lastQ.textureIndex == textureIndex && lastQ.l0 == l0 && lastQ.l1 == l1 && lastQ.l2 == l2 && lastQ.l3 == l3) {
#endif
                lastQ.w++;
            } else {
                QuadFullAO q = lastQ = qq [qqCount++];
                q.x = (byte)x;
                q.y = (byte)y;
                q.w = 1;
                q.h = 1;
#if USES_TINTING
				q.color = color;
#endif
                q.l0 = l0;
                q.l1 = l1;
                q.l2 = l2;
                q.l3 = l3;
                q.textureIndex = textureIndex;
            }
        }

        public void FlushTriangles (FaceDirection direction, int slice, List<Vector3> vertices, List<int> indices, List<Vector4> uv0, List<Vector3> normals, List<Color32> colors)
        {
            if (qqCount == 0) {
                return;
            }
            Vector3 pos;
            Vector4 uv;
            Vector3 normal;
            switch (direction) {
            case FaceDirection.Back: normal = Misc.vector3back; break;
            case FaceDirection.Forward: normal = Misc.vector3forward; break;
            case FaceDirection.Left: normal = Misc.vector3left; break;
            case FaceDirection.Right: normal = Misc.vector3right; break;
            case FaceDirection.Top: normal = Misc.vector3up; break;
            default: normal = Misc.vector3down; break;
            }
            int index = vertices.Count;
            for (int k = 0; k < qqCount; k++) {
                QuadFullAO q1 = qq [k];
                if (q1.x == USED) {
                    continue;
                }
                for (int j = k + 1; j < qqCount; j++) {
                    QuadFullAO q2 = qq [j];
                    if (q2.x == USED)
                        continue;
#if USES_TINTING
					if (q1.y == q2.y && q1.h == q2.h && q1.x + q1.w == q2.x && q1.textureIndex == q2.textureIndex && q1.l0 == q2.l0 && q1.l1 == q2.l1 && q1.l2 == q2.l2 && q1.l3 == q2.l3 && q1.color.r == q2.color.r && q1.color.g == q2.color.g && q1.color.b == q2.color.b) {
#else
                    if (q1.y == q2.y && q1.h == q2.h && q1.x + q1.w == q2.x && q1.textureIndex == q2.textureIndex && q1.l0 == q2.l0 && q1.l1 == q2.l1 && q1.l2 == q2.l2 && q1.l3 == q2.l3) {
#endif

                        q1.w += q2.w;
                        q2.x = USED;
                        continue;
                    }
#if USES_TINTING
					if (q1.x == q2.x && q1.w == q2.w && q1.y + q1.h == q2.y && q1.textureIndex == q2.textureIndex && q1.l0 == q2.l0 && q1.l1 == q2.l1 && q1.l2 == q2.l2 && q1.l3 == q2.l3 && q1.color.r == q2.color.r && q1.color.g == q2.color.g && q1.color.b == q2.color.b) {
#else
                    if (q1.x == q2.x && q1.w == q2.w && q1.y + q1.h == q2.y && q1.textureIndex == q2.textureIndex && q1.l0 == q2.l0 && q1.l1 == q2.l1 && q1.l2 == q2.l2 && q1.l3 == q2.l3) {
#endif
                        q1.h += q2.h;
                        q2.x = USED;
                        continue;
                    }
                }

                switch (direction) {
                case FaceDirection.Top:
                    pos.y = slice - (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1);
                    pos.x = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    pos.z = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    vertices.Add (pos);
                    pos.z += q1.h;
                    vertices.Add (pos);
                    pos.z -= q1.h;
                    pos.x += q1.w;
                    vertices.Add (pos);
                    pos.z += q1.h;
                    vertices.Add (pos);
                    break;
                case FaceDirection.Bottom:
                    pos.y = slice - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    pos.x = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE + q1.w;
                    pos.z = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    vertices.Add (pos);
                    pos.z += q1.h;
                    vertices.Add (pos);
                    pos.z -= q1.h;
                    pos.x -= q1.w;
                    vertices.Add (pos);
                    pos.z += q1.h;
                    vertices.Add (pos);
                    break;
                case FaceDirection.Left:
                    pos.x = slice - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    pos.z = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE + q1.w;
                    pos.y = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    vertices.Add (pos);
                    pos.y += q1.h;
                    vertices.Add (pos);
                    pos.y -= q1.h;
                    pos.z -= q1.w;
                    vertices.Add (pos);
                    pos.y += q1.h;
                    vertices.Add (pos);
                    break;
                case FaceDirection.Right:
                    pos.x = slice - (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1);
                    pos.z = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    pos.y = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    vertices.Add (pos);
                    pos.y += q1.h;
                    vertices.Add (pos);
                    pos.z += q1.w;
                    pos.y -= q1.h;
                    vertices.Add (pos);
                    pos.y += q1.h;
                    vertices.Add (pos);
                    break;
                case FaceDirection.Back:
                    pos.z = slice - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    pos.x = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    pos.y = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    vertices.Add (pos);
                    pos.y += q1.h;
                    vertices.Add (pos);
                    pos.x += q1.w;
                    pos.y -= q1.h;
                    vertices.Add (pos);
                    pos.y += q1.h;
                    vertices.Add (pos);
                    break;
                case FaceDirection.Forward:
                    pos.z = slice - (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1);
                    pos.x = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE + q1.w;
                    pos.y = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
                    vertices.Add (pos);
                    pos.y += q1.h;
                    vertices.Add (pos);
                    pos.x -= q1.w;
                    pos.y -= q1.h;
                    vertices.Add (pos);
                    pos.y += q1.h;
                    vertices.Add (pos);
                    break;
                }

                // texture coordinates
                uv.x = 0;
                uv.y = 0;
                uv.z = q1.textureIndex;
                uv.w = q1.l0;
                uv0.Add (uv);
                uv.y = q1.h;
                uv.w = q1.l1;
                uv0.Add (uv);
                uv.x = q1.w;
                uv.y = 0f;
                uv.w = q1.l2;
                uv0.Add (uv);
                uv.y = q1.h;
                uv.w = q1.l3;
                uv0.Add (uv);

#if USES_TINTING
					colors.Add (q1.color);
					colors.Add (q1.color);
					colors.Add (q1.color);
					colors.Add (q1.color);
#endif

                normals.Add (normal);
                normals.Add (normal);
                normals.Add (normal);
                normals.Add (normal);

                if (q1.l0 + q1.l3 > q1.l1 + q1.l2) {
                    indices.Add (index);
                    indices.Add (index + 1);
                    indices.Add (index + 3);
                    indices.Add (index + 3);
                    indices.Add (index + 2);
                    indices.Add (index + 0);
                } else {
                    indices.Add (index);
                    indices.Add (index + 1);
                    indices.Add (index + 2);
                    indices.Add (index + 3);
                    indices.Add (index + 2);
                    indices.Add (index + 1);
                }

                index += 4;
            }

            // Clear for next usage
            qqCount = 0;
        }

    }
}
