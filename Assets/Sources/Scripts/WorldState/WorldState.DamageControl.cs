using Mirror;
using UnityEngine;
using VoxelPlay;

public partial class WorldState : NetworkBehaviour
{
    /// <summary>
    /// Returns the nearest player to a given position
    /// </summary>
    public NetworkPlayer GetNearestPlayer(Vector3 position)
    {
        int playerCount = NetworkPlayers.Count;
        NetworkPlayer nearest = null;
        float minDist = float.MaxValue;
        for (int k = 0; k < playerCount; k++)
        {
            NetworkPlayer otherPlayer = NetworkPlayers[k];
            float dist = FastVector.SqrDistanceByValue(position, otherPlayer.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = otherPlayer;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Called whenever a player hits another player or mob. This logic runs at server always.
    /// </summary>
    [Server]
    public void CharacterGetDamage(NetworkDamageTaker damageTaker, ILivingEntity attacker, int damageAmount)
    {
        ILivingEntity receiver = damageTaker.entity;
        if (!receiver.isAlive)
        {
            return;
        }

        damageTaker.ApplyDamage(attacker, damageAmount);

        if (receiver.isAlive)
        {
            attacker?.IncreaseScore(receiver.GetScoreByHit());
        }
        else
        {
            attacker?.IncreaseScore(receiver.GetScoreByKill());
            AnnounceKill(damageTaker, attacker);
        }

        RebuildTopPlayers();
        ShowHealthIndicator(damageTaker, attacker);
    }

    private void AnnounceKill(NetworkDamageTaker damageTaker, ILivingEntity attacker)
    {
        if (damageTaker.entity is NetworkPlayer)
        {
            string message = damageTaker.entity.GetScreenName() + " was killed";
            if (attacker != null)
            {
                message += " by " + attacker.GetScreenName();
            }

            SendMessageToAllPlayers(message);
        }
    }

    private void ShowHealthIndicator(NetworkDamageTaker damageTaker, ILivingEntity attacker)
    {
        if (attacker is NetworkPlayer networkPlayer)
        {
            networkPlayer.RpcShowTargetLifebarIndicator(damageTaker.entity.GetTransform(),
                damageTaker.damageMultiplier);
        }
    }


    [Server]
    public void AreaDamage(Vector3 center, float distance, float damage, ILivingEntity attacker)
    {
        // check damage to players
        foreach (NetworkPlayer player in NetworkPlayers)
        {
            float dist = Vector3.Distance(player.transform.position, center);
            int damageTaken = (int)(damage * Mathf.Clamp01(1f - dist / distance));
            if (damageTaken > 0)
            {
                NetworkDamageTaker damageTaker = player.GetComponentInChildren<NetworkDamageTaker>();
                if (damageTaker != null)
                {
                    CharacterGetDamage(damageTaker, attacker, damageTaken);
                }
            }
        }

        // check damage to mobs
        foreach (NetworkMob mob in NetworkMobs)
        {
            float dist = Vector3.Distance(mob.transform.position, center);
            int damageTaken = (int)(damage * Mathf.Clamp01(1f - dist / distance));
            if (damageTaken > 0)
            {
                NetworkDamageTaker damageTaker = mob.GetComponentInChildren<NetworkDamageTaker>();
                if (damageTaker != null)
                {
                    CharacterGetDamage(damageTaker, attacker, damageTaken);
                }
            }

            // notify sound
            if (mob.isAlive)
            {
                mob.NotifyLoudSound(center, attacker);
            }
        }
    }

    /// <summary>
    /// Called when a mob hits a player. Runs on server.
    /// </summary>
    [Server]
    public void MobDamagesPlayer(NetworkDamageTaker damageTaker, ILivingEntity attacker, int damageAmount)
    {
        ILivingEntity receiver = damageTaker.entity;
        bool wasAlive = receiver.isAlive;
        damageTaker.ApplyDamage(attacker, damageAmount);
        bool isAlive = receiver.isAlive;
        if (wasAlive != isAlive)
        {
            attacker.Disengage(receiver);
            if (receiver is NetworkPlayer)
            {
                SendMessageToAllPlayers(receiver.GetScreenName() + " was killed by " + attacker.GetScreenName() + ".");
            }
        }
    }
}