using UnityEngine;
using System.Collections.Generic;
using Mirror;
using VoxelPlay;

public partial class WorldState : NetworkBehaviour
{
    public static bool IsDedicatedServer;
    [SerializeField] private bool dropInventoryOnDeath;

    [Header("Mob List")]
    public List<NetworkMob> NetworkMobs = new List<NetworkMob>();

    [Header("Mob Spawners")]
    public List<ServerPrefabSpawner> MobSpawners = new List<ServerPrefabSpawner>();
    public List<ServerRepeatingSpawner> MobRepeatedSpawners = new List<ServerRepeatingSpawner>();

    [Header("Common Prefabs")]
    public GameObject VoxelPrefab;

    public List<NetworkPlayer> NetworkPlayers = new List<NetworkPlayer>();

    public bool DropInventoryOnDeath => dropInventoryOnDeath;
    
    private static WorldState _instance;
    public static WorldState Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WorldState>();
            }
            return _instance;
        }
    }

    private float _islandRadiusParam = 50;
    private float _islandSlopeFactor = 0.1f;

    private VoxelPlayEnvironment _env;
    
    public override void OnStartServer()
    {
        base.OnStartServer();

        // set spawner amount options
        for (int i = 0; i < MobSpawners.Count; i++)
        {
            var spawner = MobSpawners[i];
            spawner.LoadSettingsSpawnProbability();
        }
        for (int i = 0; i < MobRepeatedSpawners.Count; i++)
        {
            var spawner = MobRepeatedSpawners[i];
            spawner.LoadSettingsSpawnProbability();
        }

        _env = VoxelPlayEnvironment.instance;

        // Enables server mode only if needed to avoid rendering anything on the server
        if (IsDedicatedServer)
        {
            _env.serverMode = true;
            _env.onlyRenderInFrustum = false;
            _env.unloadFarChunks = false;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.farClipPlane = 1;
            Camera.main.backgroundColor = Color.black;
            // Display message
            gameObject.AddComponent<DedicatedServerMessage>();
        }

        chunkData = _env.GetChunkRawBuffer();
        _env.OnChunkChanged += OnChunkChangedMethod;

        // Enable detail generators only at server; spawned objects on server will appear across clients
        _env.enableDetailGenerators = true;

        // set island size when clients connects to the server (the island size params are syncvar fields)
        float optionIslandSize = PlayerPrefs.GetFloat(TerrainKeyword.ISLAND_SIZE);
        switch (optionIslandSize)
        {
            case 0:
                _islandRadiusParam = 0f;
                _islandSlopeFactor = 0.15f;
                break;
            case 1:
                _islandRadiusParam = 50f;
                _islandSlopeFactor = 0.1f;
                break;
            case 2:
                _islandRadiusParam = 100f;
                _islandSlopeFactor = 0.075f;
                break;
        }

        SetIslandSize();
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        _env = VoxelPlayEnvironment.instance;

        // In OnStartClient, the SyncVars for island size are already set, so we pass them to the terrain generator of Voxel Play
        SetIslandSize();

        // When VP completes initialization, we'll request the server to send us the Sun direction and any changed chunk so we update them in this client
        _env.OnInitialized += () =>
        {
            if (!isServer)
            {
                    // A new client has joined => request Sun direction from server as well as any modified chunk
                    CmdRequestChangedChunks(_env.playerGameObject);
            }
        };
    }

    // Pass island parameters to the terrain generator of Voxel Play
    private void SetIslandSize()
    {
        // Set terrain generator island size
        TerrainDefaultGenerator tg = (TerrainDefaultGenerator)_env.world.terrainGenerator;
        for (int k = 0; k < tg.Steps.Length; k++)
        {
            StepData step = tg.Steps[k];
            if (step.operation == TerrainStepType.Island)
            {
                step.param2 = _islandSlopeFactor;
                step.param = _islandRadiusParam;
                tg.Steps[k] = step; // step is a struct, we need to place it back into the array
                break;
            }
        }
    }

    /// <summary>
    /// Sends a message to all players
    /// </summary>
    /// <param name="text"></param>
    [ClientRpc]
    private void SendMessageToAllPlayers(string text)
    {
        _env.ShowMessage("<color=yellow>" + text + "</color>");
    }

    /// <summary>
    /// Sends a localized message to all players
    /// </summary>
    /// <param name="text"></param>
    /// <param name="textType"></param>
    [ClientRpc]
    private void SendPlayerInfoMessageToAllPlayers(string text, VoxelPlayEnvironment.TextType textType)
    {
        _env.ShowPlayerMessage("<color=yellow>" + text + "</color>", textType);
    }
}