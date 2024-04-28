using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    /// <summary>
    /// Terrain generator more voxels class
    /// This class inherits from TerrainDefaultGenerator and overrides the PaintChunk to take into account the additional voxels provided by BiomeDefinition
    /// </summary>

    [CreateAssetMenu(menuName = "Voxel Play/Terrain Generators/Multi-Step Terrain Generator More Voxels", fileName = "MultiStepTerrainGeneratorMoreVoxels", order = 102)]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001906-terrain-generators")]
    public class TerrainGeneratorMoreVoxels : TerrainDefaultGenerator {


        protected override void Init() {
            if (env != null && env.world != null && env.world.biomes != null) {
                for (int k = 0; k < env.world.biomes.Length; k++) {
                    BiomeDefinition biome = env.world.biomes[k];
                    if (biome != null) {
                        biome.InitMoreVoxels();
                    }
                }
                if (env.world.defaultBiome != null) {
                    env.world.defaultBiome.InitMoreVoxels();
                }
            }
            base.Init();
        }


        /// <summary>
        /// Paints the terrain inside the chunk defined by its central "position"
        /// </summary>
        /// <returns><c>true</c>, if terrain was painted, <c>false</c> otherwise.</returns>
        /// <param name="position">Central position of the chunk.</param>
        public override bool PaintChunk(VoxelChunk chunk) {

            Vector3 position = chunk.position;
            if (position.y + VoxelPlayEnvironment.CHUNK_HALF_SIZE < minHeight) {
                chunk.isAboveSurface = false;
                return false;
            }
            int bedrockRow = -1;
            if ((object)bedrockVoxel != null && position.y < minHeight + VoxelPlayEnvironment.CHUNK_HALF_SIZE) {
                bedrockRow = (int)(minHeight - (position.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE) + 1) * ONE_Y_ROW - 1;
            }
            position.x -= VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            position.y -= VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            position.z -= VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            Vector3 pos;

            int waterLevel = env.waterLevel > 0 ? env.waterLevel : -1;
            Voxel[] voxels = chunk.voxels;

            bool hasContent = false;
            bool isAboveSurface = false;
            generation++;
            env.GetHeightMapInfoFast(position.x, position.z, heightChunkData);
            int shiftAmount = (int)Mathf.Log(VoxelPlayEnvironment.CHUNK_SIZE, 2);

            // iterate 256 slice of chunk (z/x plane = 16*16 positions)
            for (int arrayIndex = 0; arrayIndex < VoxelPlayEnvironment.CHUNK_SIZE * VoxelPlayEnvironment.CHUNK_SIZE; arrayIndex++) {
                float groundLevel = heightChunkData[arrayIndex].groundLevel;
                float surfaceLevel = waterLevel > groundLevel ? waterLevel : groundLevel;
                if (surfaceLevel < position.y) {
                    // position is above terrain or water
                    isAboveSurface = true;
                    continue;
                }
                BiomeDefinition biome = heightChunkData[arrayIndex].biome;
                if ((object)biome == null) {
                    biome = world.defaultBiome;
                    if ((object)biome == null)
                        continue;
                }

                int y = (int)(surfaceLevel - position.y);
                if (y >= VoxelPlayEnvironment.CHUNK_SIZE) {
                    y = VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE;
                }
                pos.y = position.y + y;
                pos.x = position.x + (arrayIndex & VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE);
                pos.z = position.z + (arrayIndex >> shiftAmount);

                // Place voxels
                int voxelIndex = y * ONE_Y_ROW + arrayIndex;
                if (pos.y > groundLevel) {
                    // water above terrain
                    if (pos.y == surfaceLevel) {
                        isAboveSurface = true;
                    }
                    while (pos.y > groundLevel && voxelIndex >= 0) {
                        voxels[voxelIndex].SetFastWater(waterVoxel);
                        voxelIndex -= ONE_Y_ROW;
                        pos.y--;
                    }
                    // Underwater vegetation
                    if (env.enableVegetation && biome.underwaterVegetationDensity > 0 && biome.underwaterVegetation.Length > 0 && pos.y == groundLevel) {
                        float rn = WorldRand.GetValue(pos);
                        if (rn < biome.underwaterVegetationDensity) {
                            if (voxelIndex >= VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW) {
                                // request one vegetation voxel one position above which means the chunk above this one
                                Vector3 abovePos = pos;
                                abovePos.y++;
                                env.RequestVegetationCreation(abovePos, env.GetUnderwaterVegetation(biome, rn / biome.underwaterVegetationDensity));
                            } else if (voxels[voxelIndex + ONE_Y_ROW].opaque < 15) {
                                // directly place a vegetation voxel above this voxel
                                voxels[voxelIndex + ONE_Y_ROW].Set(env.GetUnderwaterVegetation(biome, rn / biome.underwaterVegetationDensity));
                                env.vegetationCreated++;
                            }
                        }
                    }

                } else if (pos.y == groundLevel) {
                    isAboveSurface = true;
                    if (voxels[voxelIndex].hasContent == 0) {
                        if (paintShore && pos.y == waterLevel) {
                            // this is on the shore, place a shoreVoxel
                            voxels[voxelIndex].Set(shoreVoxel);
                        } else {
                            // we're at the surface of the biome => draw the voxel top of the biome and also check for random vegetation and trees
                            VoxelDefinition topVoxel = biome.GetVoxelTop(pos);
                            voxels[voxelIndex].Set(topVoxel);
#if UNITY_EDITOR
                            if (!env.draftModeActive) {
#endif
                                // Check tree probability
                                if (pos.y > waterLevel) {
                                    float rn = WorldRand.GetValue(pos);
                                    if (biome.treeDensity > 0 && rn < biome.treeDensity && biome.trees.Length > 0) {
                                        // request one tree at this position
                                        env.RequestTreeCreation(chunk, pos, env.GetTree(biome.trees, rn / biome.treeDensity));
                                    } else if (biome.vegetationDensity > 0 && rn < biome.vegetationDensity && biome.vegetation.Length > 0) {
                                        if (voxelIndex >= VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE * ONE_Y_ROW) {
                                            // request one vegetation voxel one position above which means the chunk above this one
                                            Vector3 abovePos = pos;
                                            abovePos.y++;
                                            env.RequestVegetationCreation(abovePos, env.GetVegetation(biome, rn / biome.vegetationDensity));
                                        } else {
                                            // directly place a vegetation voxel above this voxel
                                            if (env.enableVegetation) {
                                                voxels[voxelIndex + ONE_Y_ROW].Set(env.GetVegetation(biome, rn / biome.vegetationDensity));
                                                env.vegetationCreated++;
                                            }
                                        }
                                    }
                                }
#if UNITY_EDITOR
                            }
#endif
                        }
                        voxelIndex -= ONE_Y_ROW;
                        pos.y--;
                    }
                }

                biome.biomeGeneration = generation;

                // fill hole with water
                while (voxelIndex >= 0 && voxels[voxelIndex].hasContent == 2 && pos.y <= waterLevel) {
                    voxels[voxelIndex].SetFastWater(waterVoxel);
                    voxelIndex -= ONE_Y_ROW;
                    pos.y--;
                }

                // Continue filling down
                for (; voxelIndex > bedrockRow; voxelIndex -= ONE_Y_ROW, pos.y--) {
                    if (voxels[voxelIndex].hasContent == 0) {
                        VoxelDefinition dirtVoxel = biome.GetVoxelDirt(pos);
                        voxels[voxelIndex].SetFastOpaque(dirtVoxel);
                    } else if (voxels[voxelIndex].hasContent == 2 && pos.y <= waterLevel) { // hole under water level -> fill with water
                        voxels[voxelIndex].SetFastWater(waterVoxel);
                    }
                }
                if (voxelIndex >= 0 && bedrockRow >= 0) {
                    voxels[voxelIndex].SetFastOpaque(bedrockVoxel);
                }
                hasContent = true;
            }

            // Spawn random ore
            if (addOre) {
                // Check if there's any ore in this chunk (randomly)
                float noiseValue = WorldRand.GetValue(chunk.position);
                for (int b = 0; b < world.biomes.Length; b++) {
                    BiomeDefinition biome = world.biomes[b];
                    if (biome.biomeGeneration != generation)
                        continue;
                    for (int o = 0; o < biome.ores.Length; o++) {
                        if (biome.ores[o].ore == null)
                            continue;
                        if (biome.ores[o].probabilityMin <= noiseValue && biome.ores[o].probabilityMax >= noiseValue) {
                            // ore picked; determine the number of veins in this chunk
                            int veinsCount = biome.ores[o].veinsCountMin + (int)(WorldRand.GetValue() * (biome.ores[o].veinsCountMax - biome.ores[o].veinsCountMin + 1f));
                            for (int vein = 0; vein < veinsCount; vein++) {
                                Vector3 veinPos = chunk.position;
                                veinPos.x += vein;
                                // Determine random vein position in the chunk
                                Vector3 v = WorldRand.GetVector3(veinPos, VoxelPlayEnvironment.CHUNK_SIZE);
                                int px = (int)v.x;
                                int py = (int)v.y;
                                int pz = (int)v.z;
                                veinPos = env.GetVoxelPosition(veinPos, px, py, pz);
                                int oreIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                                int veinSize = biome.ores[o].veinMinSize + (oreIndex % (biome.ores[o].veinMaxSize - biome.ores[o].veinMinSize + 1));
                                // span ore vein
                                SpawnOre(chunk, biome.ores[o].ore, veinPos, px, py, pz, veinSize, biome.ores[o].depthMin, biome.ores[o].depthMax);
                            }
                            break;
                        }
                    }
                }
            }

            // Finish, return
            chunk.isAboveSurface = isAboveSurface;
            return hasContent;
        }


        void SpawnOre(VoxelChunk chunk, VoxelDefinition oreDefinition, Vector3 veinPos, int px, int py, int pz, int veinSize, int minDepth, int maxDepth) {
            int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
            while (veinSize-- > 0 && voxelIndex >= 0 && voxelIndex < chunk.voxels.Length) {
                // Get height at position
                int groundLevel = heightChunkData[pz * VoxelPlayEnvironment.CHUNK_SIZE + px].groundLevel;
                int depth = (int)(groundLevel - veinPos.y);
                if (depth < minDepth || depth > maxDepth)
                    return;

                // Replace voxel with ore
                if (chunk.voxels[voxelIndex].opaque >= VoxelPlayEnvironment.CHUNK_SIZE_MINUS_ONE) {
                    chunk.voxels[voxelIndex].SetFastOpaque(oreDefinition);
                }
                // Check if spawn continues
                Vector3 prevPos = veinPos;
                float v = WorldRand.GetValue(veinPos);
                int dir = (int)(v * 5);
                switch (dir) {
                    case 0: // down
                        veinPos.y--;
                        voxelIndex -= ONE_Y_ROW;
                        break;
                    case 1: // right
                        veinPos.x++;
                        voxelIndex++;
                        break;
                    case 2: // back
                        veinPos.z--;
                        voxelIndex -= ONE_Z_ROW;
                        break;
                    case 3: // left
                        veinPos.x--;
                        voxelIndex--;
                        break;
                    case 4: // forward
                        veinPos.z++;
                        voxelIndex += ONE_Z_ROW;
                        break;
                }
                if (veinPos.x == prevPos.x && veinPos.y == prevPos.y && veinPos.z == prevPos.z) {
                    veinPos.y--;
                    voxelIndex -= ONE_Y_ROW;
                }
            }
        }




    }

}