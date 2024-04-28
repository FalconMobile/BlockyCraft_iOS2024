using System;
using System.Collections;
using UnityEngine;
using Mirror;
using VoxelPlay;

/// <summary>
/// Network version of VoxelPlay Item, changed mainly because multiple players
/// exist in the world and because objects need to be spawned on the server
/// </summary>

public class NetworkItem : NetworkBehaviour
{
    /// <summary>
    /// The player we are going to give this item to, set below OnTriggerEnter
    /// </summary>
    NetworkPlayer currentNetworkPlayer;

    // <summary>
    /// The item represented by this object.
    /// </summary>
    public ItemDefinition itemDefinition;

    /// <summary>
    /// The item name represented by this object (ie. voxel type name or other item name)
    /// </summary>
    [SyncVar] public string voxelTypeName;

    [SyncVar] public float quantity = 1f;

    [NonSerialized]
    public Vector3 initialVelocity;

    const float PICK_UP_END_DISTANCE_SQR = 0.1f;

    SphereCollider itemPickerTriggerCollider;

    Rigidbody rb;


    void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;

        // assign proper item definition
        if (itemDefinition == null && !string.IsNullOrEmpty(voxelTypeName))
        {
            VoxelDefinition voxelDefinition = env.GetVoxelDefinition(voxelTypeName);
            itemDefinition = env.GetItemDefinition(ItemCategory.Voxel, voxelDefinition);
        }

        // assign textures to this cube based on the voxel type
        if (!string.IsNullOrEmpty(voxelTypeName) && itemDefinition.category == ItemCategory.Voxel)
        {
            VoxelDefinition voxelDefinition = env.GetVoxelDefinition(voxelTypeName);
            GetComponentInChildren<Renderer>().material.mainTexture = voxelDefinition.GetIcon();
        }

        if (isServer)
        {
            rb.velocity = initialVelocity;
            SphereCollider[] colliders = GetComponentsInChildren<SphereCollider>();
            for (int k = 0; k < colliders.Length; k++)
            {
                if (colliders[k].isTrigger)
                {
                    itemPickerTriggerCollider = colliders[k];
                    Invoke(nameof(ActivatePickupTrigger), 0.75f);
                }
            }
        }
        else
        {
            if (rb != null)
            {
                Destroy(rb);
            }
            enabled = false;
        }
    }

    public override void OnStartClient()
    {
        // If this item has this name, it means this arrow is instantiated on the server after a collision of a fired arrow, so we play the hit sound here on the client
        if ("ImpactArrow".Equals(name))
        {
            GetComponent<AudioSource>().Play();
        }
    }

    void ActivatePickupTrigger()
    {
        // allow the item to be picked up by players
        itemPickerTriggerCollider.enabled = true;
    }


    void Update()
    {
        if (this == null) return;

        if (transform.position.y < -1000)
        {
            DestroySelf();
            return;
        }
        if (itemDefinition == null)
        {
            // safety check in case the object drops to infinite
            Debug.Log("Item not set on NetworkItem.");
            DestroySelf();
            return;
        }

        if (currentNetworkPlayer == null) return;

        // Check if player is near, alteration here from original to pass through currentPlayer and not THE player
        Vector3 playerPosition = currentNetworkPlayer.transform.position;
        Vector3 pos = transform.position;

        float dx = playerPosition.x - pos.x;
        float dy = playerPosition.y + 1f - pos.y;
        float dz = playerPosition.z - pos.z;

        float grabSpeed = Time.deltaTime * 10f;
        pos.x += dx * grabSpeed;
        pos.y += dy * grabSpeed;
        pos.z += dz * grabSpeed;
        transform.position = pos;

        float dist = dx * dx + dy * dy + dz * dz;
        if (dist < PICK_UP_END_DISTANCE_SQR)
        {
            // Adds object to found player's inventory
            currentNetworkPlayer.AddItem((int)quantity, itemDefinition.category, itemDefinition.category == ItemCategory.Voxel ? itemDefinition.voxelType.name : itemDefinition.name);

            // We need to destroy, because we don't want other players to be able to pick up the same object
            DestroySelf();
        }
    }

    /// <summary>
    /// Called when a player enters the trigger sphere of the pickup
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        NetworkPlayer networkPlayer = other.transform.root.GetComponentInChildren<NetworkPlayer>();
        if (networkPlayer != null && networkPlayer.isAlive)
        {
            currentNetworkPlayer = networkPlayer;

            // Also disable trigger as this item now will be added to this player inventory
            if (itemPickerTriggerCollider != null)
            {
                itemPickerTriggerCollider.enabled = false;
            }

            // Avoid collisions issues when grabbing the item
            if (rb != null)
            {
                Destroy(rb);
            }
        }
    }

    /// <summary>
    /// Called when item has been added to player's inventory,
    /// this happens on the Server and destroys the object for all clients
    /// so that you can't pick it up multiple times
    /// </summary>
    [Server]
    void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}