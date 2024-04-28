using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using Random = UnityEngine.Random;

//#define DEBUG_RAYCAST

namespace VoxelPlay
{

    public enum ColliderTypes
    {
        AnyCollider = 0,
        OnlyVoxels = 1,
        IgnorePlayer = 2
    }

    public enum ParticleBurstStyle
    {
        Explosion = 0
    }


    public struct VoxelHitInfo
    {

        /// <summary>
        /// The world space position of the ray hit
        /// </summary>
        public Vector3d point;


        float lastSqrDistance;
        float computedDistance;

        /// <summary>
        /// Distance to the hit position
        /// </summary>
        public float distance {
            get {
                if (lastSqrDistance != sqrDistance) {
                    computedDistance = Mathf.Sqrt (sqrDistance);
                }
                return computedDistance;
            }
            set {
                computedDistance = value;
                sqrDistance = computedDistance * computedDistance;
                lastSqrDistance = sqrDistance;
            }
        }


        /// <summary>
        /// Squared distance (distance * distance) to the hit position
        /// </summary>
        public float sqrDistance;

        /// <summary>
        /// The index of the voxel being hit in the chunk.voxels array
        /// </summary>
        public int voxelIndex;

        /// <summary>
        /// The chunk to which the voxel belongs to
        /// </summary>
        public VoxelChunk chunk;

        /// <summary>
        /// The center of the voxel
        /// </summary>
        public Vector3d voxelCenter;

        /// <summary>
        /// The center of the hit object. If it's a voxel, this equals to voxelCenter. If it's a custom voxel, it uses placeholder.bounds.center.
        /// </summary>
        public Vector3d center {
            get {
                if (placeholder != null) {
                    return placeholder.GetWorldSpaceBounds ().center;
                } else {
                    return voxelCenter;
                }
            }
        }

        /// <summary>
        /// The normal of the side of the voxel being hit
        /// </summary>
        public Vector3 normal;

        /// <summary>
        /// Copy of the voxel hit. This copy does not change even if the position has been cleared after the RayCast finishes. To get the voxel at this position at this moment, call GetVoxelNow()
        /// </summary>
        /// <value>The voxel.</value>
        public Voxel voxel;

        /// <summary>
        /// The collider of the gameobject which is hit by the ray
        /// </summary>
        public Collider collider;

        /// <summary>
        /// If the voxel hit has a placeholder attached, a reference to it is returned here
        /// </summary>
        public VoxelPlaceholder placeholder;

        /// <summary>
        /// If the hit is on an item
        /// </summary>
        public Item item;


        public void Clear ()
        {
            placeholder = null;
            chunk = null;
            voxelIndex = -1;
            collider = null;
            item = null;
        }

        /// <summary>
        /// Returns a copy of the voxel at this position
        /// </summary>
        public Voxel GetVoxelNow ()
        {
            if (chunk != null && voxelIndex >= 0) {
                return chunk.voxels [voxelIndex];
            }
            return Voxel.Empty;
        }

    }

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {
        const int DEFAULT_DESTROYED_VOXEL_PARTICLE_AMOUNT = 20;

        Dictionary<Vector3d, bool> tempVoxelPositions;
        int tempVoxelIndicesCrumbleCount;

        int destroyedVoxelParticlesAmount = DEFAULT_DESTROYED_VOXEL_PARTICLE_AMOUNT;

        void InitPhysics ()
        {

            if (tempVoxelPositions == null) {
                tempVoxelPositions = new Dictionary<Vector3d, bool> (100);
            } else {
                tempVoxelPositions.Clear ();
            }

            if (layerVoxels == layerParticles) {
                layerParticles = layerVoxels + 1;
            }
            Physics.IgnoreLayerCollision (layerVoxels, layerVoxels);
        }

        bool RayCastFast (Vector3d origin, Vector3 direction, out VoxelHitInfo hitInfo, float maxDistance = 0, bool createChunksIfNeeded = false, byte minOpaque = 0, ColliderTypes colliderTypes = ColliderTypes.AnyCollider, int layerMask = -1, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {

            bool voxelHit = RayCastFastVoxel (origin, direction, out hitInfo, maxDistance, createChunksIfNeeded, minOpaque);
            if ((colliderTypes & ColliderTypes.OnlyVoxels) != 0) {
                return voxelHit;
            }

            if (voxelHit) {
                maxDistance = hitInfo.distance - 0.01f;
            }
            // Cast a normal raycast to detect normal gameobjects within ray
            RaycastHit hit;
            if (Physics.Raycast (origin, direction, out hit, maxDistance, layerMask, queryTriggerInteraction)) {
                // ensures this is not a normal voxel in which case keep current hitInfo data
                Transform parent = hit.collider.transform.parent;
                if (parent != chunksRoot && parent != fxRoot) {
                    if ((colliderTypes & ColliderTypes.IgnorePlayer) != 0 && (UnityEngine.Object)characterController != null && characterController.transform.root == hit.collider.transform.root) {
                        return voxelHit;
                    }
                    hitInfo.distance = hit.distance;
                    hitInfo.point = hit.point;
                    hitInfo.normal = hit.normal;
                    hitInfo.collider = hit.collider;

                    // Check if gameobject is an item
                    Item item = hit.collider.GetComponent<Item> ();
                    if (item != null) {
                        if (item.itemChunk != null) {
                            hitInfo.chunk = item.itemChunk;
                            hitInfo.voxelIndex = item.itemVoxelIndex;
                            hitInfo.voxel = item.itemChunk.voxels [hitInfo.voxelIndex];
                            hitInfo.voxelCenter = GetVoxelPosition (item.itemChunk, item.itemVoxelIndex);
                        }
                        hitInfo.item = item;
                        hitInfo.placeholder = null;
                        return true;
                    }

                    // Check if gameobject is a dynamic voxel
                    hitInfo.voxelIndex = -1;
                    VoxelPlaceholder placeholder = hit.collider.GetComponentInParent<VoxelPlaceholder> ();
                    if (placeholder != null) {
                        hitInfo.chunk = placeholder.chunk;
                        hitInfo.voxelIndex = placeholder.voxelIndex;
                        hitInfo.voxel = placeholder.chunk.voxels [placeholder.voxelIndex];
                        hitInfo.voxelCenter = placeholder.transform.position;
                        hitInfo.placeholder = placeholder;
                        if (hitInfo.voxel.type.ignoresRayCast) return false;
                    }
                    return true;
                }
            }

            return voxelHit;
        }

        VoxelChunk RayCastFastVoxel (Vector3d origin, Vector3 direction, out VoxelHitInfo hitInfo, float maxDistance = 0, bool createChunksIfNeeded = false, byte minOpaque = 0)
        {

#if DEBUG_RAYCAST
												GameObject o;
#endif

            double maxDistanceSqr = maxDistance == 0 ? 1000 * 1000 : maxDistance * maxDistance;

            // Ray march throuch chunks until hit one loaded chunk
            Vector3d position = origin;
            VoxelChunk chunk = null;
            hitInfo = new VoxelHitInfo ();
            hitInfo.voxelIndex = -1;

            Vector3 viewDirSign = FastVector.Sign (ref direction);
            Vector3 viewSign = new Vector3 ((viewDirSign.x + 1f) * 0.5f, (viewDirSign.y + 1f) * 0.5f, (viewDirSign.z + 1f) * 0.5f); // 0 = left, 1 = right

            float vxz, vzy, vxy;
            if (direction.y != 0) {
                float a = direction.x / direction.y;
                float b = direction.z / direction.y;
                vxz = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxz = 1000000f;
            }
            if (direction.x != 0) {
                float a = direction.z / direction.x;
                float b = direction.y / direction.x;
                vzy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vzy = 1000000f;
            }
            if (direction.z != 0) {
                float a = direction.x / direction.z;
                float b = direction.y / direction.z;
                vxy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxy = 1000000f;
            }

            Vector3 v3 = new Vector3 (vzy, vxz, vxy);
            Vector3 viewSignChunk = viewSign * CHUNK_SIZE;
            Vector3 viewDirSignOffset = viewDirSign * 0.002f;

            int chunkCount = 0;
            double t;
            Vector3d normal = Vector3d.zero;
            Vector3d db;

            while (chunkCount++ < 500) { // safety counter to avoid any potential infinite loop

                // Check max distance
                double distSqr = (position.x - origin.x) * (position.x - origin.x) + (position.y - origin.y) * (position.y - origin.y) + (position.z - origin.z) * (position.z - origin.z);
                if (distSqr > maxDistanceSqr)
                    return null;


#if DEBUG_RAYCAST
																o = GameObject.CreatePrimitive(PrimitiveType.Cube);
																o.transform.localScale = Misc.Vector3one * 0.15f;
																o.transform.position = position;
																DestroyImmediate(o.GetComponent<Collider>());
																o.GetComponent<Renderer>().material.color = Color.blue;
#endif

                FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);

                chunk = null;
                if (createChunksIfNeeded) {
                    chunk = GetChunkOrCreate (chunkX, chunkY, chunkZ);
                } else {
                    int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
                    int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
                    int hash = x00 + y00 + chunkZ;
                    chunk = GetChunkIfExists (hash);
                }

                chunkX *= CHUNK_SIZE;
                chunkY *= CHUNK_SIZE;
                chunkZ *= CHUNK_SIZE;

                if (chunk) {
                    // Ray-march through chunk
                    Voxel [] voxels = chunk.voxels;
                    Vector3d inPosition = position;

                    for (int k = 0; k < CHUNK_SIZE * 4; k++) {

#if DEBUG_RAYCAST
																								o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
																								o.transform.localScale = Misc.Vector3one * 0.1f;
																								o.transform.position = inPosition;
																								DestroyImmediate(o.GetComponent<Collider>());
																								o.GetComponent<Renderer>().material.color = Color.yellow;
#endif

                        // Check voxel content
                        FastMath.FloorToInt (inPosition.x, inPosition.y, inPosition.z, out int fx, out int fy, out int fz);
                        int py = fy - chunkY;
                        int pz = fz - chunkZ;
                        int px = fx - chunkX;
                        if (px < 0 || px >= CHUNK_SIZE || py < 0 || py >= CHUNK_SIZE || pz < 0 || pz >= CHUNK_SIZE) {
                            break;
                        }

                        int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                        if (voxels [voxelIndex].hasContent == 1 && (minOpaque == 255 || voxels [voxelIndex].opaque >= minOpaque) && !VoxelIsHidden (chunk, voxelIndex)) {

                            VoxelDefinition vd = voxelDefinitions [voxels [voxelIndex].typeIndex];
                            VoxelPlaceholder placeholder = null;
                            if (!vd.ignoresRayCast && (vd.renderType != RenderType.Custom || (!vd.prefabUsesCollider && RayIntersectsCustomVoxel (origin, direction, chunk, voxelIndex, out placeholder)))) {
                                // Check max distance
                                distSqr = (inPosition.x - origin.x) * (inPosition.x - origin.x) + (inPosition.y - origin.y) * (inPosition.y - origin.y) + (inPosition.z - origin.z) * (inPosition.z - origin.z);
                                if (distSqr > maxDistanceSqr)
                                    return null;

                                // Check water level or grass height
                                float voxelHeight = 0;
                                if (vd.renderType == RenderType.Water) {
                                    voxelHeight = voxels [voxelIndex].GetWaterLevel () / 15f;
                                } else if (vd.renderType == RenderType.CutoutCross) {
                                    voxelHeight = vd.scale.y;
                                }
                                bool hit = true;
                                Vector3d voxelCenter = new Vector3d (chunkX + px + 0.5, chunkY + py + 0.5, chunkZ + pz + 0.5);
                                Vector3d localHitPos = inPosition - voxelCenter;
                                if (voxelHeight > 0 && voxelHeight < 1f && direction.y != 0) {
                                    t = localHitPos.y + 0.5 - voxelHeight;
                                    if (t > 0) {
                                        t = t * Math.Sqrt (1 + (direction.x * direction.x + direction.z * direction.z) / (direction.y * direction.y));
                                        localHitPos += (float)t * direction;
                                        hit = localHitPos.x >= -0.5 && localHitPos.x <= 0.5 && localHitPos.z >= -0.5 && localHitPos.z <= 0.5;
                                    }
                                }
                                if (hit) {
                                    hitInfo = new VoxelHitInfo ();
                                    hitInfo.chunk = chunk;
                                    hitInfo.voxel = voxels [voxelIndex];
                                    hitInfo.point = inPosition - normal;
                                    hitInfo.sqrDistance = (float)distSqr;
                                    hitInfo.voxelIndex = voxelIndex;
                                    hitInfo.voxelCenter = voxelCenter;
                                    hitInfo.placeholder = placeholder;
                                    if (vd.renderType == RenderType.CutoutCross) {
                                        hitInfo.normal = -direction;
                                    } else {
                                        if (localHitPos.y >= 0.495) {
                                            hitInfo.normal = Misc.vector3up;
                                        } else if (localHitPos.y <= -0.495) {
                                            hitInfo.normal = Misc.vector3down;
                                        } else if (localHitPos.x < -0.495) {
                                            hitInfo.normal = Misc.vector3left;
                                        } else if (localHitPos.x > 0.495) {
                                            hitInfo.normal = Misc.vector3right;
                                        } else if (localHitPos.z < -0.495) {
                                            hitInfo.normal = Misc.vector3back;
                                        } else if (localHitPos.z > 0.495) {
                                            hitInfo.normal = Misc.vector3forward;
                                        }
                                    }

#if DEBUG_RAYCAST
																												o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
																												o.transform.localScale = Misc.Vector3one * 0.15f;
																												o.transform.position = inPosition;
																												DestroyImmediate(o.GetComponent<Collider>());
																												o.GetComponent<Renderer>().material.color = Color.red;
#endif

                                    return chunk;
                                }
                            }
                        }

                        db.x = (fx + viewSign.x - inPosition.x) * v3.x;
                        db.y = (fy + viewSign.y - inPosition.y) * v3.y;
                        db.z = (fz + viewSign.z - inPosition.z) * v3.z;

                        db.x = db.x < 0 ? -db.x : db.x;
                        db.y = db.y < 0 ? -db.y : db.y;
                        db.z = db.z < 0 ? -db.z : db.z;

                        t = db.x;
                        normal.x = viewDirSignOffset.x;
                        normal.y = 0;
                        normal.z = 0;
                        if (db.y < t) {
                            t = db.y;
                            normal.x = 0;
                            normal.y = viewDirSignOffset.y;
                        }
                        if (db.z < t) {
                            t = db.z;
                            normal.x = 0;
                            normal.y = 0;
                            normal.z = viewDirSignOffset.z;
                        }

                        inPosition.x += direction.x * t + normal.x;
                        inPosition.y += direction.y * t + normal.y;
                        inPosition.z += direction.z * t + normal.z;
                    }
                }

                db.x = (chunkX + viewSignChunk.x - position.x) * v3.x;
                db.y = (chunkY + viewSignChunk.y - position.y) * v3.y;
                db.z = (chunkZ + viewSignChunk.z - position.z) * v3.z;

                db.x = db.x < 0 ? -db.x : db.x;
                db.y = db.y < 0 ? -db.y : db.y;
                db.z = db.z < 0 ? -db.z : db.z;

                t = db.x;
                normal.x = viewDirSignOffset.x;
                normal.y = 0;
                normal.z = 0;
                if (db.y < t) {
                    t = db.y;
                    normal.x = 0;
                    normal.y = viewDirSignOffset.y;
                }
                if (db.z < t) {
                    t = db.z;
                    normal.x = 0;
                    normal.y = 0;
                    normal.z = viewDirSignOffset.z;
                }

                position.x += direction.x * t + normal.x;
                position.y += direction.y * t + normal.y;
                position.z += direction.z * t + normal.z;

            }
            return null;
        }

        /// <summary>
        /// Checks if a ray intersects an AABB volume of a custom voxel
        /// </summary>
        bool RayIntersectsCustomVoxel(Vector3d origin, Vector3 direction, VoxelChunk chunk, int voxelIndex, out VoxelPlaceholder placeholder)
        {
            placeholder = GetVoxelPlaceholder (chunk, voxelIndex, false);
            if (placeholder == null) return true;

            Rayd ray = new Rayd (origin, direction);
            Bounds bounds = placeholder.GetWorldSpaceBounds ();
            return ray.Intersects (bounds);
        }


        int LineCastFastVoxel (Vector3d startPosition, Vector3d endPosition, VoxelIndex [] indices, int startIndex = 0, byte minOpaque = 0)
        {

#if DEBUG_RAYCAST
                                                GameObject o;
#endif

            // Ray march throuch chunks and fill indices array
            Vector3d position = startPosition;
            VoxelChunk chunk = null;
            double maxDistanceSqr = FastVector.SqrDistanceByValue (startPosition, endPosition);

            Vector3 direction = FastVector.NormalizedDirectionByValue (ref startPosition, ref endPosition);
            Vector3 viewDirSign = FastVector.Sign (ref direction);
            Vector3 viewSign = new Vector3 ((viewDirSign.x + 1f) * 0.5f, (viewDirSign.y + 1f) * 0.5f, (viewDirSign.z + 1f) * 0.5f); // 0 = left, 1 = right

            float vxz, vzy, vxy;
            if (direction.y != 0) {
                float a = direction.x / direction.y;
                float b = direction.z / direction.y;
                vxz = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxz = 1000000f;
            }
            if (direction.x != 0) {
                float a = direction.z / direction.x;
                float b = direction.y / direction.x;
                vzy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vzy = 1000000f;
            }
            if (direction.z != 0) {
                float a = direction.x / direction.z;
                float b = direction.y / direction.z;
                vxy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxy = 1000000f;
            }

            Vector3 v3 = new Vector3 (vzy, vxz, vxy);
            Vector3 viewSignChunk = viewSign * CHUNK_SIZE;
            Vector3 viewDirSignOffset = viewDirSign * 0.002f;

            int chunkCount = 0;
            double t;
            Vector3d normal = Vector3d.zero, db;

            while (chunkCount++ < 500) { // safety counter to avoid any potential infinite loop

                // Check max distance
                double distSqr = (position.x - startPosition.x) * (position.x - startPosition.x) + (position.y - startPosition.y) * (position.y - startPosition.y) + (position.z - startPosition.z) * (position.z - startPosition.z);
                if (distSqr > maxDistanceSqr)
                    break;


#if DEBUG_RAYCAST
                                                                o = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                                                o.transform.localScale = Misc.Vector3one * 0.15f;
                                                                o.transform.position = position;
                                                                DestroyImmediate(o.GetComponent<Collider>());
                                                                o.GetComponent<Renderer>().material.color = Color.blue;
#endif

                FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);

                int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
                int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
                int hash = x00 + y00 + chunkZ;
                chunk = GetChunkIfExists (hash);

                chunkX *= CHUNK_SIZE;
                chunkY *= CHUNK_SIZE;
                chunkZ *= CHUNK_SIZE;

                if (chunk) {
                    // Ray-march through chunk
                    Voxel [] voxels = chunk.voxels;
                    Vector3d inPosition = position;

                    for (int k = 0; k < 64; k++) {

#if DEBUG_RAYCAST
                                                                                                o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                                                                                o.transform.localScale = Misc.Vector3one * 0.1f;
                                                                                                o.transform.position = inPosition;
                                                                                                DestroyImmediate(o.GetComponent<Collider>());
                                                                                                o.GetComponent<Renderer>().material.color = Color.yellow;
#endif

                        // Check voxel content
                        int fx, fy, fz;
                        FastMath.FloorToInt (inPosition.x, inPosition.y, inPosition.z, out fx, out fy, out fz);
                        int py = fy - chunkY;
                        int pz = fz - chunkZ;
                        int px = fx - chunkX;
                        if (px < 0 || px >= CHUNK_SIZE || py < 0 || py >= CHUNK_SIZE || pz < 0 || pz >= CHUNK_SIZE) {
                            break;
                        }

                        int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                        if (voxels [voxelIndex].hasContent == 1 && (minOpaque == 255 || voxels [voxelIndex].opaque >= minOpaque)) {
                            if (startIndex >= indices.Length) {
                                chunkCount = int.MaxValue;
                                break;
                            }

                            // Check max distance
                            distSqr = (inPosition.x - startPosition.x) * (inPosition.x - startPosition.x) + (inPosition.y - startPosition.y) * (inPosition.y - startPosition.y) + (inPosition.z - startPosition.z) * (inPosition.z - startPosition.z);
                            if (distSqr > maxDistanceSqr) {
                                chunkCount = int.MaxValue;
                                break;
                            }

                            indices [startIndex].chunk = chunk;
                            indices [startIndex].voxelIndex = voxelIndex;
                            indices [startIndex].position = inPosition;
                            startIndex++;
                        }

                        db.x = (fx + viewSign.x - inPosition.x) * v3.x;
                        db.y = (fy + viewSign.y - inPosition.y) * v3.y;
                        db.z = (fz + viewSign.z - inPosition.z) * v3.z;

                        db.x = db.x < 0 ? -db.x : db.x;
                        db.y = db.y < 0 ? -db.y : db.y;
                        db.z = db.z < 0 ? -db.z : db.z;

                        t = db.x;
                        normal.x = viewDirSignOffset.x;
                        normal.y = 0;
                        normal.z = 0;
                        if (db.y < t) {
                            t = db.y;
                            normal.x = 0;
                            normal.y = viewDirSignOffset.y;
                        }
                        if (db.z < t) {
                            t = db.z;
                            normal.x = 0;
                            normal.y = 0;
                            normal.z = viewDirSignOffset.z;
                        }

                        inPosition.x += direction.x * t + normal.x;
                        inPosition.y += direction.y * t + normal.y;
                        inPosition.z += direction.z * t + normal.z;
                    }
                }

                db.x = (chunkX + viewSignChunk.x - position.x) * v3.x;
                db.y = (chunkY + viewSignChunk.y - position.y) * v3.y;
                db.z = (chunkZ + viewSignChunk.z - position.z) * v3.z;

                db.x = db.x < 0 ? -db.x : db.x;
                db.y = db.y < 0 ? -db.y : db.y;
                db.z = db.z < 0 ? -db.z : db.z;

                t = db.x;
                normal.x = viewDirSignOffset.x;
                normal.y = 0;
                normal.z = 0;
                if (db.y < t) {
                    t = db.y;
                    normal.x = 0;
                    normal.y = viewDirSignOffset.y;
                }
                if (db.z < t) {
                    t = db.z;
                    normal.x = 0;
                    normal.y = 0;
                    normal.z = viewDirSignOffset.z;
                }

                position.x += direction.x * t + normal.x;
                position.y += direction.y * t + normal.y;
                position.z += direction.z * t + normal.z;

            }
            return startIndex;
        }



        int LineCastFastChunk (Vector3d startPosition, Vector3d endPosition, VoxelChunk [] chunks, int startIndex = 0)
        {

#if DEBUG_RAYCAST
			GameObject o;
#endif

            // Ray march throuch chunks and fill indices array
            Vector3d position = startPosition;
            VoxelChunk chunk = null;
            double maxDistanceSqr = FastVector.SqrDistanceByValue (startPosition, endPosition);

            Vector3 direction = FastVector.NormalizedDirectionByValue (ref startPosition, ref endPosition);
            Vector3 viewDirSign = FastVector.Sign (ref direction);
            Vector3 viewSign = new Vector3 ((viewDirSign.x + 1f) * 0.5f, (viewDirSign.y + 1f) * 0.5f, (viewDirSign.z + 1f) * 0.5f); // 0 = left, 1 = right

            float vxz, vzy, vxy;
            if (direction.y != 0) {
                float a = direction.x / direction.y;
                float b = direction.z / direction.y;
                vxz = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxz = 1000000f;
            }
            if (direction.x != 0) {
                float a = direction.z / direction.x;
                float b = direction.y / direction.x;
                vzy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vzy = 1000000f;
            }
            if (direction.z != 0) {
                float a = direction.x / direction.z;
                float b = direction.y / direction.z;
                vxy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxy = 1000000f;
            }

            Vector3 v3 = new Vector3 (vzy, vxz, vxy);
            Vector3 viewSignChunk = viewSign * CHUNK_SIZE;
            Vector3 viewDirSignOffset = viewDirSign * 0.002f;

            int chunkX, chunkY, chunkZ;
            int chunkCount = 0;
            double t;
            Vector3d normal = Vector3d.zero, db;

            while (chunkCount++ < 500) { // safety counter to avoid any potential infinite loop

                // Check max distance
                double distSqr = (position.x - startPosition.x) * (position.x - startPosition.x) + (position.y - startPosition.y) * (position.y - startPosition.y) + (position.z - startPosition.z) * (position.z - startPosition.z);
                if (distSqr > maxDistanceSqr)
                    break;

                FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);

                int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
                int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
                int hash = x00 + y00 + chunkZ;
                chunk = GetChunkIfExists (hash);

                chunkX *= CHUNK_SIZE;
                chunkY *= CHUNK_SIZE;
                chunkZ *= CHUNK_SIZE;

                if (chunk) {
                    if (startIndex >= chunks.Length) {
                        break;
                    }
                    chunks [startIndex++] = chunk;
                }

                db.x = (chunkX + viewSignChunk.x - position.x) * v3.x;
                db.y = (chunkY + viewSignChunk.y - position.y) * v3.y;
                db.z = (chunkZ + viewSignChunk.z - position.z) * v3.z;

                db.x = db.x < 0 ? -db.x : db.x;
                db.y = db.y < 0 ? -db.y : db.y;
                db.z = db.z < 0 ? -db.z : db.z;

                t = db.x;
                normal.x = viewDirSignOffset.x;
                normal.y = 0;
                normal.z = 0;
                if (db.y < t) {
                    t = db.y;
                    normal.x = 0;
                    normal.y = viewDirSignOffset.y;
                }
                if (db.z < t) {
                    t = db.z;
                    normal.x = 0;
                    normal.y = 0;
                    normal.z = viewDirSignOffset.z;
                }

                position.x += direction.x * t + normal.x;
                position.y += direction.y * t + normal.y;
                position.z += direction.z * t + normal.z;

            }
            return startIndex;
        }



        bool HitVoxelFast (Vector3d origin, Vector3 direction, int damage, out VoxelHitInfo hitInfo, float maxDistance = 0, int damageRadius = 1, bool addParticles = true, bool playSound = true, int layerMask = -1, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {

            RayCastFast (origin, direction, out hitInfo, maxDistance, false, 0, ColliderTypes.IgnorePlayer, layerMask, queryTriggerInteraction);
            VoxelChunk chunk = hitInfo.chunk;
            if ((object)chunk == null || hitInfo.voxelIndex < 0) {
                lastHitInfo.chunk = null;
                lastHitInfo.voxelIndex = -1;
                return false;
            }

            lastHitInfo = hitInfo;
            DamageVoxelFast (ref hitInfo, damage, addParticles, playSound);
            if (damageRadius > 1) {
                Vector3d otherPos;
                Vector3d explosionPosition = hitInfo.voxelCenter + hitInfo.normal * damageRadius;
                damageRadius--;

                for (int y = -damageRadius; y <= damageRadius; y++) {
                    otherPos.y = lastHitInfo.voxelCenter.y + y;
                    for (int z = -damageRadius; z <= damageRadius; z++) {
                        otherPos.z = lastHitInfo.voxelCenter.z + z;
                        for (int x = -damageRadius; x <= damageRadius; x++) {
                            if (x == 0 && z == 0 && y == 0)
                                continue;
                            VoxelChunk otherChunk;
                            int otherIndex;
                            otherPos.x = lastHitInfo.voxelCenter.x + x;
                            if (GetVoxelIndex (otherPos, out otherChunk, out otherIndex, false)) {
                                if (GetVoxelVisibility (otherChunk, otherIndex)) {
                                    FastVector.NormalizedDirection (ref explosionPosition, ref otherPos, out direction);
                                    if (RayCast (explosionPosition, direction, out hitInfo, damageRadius)) {
                                        DamageVoxelFast (ref hitInfo, damage, addParticles, playSound);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }


        int DamageAreaFast (Vector3d origin, int damage, int damageRadius = 1, bool distanceAttenuation = true, bool addParticles = true, List<VoxelIndex> results = null, bool playSound = false, bool showDamageCracks = false, bool canAddRecoverableVoxel = true)
        {
            if (captureEvents && OnVoxelAfterAreaDamage != null) {
                if (results == null) {
                    results = new List<VoxelIndex> ();
                }
            }

            bool hasResults = results != null;
            if (hasResults) {
                results.Clear ();
            }
            if (damageRadius < 0 || damage < 1) {
                return 0;
            }

            int damagedCount = 0;
            List<VoxelIndex> tempVoxelIndicesDamageArea = BufferPool<VoxelIndex>.Get ();
            VoxelHitInfo hitInfo = new VoxelHitInfo ();
            GetVoxelIndices (origin, damageRadius, tempVoxelIndicesDamageArea);
            if (captureEvents && OnVoxelBeforeAreaDamage != null) {
                OnVoxelBeforeAreaDamage (tempVoxelIndicesDamageArea);
            }
            int count = tempVoxelIndicesDamageArea.Count;

            float damageRadiusSqr = damageRadius * damageRadius;
            destroyedVoxelParticlesAmount = 5;
            for (int k = 0; k < count; k++) {
                VoxelIndex vi = tempVoxelIndicesDamageArea [k];
                VoxelChunk otherChunk = vi.chunk;
                int otherIndex = vi.voxelIndex;
                int dam = damage;
                if (distanceAttenuation && vi.sqrDistance > 1) {
                    float atten = (damageRadiusSqr - vi.sqrDistance) / damageRadiusSqr;
                    dam = (int)(damage * atten);
                }
                if (dam > 0) {
                    // Approximates a theorical hit point
                    FastVector.NormalizedDirection (ref vi.position, ref origin, out hitInfo.normal);
                    hitInfo.chunk = otherChunk;
                    hitInfo.voxelIndex = otherIndex;
                    hitInfo.voxel = otherChunk.voxels [otherIndex];
                    hitInfo.voxelCenter = GetVoxelPosition (otherChunk, otherIndex);
                    hitInfo.sqrDistance = vi.sqrDistance;
                    hitInfo.point = hitInfo.voxelCenter + hitInfo.normal * 0.5f;

                    int damageTaken = DamageVoxelFast (ref hitInfo, dam, addParticles, playSound, showDamageCracks, canAddRecoverableVoxel);
                    if (hasResults) {
                        VoxelIndex di = vi;
                        di.damageTaken = damageTaken;
                        results.Add (di);
                    }
                    damagedCount++;
                }
            }

            BufferPool<VoxelIndex>.Release (tempVoxelIndicesDamageArea);
            if (captureEvents && OnVoxelAfterAreaDamage != null) {
                OnVoxelAfterAreaDamage (results);
            }
            destroyedVoxelParticlesAmount = DEFAULT_DESTROYED_VOXEL_PARTICLE_AMOUNT;
            return damagedCount;
        }

        /// <summary>
        /// Performs the voxel damage.
        /// </summary>
        /// <returns>The actual damage taken by the voxe.</returns>
        /// <param name="hitInfo">Hit info.</param>
        /// <param name="damage">Damage.</param>
        /// <param name="addParticles">If set to <c>true</c> add particles.</param>
        /// <param name="canAddRecoverableVoxel">If true, when voxel is destroyed, a floating recoverable voxel can be dropped.</param>
        int DamageVoxelFast (ref VoxelHitInfo hitInfo, int damage, bool addParticles, bool playSound, bool showDamageCracks = true, bool canAddRecoverableVoxel = true)
        {

            VoxelChunk chunk = hitInfo.chunk;
            if (hitInfo.voxel.typeIndex == 0 || hitInfo.voxelIndex < 0)
                return 0;
            lastHitInfo = hitInfo;
            VoxelDefinition voxelType = voxelDefinitions [hitInfo.voxel.typeIndex];
            byte voxelTypeResistancePoints = voxelType.resistancePoints;

            if (damage < 255) { 
                if (voxelTypeResistancePoints == (byte)0) {
                    damage = 0;
                } else if (voxelTypeResistancePoints == (byte)255) {
                    if (playSound) {
                        PlayImpactSound (hitInfo.voxel.type.impactSound, hitInfo.voxelCenter);
                    }
                    damage = 0;
                }
            }

            if (captureEvents) {
                if (OnVoxelDamaged != null) {
                    OnVoxelDamaged (chunk, hitInfo.voxelIndex, ref damage);
                }
                if (OnVoxelDamagedHitInfo != null) {
                    OnVoxelDamagedHitInfo (hitInfo, ref damage);
                }
            }

            if (damage == 0)
                return 0;

            // Get voxel damage indicator GO's name
            bool destroyed = voxelType.resistancePoints <= damage;
            int resistancePointsLeft = 0;
            VoxelPlaceholder placeholder = null;
            if (!destroyed) {
                placeholder = GetVoxelPlaceholder (chunk, hitInfo.voxelIndex, true);
                resistancePointsLeft = placeholder.resistancePointsLeft - damage;
                if (resistancePointsLeft <= 0) {
                    resistancePointsLeft = 0;
                    destroyed = true;
                }
                placeholder.resistancePointsLeft = resistancePointsLeft;
            }

            if (voxelType.renderType == RenderType.Invisible)
                addParticles = false;

            int particlesAmount;
            float voxelLight;

            if (destroyed) {

                // Add recoverable voxel on the scene (not for vegetation)
                if (voxelType.renderType != RenderType.Invisible && voxelType.renderType != RenderType.CutoutCross && voxelType.canBeCollected && !buildMode) {
                    if (canAddRecoverableVoxel) {
                        bool create = true;

                        if (captureEvents && OnVoxelBeforeDropItem != null) {
                            OnVoxelBeforeDropItem (chunk, hitInfo, out create);
                        }
                        if (create) {
                            CreateRecoverableVoxel (hitInfo.voxelCenter, voxelType, hitInfo.voxel.color);
                        }
                    }
                }

                // Destroy the voxel
                VoxelDestroyFast (chunk, hitInfo.voxelIndex);

                // Check if grass is on top and remove it as well
                VoxelChunk topChunk;
                int topIndex;
                if (GetVoxelIndex (hitInfo.voxelCenter + Misc.vector3up, out topChunk, out topIndex, false)) {
                    if (topChunk.voxels [topIndex].typeIndex != 0 && voxelDefinitions [topChunk.voxels [topIndex].typeIndex].renderType == RenderType.CutoutCross) {
                        byte light = topChunk.voxels [topIndex].lightOrTorch;
                        topChunk.voxels [topIndex].Clear (light);
                        RegisterChunkChanges (topChunk);
                    }
                }

                // Max particles
                particlesAmount = destroyedVoxelParticlesAmount;

                if (playSound) {
                    PlayDestructionSound (voxelType.destructionSound, hitInfo.voxelCenter);
                }

                // Gets light at the destroyed voxel position
                voxelLight = GetVoxelLight (hitInfo.voxelCenter);

            } else {

                // Gets ambient light near surface
                voxelLight = GetVoxelLight (hitInfo.point + hitInfo.normal * 0.5f);

                // Add damage indicator
                float lifePerc = (float)resistancePointsLeft / voxelTypeResistancePoints;
                int textureIndex = FastMath.FloorToInt (world.voxelDamageTextures.Length * lifePerc);

                if (showDamageCracks && voxelType.showDamageCracks) {
                    if (placeholder.damageIndicator == null) {
                        if (damagedVoxelPrefab == null) {
                            damagedVoxelPrefab = Resources.Load<GameObject> ("VoxelPlay/Prefabs/DamagedVoxel");
                        }
                        GameObject g = Instantiate (damagedVoxelPrefab);
                        g.name = DAMAGE_INDICATOR;
                        Transform tDamageIndicator = g.transform;
                        placeholder.damageIndicator = tDamageIndicator.GetComponent<Renderer> ();
                        if (placeholder.modelMeshFilter != null && placeholder.modelMeshFilter.sharedMesh != null) {
                            tDamageIndicator.SetParent (placeholder.modelMeshFilter.transform, false);
                            Mesh mesh = placeholder.modelMeshFilter.sharedMesh;
                            tDamageIndicator.localPosition = mesh.bounds.center;
                            tDamageIndicator.localScale = mesh.bounds.size * 1.001f;
                        } else {
                            tDamageIndicator.SetParent (placeholder.transform, false);
                            tDamageIndicator.localPosition = placeholder.bounds.center;
                            tDamageIndicator.localScale = placeholder.bounds.size * 1.001f;
                        }
                    }

                    if (world.voxelDamageTextures.Length > 0) {
                        if (textureIndex >= world.voxelDamageTextures.Length) {
                            textureIndex = world.voxelDamageTextures.Length - 1;
                        }
                        Material mi = placeholder.damageIndicatorMaterial; // gets a copy of material the first time it's used
                        mi.mainTexture = world.voxelDamageTextures [textureIndex];
                        mi.SetFloat (ShaderParams.VoxelLight, voxelLight);
                        placeholder.damageIndicator.enabled = true;
                    }
                }

                // Particle amount depending of damage
                particlesAmount = (6 - (int)(5f * lifePerc)) + 3;

                // Sets health recovery for the voxel
                placeholder.StartHealthRecovery (chunk, hitInfo.voxelIndex, world.damageDuration);

                if (playSound) {
                    PlayImpactSound (voxelType.impactSound, hitInfo.voxelCenter);
                }
            }

            // Add random particles
            if (addParticles) {
                Vector3 camForward = cameraMain.transform.forward;
                float now = Time.time;
                for (int k = 0; k < particlesAmount; k++) {
                    int ppeIndex = GetParticleFromPool ();
                    if (ppeIndex < 0)
                        break;

                    // Scale of particle
                    Renderer particleRenderer = particlePool [ppeIndex].renderer;
                    float startScale, endScale;
                    if (destroyed) {
                        if (voxelType.renderType == RenderType.CutoutCross) {   // smaller particles for vegetation
                            float rnd = Random.Range (0.03f, 0.04f);
                            startScale = rnd; ;
                            endScale = rnd * 0.6f;
                        } else {
                            float rnd = Random.Range (0.04f, 0.1f);
                            startScale = rnd * 4f;
                            endScale = rnd;
                        }
                    } else {
                        float rnd = Random.Range (0.03f, 0.06f);
                        startScale = endScale = rnd;
                    }
                    particleRenderer.transform.localScale = new Vector3 (startScale, startScale, startScale);
                    particlePool [ppeIndex].startScale = startScale;
                    particlePool [ppeIndex].endScale = endScale;

                    // Set particle texture
                    Material instanceMat = particleRenderer.sharedMaterial;
                    SetParticleMaterialTextures (instanceMat, voxelType, hitInfo.voxel.color);
                    instanceMat.mainTextureOffset = new Vector2 (Random.value, Random.value);
                    instanceMat.mainTextureScale = new Vector2 (0.05f, 0.05f);
                    instanceMat.SetFloat (ShaderParams.VoxelLight, voxelLight);
                    instanceMat.SetFloat (ShaderParams.FlashDelay, 0);

                    // Set position
                    Rigidbody rb = particlePool [ppeIndex].rigidBody;
                    Vector3d particlePos;
                    if (destroyed) {
                        Vector3 expelDir = Random.insideUnitSphere;
                        particlePos = hitInfo.center;
                        FastVector.Add (ref particlePos, ref expelDir, 0.6f);
                        float rnd = Random.value * 125f;
                        rb.AddForce (expelDir * rnd);
                    } else {
                        particlePos = hitInfo.point;
                        Vector3 v1 = new Vector3 (-hitInfo.normal.y, hitInfo.normal.z, hitInfo.normal.x);
                        Vector3 v2 = new Vector3 (-hitInfo.normal.z, hitInfo.normal.x, hitInfo.normal.y);
                        Vector3 dx = (Random.value - 0.5f) * 0.7f * v1;
                        Vector3 dy = (Random.value - 0.5f) * 0.7f * v2;
                        particlePos += hitInfo.normal * 0.001f + dx + dy;
                        rb.AddForce (camForward * (Random.value * -125f));
                        rb.AddForce (new Vector3 (0, 25f, 0));
                    }
                    rb.AddTorque (Random.onUnitSphere * 100f);
                    rb.useGravity = true;

                    // Self-destruct
                    particlePool [ppeIndex].creationTime = now;
                    particlePool [ppeIndex].destructionTime = now + 2.5f + Random.value;

                    // Anotate particle voxel so light is not recalculated this frame
                    particleRenderer.transform.position = particlePos;
                    particlePool [ppeIndex].lastX = (int)particlePos.x;
                    particlePool [ppeIndex].lastY = (int)particlePos.y;
                    particlePool [ppeIndex].lastZ = (int)particlePos.z;
                }
            }

            if (captureEvents) {
                if (OnVoxelAfterDamaged != null) {
                    OnVoxelAfterDamaged (chunk, hitInfo.voxelIndex, damage);
                }
                if (OnVoxelAfterDamagedHitInfo != null) {
                    OnVoxelAfterDamagedHitInfo (hitInfo, damage);
                }
            }

            return damage;
        }

        /// <summary>
        /// Creates a bunch of particles at a given position and moves them according to style
        /// </summary>
        public virtual void ParticleBurst (Vector3d position, ParticleBurstStyle style, int particleCount, float intensity, VoxelDefinition materialVoxelDefinition = null, Gradient colors = null)
        {
            if (materialVoxelDefinition == null) {
                Voxel voxel = GetVoxel (position, false);
                materialVoxelDefinition = voxel.type;
            }

            // Add random particles
            float now = Time.time;
            for (int k = 0; k < particleCount; k++) {
                int ppeIndex = GetParticleFromPool ();
                if (ppeIndex < 0)
                    return;

                // Scale of particle
                Renderer particleRenderer = particlePool [ppeIndex].renderer;
                float startScale, endScale;
                float rnd;
                if (materialVoxelDefinition.renderType == RenderType.CutoutCross) {   // smaller particles for vegetation
                    rnd = Random.Range (0.03f, 0.04f);
                    startScale = rnd; ;
                    endScale = rnd * 0.6f;
                } else {
                    rnd = Random.Range (0.04f, 0.1f);
                    startScale = rnd * 4f;
                    endScale = rnd;
                }
                particleRenderer.transform.localScale = new Vector3 (startScale, startScale, startScale);
                particlePool [ppeIndex].startScale = startScale;
                particlePool [ppeIndex].endScale = endScale;

                // Set particle texture
                Material instanceMat = particleRenderer.sharedMaterial;
                Color tintColor = colors != null ? colors.Evaluate (Random.value) : Misc.colorWhite;
                SetParticleMaterialTextures (instanceMat, materialVoxelDefinition, tintColor);
                instanceMat.mainTextureOffset = new Vector2 (Random.value, Random.value);
                instanceMat.mainTextureScale = new Vector2 (0.05f, 0.05f);
                float voxelLight = GetVoxelLight (position);
                instanceMat.SetFloat (ShaderParams.VoxelLight, voxelLight);
                instanceMat.SetFloat (ShaderParams.FlashDelay, 0);

                // Set position
                Rigidbody rb = particlePool [ppeIndex].rigidBody;
                Vector3d particlePos;
                Vector3 expelDir = Random.insideUnitSphere;
                particlePos = position;
                FastVector.Add (ref particlePos, ref expelDir, 0.6f);

                switch (style) {
                case ParticleBurstStyle.Explosion:
                    rnd = Random.value * 125f;
                    rb.AddForce (expelDir * rnd * intensity);
                    rb.AddTorque (Random.onUnitSphere * 100f);
                    break;
                }
                rb.useGravity = true;

                // Self-destruct
                particlePool [ppeIndex].creationTime = now;
                particlePool [ppeIndex].destructionTime = now + 2.5f + Random.value;

                // Anotate particle voxel so light is not recalculated this frame
                particleRenderer.transform.position = particlePos;
                particlePool [ppeIndex].lastX = (int)particlePos.x;
                particlePool [ppeIndex].lastY = (int)particlePos.y;
                particlePool [ppeIndex].lastZ = (int)particlePos.z;
            }
        }


        /// <summary>
        /// Plays impact sound at position.
        /// </summary>
        /// <param name="sound">Custom audioclip or pass null to use default impact sound defined in Voxel Play Environment component.</param>
        public virtual void PlayImpactSound (AudioClip sound, Vector3 position)
        {
            if (sound == null)
                sound = defaultImpactSound;
            if (sound != null) {
                AudioSource.PlayClipAtPoint (sound, position);
            }
        }

        /// <summary>
        /// Plays voxel build sound at position
        /// </summary>
        /// <param name="sound">Custom audioclip or pass null to use default build sound defined in Voxel Play Environment component.</param>
        public virtual void PlayBuildSound (AudioClip sound, Vector3 position)
        {
            if (sound == null)
                sound = defaultBuildSound;
            if (sound != null) {
                AudioSource.PlayClipAtPoint (sound, position);
            }
        }

        /// <summary>
        /// Plays voxel destruction sound at position
        /// </summary>
        /// <param name="sound">Custom audioclip or pass null to use default destruction sound defined in Voxel Play Environment component.</param>
        public virtual void PlayDestructionSound (AudioClip sound, Vector3 position)
        {
            if (sound == null)
                sound = defaultDestructionSound;
            if (sound != null) {
                AudioSource.PlayClipAtPoint (sound, position);
            }
        }


        /// <summary>
        /// Checks if there's a voxel at given position.
        /// </summary>
        /// <returns><c>true</c>, if collision was checked, <c>false</c> otherwise.</returns>
        /// <param name="position">Position.</param>
        public virtual bool CheckCollision (Vector3d position)
        {
            FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int x, out int y, out int z);
            VoxelChunk chunk = GetChunkOrCreate (x, y, z);
            if ((object)chunk != null) {
                Voxel [] voxels = chunk.voxels;
                int py = (int)(position.y - y * CHUNK_SIZE);
                int pz = (int)(position.z - z * CHUNK_SIZE);
                int px = (int)(position.x - x * CHUNK_SIZE);
                int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                bool collision = voxels[voxelIndex].hasContent == 1 && voxelDefinitions[voxels[voxelIndex].typeIndex].isSolid;
                return collision;
            }
            return false;
        }

        /// <summary>
        /// Returns a VoxelHitInfo structure from basic hit data
        /// </summary>
        public bool BuildVoxelHitInfo(out VoxelHitInfo hitInfo, Vector3d voxelCenter, Vector3d hitPoint, Vector3 normal = default(Vector3)) {
            hitInfo = new VoxelHitInfo {
                voxelCenter = voxelCenter,
                point = hitPoint
            };
            if (normal == default(Vector3))
            {
                Vector3d localHitPos = hitPoint - voxelCenter;
                if (localHitPos.y >= 0.495)
                {
                    hitInfo.normal = Misc.vector3up;
                }
                else if (localHitPos.y <= -0.495)
                {
                    hitInfo.normal = Misc.vector3down;
                }
                else if (localHitPos.x < -0.495)
                {
                    hitInfo.normal = Misc.vector3left;
                }
                else if (localHitPos.x > 0.495)
                {
                    hitInfo.normal = Misc.vector3right;
                }
                else if (localHitPos.z < -0.495)
                {
                    hitInfo.normal = Misc.vector3back;
                }
                else if (localHitPos.z > 0.495)
                {
                    hitInfo.normal = Misc.vector3forward;
                }
            }
            if (!GetVoxelIndex (voxelCenter, out hitInfo.chunk, out hitInfo.voxelIndex)) {
                return false;
            }
            hitInfo.voxel = GetVoxel (voxelCenter, false);
            return hitInfo.voxel != Voxel.Empty;
        }


        /// <summary>
        /// Returns a VoxelHitInfo structure from basic hit data
        /// </summary>
        public bool BuildVoxelHitInfo(out VoxelHitInfo hitInfo, VoxelChunk chunk, int voxelIndex, Vector3d voxelCenter, Vector3d hitPoint, Vector3 normal = default(Vector3))
        {
            hitInfo = new VoxelHitInfo
            {
                voxelCenter = voxelCenter,
                point = hitPoint
            };
            if (normal == default(Vector3))
            {
                Vector3d localHitPos = hitPoint - voxelCenter;
                if (localHitPos.y >= 0.495)
                {
                    hitInfo.normal = Misc.vector3up;
                }
                else if (localHitPos.y <= -0.495)
                {
                    hitInfo.normal = Misc.vector3down;
                }
                else if (localHitPos.x < -0.495)
                {
                    hitInfo.normal = Misc.vector3left;
                }
                else if (localHitPos.x > 0.495)
                {
                    hitInfo.normal = Misc.vector3right;
                }
                else if (localHitPos.z < -0.495)
                {
                    hitInfo.normal = Misc.vector3back;
                }
                else if (localHitPos.z > 0.495)
                {
                    hitInfo.normal = Misc.vector3forward;
                }
            }
            hitInfo.chunk = chunk;
            hitInfo.voxelIndex = voxelIndex;
            hitInfo.voxel = chunk.voxels[voxelIndex];
            return hitInfo.voxel != Voxel.Empty;
        }


    }



}
