using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    [CreateAssetMenu (menuName = "Voxel Play/Detail Generators/Cave Generator", fileName = "CaveGenerator", order = 101)]
    public class CaveDefaultGenerator : VoxelPlayDetailGenerator
    {
        [Header ("Tunnels")]
        [Range(0, 0.1f)]
        public float spawnProbability = 0.02f;

        [Range (0.0f, 3f)]
        [Tooltip("If tunnels should be mostly straight lines (high value) or can make ripples and short loops (near 0)")]
        public float linearity = 2.6f;

        [Range (0, 1f)]
        [Tooltip("If tunnels are mostly horizontal (near 0) or can go up and down (near 1)")]
        public float verticality = 0.1f;

        [Tooltip("Renders the path of the tunnel as a sequence of cubes visible in the SceneView. Only works inside Unity Editor.")]
        public bool debug;


        struct HoleWorm
        {
            public Vector3d head;
            public int life;
            public int lastX, lastY, lastZ;
            public float iterX, iterY, iterZ;
        }

        List<HoleWorm> worms;

        VoxelPlayEnvironment env;
        Dictionary<Vector3d, bool> wormBorn;
        float [] noiseValuesX;
        float [] noiseValuesY;
        float [] noiseValuesZ;
        const int NOISE_COUNT = 1024;
        int caveBorder;

        /// <summary>
        /// Initialization method. Called by Voxel Play at startup.
        /// </summary>
        public override void Init ()
        {
            env = VoxelPlayEnvironment.instance;
            wormBorn = new Dictionary<Vector3d, bool> (100);

            GenerateNoise (ref noiseValuesX, 0);
            GenerateNoise (ref noiseValuesY, 0.333f);
            GenerateNoise (ref noiseValuesZ, 0.666f);

            float l = 3.1f - linearity;
            noiseValuesX.Remap (-l, l);
            noiseValuesZ.Remap (-l, l);
            float v = verticality;
            noiseValuesY.Remap (-v - 0.02f, v); // tend to go down

            worms = new List<HoleWorm> (100);
        }

        void GenerateNoise (ref float [] noiseValues, float seed)
        {
            noiseValues = new float [NOISE_COUNT];
            const int octaves = 8;
            float freq = 1f;
            float amp = 1f;
            for (int o = 0; o < octaves; o++) {
                float s = (float)o / octaves + seed;
                for (int k = 0; k < NOISE_COUNT; k++) {
                    float a = freq * k / NOISE_COUNT + s;
                    float v = amp * Mathf.Sin (a * Mathf.PI);
                    noiseValues [k] += v;
                }
                freq *= 2f;
                amp *= 0.75f;
            }
        }

        /// <summary>
        /// Called by Voxel Play to inform that player has moved onto another chunk so new detail can start generating
        /// </summary>
        /// <param name="position">Current player position.</param>
        /// <param name="checkOnlyBorders">True means the player has moved to next chunk. False means player position is completely new and all chunks in
        /// range should be checked for detail in this call.</param>
        /// <param name="endTime">Provides a maximum time frame for execution this frame. Compare this with env.stopwatch milliseconds.</param>
        public override bool ExploreArea (Vector3d position, bool checkOnlyBorders, long endTime)
        {
            int explorationRange = env.visibleChunksDistance + 10;
            int minz = -explorationRange;
            int maxz = +explorationRange;
            int minx = -explorationRange;
            int maxx = +explorationRange;
            HoleWorm worm;
            Vector3d pos = position;
            float prob = 1f - spawnProbability;
            for (int z = minz; z <= maxz; z++) {
                for (int x = minx; x < maxx; x++) {
                    if (checkOnlyBorders && z > minz && z < maxz && x > minx && x < maxx)
                        continue;
                    pos.x = position.x + x * VoxelPlayEnvironment.CHUNK_SIZE;
                    pos.z = position.z + z * VoxelPlayEnvironment.CHUNK_SIZE;
                    pos = env.GetChunkPosition (pos);
                    if (WorldRand.GetValue (pos) > prob) {
                        pos.y = env.GetTerrainHeight (pos);
                        if (pos.y > env.waterLevel && !wormBorn.TryGetValue (pos, out bool born)) {
                            if (!born) {
                                worm.head = pos;
                                worm.life = 2000;
                                worm.lastX = worm.lastY = worm.lastZ = int.MinValue;
                                worm.iterX = WorldRand.Range (0, NOISE_COUNT, pos) + 1000000;
                                worm.iterY = WorldRand.Range (0, NOISE_COUNT) + 1000000;
                                worm.iterZ = WorldRand.Range (0, NOISE_COUNT) + 1000000;
                                worms.Add (worm);
                            }
                            wormBorn [pos] = true;
                        }
                    }
                }
            }

            if (!checkOnlyBorders) {
                for (int k = 0; k < 1000; k++) {
                    if (DoWork (endTime))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Move worms
        /// </summary>
        public override bool DoWork (long endTime)
        {

            int count = worms.Count;
            if (count == 0)
                return false;
            const int MAX_ITERATIONS = 10000;

            for (int p = 0; p < MAX_ITERATIONS; p++) {
                for (int k = 0; k < count; k++) {
                    env.STAGE = 3000;
                    HoleWorm worm = worms [k];

                    worm.iterX += 0.5f;
                    uint xx = (uint)((worm.head.y + worm.head.z + worm.iterX) % NOISE_COUNT);
                    worm.head.x += noiseValuesX [xx];

                    worm.iterY += 0.1f;
                    uint yy = (uint)((worm.head.z + worm.head.x + worm.iterY) % NOISE_COUNT);
                    float incy = noiseValuesY [yy];
                    // force tunnel to go down at begining
                    if (worm.life < 50 && incy > 0) incy = -incy;
                    worm.head.y += incy;

                    worm.iterZ += 0.5f;
                    uint zz = (uint)((worm.head.y + worm.head.x + worm.iterZ) % NOISE_COUNT);
                    worm.head.z += noiseValuesZ [zz];

                    int ix = (int)(worm.head.x);
                    int iy = (int)(worm.head.y);
                    int iz = (int)(worm.head.z);
                    env.STAGE = 3001;
                    if (ix != worm.lastX || iy != worm.lastY || iz != worm.lastZ) {

#if UNITY_EDITOR
                        if (debug) {
                            GameObject o = GameObject.CreatePrimitive (PrimitiveType.Cube);
                            o.transform.SetParent (env.worldRoot, false);
                            o.name = "Tunnel Debug Position";
                            o.transform.position = worm.head;
                        }
#endif

                        worm.lastX = ix;
                        worm.lastY = iy;
                        worm.lastZ = iz;

                        // keep this order of assignment to improve randomization
                        int minx = ix - (caveBorder++ & 7);
                        int miny = iy - (caveBorder++ & 7);
                        int maxx = ix + (caveBorder++ & 3);
                        int minz = iz - (caveBorder++ & 7);
                        int maxy = iy + (caveBorder++ & 3);
                        int maxz = iz + (caveBorder++ & 3);
                        int mx = (maxx + minx) / 2;
                        int my = (maxy + miny) / 2;
                        int mz = (maxz + minz) / 2;

                        VoxelChunk chunk = null;
                        int lastChunkX = int.MinValue, lastChunkY = int.MinValue, lastChunkZ = int.MinValue;
                        for (int y = miny; y < maxy; y++) {
                            int chunkY = FastMath.FloorToInt (y / (float)VoxelPlayEnvironment.CHUNK_SIZE);
                            int py = y - chunkY * VoxelPlayEnvironment.CHUNK_SIZE;
                            int voxelIndexY = py * ONE_Y_ROW;
                            int dy = y - my;
                            dy *= dy;
                            for (int z = minz; z < maxz; z++) {
                                int chunkZ = FastMath.FloorToInt (z / (float)VoxelPlayEnvironment.CHUNK_SIZE);
                                int pz = z - chunkZ * VoxelPlayEnvironment.CHUNK_SIZE;
                                int voxelIndexZ = voxelIndexY + pz * ONE_Z_ROW;
                                int dz = z - mz;
                                dz *= dz;
                                int dyz = dy + dz;
                                for (int x = minx; x < maxx; x++) {
                                    int dx = x - mx;
                                    dx *= dx;
                                    if (dx + dyz > 21) {
                                        continue;
                                    }
                                    int chunkX = FastMath.FloorToInt (x / (float)VoxelPlayEnvironment.CHUNK_SIZE);
                                    if (chunkX != lastChunkX || chunkZ != lastChunkZ || chunkY != lastChunkY) {

                                        lastChunkX = chunkX;
                                        lastChunkY = chunkY;
                                        lastChunkZ = chunkZ;
                                        env.STAGE = 3004;
                                        chunk = env.GetChunkUnpopulated (chunkX, chunkY, chunkZ);
                                        env.STAGE = 3005;
                                        if (chunk.isPopulated) {
                                            worm.life = 0;
                                            y = maxy;
                                            z = maxz;
                                            break;
                                        }
                                        chunk.hasCaves = true;
                                        // mark the chunk as modified by this detail generator
                                        SetChunkIsDirty (chunk);
                                    }

                                    int px = x - chunkX * VoxelPlayEnvironment.CHUNK_SIZE;
                                    int voxelIndex = voxelIndexZ + px;

                                    // set this voxel as a "hole" (hasContent = 2) so it doesn't get filled by terrain generator when it creates terrain
                                    chunk.voxels [voxelIndex].hasContent = 2;
                                }
                            }
                        }
                        worm.life--;
                        if (worm.life <= 0) {
                            env.STAGE = 3007;
                            worms.RemoveAt (k);
                            env.STAGE = 0;
                            return true;
                        }
                    }
                    // Worm is a struct, update the list
                    worms [k] = worm;

                    long elapsed = env.stopWatch.ElapsedMilliseconds;
                    if (elapsed >= endTime) {
                        return true;
                    }
                }
                env.STAGE = 0;
            }
            return false;
        }

    }

    public partial class VoxelChunk
    {
        [NonSerialized]
        public bool hasCaves;
    }

}