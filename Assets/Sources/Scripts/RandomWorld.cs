using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelPlay;
using Random = UnityEngine.Random;

public class RandomWorld : MonoBehaviour
{
    [Range(0, 1000)]
    [Tooltip("Значение не ниже 0")]
    [FormerlySerializedAs("SeedRandomMin")]
    [SerializeField]
    private int seedRandomMin;

    [Range(0, 1000)]
    [Tooltip("Значение не больше 1000")]
    [FormerlySerializedAs("SeedRandomMax")]
    [SerializeField]
    private int seedRandomMax;

    [FormerlySerializedAs("VoxelPlayEnvironment")]
    [SerializeField]
    private VoxelPlayEnvironment voxelPlayEnvironment;

    [FormerlySerializedAs("WorlDefinition")]
    [SerializeField]
    private List<WorldDefinition> worldsDefinitions;

    [NonSerialized] public static bool isNewGame;

    private const string IdLastGame = "IdLastGame";
    private const string SeedLastGame = "SeedLastGame";


    public void Awake()
    {
        if (isNewGame)
        {
            int mapId = Random.Range(0, worldsDefinitions.Count - 1);
            int seedId = Random.Range(seedRandomMin, seedRandomMax);

            PlayerPrefs.SetInt(IdLastGame, mapId);
            PlayerPrefs.SetInt(SeedLastGame, seedId);

            worldsDefinitions[mapId].seed = seedId;
            voxelPlayEnvironment.world = worldsDefinitions[mapId];
        }
        else
        {
            worldsDefinitions[PlayerPrefs.GetInt(IdLastGame, Random.Range(0, worldsDefinitions.Count - 1))]
                .seed = PlayerPrefs.GetInt(SeedLastGame, Random.Range(seedRandomMin, seedRandomMax));
            voxelPlayEnvironment.world =
                worldsDefinitions[PlayerPrefs.GetInt(IdLastGame, Random.Range(0, worldsDefinitions.Count - 1))];
        }
    }
}