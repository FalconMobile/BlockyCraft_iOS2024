using System.Collections.Generic;
using UnityEngine;
using Mirror;
using VoxelPlay;

    /// <summary>
    /// Spawns prefab at certain rate if some player is near the spawnable area
    /// </summary>
    public class ServerRepeatingSpawner : NetworkBehaviour
    {
        public string Name = string.Empty;
        public float interval = 5f;
        [Range(0, 1)]
        public float probability = 0.1f;
        public Bounds spawnArea = new Bounds(Vector3.zero, new Vector3(30, 0, 30));
        public GameObject[] prefab;
        public List<VoxelDefinition> sacredVoxels;

        private VoxelPlayEnvironment _env;

        public void LoadSettingsSpawnProbability()
        {
            float interval = 5f - PlayerPrefs.GetFloat(Name, 1f) * 2f;
            float probability = 0.2f + PlayerPrefs.GetFloat(Name, 1f) * 0.15f;
            SetSpawnRate(interval, probability);
        }

        private void Start()
        {
            if (prefab == null || prefab.Length == 0)
            {
                Debug.Log("No prefabs set in Nightly Spawner.");
                enabled = false;
                return;
            }

            _env = VoxelPlayEnvironment.instance;
            _env.OnInitialized += VP_OnInitialized;
            _env.OnVoxelAfterDamaged += Env_OnVoxelAfterDamaged;
        }

        private void Env_OnVoxelAfterDamaged(VoxelChunk chunk, int voxelIndex, int damage)
        {
            // if players hit a "sacred" head, spawn a few skeletons
            VoxelDefinition vd = chunk.voxels[voxelIndex].type;
            if (sacredVoxels != null && sacredVoxels.Contains(vd))
            {
                int randomCount = Random.Range(1, 5);
                for (int k=0;k<randomCount;k++)
                {
                    SpawnPrefab();
                }
            }
        }

        private void VP_OnInitialized()
        {
            InvokeRepeating(nameof(CheckSpawn), interval, interval);
        }

        private void SetSpawnRate(float interval, float probability)
        {
            this.interval = interval;
            this.probability = probability;
            if (_env != null && _env.initialized)
            {
                CancelInvoke();
                VP_OnInitialized();
            }
        }

        private void CheckSpawn()
        {
            //if (env.sun.transform.forward.y > 0) // uncomment to spawn at night
            {
                if (Random.value < probability)
                {
                    SpawnPrefab();
                }
            }
        }

        private void SpawnPrefab()
        {
            Vector3 spawnPos = new Vector3(Random.Range(spawnArea.min.x, spawnArea.max.x), 0, Random.Range(spawnArea.min.z, spawnArea.max.z));
            // Pick nearest player
            NetworkPlayer nearestPlayer = WorldState.Instance.GetNearestPlayer(spawnPos);
            if (nearestPlayer != null)
            {
                spawnPos.y = nearestPlayer.transform.position.y;
                float nearestPlayerDistance = Vector3.Distance(spawnPos, nearestPlayer.transform.position);
                if (nearestPlayerDistance > 10 && nearestPlayerDistance < 50)
                {
                    int prefabIndex = Random.Range(0, prefab.Length);
                    Spawn(spawnPos, prefab[prefabIndex]);
                }
            }
        }

        private void Spawn(Vector3 spawnPos, GameObject prefab)
        {
            spawnPos.y = _env.GetTerrainHeight(spawnPos);
            GameObject o = Instantiate(prefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(o);
        }
    }
