using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Network Arrow is the version of the arrow that is spawned open firing the bow
/// It is Server authorative and it's position is observed by the clients
/// </summary>
public class NetworkArrow : NetworkBehaviour
{

    // Speed of the arrow
    public float speed = 2.5f;

    // Damage points
    public int damage = 20;

    Collider thisCollider;
    Rigidbody thisRigidbody;
    public ILivingEntity owner;

    // Make sure we can only hit something once
    bool spent;


    // This is the ArrowPickup that will be spawned upon collision with anything but a player
    public GameObject ArrowPickup;

    // Blood particles reference
    public GameObject BloodParticles;

    static readonly List<Collider> colliders = new List<Collider>();

    private void Start()
    {
        thisCollider = GetComponent<Collider>();

        if (!isClientOnly)
        {
            thisCollider.enabled = true;
            owner.GetTransform().GetComponentsInChildren(true, colliders);
            for (int k = 0; k < colliders.Count; k++)
            {
                Physics.IgnoreCollision(thisCollider, colliders[k]);
            }

            thisRigidbody = gameObject.AddComponent<Rigidbody>();
            thisRigidbody.mass = 0.1f;
            thisRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            thisRigidbody.AddForce(transform.forward * speed, ForceMode.Impulse);
            thisRigidbody.AddTorque(transform.right * (-0.1f * speed));

            InvokeRepeating(nameof(DestroyRunaways), 5, 5);
        }
    }

    /// <summary>
    /// Called upon collision which should happen only on Server as Clients have collider disabled
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {

        // Because multiple collisions can be registered in one frame, we only want the first one to 
        // affect our game, so if the arrow hasn't been 'spent' - spend it

        if (spent) return;

        // If we hit a character
        NetworkDamageTaker characterHit = null;
        if (collision.collider != null)
        {
            characterHit = collision.collider.GetComponentInChildren<NetworkDamageTaker>();
        }
        if (characterHit != null)
        {
            // Damage impacted character
            WorldState.Instance.CharacterGetDamage(characterHit, owner, damage);

            // Spawn some blood particles
            GameObject bloodParticles = Instantiate(BloodParticles, collision.contacts[0].point, Quaternion.Euler(collision.contacts[0].normal));
            NetworkServer.Spawn(bloodParticles);
        }
        else
        {
            // If we hit anything else spawn a pickup using Mirror
            // Spawn an arrow pickup from the prefab set in editor
            GameObject arrowPickup = Instantiate(ArrowPickup, transform.position, transform.rotation);
            arrowPickup.name = "ImpactArrow";
            NetworkServer.Spawn(arrowPickup);
        }

        spent = true;

        // Destroy the arrow in any case, this all happens on the server so it will happen on the clients
        NetworkServer.Destroy(gameObject);
    }

    void DestroyRunaways()
    {
        if (transform.position.y < -1000)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}