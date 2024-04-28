using UnityEngine;
using System.Collections.Generic;
using Mirror;
using VoxelPlay;

public partial class WorldState : NetworkBehaviour
{
    public void RegisterMob(NetworkMob newMob)
    {
        Debug.Log($"WorldState RegisterMob {newMob.screenName} ");
        NetworkMobs.Add(newMob);
    }

    public void UnregisterMob(NetworkMob mob)
    {
        if (NetworkMobs.Contains(mob))
        {
            NetworkMobs.Remove(mob);
        }
    }
}