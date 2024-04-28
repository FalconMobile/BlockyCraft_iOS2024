using UnityEngine;
using VoxelPlay;
using Mirror;

public class BombExploder : NetworkBehaviour
{
    public GameObject sparks;
    public int damageRadius = 5;
    public int damage = 100;
    public AudioClip explosionSound;
    public bool isArmed;
    public float countdown = 5;
    public ILivingEntity owner;

    bool hasExploded;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (isArmed)
        {
            Invoke(nameof(Explode), countdown);
        }
        else
        {
            sparks.SetActive(false);
        }
    }

    [Server]
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
        Vector3 explosionPosition = transform.position;
        if (env != null)
        {
            env.VoxelDamage(explosionPosition, damage, damageRadius, attenuateDamageWithDistance: false, addParticles: false, playSound: false, canAddRecoverableVoxel: false);
            RpcExplode(explosionPosition);
        }
        // damage nearby mobs or players
        WorldState.Instance.AreaDamage(explosionPosition, damageRadius, damage, owner);

        // hide the bomb for 1 second and then destroy it from the server; this gives time to send the explosion message to clients
        GetComponentInChildren<Renderer>().enabled = false;
        Invoke(nameof(DestroyWithDelay), 1);

        if (gameObject != null)
        {
            Destroy(gameObject);   
        }
    }

    void DestroyWithDelay()
    {
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    void RpcExplode(Vector3 position)
    {
        VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
        if (env == null) return;

        if (explosionSound != null)
        {
            env.PlayDestructionSound(explosionSound, transform.position);
        }
        Gradient colors = new Gradient();
        colors.colorKeys = new GradientColorKey[] { new GradientColorKey(new Color(1f, 1f, 0), 0), new GradientColorKey(new Color(1f, 0.5f, 0.5f), 1) };
        env.ParticleBurst(position, ParticleBurstStyle.Explosion, 50, 10, null, colors);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // check if bomb collides with a mob, then make it explode automatically (more fun)
        if (isServer)
        {
            if (collision.collider.transform.root.GetComponentInChildren<NetworkDamageTaker>() != null)
            {
                CancelInvoke();
                Explode();
            }

        }
    }

}