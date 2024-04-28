using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay {


    public partial class VoxelPlayEnvironment : MonoBehaviour {

        #region Chunk unload management

        bool needCheckUnloadChunks;
        int checkChunksVisibleDistanceIndex = -1;

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        void TriggerFarChunksUnloadCheck() {
            needCheckUnloadChunks = true;
        }

        void CheckChunksVisibleDistance(long maxFrameTime) {
            if (needCheckUnloadChunks) {
                if (checkChunksVisibleDistanceIndex < 0) { // nothing left to check, then we can start a chunk distance check again
                    needCheckUnloadChunks = false;
                    checkChunksVisibleDistanceIndex = chunksPoolFirstReusableIndex;
                }
            }
            if (checkChunksVisibleDistanceIndex >= 0) {
                CheckChunksVisibleDistanceLoop(maxFrameTime);
            }
        }

        void CheckChunksVisibleDistanceLoop(long maxFrameTime) {

            try {
                bool eventOut = OnChunkExitVisibleDistance != null;
                bool eventIn = OnChunkEnterVisibleDistance != null;
                int max = checkChunksVisibleDistanceIndex + 200;
                if (max >= chunksPoolLoadIndex) max = chunksPoolLoadIndex;
                double visibleDistanceSqr = (_visibleChunksDistance + 1) * CHUNK_SIZE;
                visibleDistanceSqr *= visibleDistanceSqr;
                while (checkChunksVisibleDistanceIndex < max) {
                    VoxelChunk chunk = chunksPool[checkChunksVisibleDistanceIndex];
                    checkChunksVisibleDistanceIndex++;
                    if (chunk.isPopulated && !chunk.isCloud) {
                        double dist = FastVector.SqrMaxDistanceXorZ(ref chunk.position, ref currentAnchorPos);
                        if (dist > visibleDistanceSqr) {
                            if (chunk.visibleDistanceStatus != ChunkVisibleDistanceStatus.OutOfVisibleDistance) {
                                chunk.visibleDistanceStatus = ChunkVisibleDistanceStatus.OutOfVisibleDistance;
                                if (unloadFarChunks || eventOut) {
                                    if (eventOut) { OnChunkExitVisibleDistance(chunk); }
                                    if (unloadFarChunks) { chunk.gameObject.SetActive (false); }
                                    if (unloadFarNavMesh) { ReleaseChunkNavMesh (chunk); }
                                    if (stopWatch.ElapsedMilliseconds >= maxFrameTime) break;
                                }
                            }
                        } else if (chunk.visibleDistanceStatus != ChunkVisibleDistanceStatus.WithinVisibleDistance) {
                            chunk.visibleDistanceStatus = ChunkVisibleDistanceStatus.WithinVisibleDistance;
                            if (unloadFarChunks || eventIn) {
                                if (unloadFarChunks) { chunk.gameObject.SetActive (true); }
                                if (unloadFarNavMesh && chunk.navMeshSourceIndex < 0) { AddChunkNavMesh (chunk); }
                                if (eventIn) { OnChunkEnterVisibleDistance(chunk); }
                                if (stopWatch.ElapsedMilliseconds >= maxFrameTime) break;
                            }
                        }
                    }
                }
                if (checkChunksVisibleDistanceIndex >= chunksPoolLoadIndex) {
                    checkChunksVisibleDistanceIndex = -1;
                }
            } catch (Exception ex) {
                ShowExceptionMessage(ex);
            }
        }

        /// <summary>
        /// Sets the unloadFarChunks setting value and refresh chunk visible status
        /// </summary>
        /// <param name="unloadFarChunks"></param>
        public void SetUnloadFarChunks(bool unloadFarChunks) {

            this.unloadFarChunks = unloadFarChunks;

            if (unloadFarChunks) {

                float visibleDistanceSqr = (_visibleChunksDistance + 1) * CHUNK_SIZE;
                visibleDistanceSqr *= visibleDistanceSqr;
                bool eventOut = OnChunkExitVisibleDistance != null;
                bool eventIn = OnChunkEnterVisibleDistance != null;

                for (int k = 0; k < chunksPoolLoadIndex; k++) {
                    VoxelChunk chunk = chunksPool[k];
                    if (chunk.isPopulated && !chunk.isCloud) {
                        double dist = FastVector.SqrMaxDistanceXorZ(ref chunk.position, ref currentAnchorPos);
                        if (dist > visibleDistanceSqr) {
                            if (chunk.visibleDistanceStatus != ChunkVisibleDistanceStatus.OutOfVisibleDistance) {
                                chunk.visibleDistanceStatus = ChunkVisibleDistanceStatus.OutOfVisibleDistance;
                                chunk.gameObject.SetActive(false);
                                if (eventOut) { OnChunkExitVisibleDistance(chunk); }
                            }
                        }
                    } else if (chunk.visibleDistanceStatus != ChunkVisibleDistanceStatus.WithinVisibleDistance) {
                        chunk.visibleDistanceStatus = ChunkVisibleDistanceStatus.WithinVisibleDistance;
                        chunk.gameObject.SetActive(true);
                        if (eventIn) { OnChunkEnterVisibleDistance(chunk); }
                    }
                }
            } else {
                for (int k = 0; k < chunksPoolLoadIndex; k++) {
                    VoxelChunk chunk = chunksPool[k];
                    if (chunk.isRendered && !chunk.isCloud && !chunk.gameObject.activeSelf) {
                        chunk.visibleDistanceStatus = ChunkVisibleDistanceStatus.WithinVisibleDistance;
                        chunk.gameObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if chunk is within visible distance
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public bool ChunkIsWithinVisibleDistance(VoxelChunk chunk) {
            if (!unloadFarChunks) return true;
            double visibleDistanceSqr = (_visibleChunksDistance + 1) * CHUNK_SIZE;
            visibleDistanceSqr *= visibleDistanceSqr;
            double dist = FastVector.SqrMaxDistanceXorZ(ref chunk.position, ref currentAnchorPos);
            return dist <= visibleDistanceSqr;
        }


        #endregion

    }



}
