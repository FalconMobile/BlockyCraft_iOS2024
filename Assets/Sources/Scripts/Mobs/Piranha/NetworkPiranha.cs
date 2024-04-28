using UnityEngine;
using VoxelPlay;
using Mirror;

namespace IslandAdventureBattleRoyale
{
    /// <summary>
    /// Controls piranha. This is spawned when player stays on water for some time. Check NetworkMob base class for more details.
    /// </summary>
    public class NetworkPiranha : NetworkMob
    {
        public float speed = 10f;

        public float steerSpeed = 20f;

        /// <summary>
        /// Damage caused by this mob
        /// </summary>
        public int damage = 10;

        /// <summary>
        /// Time between attackes
        /// </summary>
        public float attackDelay = 3;

        // Audio Effects
        public AudioClip attackSoundEffect;
        public AudioClip hitSoundEffect;
        public AudioClip deathSoundEffect;

        float lastAttackTime;
        Vector3 destination;

        public override void InitMob()
        {
            thisAnimator.applyRootMotion = false;
        }


        public override void Disengage(ILivingEntity target)
        {
            if (this.target == target)
            {
                this.target = null;
                SwitchState(MobState.ReturnToStartPosition);
            }
        }

        public override void IncreaseScore(int amount)
        {
        }


        public override void ManageState()
        {

            switch (mobState)
            {
                case MobState.Idle:
                    {
                        // Always go after nearest player
                        NetworkPlayer nearest = WorldState.Instance.GetNearestPlayer(transform.position);
                        if (nearest != null)
                        {
                            target = nearest;
                            SwitchState(MobState.EngagingPlayerMelee);
                        }
                        else
                        {
                            NetworkServer.Destroy(gameObject);
                        }
                    }
                    break;

                case MobState.ReturnToStartPosition:
                    {
                        destination = initialPosition;
                        float distanceToTargetSqr = FastVector.SqrDistanceByValue(transform.position, destination);
                        if (distanceToTargetSqr < 3)
                        {
                            NetworkServer.Destroy(gameObject);
                        }
                    }
                    break;

                case MobState.EngagingPlayerMelee:
                    {
                        if (!targetIsValid || !target.isAlive)
                        {
                            SwitchState(MobState.ReturnToStartPosition);
                            return;
                        }

                        Vector3 targetPosition = target.GetTransform().position;
                        float distanceToTargetSqr = FastVector.SqrDistanceByValue(transform.position, targetPosition);

                        if (distanceToTargetSqr > maxEngageDistance * maxEngageDistance)
                        {
                            NetworkServer.Destroy(gameObject);
                            return;
                        }

                        if (distanceToTargetSqr < 8)
                        { // melee attack
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
                            destination = targetPosition + (transform.position - targetPosition).normalized * 2f;
                            if (destination.y > env.waterLevel)
                            {
                                destination.y = env.waterLevel;
                            }
                            if (env.GetWaterDepth(destination) <= 2)
                            {
                                SwitchState(MobState.Idle);
                            }
                        }
                    }
                    break;
            }
        }


        private void Update()
        {
            if (isAlive && isReady && mobState != MobState.Idle)
            {
                float deltaTime = Time.deltaTime;

                Vector3 newDirection = (destination - transform.position).normalized;
                transform.forward = Vector3.RotateTowards(transform.forward, newDirection, steerSpeed * Mathf.Deg2Rad * deltaTime, 1f);
                Vector3 move = new Vector3(transform.forward.x * speed * deltaTime, transform.forward.y * speed * deltaTime, transform.forward.z * speed * deltaTime);
                transform.position += move;

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
    }
}
