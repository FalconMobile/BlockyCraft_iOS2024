using UnityEngine;
using VoxelPlay;

namespace VoxelPlayDemos
{
    public class GrenadeExploder : MonoBehaviour
    {

        public int damageRadius = 5;
        public int damage = 100;
        public AudioClip explosionSound;

        bool hasExploded;

        private void OnCollisionEnter (Collision collision)
        {
            if (hasExploded) return;
            hasExploded = true;

            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null) return;
            env.VoxelDamage (transform.position, damage, damageRadius, attenuateDamageWithDistance: true, addParticles: false, playSound: false, canAddRecoverableVoxel: false);
            if (explosionSound != null) {
                env.PlayDestructionSound (explosionSound, transform.position);
            }
            Gradient colors = new Gradient ();
            colors.colorKeys = new GradientColorKey [] { new GradientColorKey (new Color(1f, 1f, 0), 0), new GradientColorKey (new Color(1f, 0.5f, 0.5f), 1) };
            env.ParticleBurst (transform.position, ParticleBurstStyle.Explosion, 50, 10, null, colors);
            Destroy (gameObject);
        }

    }

}