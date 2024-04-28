using UnityEngine;
using VoxelPlay;


namespace IslandAdventureBattleRoyale
{
    /// <summary>
    /// Controls pig. Check NetworkMob base class for more details.
    /// </summary>
    public class NetworkPig : NetworkMob
    {

        /// <summary>
        /// Biome where this animal hunts or eats
        /// </summary>
        public BiomeDefinition huntBiome;

        /// <summary>
        /// Destination when hunting
        /// </summary>
        public Vector3 destination;

        // Audio Effects
        public AudioClip hitSoundEffect;
        public AudioClip deathSoundEffect;

        public override void InitMob()
        {
            thisAnimator.applyRootMotion = false;
            if (huntBiome == null)
            {
                Debug.LogWarning("Hunt biome not set for pig");
            }
        }



        public override void ManageState()
        {

            switch (mobState)
            {
                case MobState.Idle:
                    {
                        FindPlayer(4);
                    }
                    break;
                case MobState.Fleeing:
                    {
                        if (FastVector.SqrDistanceByValue(destination, transform.position) < 9)
                        {
                            FleeToRandomPosition();
                        }
                        if (!targetIsValid || !target.isAlive || Vector3.Distance(target.GetTransform().position, transform.position) > 50)
                        {
                            SwitchState(MobState.ReturnToStartPosition);
                        }
                        break;
                    }
                case MobState.ReturnToStartPosition:
                    {
                        float distanceSqr = FastVector.SqrDistanceByValue(initialPosition, transform.position);
                        if (distanceSqr > 1)
                        {
                            MoveTo(initialPosition);
                        }
                        else
                        {
                            SwitchState(MobState.Idle);
                        }
                    }
                    break;
                case MobState.EngagingPlayerMelee:
                    {
                        SwitchState(MobState.Fleeing);
                    }
                    break;
            }
        }


        void FleeToRandomPosition()
        {
            if (huntBiome == null || agent == null) return;

            for (int k = 0; k < 100; k++)
            {
                Vector3 newPosition = transform.position + Random.insideUnitSphere * 50;
                if (env.GetBiome(newPosition) == huntBiome)
                {
                    destination = newPosition;
                    destination.y = env.GetTerrainHeight(newPosition);
                    SwitchState(MobState.Fleeing);
                    MoveTo(destination);
                    return;
                }

            }
        }

        public override void NotifyLoudSound(Vector3 soundSourcePosition, ILivingEntity shooter)
        {
            if (shooter is NetworkPlayer)
            {
                const float AWARENESS_DISTANCE_SQR = 50 * 50;
                float sqrDistance = (transform.position - soundSourcePosition).sqrMagnitude;
                if (sqrDistance < AWARENESS_DISTANCE_SQR)
                {
                    target = shooter;
                    FleeToRandomPosition();
                }
            }
        }

        void FindPlayer(float playerDistance)
        {
            // If player is within 10 meters of panther, the panther will attack player
            NetworkPlayer nearest = WorldState.Instance.GetNearestPlayer(transform.position);
            if (nearest != null)
            {
                float distance = FastVector.SqrDistanceByValue(transform.position, nearest.transform.position);
                if (distance < playerDistance * playerDistance)
                {
                    target = nearest;
                    FleeToRandomPosition();
                }
            }

        }

        /// <summary>
        /// Animation events, called by the animator
        /// </summary>
        // Triggered when Mob's Attack begins
        public void AttackStartEvent()
        {
        }

        // Triggered when Mob's Attack makes contact
        public void AttackHitEvent()
        {
            
        }

        // Triggered when Mob's Hit Animation starts (receives a hit)
        public void HitEvent()
        {
            thisAudioSource.PlayOneShot(hitSoundEffect);
        }

        // Triggered when Mob's Death Animation starts
        public void DeathEvent()
        {
            thisAudioSource.PlayOneShot(deathSoundEffect);
        }
    }   
}
