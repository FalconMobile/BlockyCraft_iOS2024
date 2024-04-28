using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FreeRespawnesController
{
    public const string FREE_SPAWN_KEY = "SpawnKey";

    public static int GetFreeSpawns()
    {
        return PlayerPrefs.GetInt(FREE_SPAWN_KEY);
    }

    public static void AddFreeSpawns(int count)
    {
        int current = GetFreeSpawns();
        PlayerPrefs.SetInt(FREE_SPAWN_KEY, current + count);
    }


    public static void ConsumeFreeSpawn()
    {
        int current = GetFreeSpawns();
        if (current > 0)
        {
            PlayerPrefs.SetInt(FREE_SPAWN_KEY, current - 1);
        }
    }
}
