using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {
        public const byte FULL_OPAQUE = 15;
        public const byte FULL_LIGHT = 15;

        [NonSerialized]
        public VoxelDefinition [] voxelDefinitions;
        public int voxelDefinitionsCount;

        [NonSerialized]
        public VoxelDefinition currentWaterVoxelDefinition;

        GameObject dynamicVoxelTemplate;
        GameObject defaultVoxelPrefab;
        GameObject voxelHighlightGO;
        VoxelHighlight voxelHighlight;
        readonly float [] collapsingOffsets = {
            0, 1, 0,
            -1, 1, -1,
            1, 1, 1,
            -1, 1, 1,
            1, 1, -1,
            1, 1, 0,
            0, 1, 1,
            -1, 1, 0,
            0, 1, -1,
            -1, 1, -1,
            1, 0, 1,
            -1, 0, 1,
            1, 0, -1,
            1, 0, 0,
            0, 0, 1,
            -1, 0, 0,
            0, 0, -1,
            -1, 0, -1
        };


        List<Vector3> tempVertices;
        List<Vector3> tempNormals;
        int [] tempIndices;
        int tempIndicesPos;
        List<Vector4> tempUVs;
        List<Color32> tempColors;

        void VoxelDestroyFast (VoxelChunk chunk, int voxelIndex)
        {

            // Ensure there's content on this position
            if (chunk.voxels [voxelIndex].hasContent != 1)
                return;

            if (captureEvents && OnVoxelBeforeDestroyed != null) {
                OnVoxelBeforeDestroyed (chunk, voxelIndex);
            }

            // Fetch voxel properties for replacement and collapse before clearing it
            VoxelDefinition vd = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
            bool triggerCollapse = vd.triggerCollapse && !constructorMode && !buildMode;
            VoxelDefinition replaceType = vd.replacedBy;

            // Clears light (important: this needs to be called before clearing the voxel)
            // We reverse cast a full light from this position
            chunk.voxels [voxelIndex].torchLight = FULL_LIGHT;
            chunk.voxels [voxelIndex].light = FULL_LIGHT;
            ClearLightmapAtPosition (chunk, voxelIndex);

            // Clear voxel
            chunk.ClearVoxel (voxelIndex, effectiveGlobalIllumination ? (byte)0 : (byte)15);

            // Voxel replacement
            if ((object)replaceType != null) {
                chunk.voxels [voxelIndex].Set (replaceType);
            }

            ChunkRequestRefresh (chunk, false, true);
            SpreadLightmapAroundPosition (chunk, voxelIndex);

            // Force rebuild neighbour meshes if destroyed voxel is on a border
            RebuildNeighboursIfNeeded (chunk, voxelIndex);

            // Events
            RegisterChunkChanges (chunk);
            if (captureEvents && OnVoxelDestroyed != null) {
                OnVoxelDestroyed (chunk, voxelIndex);
            }

            // Check if it was surrounded by water. If it was, add water expander
            Vector3d voxelPosition = GetVoxelPosition (chunk, voxelIndex);
            MakeSurroundingWaterExpand (voxelPosition);

            // Check if voxels on top can fall
            if (triggerCollapse && world.collapseOnDestroy) {
                VoxelCollapse (voxelPosition, world.collapseAmount, null, world.consolidateDelay);
            }
        }

        void VoxelDestroyFastSingle (VoxelChunk chunk, int voxelIndex)
        {
            chunk.ClearVoxel (voxelIndex, effectiveGlobalIllumination ? (byte)0 : (byte)15);
            RegisterChunkChanges (chunk);
        }

        /// <summary>
        /// Puts a voxel in the given position. Takes care of informing neighbour chunks.
        /// </summary>
        /// <returns>Returns the affected chunk and voxel index</returns>
        /// <param name="position">Position.</param>
        void VoxelPlaceFast (Vector3d position, VoxelDefinition voxelType, out VoxelChunk chunk, out int voxelIndex, Color32 tintColor, float amount = 1f, int rotation = 0, bool refresh = true)
        {
            if (GetVoxelIndex (position, out chunk, out voxelIndex)) {
                if (captureEvents && OnVoxelBeforePlace != null) {
                    OnVoxelBeforePlace (position, chunk, voxelIndex, ref voxelType, ref tintColor);
                    if (voxelType == null)
                        return;
                }
            } else {
                return;
            }

            if (refresh) {
                // Clear lighting
                ClearLightmapAtPosition (chunk, voxelIndex);
            }

            // If it's water, add flood
            if (voxelType.spreads) {
                if (chunk.voxels [voxelIndex].hasContent == 0 || voxelType.spreadReplaceThreshold > chunk.voxels [voxelIndex].opaque) {
                    chunk.SetVoxel (voxelIndex, voxelType, tintColor);
                }
                chunk.voxels [voxelIndex].SetWaterLevel ((Mathf.CeilToInt (amount * 15f)));
                AddWaterFloodInt (ref position, voxelType);
            } else {
                // Set voxel type preserving water level
                chunk.voxels [voxelIndex].Set (voxelType, tintColor);
            }

            // Apply rotation
            if (voxelType.allowsTextureRotation) {
                chunk.voxels [voxelIndex].SetTextureRotation (rotation);
            }

            // Add light source
            if (voxelType.lightIntensity > 0) {
                chunk.AddLightSource (voxelIndex, voxelType.lightIntensity);
                SetTorchLightmap (chunk, voxelIndex, voxelType.lightIntensity);
            }

            if (captureEvents && OnVoxelAfterPlace != null) {
                OnVoxelAfterPlace (position, chunk, voxelIndex);
            }

            // Issue a chunk refresh
            if (refresh) {
                ChunkRequestRefresh (chunk, false, true);
            }

            RegisterChunkChanges (chunk);
        }


        /// <summary> 
        /// Internal method that puts a voxel in a given position. This method does not inform to neighbours. Only used by non-contiguous structures, like trees or vegetation.
        /// For terrain or large scale buildings, use VoxelPlaceFast.
        /// </summary>
        /// <param name="position">Position.</param>
        /// <param name="voxelType">Voxel type.</param>
        /// <param name="chunk">Chunk.</param>
        /// <param name="voxelIndex">Voxel index.</param>
        void VoxelSingleSet (Vector3d position, VoxelDefinition voxelType, out VoxelChunk chunk, out int voxelIndex, Color32 tintColor)
        {
            if (GetVoxelIndex (position, out chunk, out voxelIndex)) {
                if (captureEvents && OnVoxelBeforePlace != null) {
                    OnVoxelBeforePlace (position, chunk, voxelIndex, ref voxelType, ref tintColor);
                    if (voxelType == null)
                        return;
                }
                chunk.voxels [voxelIndex].Set (voxelType, tintColor);
                if (captureEvents && OnVoxelAfterPlace != null) {
                    OnVoxelAfterPlace (position, chunk, voxelIndex);
                }
            }
        }


        /// <summary>
        /// Converts a voxel into dynamic type
        /// </summary>
        /// <param name="chunk">Chunk.</param>
        /// <param name="voxelIndex">Voxel index.</param>
        GameObject VoxelSetDynamic (VoxelChunk chunk, int voxelIndex, bool addRigidbody, float duration)
        {
            if ((object)chunk == null || chunk.voxels [voxelIndex].hasContent == 0)
                return null;

            VoxelDefinition vd = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
            if (!vd.renderType.supportsDynamic ()) {
                return null;
            }

            VoxelPlaceholder placeholder = GetVoxelPlaceholder (chunk, voxelIndex, true);
            if (placeholder == null)
                return null;

            // Already dynamic?
            if (vd.isDynamic) return placeholder.gameObject;

            // Add rigid body (only if it's not a custom voxel with a rigidbody already)
            if (vd.renderType == RenderType.Custom && vd.prefabUsesRigidbody) {
                addRigidbody = false;
            }

            if (addRigidbody) {
                Rigidbody rb = placeholder.GetComponentInChildren<Rigidbody> ();
                if (rb == null) {
                    // check it has a collider
                    if (!placeholder.GetComponentInChildren<Collider> ()) {
                        if (placeholder.modelInstance != null) {
                            MeshCollider collider = placeholder.modelInstance.AddComponent<MeshCollider> ();
                            collider.convex = true;
                        }
                    }
                    placeholder.rb = placeholder.gameObject.AddComponent<Rigidbody> ();
                }
            }

            // If it's a custom model ignore it as it's already a gameobject
            if (vd.renderType == RenderType.Custom)
                return placeholder.gameObject;

            VoxelDefinition vdDyn = vd.dynamicDefinition;

            if (vdDyn == null) {
                // Setup and save voxel definition
                vd.dynamicDefinition = vdDyn = ScriptableObject.CreateInstance<VoxelDefinition> ();
                vdDyn.name = vd.name + " (Dynamic)";
                vdDyn.isDynamic = true;
                vdDyn.doNotSave = true;
                vdDyn.staticDefinition = vd;
                vdDyn.renderType = RenderType.Custom;
                vdDyn.textureIndexBottom = vd.textureIndexBottom;
                vdDyn.textureIndexSide = vd.textureIndexSide;
                vdDyn.textureIndexTop = vd.textureIndexTop;
                vdDyn.textureThumbnailTop = vd.textureThumbnailTop;
                vdDyn.textureThumbnailSide = vd.textureThumbnailSide;
                vdDyn.textureThumbnailBottom = vd.textureThumbnailBottom;
                vdDyn.textureSideIndices = vd.textureSideIndices;
                vdDyn.scale = vd.scale;
                vdDyn.offset = vd.offset;
                vdDyn.offsetRandomRange = vd.offsetRandomRange;
                vdDyn.rotation = vd.rotation;
                vdDyn.rotationRandomY = vd.rotationRandomY;
                vdDyn.sampleColor = vd.sampleColor;
                vdDyn.promotesTo = vd.promotesTo;
                vdDyn.playerDamageDelay = vd.playerDamageDelay;
                vdDyn.playerDamage = vd.playerDamage;
                vdDyn.pickupSound = vd.pickupSound;
                vdDyn.landingSound = vd.landingSound;
                vdDyn.jumpSound = vd.jumpSound;
                vdDyn.impactSound = vd.impactSound;
                vdDyn.footfalls = vd.footfalls;
                vdDyn.destructionSound = vd.destructionSound;
                vdDyn.canBeCollected = vd.canBeCollected;
                vdDyn.dropItem = GetItemDefinition (ItemCategory.Voxel, vd);
                vdDyn.buildSound = vd.buildSound;
                vdDyn.navigatable = true;
                vdDyn.windAnimation = false;
                vdDyn.textureSideIndices = null;
                vdDyn.isSolid = vd.isSolid;
                if (vd.renderType == RenderType.Custom) {
                    vdDyn.model = vd.prefab;
                } else {
                    vdDyn.model = MakeDynamicCubeFromVoxel (chunk, voxelIndex);
                }
                AddVoxelTextures (vdDyn);
            }

            // Clear any vegetation on top if voxel can be moved (has a rigidbody) to avoid floating grass block
            if (placeholder.rb != null) {
                if (GetVoxelIndex (chunk, voxelIndex, 0, 1, 0, out VoxelChunk topChunk, out int topIndex)) {
                    if (topChunk.voxels [topIndex].hasContent == 1 && voxelDefinitions [topChunk.voxels [topIndex].typeIndex].renderType == RenderType.CutoutCross) {
                        VoxelDestroyFast (topChunk, topIndex);
                    }
                }
            }
            Color32 color = chunk.voxels [voxelIndex].color;
            int textureRotation = chunk.voxels [voxelIndex].GetTextureRotation ();
            chunk.voxels [voxelIndex].Set (vdDyn, color);
            chunk.voxels [voxelIndex].SetTextureRotation (textureRotation);
            chunk.SetNeedsColliderRebuild ();

            if (duration > 0) {
                placeholder.SetCancelDynamic (duration);
            }

            // Refresh neighbours
            RebuildNeighboursIfNeeded (chunk, voxelIndex);

            return placeholder.gameObject;
        }

        /// <summary>
        /// Finds all voxels with "willCollapse" connected to a given position
        /// </summary>
        /// <returns>The crumbly voxel indices.</returns>
        /// <param name="position">Position.</param>
        /// <param name="voxelIndices">Results.</param>
        int GetCrumblyVoxelIndices (Vector3d position, int amount, List<VoxelIndex> voxelIndices)
        {
            voxelIndices.Clear ();
            tempVoxelPositions.Clear ();
            tempVoxelIndicesCrumbleCount = 0;
            GetCrumblyVoxelRecursive (new Vector3d (position.x, position.y + 1f, position.z), position, amount, voxelIndices);
            return tempVoxelIndicesCrumbleCount;
        }


        void GetCrumblyVoxelRecursive (Vector3d originalPosition, Vector3d position, int amount, List<VoxelIndex> voxelIndices)
        {
            if (tempVoxelIndicesCrumbleCount >= amount)
                return;
            int c = 0;
            VoxelIndex vi = new VoxelIndex ();
            for (int k = 0; k < collapsingOffsets.Length; k += 3) {
                Vector3d pos = position;
                pos.x += collapsingOffsets [k];
                pos.y += collapsingOffsets [k + 1];
                pos.z += collapsingOffsets [k + 2];
                double dx = pos.x > originalPosition.x ? pos.x - originalPosition.x : originalPosition.x - pos.x;
                double dz = pos.z > originalPosition.z ? pos.z - originalPosition.z : originalPosition.z - pos.z;
                if (dx > 8 || dz > 8)
                    continue;
                if (!tempVoxelPositions.TryGetValue (pos, out _)) {
                    tempVoxelPositions [pos] = true;
                    if (GetVoxelIndex (pos, out VoxelChunk chunk, out int voxelIndex, false) && chunk.voxels [voxelIndex].hasContent == 1) {
                        VoxelDefinition vd = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
                        if (vd.willCollapse && (vd.renderType == RenderType.Custom || chunk.voxels [voxelIndex].opaque >= 3)) {
                            vi.chunk = chunk;
                            vi.voxelIndex = voxelIndex;
                            vi.position = pos;
                            voxelIndices.Add (vi);
                            tempVoxelIndicesCrumbleCount++;
                            c++;
                            if (tempVoxelIndicesCrumbleCount >= amount)
                                break;
                        }
                    }
                }
            }
            int lastCount = tempVoxelIndicesCrumbleCount;
            for (int k = 1; k <= c; k++) {
                GetCrumblyVoxelRecursive (originalPosition, voxelIndices [lastCount - k].position, amount, voxelIndices);
            }
        }

        /// <summary>
        /// Returns the default voxel prefab (usually a cube; the prefab is located in Defaults folder)
        /// </summary>
        /// <returns>The default voxel prefab.</returns>
        GameObject GetDefaultVoxelPrefab ()
        {
            if (defaultVoxelPrefab == null) {
                defaultVoxelPrefab = Resources.Load<GameObject> ("VoxelPlay/Defaults/DefaultModel/Cube");
            }
            return defaultVoxelPrefab;
        }


        void InitTempVertices ()
        {
            tempVertices = new List<Vector3> (36);
            tempNormals = new List<Vector3> (36);
            tempUVs = new List<Vector4> (36);
            tempIndices = new int [36];
            tempColors = new List<Color32> (36);

        }

        /// <summary>
        /// Creates a gameobject with geometry and materials based on the triangle renderer but lmited to one voxel
        /// </summary>
        /// <param name="chunk">Chunk.</param>
        /// <param name="voxelIndex">Voxel index.</param>
        GameObject MakeDynamicCubeFromVoxel (VoxelChunk chunk, int voxelIndex)
        {
            VoxelDefinition type = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
            Color32 tintColor = chunk.voxels [voxelIndex].color;

            Mesh mesh = null;
            if (type.dynamicMeshes == null) {
                type.dynamicMeshes = new Dictionary<Color, Mesh> ();
            } else {
                type.dynamicMeshes.TryGetValue (tintColor, out mesh);
            }

            if (mesh == null) {
                // Create cube mesh procedurally
                tempVertices.Clear ();
                tempNormals.Clear ();
                tempUVs.Clear ();
                tempColors.Clear ();
                tempIndicesPos = 0;

                AddFace (MeshingThread.faceVerticesBack, MeshingThread.normalsBack, type.textureIndexSide, tintColor);
                AddFace (MeshingThread.faceVerticesForward, MeshingThread.normalsForward, type.textureIndexSide, tintColor);
                AddFace (MeshingThread.faceVerticesLeft, MeshingThread.normalsLeft, type.textureIndexSide, tintColor);
                AddFace (MeshingThread.faceVerticesRight, MeshingThread.normalsRight, type.textureIndexSide, tintColor);
                AddFace (MeshingThread.faceVerticesTop, MeshingThread.normalsUp, type.textureIndexTop, tintColor);
                AddFace (MeshingThread.faceVerticesBottom, MeshingThread.normalsDown, type.textureIndexBottom, tintColor);

                mesh = new Mesh ();
                mesh.SetVertices (tempVertices);
                mesh.SetUVs (0, tempUVs);
                mesh.SetNormals (tempNormals);
                if (enableTinting) {
                    mesh.SetColors (tempColors);
                }
                mesh.triangles = tempIndices;
                type.dynamicMeshes [tintColor] = mesh;
            }

            if (dynamicVoxelTemplate == null) {
                dynamicVoxelTemplate = Resources.Load<GameObject> ("VoxelPlay/Prefabs/DynamicVoxelTemplate");
            }
            GameObject obj = Instantiate (dynamicVoxelTemplate);
            obj.SetActive (false);

            MeshFilter mf = obj.GetComponent<MeshFilter> ();
            mf.mesh = mesh;

            MeshRenderer mr = obj.GetComponent<MeshRenderer> ();

            Material mat = null;
            if (type.overrideMaterial) {
                mat = type.overrideMaterialNonGeo;
            }
            if (mat == null) {
                if (type.renderType == RenderType.Custom) {
                    mat = GetDynamicVoxelMaterialFromCustom (type);
                } else {
                    mat = type.renderType == RenderType.Cutout ? matDynamicCutout : matDynamicOpaque;
                }
            }
            mr.sharedMaterial = mat;

            obj.transform.SetParent (worldRoot, false);

            return obj;
        }



        Material GetDynamicVoxelMaterialFromCustom (VoxelDefinition template)
        {
            Material refMat = template.material;
            bool useCutout = template.renderType == RenderType.Cutout ? matDynamicCutoutNonArray : matDynamicOpaqueNonArray;
            return GetDynamicVoxelMaterialFromCustom (refMat, useCutout);
        }

        Material GetDynamicVoxelMaterialFromCustom (Material refMat, bool useCutout)
        {
            Material instancingMat = useCutout ? matDynamicCutoutNonArray : matDynamicOpaqueNonArray;
            instancingMat = Instantiate (instancingMat);
            instancingMat.DisableKeyword (SKW_VOXELPLAY_GPU_INSTANCING);
            if (refMat != null) {
                if (refMat.HasProperty (ShaderParams.Color) && instancingMat.HasProperty (ShaderParams.Color)) {
                    instancingMat.SetColor (ShaderParams.Color, refMat.GetColor (ShaderParams.Color));
                }
                if (refMat.HasProperty ("_MainTex") && instancingMat.HasProperty ("_MainTex")) {
                    instancingMat.SetTexture ("_MainTex", refMat.GetTexture ("_MainTex"));
                }
                if (refMat.HasProperty ("_BumpMap") && instancingMat.HasProperty ("_BumpMap")) {
                    instancingMat.SetTexture ("_BumpMap", refMat.GetTexture ("_BumpMap"));
                }
            }
            return instancingMat;
        }

        void AddFace (Vector3 [] faceVertices, Vector3 [] normals, int textureIndex, Color32 tintColor)
        {
            int index = tempVertices.Count;
            tempVertices.AddRange (faceVertices);
            tempNormals.AddRange (normals);

            tempIndices [tempIndicesPos++] = index;
            tempIndices [tempIndicesPos++] = index + 1;
            tempIndices [tempIndicesPos++] = index + 3;
            tempIndices [tempIndicesPos++] = index + 3;
            tempIndices [tempIndicesPos++] = index + 2;
            tempIndices [tempIndicesPos++] = index + 0;

            Vector4 v4 = new Vector4 (0, 0, textureIndex, 15f);
            tempUVs.Add (v4);
            v4.y = 1f;
            tempUVs.Add (v4);
            v4.x = 1f;
            v4.y = 0;
            tempUVs.Add (v4);
            v4.y = 1f;
            tempUVs.Add (v4);
            if (enableTinting) {
                tempColors.Add (tintColor);
                tempColors.Add (tintColor);
                tempColors.Add (tintColor);
                tempColors.Add (tintColor);
            }
        }


        void GetVoxelNeighbourhood27 (VoxelChunk chunk, int voxelIndex, ref VoxelIndex [] voxelIndices)
        {
            const int ARRAY_SIZE = 3;
            const int SIZE_Y_ROW = ARRAY_SIZE * ARRAY_SIZE;
            const int SIZE_Z_ROW = ARRAY_SIZE;

            // fill center voxel
            int index = 1 * SIZE_Y_ROW + 1 * SIZE_Z_ROW + 1;
            voxelIndices [index].chunk = chunk;
            voxelIndices [index].voxelIndex = voxelIndex;

            VoxelChunk otherChunk;
            int otherVoxelIndex;
            for (int arrayIndex = 0, offsetY = -1; offsetY <= 1; offsetY++) {
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++) {
                    for (int offsetX = -1; offsetX <= 1; offsetX++, arrayIndex++) {
                        if (GetVoxelIndex (chunk, voxelIndex, offsetX, offsetY, offsetZ, out otherChunk, out otherVoxelIndex)) {
                            voxelIndices [arrayIndex].chunk = otherChunk;
                        } else {
                            voxelIndices [arrayIndex].chunk = null;
                        }
                        voxelIndices [arrayIndex].voxelIndex = otherVoxelIndex;
                    }
                }
            }
        }


        void GetVoxelNeighbourhood9 (VoxelChunk chunk, int voxelIndex, ref VoxelIndex [] voxelIndices)
        {
            const int ARRAY_SIZE = 3;
            const int SIZE_Z_ROW = ARRAY_SIZE;

            // fill center voxel
            int index = 1 * SIZE_Z_ROW + 1;
            voxelIndices [index].chunk = chunk;
            voxelIndices [index].voxelIndex = voxelIndex;

            VoxelChunk otherChunk;
            int otherVoxelIndex;
            for (int arrayIndex = 0, offsetZ = -1; offsetZ <= 1; offsetZ++) {
                for (int offsetX = -1; offsetX <= 1; offsetX++, arrayIndex++) {
                    if (GetVoxelIndex (chunk, voxelIndex, offsetX, 0, offsetZ, out otherChunk, out otherVoxelIndex)) {
                        voxelIndices [arrayIndex].chunk = otherChunk;
                    } else {
                        voxelIndices [arrayIndex].chunk = null;
                    }
                    voxelIndices [arrayIndex].voxelIndex = otherVoxelIndex;
                }
            }
        }


        VoxelProperty VoxelGetProperty (VoxelChunk chunk, int voxelIndex, int propertyId)
        {
            if ((object)chunk == null || chunk.voxelsProperties == null) {
                return default (VoxelProperty);
            }
            FastHashSet<VoxelProperty> voxelProperties;
            if (!chunk.voxelsProperties.TryGetValue (voxelIndex, out voxelProperties)) {
                return default (VoxelProperty);
            }
            voxelProperties.TryGetValue (propertyId, out VoxelProperty prop);
            return prop;
        }




    }

}
