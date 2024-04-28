using System;
using System.Collections.Generic;
using UnityEngine;


namespace VoxelPlay
{

    public enum ChunkRenderState : byte
    {
        Pending,
        RenderingRequested,
        RenderingComplete
    }

    public enum ChunkVisibleDistanceStatus : byte
    {
        Unknown = 0,
        WithinVisibleDistance = 1,
        OutOfVisibleDistance = 2
    }

    public partial class VoxelChunk : MonoBehaviour
    {
        /// <summary>
        /// Index of this chunk in the pool
        /// </summary>
        [NonSerialized]
        public int poolIndex;

        /// <summary>
        /// Voxels definition
        /// </summary>
        [NonSerialized]
        public Voxel [] voxels;

        /// <summary>
        /// Number of voxels in this chunk that contribute to mesh or custom types
        /// </summary>
        [NonSerialized]
        public int totalVisibleVoxelsCount;

        /// <summary>
        /// Chunk center position in local space. A 16x16x16 chunk starts at position-8 and ends on position+8
        /// </summary>
        [NonSerialized] public Vector3d position;

        /// <summary>
        /// If the chunk is visible in frustum. This value is stored for internal optimization purposes and could not reflect the current state, call ChunkIsInFrustum() instead if you want to know if a chunk is within camera frustum.
        /// </summary>
        [NonSerialized] public bool visibleInFrustum;

        [NonSerialized] public int frustumCheckIteration;

        [NonSerialized] public int voxelSignature;

        [NonSerialized] public MeshFilter mf;

        [NonSerialized] public MeshRenderer mr;

        [NonSerialized] public MeshCollider mc;

        [NonSerialized] public bool allowTrees = true;

        [NonSerialized] public int navMeshSourceIndex = -1;

        /// <summary>
        /// When the chunk navmesh was requested to be updated
        /// </summary>
        [NonSerialized] public float navMeshUpdateRequestTime;

        [NonSerialized] public Mesh navMesh;

        /// <summary>
        /// If this chunk is currently within visible distance or not
        /// </summary>
        [NonSerialized] public ChunkVisibleDistanceStatus visibleDistanceStatus;

        /// <summary>
        /// A flag that specified if this chunk is being hit by day light from above
        /// </summary>
		[NonSerialized] public bool isAboveSurface;

        /// <summary>
        /// A flag that specifies that the chunk mesh needs to be rebuilt when it gets refreshed
        /// </summary>
        [NonSerialized] public bool needsLightmapRebuild;

        /// <summary>
        /// A flag that specifies that the chunk mesh needs to be rebuilt when it gets refreshed
        /// </summary>
        [NonSerialized] public bool needsMeshRebuild;

        /// <summary>
        /// A flag that specifies that the chunk to be rendered will ignore frustum (ie. can be a chunk required by a distant AI)
        /// </summary>
        [NonSerialized] public bool ignoreFrustum;

        /// <summary>
        /// If chunk has been filled/populated with voxels. It might not been rendered yet.
        /// </summary>
        [NonSerialized] public bool isPopulated;

        /// <summary>
        /// Chunk is pending rendering (in queue)
        /// </summary>
        [NonSerialized] public bool inqueue;

        /// <summary>
        /// If this chunk can be reused, or it's a special chunk that needs to stay as it's
        /// </summary>
        /// <value><c>true</c> if can be reused; otherwise, <c>false</c>.</value>
        [NonSerialized] public bool cannotBeReused;

        /// <summary>
        /// if this chunk is used for cloud rendering.
        /// </summary>
        [NonSerialized] public bool isCloud;

        /// <summary>
        /// A counter to avoid adding this chunk more than once to a list of modified chunks when performing several modifications of the same chunk in a loop
        /// </summary>
        [NonSerialized]
        public int modifiedTag;

        /// <summary>
        /// Chunk has been modified in game
        /// </summary>
        public bool modified;

        /// <summary>
        /// Session flag that controls if chunks modifications should be marked as such. If this is disabled, chunk.modified won't be set hence when saving game, those modifications won't be considered
        /// This is useful when modifying the chunks by using the API while loading a game or creating a world procedurally, where modifications come from code itself and can be reproducible among sessions.
        /// </summary>
        public static bool markModifiedChunks = true;

        /// <summary>
        /// The framecount where this chunk was added to the notification queue (helps prevent sending OnChunkChanged notifications only once per frame)
        /// </summary>
        public int modifiedFrameCount;

        /// <summary>
        /// Returns true if the modifiedCount value is lower than the given value and then updates the modified and mofieidCount value.
        /// </summary>
        /// <returns></returns>
        public bool SetModified(int tag)
        {
            if (markModifiedChunks) {
                modified = true;
            }
            if (modifiedTag != tag) {
                modifiedTag = tag;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the chunk has been rendered at least once (and it might have no visible contents)
        /// </summary>
        [NonSerialized] public ChunkRenderState renderState = ChunkRenderState.Pending;

        /// <summary>
        /// Chunk has been rendered and uploaded to the GPU?
        /// </summary>
        public bool isRendered { get { return renderState == ChunkRenderState.RenderingComplete; } }


        /// <summary>
        /// Returns true if chunk has collider (not empty)
        /// </summary>
        public bool hasColliderMesh { get { return (object)mc != null && mc.sharedMesh != null; } }


        /// <summary>
        /// A dirty chunk is a chunk obtained with ChunkGetUnpopulated which might have unknown lightmap state and must be cleared again before terrain generator uses it
        /// </summary>
        [NonSerialized]
        public bool isDirty;

        /// <summary>
        /// The frame number where this chunk is rendered. Used for optimization.
        /// </summary>
        [NonSerialized]
        public int renderingFrame;

        /// <summary>
        /// Light sources in this chunk (ie. torches)
        /// </summary>
        [NonSerialized]
        public List<LightSource> lightSources;

        /// <summary>
        /// Voxel placeholders in this chunk. A placeholder is used to provide additional visual or interaction to a specific voxel (ie. damage cracks, physics, ...)
        /// </summary>
        [NonSerialized]
        public FastHashSet<VoxelPlaceholder> placeholders;

        /// <summary>
        /// Items spawn in this chunk
        /// </summary>
        [NonSerialized]
        public FastList<Item> items;

        /// <summary>
        /// Additional optional data for some voxels. Used internally.
        /// </summary>
        [NonSerialized]
        public FastHashSet<VoxelHiddenData> voxelsExtraData;

        /// <summary>
        /// Additional user-defined voxel properties.
        /// </summary>
        [NonSerialized]
        public FastHashSet<FastHashSet<VoxelProperty>> voxelsProperties;

        /// <summary>
        /// Link to neighbour chunk
        /// </summary>
        [NonSerialized]
        public VoxelChunk top, bottom, left, right, forward, back;

        /// <summary>
        /// Used to accelerate certain algorithms
        /// </summary>
        [NonSerialized]
        public int tempFlag;


        [NonSerialized]
        public bool lightmapIsClear;


        /// <summary>
        /// Forces a collider rebuild by resetting the voxelSignature field. This is used when changing a voxel type which affects only colliders (ie. SetDynamic).
        /// </summary>
        public void SetNeedsColliderRebuild()
        {
            voxelSignature = -1;
        }

        /// <summary>
		/// Clears the lightmap of this chunk or initializes it with a value
		/// </summary>
		public void ClearLightmap (byte value = 0)
        {
            if (lightmapIsClear && voxels[VoxelPlayEnvironment.CHUNK_VOXEL_COUNT - 1].light == value)
                return;
            for (int k = 0; k < voxels.Length; k++) {
                voxels [k].light = value;
                voxels [k].torchLight = 0;
            }
            lightmapIsClear = true;
            needsLightmapRebuild = true;
        }

        /// <summary>
        /// Removes all existing voxels in this chunk.
        /// </summary>
        public void ClearVoxels (byte light)
        {
            if (lightSources != null) {
                int lightSourcesCount = lightSources.Count;
                for (int k = lightSourcesCount - 1; k >= 0; k--) {
                    LightSource ls = lightSources [k];
                    if (ls.gameObject != null) {
                        Misc.DestroySafely (ls.gameObject);
                    }
                }
                lightSources.Clear ();
            }
            if (placeholders != null) {
                int phCount = placeholders.Count;
                for (int k = 0; k < phCount; k++) {
                    if (placeholders.entries [k].key >= 0) {
                        VoxelPlaceholder ph = placeholders.entries [k].value;
                        if (ph != null) {
                            Misc.DestroySafely (ph.gameObject);
                        }
                    }
                }
                placeholders.Clear ();
            }
            if (voxelsExtraData != null) {
                voxelsExtraData.Clear ();
            }
            if (voxelsProperties != null) {
                voxelsProperties.Clear ();
            }

            Voxel.Clear (voxels, light);
            lightmapIsClear = true;
        }

        /// <summary>
        /// Clears a single voxel
        /// </summary>
        /// <param name="voxelIndex">Index of voxel in the chunk</param>
        /// <param name="light">Light intensity left at the empty position</param>
        public void ClearVoxel (int voxelIndex, byte light)
        {
            if (lightSources != null) {
                int lightSourcesCount = lightSources.Count;
                for (int k = lightSourcesCount - 1; k >= 0; k--) {
                    LightSource ls = lightSources [k];
                    if (ls.voxelIndex == voxelIndex) {
                        if (ls.gameObject != null) {
                            Misc.DestroySafely (ls.gameObject);
                        }
                        lightSources.RemoveAt (k);
                    }
                }
            }
            if (placeholders != null) {
                VoxelPlaceholder placeholder;
                if (placeholders.TryGetValue (voxelIndex, out placeholder)) {
                    if (placeholder != null && placeholder.voxelIndex == voxelIndex) {
                        Misc.DestroySafely (placeholder.gameObject);
                        placeholders.Remove (voxelIndex);
                    }
                }
            }
            if (voxelsExtraData != null) {
                voxelsExtraData.Remove (voxelIndex);
            }
            if (voxelsProperties != null) {
                voxelsProperties.Remove (voxelIndex);
            }
            voxels [voxelIndex].Clear (light);
        }

        /// <summary>
        /// Sets links to neighbours
        /// </summary>
        public void ComputeNeighbours ()
        {
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;

            Vector3d topPosition = position;
            topPosition.y += VoxelPlayEnvironment.CHUNK_SIZE;
            env.GetChunk (topPosition, out top, false);
            if ((object)top != null) {
                top.bottom = this;
            }

            Vector3d bottomPosition = position;
            bottomPosition.y -= VoxelPlayEnvironment.CHUNK_SIZE;
            env.GetChunk (bottomPosition, out bottom, false);
            if ((object)bottom != null) {
                bottom.top = this;
            }

            Vector3d leftPosition = position;
            leftPosition.x -= VoxelPlayEnvironment.CHUNK_SIZE;
            env.GetChunk (leftPosition, out left, false);
            if ((object)left != null) {
                left.right = this;
            }
            Vector3d rightPosition = position;
            rightPosition.x += VoxelPlayEnvironment.CHUNK_SIZE;
            env.GetChunk (rightPosition, out right, false);
            if ((object)right != null) {
                right.left = this;
            }

            Vector3d forwardPosition = position;
            forwardPosition.z += VoxelPlayEnvironment.CHUNK_SIZE;
            env.GetChunk (forwardPosition, out forward, false);
            if ((object)forward != null) {
                forward.back = this;
            }

            Vector3d backPosition = position;
            backPosition.z -= VoxelPlayEnvironment.CHUNK_SIZE;
            env.GetChunk (backPosition, out back, false);
            if ((object)back != null)
                back.forward = this;
        }


        /// <summary>
        /// Returns true if this chunk contains a given position in world space
        /// </summary>
        public bool Contains (Vector3d position)
        {
            double xDiff = position.x - this.position.x;
            double yDiff = position.y - this.position.y;
            double zDiff = position.z - this.position.z;
            return (xDiff <= (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1) && xDiff >= -VoxelPlayEnvironment.CHUNK_HALF_SIZE && yDiff <= (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1) && yDiff >= -VoxelPlayEnvironment.CHUNK_HALF_SIZE && zDiff <= (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1) && zDiff >= -VoxelPlayEnvironment.CHUNK_HALF_SIZE);
        }


        /// <summary>
        /// Clears chunk state before returning it to the pool. This method is called when this chunk is reused.
        /// </summary>
        public void PrepareForReuse (byte light)
        {
            isAboveSurface = false;
            needsMeshRebuild = false;
            isPopulated = false;
            inqueue = false;
            modified = false;
            renderState = ChunkRenderState.Pending;
            allowTrees = true;
            frustumCheckIteration = 0;
            navMesh = null;
            navMeshSourceIndex = -1;
            isDirty = false;
            renderingFrame = -1;
            visibleDistanceStatus = ChunkVisibleDistanceStatus.Unknown;
            needsLightmapRebuild = false;

            if (items != null) {
                for (int k = 0; k < items.count; k++) {
                    Item item = items.values [k];
                    if (item != null && item.gameObject != null) {
                        Misc.DestroySafely (item.gameObject);
                    }
                }
                items.Clear ();
            }


            if ((object)left != null) {
                left.right = null;
                left = null;
            }
            if ((object)right != null) {
                right.left = null;
                right = null;
            }
            if ((object)forward != null) {
                forward.back = null;
                forward = null;
            }
            if ((object)back != null) {
                back.forward = null;
                back = null;
            }
            if ((object)top != null) {
                top.bottom = null;
                top = null;
            }
            if ((object)bottom != null) {
                bottom.top = null;
                bottom = null;
            }
            ClearVoxels (light);
            mr.enabled = false;
            if (mc != null) mc.enabled = false;
            gameObject.SetActive (true);
        }

        public void RemoveItem (Item item)
        {
            if (items != null) {
                if (items.Remove (item)) {
                    VoxelPlayEnvironment.instance.RegisterChunkChanges (this);
                }
            }
        }

        public void AddItem (Item item)
        {
            if (items == null) {
                items = new FastList<Item> ();
            }
            items.Add (item);
            VoxelPlayEnvironment.instance.RegisterChunkChanges (this);
        }

        public override string ToString ()
        {
            return string.Format ("[VoxelChunk: x={0}, y={1}, zm={2}]", position.x, position.y, position.z);
        }

        public LightSource GetLightSource (int voxelIndex)
        {
            if (lightSources == null) return null;
            int lsCount = lightSources.Count;
            for (int k = 0; k < lsCount; k++) {
                LightSource ls = lightSources [k];
                if (ls.voxelIndex == voxelIndex) {
                    return ls;
                }
            }
            return null;
        }

        public void AddLightSource (LightSource ls)
        {
            if (lightSources == null) {
                lightSources = new List<LightSource> ();
            }
            lightSources.Add (ls);
        }

        public void RemoveLightSource (int voxelIndex)
        {
            int count = lightSources.Count;
            for (int k = count - 1; k >= 0; k--) {
                if (lightSources [k].voxelIndex == voxelIndex) {
                    lightSources.RemoveAt (k);
                }
            }
        }

        public void AddLightSource (int voxelIndex, byte lightIntensity)
        {
            // Check if current light intensity is lower
            if (lightSources != null) {
                int count = lightSources.Count;
                for (int k = 0; k < count; k++) {
                    LightSource l = lightSources [k];
                    if (l.voxelIndex == voxelIndex) {
                        if (l.lightIntensity < lightIntensity) {
                            l.lightIntensity = lightIntensity;
                        }
                        return;
                    }
                }
            }
            LightSource ls = new LightSource ();
            ls.voxelIndex = voxelIndex;
            ls.lightIntensity = lightIntensity;
            AddLightSource (ls);
        }

        /// <summary>
        /// Sets a voxel in this chunk with a given voxelDefinition. This method updates lightmap accordingly if the voxel emits light.
        /// </summary>
        public void SetVoxel(int voxelIndex, VoxelDefinition voxelDefinition) {
            voxels[voxelIndex].Set(voxelDefinition);
            if (voxelDefinition.lightIntensity > 0) {
                AddLightSource(voxelIndex, voxelDefinition.lightIntensity);
                VoxelPlayEnvironment.instance.SetTorchLightmap(this, voxelIndex, voxelDefinition.lightIntensity);
            }
        }

        /// <summary>
        /// Sets a voxel in this chunk with a given voxelDefinition and tint color. This method updates lightmap accordingly if the voxel emits light.
        /// </summary>
        public void SetVoxel(int voxelIndex, VoxelDefinition voxelDefinition, Color32 tintColor)
        {
            voxels [voxelIndex].Set (voxelDefinition, tintColor);
            if (voxelDefinition.lightIntensity > 0) {
                AddLightSource (voxelIndex, voxelDefinition.lightIntensity);
                VoxelPlayEnvironment.instance.SetTorchLightmap (this, voxelIndex, voxelDefinition.lightIntensity);
            }
        }
    }


}