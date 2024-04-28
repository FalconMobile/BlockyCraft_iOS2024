using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

namespace VoxelPlay
{
    /// <summary>This script is designed to be run only at server.
    /// The enableDetailGenerators setting in Voxel Play Environment should be disabled
    /// and only enabled when a WorldState script is run from server (ie. in OnStartServer)</summary>
    [CreateAssetMenu(menuName = "Voxel Play/Detail Generators/Server Prefab Spawner", fileName = "ServerPrefabSpawner", order = 104)]
    public class ServerPrefabSpawner : VoxelPlayDetailGenerator
    {
        public string Name;

        public float Seed;

        [Range(0, 1f)] public float SpawnProbability;

        [FormerlySerializedAs("minInstances")]
        public int MinInstances;
        
        [FormerlySerializedAs("maxInstances")]
        public int MaxInstances;

        [FormerlySerializedAs("allowedBiomes")]
        public BiomeDefinition[] AllowedBiomes;

        [FormerlySerializedAs("prefabs")]
        public GameObject[] Prefabs;

        [FormerlySerializedAs("optimizeMaterial")]
        public bool OptimizeMaterial = true;

        private VoxelPlayEnvironment _env;
        private Shader _vpShader;

        /// <summary>Initialization method. Called by Voxel Play at startup.</summary>
        public override void Init()
        {
            _env = VoxelPlayEnvironment.instance;
            _vpShader = Shader.Find("Voxel Play/Models/Texture/Opaque");
        }

        /// <summary>Fills the given chunk with detail.
        /// Filled voxels won't be replaced by the terrain generator.
        /// Use Voxel.Empty to fill with void </summary>
        /// <param name="chunk">Chunk.</param>
        public override void AddDetail(VoxelChunk chunk)
        {
            if (Prefabs == null || Prefabs.Length == 0)
                return;

            var position = (Vector3)chunk.position;

            // Random prob of spawning a prefab over this chunk
            // position (use position as seed of random plus some variation)
            var rndPos = position;
            rndPos.x += Seed + detailGeneratorIndex;
            
            if (WorldRand.GetValue(rndPos) > SpawnProbability)
                return;

            // Check this chunk is on surface
            var altitude = _env.GetTerrainHeight(position);
            var yBottom = position.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            var yTop = position.y + VoxelPlayEnvironment.CHUNK_HALF_SIZE;
            
            if (altitude < yBottom || altitude > yTop) 
                return;

            // Pick a suitable prefab and spawns it
            var biome = _env.GetBiome(position);
            
            if (AllowedBiomes == null) 
                return;
            
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var definition in AllowedBiomes)
            {
                if (definition != biome) continue;
                
                var quantity = Random.Range(MinInstances, MaxInstances + 1);
                
                for (var q = 0; q < quantity; q++) 
                    SpawnPrefab(position);
                
                return;
            }
        }

        public void LoadSettingsSpawnProbability()
        {
            if (string.IsNullOrEmpty(Name) || PlayerPrefs.HasKey(Name) is false)
            {
                Debug.LogError("::LoadSettingsSpawnProbability string.IsNullOrEmpty(Name)" +
                               " || PlayerPrefs.HasKey(Name) is false");
                return;
            }
            
            var prefsData = PlayerPrefs.GetFloat(Name);
            //prefsData = Mathf.Clamp(prefsData, 0, 1);


            if (name == "cannibalsAmount")
            {
                SpawnProbability = prefsData * 0.01f;
            }
            else
            {
                SpawnProbability = prefsData * 0.10f;
            }
            

            //Debug.LogWarning("Setting:::" + Name + "=" + SpawnProbability);
        }

        private void SpawnPrefab(Vector3 position)
        {
            var spawnCenter = position;
            var prefabIndex = WorldRand.Range(0, Prefabs.Length);
            position.x += WorldRand.Range(2, 12) - 7;
            position.z += WorldRand.Range(2, 12) - 7;
            position.y = _env.GetTerrainHeight(position);

            spawnCenter.y = position.y;

            var prefab = Instantiate(Prefabs[prefabIndex]);

            if (OptimizeMaterial)
            {
                var render = prefab.GetComponentInChildren<Renderer>();
                if (render != null)
                {
                    var oldMat = render.sharedMaterial;
                    var isExist = oldMat != null
                                  && !oldMat.shader.name.Contains("Voxel Play/Models");
                    
                    if (isExist && _vpShader != null)
                    {
                        var newMat = new Material(_vpShader)
                            {
                                mainTexture = oldMat.mainTexture,
                                color = oldMat.color
                            };
                            render.sharedMaterial = newMat;
                    }
                }
            }
            prefab.transform.position = position;

            // make cannibal look at the spawn position so if there's a group,
            // it may look like they're talking between them
            prefab.transform.LookAt(spawnCenter);

            var behaviour = prefab.GetComponentInChildren<VoxelPlayBehaviour>();
            if (behaviour == null)
            {
                prefab.AddComponent<VoxelPlayBehaviour>();
            }

            NetworkServer.Spawn(prefab);
        }
    }
}