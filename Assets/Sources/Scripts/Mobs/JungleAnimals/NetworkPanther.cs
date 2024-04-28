using UnityEngine;
using VoxelPlay;


namespace IslandAdventureBattleRoyale
{
    /// <summary>
    /// Controls panther. Check NetworkMob base class for more details.
    /// </summary>
    public class NetworkPanther : NetworkMob
    {

        /// <summary>
        /// Biome where this animal hunts
        /// </summary>
        public BiomeDefinition huntBiome;

        /// <summary>
        /// Damage caused by this mob
        /// </summary>
        public int damage = 40;

        /// <summary>
        /// If player gets near this distance, this mob will detect him/her
        /// </summary>
        public float playerDistanceSensitivity = 5;

        /// <summary>
        /// Time between attackes
        /// </summary>
        public float attackDelay = 2;

        /// <summary>
        /// Destination when hunting
        /// </summary>
        public Vector3 destination;

        // Audio Effects
        public AudioClip attackSoundEffect;
        public AudioClip hitSoundEffect;
        public AudioClip deathSoundEffect;

        float lastAttackTime;
        float hunger, nextHunger;

        public override void InitMob()
        {
            thisAnimator.applyRootMotion = false;
            if (huntBiome == null)
            {
                Debug.LogWarning("Hunt biome not set for panther");
            }
            nextHunger = Random.Range(5, 50);
        }


        public override void Disengage(ILivingEntity target)
        {
            if (this.target == target)
            {
                this.target = null;
                nextHunger = Random.Range(5, 50);
                SwitchState(MobState.ReturnToStartPosition);
            }
        }


        public override void ManageState()
        {

            switch (mobState)
            {
                case MobState.Idle:
                    {
                        agent.speed = 3.5f;
                        FindPlayer(playerDistanceSensitivity);
                        // Check hunger
                        hunger++;
                        if (hunger > nextHunger)
                        {
                            GetRandomHuntTarget();
                            SwitchState(MobState.Hunting);
                        }
                    }
                    break;
                case MobState.Hunting:
                    {
                        FindPlayer(15);
                        if (FastVector.SqrDistanceByValue(destination, transform.position) < 9)
                        {
                            nextHunger = Random.Range(5, 50);
                            SwitchState(MobState.Idle);
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
                    if (targetIsValid)
                    {
                        if (agent.speed != 13f)
                        {
                            agent.speed = 13f;
                            agent.angularSpeed = 133;
                            thisAnimator.SetFloat(AnimationKeyword.Speed, 1f);
                        }

                        Vector3 targetPosition = target.GetTransform().position;
                        float distanceToTargetSqr = FastVector.SqrDistanceByValue(transform.position, targetPosition);

                        if (distanceToTargetSqr > maxEngageDistance * maxEngageDistance || !target.isAlive)
                        {
                            SwitchState(MobState.ReturnToStartPosition);
                            return;
                        }

                        if (distanceToTargetSqr < 9) // melee attack
                        {
                            float now = Time.time;
                            if (now - lastAttackTime > attackDelay)
                            {
                                lastAttackTime = now;
                                thisNetworkAnimator.SetTrigger(AnimationKeyword.Attack);
                                Invoke(nameof(DamageTarget), 0.5f);
                            }
                        }
                        else
                        {
                            MoveTo(targetPosition + (transform.position - targetPosition).normalized * 2f);
                        }

                    }
                    break;
            }
        }


        /// <summary>
        /// Damages player
        /// </summary>
        void DamageTarget()
        {
            // Pick a random limb to damage
            NetworkDamageTaker damageTaker = target.GetTransform().GetComponentInChildren<NetworkDamageTaker>();
            if (damageTaker != null)
            {
                WorldState.Instance.MobDamagesPlayer(damageTaker, this, damage);
            }
        }

        void GetRandomHuntTarget()
        {
            if (huntBiome == null || agent == null) return;

            for (int k = 0; k < 100; k++)
            {
                Vector3 newPosition = transform.position + Random.insideUnitSphere * 50;
                if (env.GetBiome(newPosition) == huntBiome)
                {
                    destination = newPosition;
                    destination.y = env.GetTerrainHeight(newPosition);
                    MoveTo(destination);
                    return;
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
                    SwitchState(MobState.EngagingPlayerMelee);
                }
            }

        }

        /// <summary>
        /// Animation events, called by the animator
        /// </summary>
        // Triggered when Mob's Attack begins
        public void AttackStartEvent()
        {
            thisAudioSource.PlayOneShot(attackSoundEffect);
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


        // Let the panther/jaguar hear the sound if player shoots near
        public override void NotifyLoudSound(Vector3 soundSourcePosition, ILivingEntity shooter)
        {
            if (mobState.InCombat()) return;

            if (shooter is NetworkPlayer)
            {
                const float AWARENESS_DISTANCE_SQR = 50 * 50;
                float sqrDistance = (transform.position - soundSourcePosition).sqrMagnitude;
                if (sqrDistance < AWARENESS_DISTANCE_SQR)
                {
                    target = shooter;
                    SwitchState(MobState.EngagingPlayerMelee);
                }
            }
        }
    }   
}
