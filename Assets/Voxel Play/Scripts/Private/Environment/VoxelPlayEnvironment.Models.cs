using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        /// <summary>
        /// Ensures all voxel definitions/textures used in this model are registered
        /// </summary>
        void ModelRegisterVoxelDefinitions(ModelDefinition model) {
            if (model == null || model.bits == null) return;
            int bitsCount = model.bits.Length;
            for (int k = 0; k < bitsCount; k++) {
                VoxelDefinition vd = model.bits[k].voxelDefinition;
                if ((object)vd != null) {
                    AddVoxelTextures (vd);
                }
            }
        }

        IEnumerator ModelPlaceWithDuration(Vector3d position, ModelDefinition model, float buildDuration, int rotation = 0, float colorBrightness = 1f, bool fitTerrain = false, VoxelPlayModelBuildEvent callback = null)
        {

            if (OnModelBuildStart != null)
            {
                OnModelBuildStart(model, position);
            }
            int currentIndex = 0;
            int len = model.bits.Length - 1;
            float startTime = Time.time;
            float t = 0;
            WaitForEndOfFrame w = new WaitForEndOfFrame();
            Boundsd bounds = new Boundsd(position, Misc.vector3one);

            while (t < 1f)
            {
                t = (Time.time - startTime) / buildDuration;
                if (t >= 1f)
                {
                    t = 1f;
                }
                int lastIndex = (int)(len * t);
                if (lastIndex >= currentIndex)
                {
                    ModelPlace(position, model, ref bounds, rotation, colorBrightness, fitTerrain, null, currentIndex, lastIndex);
                    currentIndex = lastIndex + 1;
                }
                yield return w;
            }

            ModelPlaceTorches(position, model, rotation);

            if (callback != null)
            {
                callback(model, position);
            }
            if (OnModelBuildEnd != null)
            {
                OnModelBuildEnd(model, position);
            }
        }


        void ModelPlace(Vector3d position, ModelDefinition model, ref Boundsd bounds, int rotation = 0, float colorBrightness = 1f, bool fitTerrain = false, List<VoxelIndex> indices = null, int indexStart = -1, int indexEnd = -1, bool useUnpopulatedChunks = false, bool refreshChunks = true)
        {
            if (model == null)
                return;
            if (indexStart < 0)
            {
                indexStart = 0;
            }
            if (indexEnd < 0)
            {
                indexEnd = model.bits.Length - 1;
            }
            if (model.fitToTerrain) {
                fitTerrain = true;
            }

            // ensure all voxel definitions are present
            for (int b = indexStart; b <= indexEnd; b++)
            {
                VoxelDefinition vd = model.bits[b].voxelDefinition;
                if (vd != null && vd.index == 0)
                {
                    AddVoxelDefinition(vd);
                }
            }

            bool indicesProvided = indices != null;
            if (indicesProvided && indexStart < 0 && indexEnd < 0)
            {
                indices.Clear();
            }
            VoxelIndex index = new VoxelIndex();
            Vector3d min = bounds.min;
            Vector3d max = bounds.max;

            List<VoxelChunk> updatedChunks = BufferPool<VoxelChunk>.Get();
            modificationTag++;

            Vector3d pos;
            int modelOneYRow = model.sizeZ * model.sizeX;
            int modelOneZRow = model.sizeX;
            int halfSizeX = model.sizeX / 2;
            int halfSizeZ = model.sizeZ / 2;

            float rotationDegrees = Voxel.GetTextureRotationDegrees(rotation);
            Vector3 zeroPos = Quaternion.Euler(0, rotationDegrees, 0) * new Vector3(-halfSizeX, 0, -halfSizeZ);

            for (int b = indexStart; b <= indexEnd; b++)
            {
                int bitIndex = model.bits[b].voxelIndex;
                int py = bitIndex / modelOneYRow;
                int remy = bitIndex - py * modelOneYRow;
                int pz = remy / modelOneZRow;
                int px = remy - pz * modelOneZRow;

                float wx = zeroPos.x, wz = zeroPos.z;
                switch (rotation)
                {
                    case 1:
                        wx += pz;
                        wz -= px;
                        break;
                    case 2:
                        wx -= px;
                        wz -= pz;
                        break;
                    case 3:
                        wx -= pz;
                        wz += px;
                        break;
                    default:
                        wx += px;
                        wz += pz;
                        break;
                }

                pos.x = position.x + model.offsetX + wx;
                pos.y = position.y + model.offsetY + py;
                pos.z = position.z + model.offsetZ + wz;

                VoxelChunk chunk;
                int voxelIndex;
                if (useUnpopulatedChunks)
                {
                    GetChunkUnpopulated(pos);
                }
                if (GetVoxelIndex(pos, out chunk, out voxelIndex))
                {
                    bool emptyVoxel = model.bits[b].isEmpty;
                    if (emptyVoxel)
                    {
                        chunk.voxels[voxelIndex] = Voxel.Hole;
                    }
                    else
                    {
                        Color32 color = model.bits[b].finalColor;
                        if (colorBrightness != 1f)
                        {
                            color.r = (byte)(color.r * colorBrightness);
                            color.g = (byte)(color.g * colorBrightness);
                            color.b = (byte)(color.b * colorBrightness);
                        }
                        VoxelDefinition vd = model.bits[b].voxelDefinition;
                        if (vd == null)
                        {
                            vd = defaultVoxel;
                        }
                        chunk.voxels[voxelIndex].Set(vd, color);
                        float modelBitRotation = (model.bits[b].rotation + 360 + rotationDegrees) % 360;
                        if (rotation != 0)
                        {
                            chunk.voxels[voxelIndex].SetTextureRotation(Voxel.GetTextureRotationFromDegrees(modelBitRotation));
                        }

                        // Add index
                        if (indicesProvided)
                        {
                            index.chunk = chunk;
                            index.voxelIndex = voxelIndex;
                            index.position = pos;
                            indices.Add(index);
                        }
                        if (pos.x < min.x)
                        {
                            min.x = pos.x;
                        }
                        if (pos.y < min.y)
                        {
                            min.y = pos.y;
                        }
                        if (pos.z < min.z)
                        {
                            min.z = pos.z;
                        }
                        if (pos.x > max.x)
                        {
                            max.x = pos.x;
                        }
                        if (pos.y > max.y)
                        {
                            max.y = pos.y;
                        }
                        if (pos.z > max.z)
                        {
                            max.z = pos.z;
                        }

                        if (fitTerrain)
                        {
                            // Fill beneath row 1
                            if (py == 0)
                            {
                                Vector3d under = pos;
                                under.y -= 1;
                                float terrainAltitude = GetTerrainHeight(under);
                                for (int k = 0; k < 100; k++, under.y--)
                                {
                                    GetVoxelIndex(under, out VoxelChunk lowChunk, out int vindex);
                                    if (under.y < terrainAltitude || (object)lowChunk == null || lowChunk.voxels[vindex].opaque == FULL_OPAQUE) break;

                                    lowChunk.voxels[vindex].Set(vd, color);

                                    if (lowChunk.SetModified(modificationTag))
                                    {
                                        updatedChunks.Add(lowChunk);
                                    }

                                    if (!lowChunk.inqueue && !useUnpopulatedChunks)
                                    {
                                        ChunkRequestRefresh(lowChunk, true, true);
                                    }
                                }
                            }
                        }
                    }

                    // Prevent tree population
                    chunk.allowTrees = false;

                    // Stamp change
                    if (chunk.SetModified(modificationTag))
                    {
                        updatedChunks.Add(chunk);
                    }

                    if (useUnpopulatedChunks)
                    {
                        chunk.isDirty = true;
                    }
                    if (!chunk.inqueue && refreshChunks)
                    {
                        ChunkRequestRefresh(chunk, true, true);
                    }
                }
            }

            RegisterChunkChanges(updatedChunks);
            BufferPool<VoxelChunk>.Release(updatedChunks);

            FastVector.Floor(ref min);
            FastVector.Ceiling(ref max);
            bounds.center = (max + min) * 0.5;
            bounds.size = max - min;
        }


        void ModelPlaceTorches(Vector3d position, ModelDefinition model, int rotation = 0)
        {

            if (model == null || model.torches == null)
                return;
            FastVector.Floor(ref position);
            Vector3d pos;
            int modelOneYRow = model.sizeZ * model.sizeX;
            int modelOneZRow = model.sizeX;
            int halfSizeX = model.sizeX / 2;
            int halfSizeZ = model.sizeZ / 2;

            float rotationDegrees = Voxel.GetTextureRotationDegrees(rotation);
            Vector3 zeroPos = Quaternion.Euler(0, rotationDegrees, 0) * new Vector3(-halfSizeX, 0, -halfSizeZ);

            // ensure all voxel definitions are present
            int tmp;
            for (int b = 0; b < model.torches.Length; b++)
            {
                int bitIndex = model.torches[b].voxelIndex;
                int py = bitIndex / modelOneYRow;
                int remy = bitIndex - py * modelOneYRow;
                int pz = remy / modelOneZRow;
                int px = remy - pz * modelOneZRow;
                Vector3 normal = model.torches[b].normal;
                float wx = zeroPos.x, wz = zeroPos.z;
                switch (rotation)
                {
                    case 1:
                        wx += pz;
                        wz -= px;
                        tmp = (int)normal.x;
                        normal.x = normal.z;
                        normal.z = -tmp;
                        break;
                    case 2:
                        wx -= px;
                        wz -= pz;
                        normal.x = -normal.x;
                        normal.z = -normal.z;
                        break;
                    case 3:
                        wx -= pz;
                        wz += px;
                        tmp = (int)normal.x;
                        normal.x = normal.z;
                        normal.z = tmp;
                        break;
                    default:
                        wx += px;
                        wz += pz;
                        break;
                }

                pos.x = position.x + model.offsetX + wx;
                pos.y = position.y + model.offsetY + py;
                pos.z = position.z + model.offsetZ + wz;
                pos -= normal;

                VoxelHitInfo hitInfo = new VoxelHitInfo();
                if (GetVoxelIndex(pos, out VoxelChunk chunk, out int voxelIndex))
                {
                    hitInfo.chunk = chunk;
                    hitInfo.voxelIndex = voxelIndex;
                    hitInfo.normal = normal;
                    hitInfo.voxelCenter = pos + Misc.vector3half;
                    TorchAttach(hitInfo, model.torches[b].itemDefinition, false);
                }
            }
        }

    }



}
