using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    [ExecuteInEditMode]
    public class DebugPosition : MonoBehaviour {

        public Vector3 position;
        public Vector3 chunkPosition;
        public int py, pz, px;
        public int voxelIndex;
        public VoxelDefinition type;
        public int hidden;
        public int opaque;
        public int hasContent;
        public int voxelLight;
        public int torchLight;
        public bool chunkAboveSurface;
        public bool chunkHidden;
        public bool chunkPopulated;
        public bool chunkRendered;
        public bool chunkIsDirty;
        public bool chunkRefresh;
        public GameObject chunkGameObject;
        public VoxelPlaceholder placeholder;
        public GameObject modelInstance;

        private void Start() {
            if (name != "DEBUG") {
                GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Collider collider = o.GetComponent<Collider>();
                DestroyImmediate(collider);
                o.transform.position = transform.position;
                o.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                o.name = "DEBUG";
                o.AddComponent<DebugPosition>();
                Destroy(this);
            }
        }
        void Update() {
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null) return;

            VoxelChunk chunk;
            position = transform.position;
            if (!env.GetVoxelIndex(transform.position, out chunk, out voxelIndex, false)) {
                hasContent = 0;
                type = null;
                modelInstance = null;
                placeholder = null;
                return;
            }
            chunkPosition = chunk.position;
            env.GetVoxelChunkCoordinates(voxelIndex, out px, out py, out pz);
            type = chunk.voxels[voxelIndex].type;
            opaque = chunk.voxels[voxelIndex].opaque;
            hasContent = chunk.voxels[voxelIndex].hasContent;
            voxelLight = chunk.voxels[voxelIndex].light;
            torchLight = chunk.voxels[voxelIndex].torchLight;
            chunkAboveSurface = chunk.isAboveSurface;
            chunkPopulated = chunk.isPopulated;
            chunkRendered = chunk.isRendered;
            chunkIsDirty = chunk.isDirty;
            chunkGameObject = chunk.gameObject;
            placeholder = env.GetVoxelPlaceholder(chunk, voxelIndex);
            if (placeholder != null) {
                modelInstance = placeholder.modelInstance;
            }
            if (chunkRefresh) {
                chunkRefresh = false;
                env.ChunkRedraw (chunk, false);
            }
        }
    }

}