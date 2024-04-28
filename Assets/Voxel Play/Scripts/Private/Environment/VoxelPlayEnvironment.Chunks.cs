using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        const string CHUNKS_ROOT = "Chunks Root";
        const string CHUNKS_EXPORT_ROOT = "Exported Chunks";

        // Optimization support
        VoxelChunk lastChunkFetch;
        int lastChunkFetchX, lastChunkFetchY, lastChunkFetchZ;
        readonly object lockLastChunkFetch = new object ();
        bool detailGeneratorsBusy;

        #region Chunk functions

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        int GetChunkHash (int chunkX, int chunkY, int chunkZ)
        {
            int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
            int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
            return x00 + y00 + chunkZ;
        }

        /// <summary>
        /// Gets the chunk if exits or create it if forceCreation is set to true.
        /// </summary>
        /// <returns><c>true</c>, if chunk fast was gotten, <c>false</c> otherwise.</returns>
        /// <param name="chunkX">Chunk x.</param>
        /// <param name="chunkY">Chunk y.</param>
        /// <param name="chunkZ">Chunk z.</param>
        /// <param name="chunk">Chunk.</param>
        /// <param name="createIfNotAvailable">If set to <c>true</c> force creation if chunk doesn't exist.</param>
		bool GetChunkFast (int chunkX, int chunkY, int chunkZ, out VoxelChunk chunk, bool createIfNotAvailable = false)
        {
            lock (lockLastChunkFetch) {
                if (lastChunkFetchX == chunkX && lastChunkFetchY == chunkY && lastChunkFetchZ == chunkZ && (object)lastChunkFetch != null) {
                    chunk = lastChunkFetch;
                    return true;
                }
            }
            int hash = GetChunkHash (chunkX, chunkY, chunkZ);
            STAGE = 501;
            bool exists = cachedChunks.TryGetValue (hash, out CachedChunk cachedChunk);
            chunk = exists ? cachedChunk.chunk : null;

            if (createIfNotAvailable) {
                if (!exists) {
                    STAGE = 502;
                    // not yet created, create it
                    chunk = CreateChunk (hash, chunkX, chunkY, chunkZ, false);
                    exists = true;
                }
                if ((object)chunk == null) { // chunk is really empty, create it with empty space
                    STAGE = 503;
                    chunk = CreateChunk (hash, chunkX, chunkY, chunkZ, true);
                }
            }
            STAGE = 0;
            if (exists) {
                lock (lockLastChunkFetch) {
                    lastChunkFetchX = chunkX;
                    lastChunkFetchY = chunkY;
                    lastChunkFetchZ = chunkZ;
                    lastChunkFetch = chunk;
                }
                return (object)chunk != null;
            }
            chunk = null;
            return false;
        }


        VoxelChunk GetChunkOrCreate (Vector3d position)
        {
            FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int x, out int y, out int z);
            VoxelChunk chunk;
            GetChunkFast (x, y, z, out chunk, true);
            return chunk;
        }


        VoxelChunk GetChunkOrCreate (int chunkX, int chunkY, int chunkZ)
        {
            GetChunkFast (chunkX, chunkY, chunkZ, out VoxelChunk chunk, createIfNotAvailable: true);
            return chunk;
        }

        VoxelChunk GetChunkIfExists (int hash)
        {
            if (cachedChunks.TryGetValue (hash, out CachedChunk cachedChunk)) {
                return cachedChunk.chunk;
            }
            return null;
        }

        bool ChunkExists (int chunkX, int chunkY, int chunkZ)
        {

            int hash = GetChunkHash (chunkX, chunkY, chunkZ);
            if (cachedChunks.TryGetValue (hash, out CachedChunk cachedChunk)) {
                return cachedChunk.chunk != null;
            }
            return false;
        }


        /// <summary>
        /// Creates the chunk.
        /// </summary>
        /// <returns>The chunk.</returns>
        /// <param name="hash">Hash.</param>
        /// <param name="chunkX">Chunk x.</param>
        /// <param name="chunkY">Chunk y.</param>
        /// <param name="chunkZ">Chunk z.</param>
        /// <param name="createEmptyChunk">If set to <c>true</c> create empty chunk.</param>
        /// <param name="complete">If set to <c>true</c> detail generators will fire as well as OnChunkCreated event. Chunk will be marked as populated and a refresh will be triggered if within view distance.</param>
        VoxelChunk CreateChunk (int hash, int chunkX, int chunkY, int chunkZ, bool createEmptyChunk, bool complete = true)
        {

            STAGE = 101;
            Vector3d position;
            position.x = chunkX * CHUNK_SIZE + CHUNK_HALF_SIZE;
            position.y = chunkY * CHUNK_SIZE + CHUNK_HALF_SIZE;
            position.z = chunkZ * CHUNK_SIZE + CHUNK_HALF_SIZE;

            STAGE = 102;
            // Create entry in the dictionary
            if (!cachedChunks.TryGetValue (hash, out CachedChunk cachedChunk)) {
                cachedChunk = new CachedChunk ();
                cachedChunks [hash] = cachedChunk;
            }

            STAGE = 103;
            VoxelChunk chunk;
            if ((object)cachedChunk.chunk == null) {
                // Fetch a new entry in the chunks pool
                if (chunksPoolFetchNew) {
                    chunksPoolFetchNew = false;
                    FetchNewChunkIndex (position);
                }
                chunk = chunksPool [chunksPoolCurrentIndex];
            } else {
                chunk = cachedChunk.chunk;
            }

            // Paint voxels
            bool chunkHasContents = false;
            chunk.position = position;

            STAGE = 104;
            if (createEmptyChunk) {
                chunk.isAboveSurface = CheckIfChunkAboveTerrain (position);
            } else {
                if (world.infinite || (position.x >= -world.extents.x && position.x <= world.extents.x && position.y >= -world.extents.y && position.y <= world.extents.y && position.z >= -world.extents.z && position.z <= world.extents.z)) {
                    if (OnChunkBeforeCreate != null) {
                        // allows a external function to fill the contents of this new chunk
                        bool isAboveSurface;
                        OnChunkBeforeCreate (position, out chunkHasContents, chunk, out isAboveSurface);
                        chunk.isAboveSurface = isAboveSurface;
                    }
                    if (!chunkHasContents) {
                        if (!chunk.isCloud) {
                            chunkHasContents = world.terrainGenerator.PaintChunk (chunk);
                        }
                        chunk.isAboveSurface |= !chunkHasContents;
                    }
                }
            }

            STAGE = 105;

            if (chunkHasContents || createEmptyChunk) {

                chunk.ComputeNeighbours ();

                if (effectiveGlobalIllumination) {
                    // Ensure that the chunk lightmap is clear; the chunk lightmap might be computed early if the chunk was obtained using GetChunkUnpopulated method
                    // We ensure it remains cleared and is rebuilt when terrain paints this chunk
                    if (chunk.isDirty) {
                        chunk.isDirty = false;
                        chunk.ClearLightmap (noLightValue);
                    }
                    // rebuild lightmap for chunks got with GetChunkUnpopulated (ie. from a saved game file)
                    chunk.needsLightmapRebuild = true;
                } else {
                    // lit chunk if not global illumination
                    chunk.ClearLightmap (15);
                }

                chunksPoolFetchNew = true;
                chunksCreated++;

                cachedChunk.chunk = chunk;

                if (complete) {
                    chunk.isPopulated = true;
                    // rebuild lightmap as this chunk is fully populated and has been modified by a detail generator
                    chunk.needsLightmapRebuild = true;

                    // Check for detail generators
                    bool useDetailGenerators = worldHasDetailGenerators && enableDetailGenerators && !detailGeneratorsBusy;
                    if (useDetailGenerators) {
                        detailGeneratorsBusy = true;
                        bool prevCaptureEvents = captureEvents;
                        captureEvents = false;
                        // detail generators shouldn't trigger events for performance reasons. Also a detail generator works same on all clients in multiplayer environment
                        // so no need to propagate these changes as every client will execute the same logic.
                        for (int d = 0; d < world.detailGenerators.Length; d++) {
                            if (world.detailGenerators [d].enabled) {
                                world.detailGenerators [d].AddDetail (chunk);
                            }
                        }
                        captureEvents = prevCaptureEvents;
                        detailGeneratorsBusy = false;
                    }

                    if (chunkHasContents) {
                        // if chunk is near camera, request a render refresh
                        bool sendRefresh = (chunkX >= visible_xmin && chunkX <= visible_xmax && chunkZ >= visible_zmin && chunkZ <= visible_zmax && chunkY >= visible_ymin && chunkY <= visible_ymax);
                        if (sendRefresh) {
                            ChunkRequestRefresh (chunk, clearLightmap: false, refreshMesh: true);
                        }
                    } else {
                        chunk.renderState = ChunkRenderState.RenderingComplete;
                    }

                    if (OnChunkAfterCreate != null) {
                        OnChunkAfterCreate (chunk);
                    }
                }

                STAGE = 0;
                return chunk;
            }
            chunk.renderState = ChunkRenderState.RenderingComplete;
            STAGE = 0;
            return null;
        }

        bool CheckIfChunkAboveTerrain (Vector3d position)
        {

            position.y += (CHUNK_HALF_SIZE - 1);
            if (position.y < waterLevel && waterLevel > 0) {
                return false;
            }

            position.x -= CHUNK_HALF_SIZE;
            position.z -= CHUNK_HALF_SIZE;
            Vector3d pos = position;

            for (int z = 0; z < CHUNK_SIZE; z++) {
                pos.z = position.z + z;
                for (int x = 0; x < CHUNK_SIZE; x++) {
                    pos.x = position.x + x;
                    float groundLevel = GetHeightMapInfoFast (pos.x, pos.z).groundLevel;
                    float surfaceLevel = waterLevel > groundLevel ? waterLevel : groundLevel;
                    if (position.y >= surfaceLevel) {
                        // chunk is above terrain or water
                        return true;
                    }
                }
            }

            return false;
        }


        void RefreshNineChunks (VoxelChunk chunk, bool forceMeshRefresh = false, bool clearLightMap = true)
        {
            if ((object)chunk == null)
                return;

            FastMath.FloorToInt (chunk.position.x / CHUNK_SIZE, chunk.position.y / CHUNK_SIZE, chunk.position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);

            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {
                    for (int x = -1; x <= 1; x++) {
                        GetChunkFast (chunkX + x, chunkY + y, chunkZ + z, out VoxelChunk neighbour);
                        if ((object)neighbour != null) {
                            ChunkRequestRefresh (neighbour, clearLightMap, forceMeshRefresh);
                        }
                    }
                }
            }
        }

        void RebuildNeighboursIfNeeded (VoxelChunk chunk, int voxelIndex)
        {
            int bx = voxelIndex & VOXELINDEX_X_EDGE_BITWISE;
            int bz = voxelIndex & VOXELINDEX_Z_EDGE_BITWISE;
            int by = voxelIndex & VOXELINDEX_Y_EDGE_BITWISE;

            if (bx == 0)
                ChunkRequestRefresh (chunk.left, clearLightmap: false, refreshMesh: true);
            else if (bx == VOXELINDEX_X_EDGE_BITWISE)
                ChunkRequestRefresh (chunk.right, clearLightmap: false, refreshMesh: true);

            if (by == 0)
                ChunkRequestRefresh (chunk.bottom, clearLightmap: false, refreshMesh: true);
            else if (by == VOXELINDEX_Y_EDGE_BITWISE)
                ChunkRequestRefresh (chunk.top, clearLightmap: false, refreshMesh: true);

            if (bz == 0)
                ChunkRequestRefresh (chunk.back, clearLightmap: false, refreshMesh: true);
            else if (bz == VOXELINDEX_Z_EDGE_BITWISE)
                ChunkRequestRefresh (chunk.forward, clearLightmap: false, refreshMesh: true);
        }


        /// <summary>
        /// Clears a chunk
        /// </summary>
        void ChunkClearFast (VoxelChunk chunk)
        {
            chunk.ClearVoxels (noLightValue);
        }

        public void ChunksExportAll ()
        {
            if (cachedChunks == null) {
                return;
            }
            GameObject exportRoot = GameObject.Find (CHUNKS_EXPORT_ROOT);
            if (exportRoot != null) {
                DestroyImmediate (exportRoot);
            }
            exportRoot = new GameObject (CHUNKS_EXPORT_ROOT);
            exportRoot.transform.position = Misc.vector3zero;

            ExportGlobalSettings settings = exportRoot.AddComponent<ExportGlobalSettings> ();
            settings.lightPosBuffer = Shader.GetGlobalVectorArray ("_VPPointLightPosition");
            settings.lightColorBuffer = Shader.GetGlobalVectorArray ("_VPPointLightColor");
            settings.lightCount = Shader.GetGlobalInt ("_VPPointLightCount");
            settings.emissionIntensity = Shader.GetGlobalFloat ("_VPEmissionIntensity");
            settings.skyTint = Shader.GetGlobalColor ("_VPSkyTint");
            settings.fogData = Shader.GetGlobalVector ("_VPFogData");
            settings.fogAmount = Shader.GetGlobalFloat ("_VPFogAmount");
            settings.exposure = Shader.GetGlobalFloat ("_VPExposure");
            settings.ambientLight = Shader.GetGlobalFloat ("_VPAmbientLight");
            settings.daylightShadowAtten = Shader.GetGlobalFloat ("_VPDaylightShadowAtten");
            settings.enableFog = Shader.IsKeywordEnabled (SKW_VOXELPLAY_GLOBAL_USE_FOG);

            foreach (KeyValuePair<int, CachedChunk> kv in cachedChunks) {
                if (kv.Value == null)
                    continue;
                VoxelChunk chunk = kv.Value.chunk;
                if ((object)chunk == null)
                    continue;
                if (chunk.mf.sharedMesh != null) {
                    chunk.gameObject.hideFlags = 0;
                    chunk.mf.sharedMesh.hideFlags = 0;
                    if (chunk.mc != null && chunk.mc.sharedMesh != null) {
                        chunk.mc.sharedMesh.hideFlags = 0;
                    }
                    chunk.transform.SetParent (exportRoot.transform, true);
                }
            }
            cachedChunks.Clear ();

#if UNITY_EDITOR
            // Mark scene as modified
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene ());
            UnityEditor.EditorUtility.DisplayDialog ("Export Chunks", "Chunks now available under 'Exported Chunks' node in hierarchy as regular gameobjects. Materials, textures and meshes are now part of the scene.\n\nThe 'ExportGlobalSettings' behaviour has been attached to 'Exported Chunks' root gameobject to keep global shader values.\nYou can now remove Voxel Play Environment gameobject completely if you wish.", "Ok");
#endif

        }

        #endregion

    }



}
