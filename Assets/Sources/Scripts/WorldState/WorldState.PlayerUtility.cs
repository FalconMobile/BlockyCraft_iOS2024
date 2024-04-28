using UnityEngine;
using System.Text;
using Mirror;
using VoxelPlay;

public partial class WorldState : NetworkBehaviour
{
    readonly StringBuilder sb = new StringBuilder();
    private string previousTopPlayers = string.Empty;

    /// <summary>
    /// Called when player joins the game
    /// </summary>
    public void RegisterPlayer(NetworkPlayer player)
    {
        CheckPlayerName(player);
        SendPlayerInfoMessageToAllPlayers(player.playerName, VoxelPlayEnvironment.TextType.PlayerJoined);

        NetworkPlayers.Add(player);

        // in dedicated server mode, ensure the Voxel Play behaviour can properly discover neighbour chunks
        if (IsDedicatedServer)
        {
            VoxelPlayBehaviour vpb = player.GetComponent<VoxelPlayBehaviour>();
            if (vpb != null)
            {
                vpb.checkNearChunks = true;
                vpb.renderChunks = true;
                vpb.chunkExtents = new Vector3(2, 1, 2);
            }
        }
    }

    /// <summary>
    /// Called when player abandons the session
    /// </summary>
    public void UnregisterPlayer(NetworkPlayer player)
    {
        if (NetworkPlayers.Contains(player))
        {
            NetworkPlayers.Remove(player);
        }

        SendPlayerInfoMessageToAllPlayers(player.playerName, VoxelPlayEnvironment.TextType.PlayerLeft);
    }


    public void PlacePlayerOnStartPosition(Transform player)
    {
        //player.position = GetPositionOnCenter(); // GetRandomPositionOnBeach();

        Vector3 islandCenter = new Vector3(0, player.position.y, 0);
        player.LookAt(islandCenter, Vector3.up);
    }

    /// <summary>
    /// TO DO: написать появление игроков на соответствующий режим
    /// </summary>
    public Vector3 GetPositionOnCenter()
    {
        var terrainHeight = _env.GetTerrainHeight(Vector3.zero);
        return new Vector3(0, terrainHeight, 0);
    }

    public Vector3 GetRandomPositionOnBeach()
    {
        for (int k = 0; k < 100; k++)
        {
            Vector3 highPos = Vector3.zero;
            Vector3 dir = Random.onUnitSphere;
            Vector3 lowPos = dir * 1000;

            for (int j = 0; j < 100; j++)
            {
                Vector3 mpos = (highPos + lowPos) * 0.5f;
                float h = _env.GetTerrainHeight(mpos);
                if (h < _env.waterLevel)
                {
                    lowPos = mpos;
                }
                else if (h > _env.waterLevel + 1)
                {
                    highPos = mpos;
                }
                else
                {
                    mpos.y = _env.GetTerrainHeight(mpos) + 0.5f;
                    return mpos;
                }
            }
        }

        Vector3 defaultPos = Random.onUnitSphere * 50;
        defaultPos.y = _env.GetTerrainHeight(defaultPos);
        return defaultPos;
    }


    /// <summary>
    /// Makes rest of mobs aware of this shoot if they're in range
    /// </summary>
    public void NotifyShoot(Vector3 soundSourcePosition, ILivingEntity shooter)
    {
        int mobCount = NetworkMobs.Count;
        for (int k = 0; k < mobCount; k++)
        {
            NetworkMob mob = NetworkMobs[k];
            if (mob.isAlive)
            {
                mob.NotifyLoudSound(soundSourcePosition, shooter);
            }
        }
    }

    private void RebuildTopPlayers()
    {
        sb.Length = 0;
        int playersCount = NetworkPlayers.Count;
        if (playersCount > 1)
        {
            sb.AppendLine("<color=green>Top Players</color>");
            NetworkPlayers.Sort(ScoreSort);
            for (int k = 0; k < playersCount; k++)
            {
                NetworkPlayer player = NetworkPlayers[k];
                if (!player.isAlive)
                {
                    sb.Append("<color=red>");
                }

                sb.Append(player.playerName);
                sb.Append(": ");
                sb.Append(player.score);
                if (!player.isAlive)
                {
                    sb.Append("</color>");
                }

                sb.AppendLine();
            }

            string topPlayers = sb.ToString();
            if (topPlayers != previousTopPlayers)
            {
                previousTopPlayers = topPlayers;

                // notify players of change
                for (int k = 0; k < playersCount; k++)
                {
                    NetworkPlayer player = NetworkPlayers[k];
                    player.NotifyTopPlayersChange(topPlayers);
                }
            }
        }
    }

    private void CheckPlayerName(NetworkPlayer player)
    {
        // give a default name to this player
        if (string.IsNullOrEmpty(player.playerName))
        {
            // ensure name is unique
            int suffix = NetworkPlayers.Count + 1;
            bool nameTaken;
            string playerName;
            do
            {
                nameTaken = false;
                playerName = "Player" + suffix;
                for (int k = 0; k < NetworkPlayers.Count; k++)
                {
                    if (playerName.Equals(NetworkPlayers[k].playerName))
                    {
                        nameTaken = true;
                        break;
                    }
                }

                suffix++;
            } while (nameTaken);

            player.playerName = playerName;
        }
    }

    private int ScoreSort(NetworkPlayer p1, NetworkPlayer p2)
    {
        return p2.score.CompareTo(p1.score);
    }
}