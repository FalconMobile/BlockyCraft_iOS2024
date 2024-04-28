using UnityEngine;

namespace IslandAdventureBattleRoyale
{

    /// <summary>
    /// Controls parrot. Check NetworkMob base class for more details.
    /// </summary>
    public class NetworkParrot : NetworkMob
    {

        public float maxAltitude = 50;
        public float flySpeed = 60;
        public float ascendSpeed = 20;
        public float steerSpeed = 30;
        public float flyRadius = 100;

        // Audio Effects
        public AudioClip hitSoundEffect;
        public AudioClip deathSoundEffect;

        float lastAltitudeCheckTime;
        float targetAltitude;
        Vector3 flyDirection = Vector3.one;

        public override void InitMob()
        {
            thisAnimator.applyRootMotion = false;
        }

        public override void ManageState()
        {
            switch (mobState)
            {
                case MobState.Idle:
                    SwitchState(MobState.Flying);
                    thisAnimator.SetFloat(AnimationKeyword.Speed, 0);
                    break;

                case MobState.Flying:
                    float now = Time.time;

                    if (now - lastAltitudeCheckTime > 3)
                    {
                        lastAltitudeCheckTime = now;
                        float altitude = env.GetTerrainHeight(transform.position, true);
                        targetAltitude = altitude + maxAltitude;
                    }

                    float rotationAngle = now % 360;
                    float cx = initialPosition.x + Mathf.Cos(rotationAngle * Mathf.Deg2Rad) * flyRadius;
                    float cz = initialPosition.z + Mathf.Sin(rotationAngle * Mathf.Deg2Rad) * flyRadius;

                    Vector3 targetPos = new Vector3(cx, targetAltitude, cz);
                    flyDirection = (targetPos - transform.position).normalized;

                    // Animate bird
                    thisAnimator.SetFloat(AnimationKeyword.Speed, 1);

                    break;
            }
        }

        private void Update()
        {
            if (isAlive && isReady)
            {
                float deltaTime = Time.deltaTime;
                transform.forward = Vector3.RotateTowards(transform.forward, flyDirection, steerSpeed * Mathf.Deg2Rad * deltaTime, 1f);
                Vector3 move = new Vector3(transform.forward.x * flySpeed * deltaTime, transform.forward.y * ascendSpeed * deltaTime, transform.forward.z * flySpeed * deltaTime);
                transform.position += move;
            }
        }

        /// <summary>
        /// Animation events, called by the animator
        /// </summary>        
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
