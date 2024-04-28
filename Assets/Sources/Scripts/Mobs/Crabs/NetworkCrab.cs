using UnityEngine;
using VoxelPlay;


namespace IslandAdventureBattleRoyale
{
    /// <summary>
    /// Controls crab. Check NetworkMob base class for more details.
    /// </summary>
    public class NetworkCrab : NetworkMob
    {
        /// <summary>
        /// Damage caused by this crab
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


        public override void ManageState()
        {

            switch (mobState)
            {
                case MobState.Idle:
                    {
                        // If player is within 5 meters of crab, the crab will attack player
                        NetworkPlayer nearest = WorldState.Instance.GetNearestPlayer(transform.position);
                        if (nearest != null)
                        {
                            float distanceSqr = FastVector.SqrDistanceByValue(transform.position, nearest.transform.position);
                            if (distanceSqr < 16)
                            {
                                target = nearest;
                                SwitchState(MobState.EngagingPlayerMelee);
                            }
                        }
                    }
                    break;
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
                        Vector3 targetPosition = target.GetTransform().position;
                        float distanceToTargetSqr = FastVector.SqrDistanceByValue(transform.position, targetPosition);

                        if (distanceToTargetSqr > maxEngageDistance * maxEngageDistance || !target.isAlive)
                        {
                            SwitchState(MobState.ReturnToStartPosition);
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
            thisAudioSource.PlayOneShot(attackSoundEffect);
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
