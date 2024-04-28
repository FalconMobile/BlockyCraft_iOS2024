using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NavMeshBuilder = UnityEngine.AI.NavMeshBuilder;


namespace VoxelPlay
{


    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        NavMeshData navMeshData;
        NavMeshDataInstance navMeshInstance;
        NavMeshBuildSettings navMeshBuildSettings;
        List<NavMeshBuildSource> navMeshSources;
        readonly Queue<int> navMeshDisposedSources = new Queue<int> ();
        AsyncOperation navMeshUpdateOperation;
        Bounds worldBounds;
        bool navMeshIsUpdating, navMeshHasNewData;
        float navMeshLastBakeTime;

        void InitNavMesh ()
        {
            if (!enableNavMesh) return;
            navMeshBuildSettings = NavMesh.GetSettingsByIndex (0);
            navMeshBuildSettings.agentClimb = 1f;
            navMeshBuildSettings.agentSlope = 60;
            navMeshBuildSettings.agentHeight = 1.5f;

            navMeshSources = Misc.GetList<NavMeshBuildSource> (lowMemoryMode, 2048);
            navMeshData = new NavMeshData ();
            navMeshInstance = NavMesh.AddNavMeshData (navMeshData);
            worldBounds = new Bounds ();
        }

        void DestroyNavMesh ()
        {
            if (navMeshInstance.valid) {
                NavMesh.RemoveNavMeshData (navMeshInstance);
            }
        }

        void AddChunkNavMesh (VoxelChunk chunk)
        {
            if (!applicationIsPlaying || (object)chunk.navMesh == null)
                return;
            if (chunk.navMeshSourceIndex < 0) {
                NavMeshBuildSource source = new NavMeshBuildSource ();
                source.shape = NavMeshBuildSourceShape.Mesh;
                source.size = chunk.navMesh.bounds.size;
                source.sourceObject = chunk.navMesh;
                source.transform = chunk.transform.localToWorldMatrix;
                if (navMeshDisposedSources.Count > 0) {
                    int freeIndex = navMeshDisposedSources.Dequeue ();
                    chunk.navMeshSourceIndex = freeIndex;
                    navMeshSources [freeIndex] = source;
                } else {
                    int count = navMeshSources.Count;
                    chunk.navMeshSourceIndex = count;
                    navMeshSources.Add (source);
                }
            } else {
                NavMeshBuildSource source = navMeshSources [chunk.navMeshSourceIndex];
                source.size = chunk.navMesh.bounds.size;
                source.sourceObject = chunk.navMesh;
                source.transform = chunk.transform.localToWorldMatrix;
                navMeshSources [chunk.navMeshSourceIndex] = source;
            }
            chunk.navMeshUpdateRequestTime = Time.time;
            worldBounds.Encapsulate (chunk.mr.bounds);
            worldBounds.Expand (0.1f);
            navMeshHasNewData = true;
        }

        /// <summary>
        /// Frees this navmesh entry so other chunks can use it
        /// </summary>
        void ReleaseChunkNavMesh (VoxelChunk chunk)
        {
            if (!applicationIsPlaying || (object)chunk.navMesh == null)
                return;
            if (chunk.navMeshSourceIndex >= 0) {
                navMeshDisposedSources.Enqueue (chunk.navMeshSourceIndex);
                chunk.navMeshSourceIndex = -1;
            }
        }

        void UpdateNavMesh ()
        {
            if (navMeshIsUpdating) {
                if (navMeshUpdateOperation.isDone) {
                    navMeshIsUpdating = false;
                    navMeshLastBakeTime = Time.time;
                }
            } else if (navMeshHasNewData) {
                try {
                    navMeshUpdateOperation = NavMeshBuilder.UpdateNavMeshDataAsync (navMeshData, navMeshBuildSettings, navMeshSources, worldBounds);
                    navMeshIsUpdating = true;
                }
#if UNITY_EDITOR
                catch (Exception ex) {
                    Debug.Log (ex.ToString ());
                }
#else
				catch {
				}
#endif

                navMeshHasNewData = false;
            }
        }
    }



}
