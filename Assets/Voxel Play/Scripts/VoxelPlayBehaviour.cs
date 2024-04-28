// Voxel Play 
// Created by Ramiro Oliva (Kronnect)
// Voxel Play Behaviour - attach this script to any moving object that should receive voxel global illumination

using UnityEngine;
using System.Collections.Generic;

namespace VoxelPlay
{

    [HelpURL ("https://kronnect.freshdesk.com/support/solutions/articles/42000001858-voxel-play-behaviour")]
    public class VoxelPlayBehaviour : MonoBehaviour
    {

        [Tooltip ("Keeps the material color or intensity of this object updated with voxel light information.")]
        public bool enableVoxelLight = true;

        [Tooltip ("Automatically replaces Standard material by Voxel Play optimized materials")]
        public bool useVoxelPlayMaterials;

        [Tooltip ("Ensures the object don't get into another voxel by falling into it or crossing it.")]
        public bool forceUnstuck = true;

        [Tooltip("Vertical shift over the ground applied to this gameobject when unstuck occurs")]
        public float unstuckOffsetY;

        [Tooltip ("If nearby chunks should be checked and created if they don't exist. This option is required to avoid this object to fall down.")]
        public bool checkNearChunks = true;

        [Tooltip ("Extents around this object to be checked")]
        public Vector3 chunkExtents;

        [Tooltip ("If chunks created around this object should also be rendered")]
        public bool renderChunks = true;

        [Tooltip ("If this gameobject will be shifted when origin shift occurs")]
        public bool useOriginShift = true;

        VoxelPlayEnvironment env;
        int lastX, lastY, lastZ;
        int lastChunkX, lastChunkY, lastChunkZ;
        Vector3d lastPosition;
        bool requireUpdateLighting;
        static readonly List<Renderer> rr = new List<Renderer> ();
        struct RendererData
        {
            public Material mat;
            public Color normalMatColor;
            public bool useMaterialColor;
        }

        RendererData [] rd;
        Rigidbody rb;
        static readonly Dictionary<Material, Material> upgradedMaterials = new Dictionary<Material, Material> ();

        static class ShaderParams
        {
            public static int VoxelLight = Shader.PropertyToID ("_VoxelLight");
            public static int Color = Shader.PropertyToID ("_Color");
            public static int BumpMap = Shader.PropertyToID("_BumpMap");
        }

        void Start ()
        {
            env = VoxelPlayEnvironment.instance;
            if (env == null) {
                DestroyImmediate (this);
                return;
            }
            env.OnChunkRender += ChunkRender;
            lastPosition = transform.position;
            lastX = int.MaxValue;
            rb = GetComponent<Rigidbody> ();

            if (useVoxelPlayMaterials) {
                Shader vpShaderOpaque = Shader.Find ("Voxel Play/Models/Texture/Opaque");
                Shader vpShaderTransp = Shader.Find ("Voxel Play/Models/Texture/Alpha");
                Shader vpShaderOpaqueBumpMap = Shader.Find("Voxel Play/Models/Texture/Opaque");
                if (vpShaderOpaque == null || vpShaderTransp == null) {
                    Debug.LogError ("Could not find Voxel Play/Models/Texture/Opaque shader.");
                } else {
                    Renderer [] rr = GetComponentsInChildren<Renderer> ();
                    for (int k = 0; k < rr.Length; k++) {
                        Material [] mats = rr [k].sharedMaterials;
                        for (int m = 0; m < mats.Length; m++) {
                            Material mat = mats [m];
                            if (mat != null && !mat.shader.name.Contains ("Voxel Play")) {
                                if (!upgradedMaterials.TryGetValue (mat, out Material upgradedMaterial)) {
                                    upgradedMaterial = Instantiate(mat);
                                    if (mat.renderQueue >= 3000 && mat.HasProperty(ShaderParams.Color) && mat.color.a < 1f)
                                    {
                                        upgradedMaterial.shader = vpShaderTransp;
                                    } else if (mat.HasProperty(ShaderParams.BumpMap)) {
                                        upgradedMaterial.shader = vpShaderOpaqueBumpMap;
                                    } else 
                                    {
                                        upgradedMaterial.shader = vpShaderOpaque;
                                    }
                                    upgradedMaterials [mat] = upgradedMaterial;
                                }
                                mats [m] = upgradedMaterial;
                            }
                        }
                        rr [k].sharedMaterials = mats;
                    }
                }
            }

            if (enableVoxelLight) {
                FetchMaterials ();
            }

            if (useOriginShift) {
                env.RegisterOriginShiftTransform (transform.root);
            }

            if (forceUnstuck) {
                CheckStuck();
            }

            if (checkNearChunks) {
                CheckNearChunks(lastPosition);
            }

        }

        void FetchMaterials ()
        {
            GetComponentsInChildren (true, rr);
            int count = rr.Count;
            rd = new RendererData [count];
            for (int k = 0; k < count; k++) {
                Renderer mr = rr [k];
                Material mat = mr.sharedMaterial;
                if (mat == null) continue;
                rd [k].useMaterialColor = !mat.shader.name.Contains ("Voxel Play/Models");
                mat = Instantiate (mat);
                mat.hideFlags = HideFlags.DontSave;
                mr.sharedMaterial = mat;
                rd [k].normalMatColor = mat.HasProperty (ShaderParams.Color) ? mat.color : Misc.colorWhite;
                mat.DisableKeyword ("VOXELPLAY_GPU_INSTANCING");
                rd [k].mat = mat;
            }
            requireUpdateLighting = true;
        }

        private void OnDestroy ()
        {
            if (env == null) return;

            env.OnChunkRender -= ChunkRender;

        }
        void ChunkRender (VoxelChunk chunk)
        {
            if (FastVector.SqrMinDistanceXZ ((Vector3)chunk.position, transform.position) < 32 * 32) {
                requireUpdateLighting = true;
            }
        }

        public void Refresh ()
        {
            lastX = int.MaxValue;
            lastChunkX = int.MaxValue;
        }

        void LateUpdate ()
        {

            if (!env.initialized)
                return;

            // Check if position has changed since previous
            Vector3d position = transform.position;
            FastMath.FloorToInt (position.x, position.y, position.z, out int x, out int y, out int z);

            if (lastX != x || lastY != y || lastZ != z) {
                requireUpdateLighting = true;

                lastPosition = position;
                lastX = x;
                lastY = y;
                lastZ = z;

                if (forceUnstuck) {
                    CheckStuck();
                }

                if (checkNearChunks) {
                    CheckNearChunks(position);
                }
            }
            if (requireUpdateLighting) {
                requireUpdateLighting = false;
                UpdateLightingNow ();
            }
        }


        void CheckStuck() {
            Vector3 pos = transform.position;
            pos.y += unstuckOffsetY + 0.1f;
            if (env.CheckCollision(pos)) {
                float deltaY = FastMath.FloorToInt(pos.y) + 1.01f - pos.y;
                pos.y += deltaY;
                if (rb != null) {
                    rb.position = pos;
                    rb.velocity = Misc.vector3zero;
                } else {
                    transform.position = pos;
                }
                lastX--;
            }
        }

        void CheckNearChunks (Vector3d position)
        {
            int chunkX, chunkY, chunkZ;
            FastMath.FloorToInt (position.x / VoxelPlayEnvironment.CHUNK_SIZE, position.y / VoxelPlayEnvironment.CHUNK_SIZE, position.z / VoxelPlayEnvironment.CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);
            if (lastChunkX != chunkX || lastChunkY != chunkY || lastChunkZ != chunkZ) {
                lastChunkX = chunkX;
                lastChunkY = chunkY;
                lastChunkZ = chunkZ;
                // Ensure area is rendered
                env.ChunkCheckArea (position, chunkExtents, renderChunks);
            }
        }


        public void UpdateLighting ()
        {
            requireUpdateLighting = true;
        }

        void UpdateLightingNow ()
        {
            if (!enableVoxelLight) return;
            if (rd == null || rd.Length == 0) {
                FetchMaterials ();
            }
            Vector3d pos = lastPosition;
            // center of voxel
            pos.x += 0.5f;
            pos.y += 0.5f;
            pos.z += 0.5f;
            float light = -1;
            int packedLight = -1;

            for (int k = 0; k < rd.Length; k++) {
                if (rd [k].mat == null) continue;

                if (rd [k].useMaterialColor) {
                    if (light < 0) {
                        light = env.GetVoxelLight (pos);
                    }
                    Color newColor = new Color (rd [k].normalMatColor.r * light, rd [k].normalMatColor.g * light, rd [k].normalMatColor.b * light, rd [k].normalMatColor.a);
                    rd [k].mat.color = newColor;
                } else {
                    if (packedLight < 0) {
                        packedLight = env.GetVoxelLightPacked (pos);
                    }
                    rd [k].mat.SetInt (ShaderParams.VoxelLight, packedLight);
                }
            }
        }

    }
}