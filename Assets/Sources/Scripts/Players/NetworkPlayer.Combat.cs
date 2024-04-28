using UnityEngine;
using Mirror;
using System.Collections;
using VoxelPlay;


/// <summary>
/// Main player script, contains most methods needed for player movement and interaction
/// </summary>

public partial class NetworkPlayer : NetworkBehaviour
{

    // Default bare hands damage
    [Header("Combat")]
    public float meleeRange = 3f;
    public int meleeBluntDamage = 20;
    public float meleeHitDelay = 0.5f;
    public GameObject bowGameObject;

    WeaponType currentWeaponType;
    float lastAttackTime;

    // Fire anchors
    Transform weaponTip;
    Transform arrowDirection;

    [SyncVar] string lastUsedItemName;
    ItemDefinition lastUsedItem;

    /// <summary>
    /// Performs a primary attack (this is called when pressing the left mouse button)
    /// </summary>
    void ExecutePrimaryAttack()
    {
        // Check weapon attack delay
        float now = Time.time;
        if (now - lastAttackTime < VPPlayer.GetHitDelay()) return;
        lastAttackTime = now;
        lastUsedItem = currentItem;

        // If we are using a bow, spawn an arrow on Server, else do Raycast on Server
        switch (currentWeaponType)
        {
            case WeaponType.Bow:
                if (!VPFPP.isRunning)
                {
                    if (VPPlayer.GetItemQuantity(lastUsedItem) > 0 && !thisAnimator.GetCurrentAnimatorStateInfo(4).IsName("Bow_Attack_Shoot"))
                    {
                        // Trigger Animation networked
                        thisNetworkAnimator.SetTrigger(AnimationKeyword.Attack);

                        CmdSpawnArrow(arrowDirTransform.position, arrowDirTransform.rotation, currentItem.name);
                        VPPlayer.ConsumeItem();

                        if (VPPlayer.GetItemQuantity(lastUsedItem) <= 0)
                        {
                            UnSelectItem();
                        }
                    }
                }
                break;

            case WeaponType.Bomb:
                // Trigger Animation networked
                thisNetworkAnimator.SetTrigger(AnimationKeyword.Attack);
                // Wait a bit so the animation has started before throwing the bomb (we should replace this with a proper animation event)
                Invoke(nameof(DelayedThrowBomb), 0.3f);
                break;

            case WeaponType.Voxel:
                Build();
                break;

            default:
                // is it a potion?
                if (currentItem != null)
                {
                    int healthPoints = currentItem.GetPropertyValue<int>("healthPoints");
                    if (healthPoints > 0)
                    {
                        UsePotion(currentItem.name);
                        if (currentItem.useSound != null)
                        {
                            thisAudioSource.PlayOneShot(currentItem.useSound);
                        }

                        VPPlayer.ConsumeItem();
                        if (VPPlayer.GetItemQuantity(lastUsedItem) <= 0)
                        {
                            UnSelectItem();
                        }
                        lastAttackTime += 2; // prevent using several potions quickly
                        return;
                    }
                }

                // Otherwise, attack with weapon or bare hands
                // Trigger Animation networked
                thisNetworkAnimator.SetTrigger(AnimationKeyword.Attack);
                // Trigger Raycast on Server
                ExecuteEntityRaycast(false);
                break;
        }
    }

    private void ExecuteEntityRaycast(bool isStraightDestroy=true)
    {
        float hitRange = isStraightDestroy ? meleeRange : VPPlayer.hitRange;
        int hitDamage = isStraightDestroy ? meleeBluntDamage : VPPlayer.hitDamage;
        
        CmdRaycastWeapon(cam.position, cam.forward, hitRange, hitDamage, isStraightDestroy);
    }

    void DelayedThrowBomb()
    {
        Vector3 targetPos = cam.transform.position + cam.transform.forward * 1000;
        Vector3 direction = (targetPos - rightHand.position).normalized;
        CmdThrowBomb(rightHand.position + direction * 0.5f, direction, currentItem.category, currentItem.name);
        VPPlayer.ConsumeItem();

        if (VPPlayer.GetItemQuantity(lastUsedItem) <= 0)
        {
            UnSelectItem();
        }
    }

    /// <summary>
    /// Called when player presses the attack button locally
    /// This runs on the Server, but direction and damage amount are sent from Client
    /// </summary>
    [Command]
    void CmdRaycastWeapon(Vector3 rayOrigin, Vector3 rayDirection, float rayLength, int weaponDamage, bool isStraightDestroy)
    {
        lastUsedItemName = currentItemName; // update syncvar, this can only be done in server
        bool isRangedWeapon = currentWeaponType.IsPistol() || currentWeaponType.IsMusket();

        // If this is the pistol or musket, spawn particles at the tip of the weapon
        if (isRangedWeapon && !isStraightDestroy)
        {
            Vector3 muzzlePosition = weaponTip != null ? weaponTip.position : Vector3.zero;
            Quaternion muzzleRotation = weaponTip != null? weaponTip.rotation: Quaternion.identity;
            GameObject muzzleFlash = Instantiate(MuzzleFlash, muzzlePosition, muzzleRotation);
            NetworkServer.Spawn(muzzleFlash);
            StartCoroutine(DestroyWithDelay(muzzleFlash, 4));
            WorldState.Instance.NotifyShoot(rayOrigin, this);
        }

        // To avoid hitting self
        rayOrigin += rayDirection * 0.5f;

        // Does the ray intersect any objects in the player layer (ignore pickups)
        if (env.RayCast(rayOrigin, rayDirection, out VoxelHitInfo hit, rayLength, isRangedWeapon ? 5 : 0, ColliderTypes.IgnorePlayer))
        {
            if (hit.voxelIndex >= 0)
            {
                // We hit a voxel
                // If destroyed, we spawn a recoverable voxel on the server while we keep the damage and particle effects on clients
                int resistancePoints = env.GetVoxelResistancePoints(hit.chunk, hit.voxelIndex);
                if (resistancePoints <= weaponDamage)
                {
                    // spawn a recoverable voxel on the server
                    VoxelDefinition voxelDefinition = hit.voxel.type;
                    if (voxelDefinition.canBeCollected && voxelDefinition.renderType != RenderType.CutoutCross) // ignore vegetation
                    {
                        GameObject dropItem = WorldState.Instance.DropVoxel(voxelDefinition, hit.voxelCenter);
                        NetworkServer.Spawn(dropItem);
                    }
                }
                // damage it from the clients so it shows particles, etc
                RpcDamageVoxel(hit.chunk.position, hit.voxelIndex, hit.voxelCenter, hit.normal, hit.point, weaponDamage);

                // damage on server as well
                if (isServerOnly)
                {
                    env.captureEvents = false;
                    DamageVoxel(hit.chunk.position, hit.voxelIndex, hit.voxelCenter, hit.normal, hit.point, weaponDamage, isClient: false);
                    env.captureEvents = true;
                }
            }
            else
            {
                NetworkDamageTaker tempDamageTaker = hit.collider.GetComponent<NetworkDamageTaker>();
                if (tempDamageTaker != null)
                {
                    if (this == (Object)tempDamageTaker.entity)
                    {
                        // avoid hitting self
                        return;
                    }

                    // On hit we send damage and we also take care of score through WorldState
                    WorldState.Instance.CharacterGetDamage(tempDamageTaker, this, weaponDamage);

                    // Example of spawning particles - you need to add the prefab here on 'NetworkPlayer' and in the Network manager
                    GameObject bloodparticles = Instantiate(BloodParticles, hit.point, Quaternion.FromToRotation(Vector3.forward, hit.normal));
                    NetworkServer.Spawn(bloodparticles);
                }

            }
        }
    }

    IEnumerator DestroyWithDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkServer.Destroy(obj);
    }


    [ClientRpc]
    void RpcDamageVoxel(Vector3 chunkPosition, int voxelIndex, Vector3 voxelCenter, Vector3 normal, Vector3 point, int damage)
    {
        DamageVoxel(chunkPosition, voxelIndex, voxelCenter, normal, point, damage, isClient: true);
    }

    void DamageVoxel(Vector3 chunkPosition, int voxelIndex, Vector3 voxelCenter, Vector3 normal, Vector3 point, int damage, bool isClient)
    {

        VoxelChunk chunk = env.GetChunk(chunkPosition);
        if (chunk == null) return;

        Voxel voxel = chunk.voxels[voxelIndex];
        if (voxel.isEmpty || voxel.type == null) return;

        // do not spawn drop voxel on client as this is done on the server
        bool canBeCollected = voxel.type.canBeCollected;
        voxel.type.canBeCollected = false;

        // prepare a hitInfo struct to inform of all impact details to the API
        env.BuildVoxelHitInfo(out VoxelHitInfo hitInfo, chunk, voxelIndex, voxelCenter, point, normal);

        // damage the voxel
        env.VoxelDamage(hitInfo, damage, addParticles: isClient, playSound: isClient);

        // restore the collectable attribute
        voxel.type.canBeCollected = canBeCollected;
    }

    /// <summary>
    /// Called when player presses attack and has the Arrow (Bow) selected
    /// Spawned on the Server the arrows are Server Authorative
    /// </summary>
    /// <param name="pos">Where to Spawn</param>
    /// <param name="rot">Direction to look at</param>
    [Command]
    void CmdSpawnArrow(Vector3 pos, Quaternion rot, string itemName)
    {

        lastUsedItemName = itemName; // update syncvar, this can only be done in server

        GameObject instancedArrow = Instantiate(Arrow, pos, rot);

        // We need to keep track of who Spawned the arrow in order to keep score
        instancedArrow.GetComponent<NetworkArrow>().owner = this;
        NetworkServer.Spawn(instancedArrow);
    }


    public void Disengage(ILivingEntity target)
    {
        // Players decide if disengage and when; this method does nothing
    }



    [Command]
    void CmdThrowBomb(Vector3 position, Vector3 direction, ItemCategory category, string objName)
    {
        GameObject itemObj;

        ItemDefinition item = env.GetItemDefinition(category, objName);
        if (item == null || item.prefab == null) return;
        itemObj = WorldState.Instance.DropItem(item, position, Quaternion.identity);

        lastUsedItemName = item.name; // update syncvar, this can only be done in server

        NetworkItem networkItem = itemObj.GetComponent<NetworkItem>();
        networkItem.initialVelocity = direction * 15f;

        BombExploder bombExploder = itemObj.GetComponent<BombExploder>();
        if (bombExploder != null)
        {
            bombExploder.isArmed = true;
            bombExploder.owner = this;
            bombExploder.damage = item.GetPropertyValue<int>("hitDamage", 100);
            bombExploder.damageRadius = item.GetPropertyValue<int>("hitDamageRadius", 5);
            bombExploder.countdown = item.GetPropertyValue<float>("explosionDelay", 4);
        }

        NetworkServer.Spawn(itemObj);

        RpcPlayItemSound(category, objName);
    }

    [ClientRpc]
    void RpcPlayItemSound(ItemCategory category, string objName)
    {
        ItemDefinition item = env.GetItemDefinition(category, objName);
        if (item != null && item.useSound != null)
        {
            thisAudioSource.PlayOneShot(item.useSound);
        }

    }



    #region Events from animations

    // Event triggered by Attack Animations, you can change this in the Import Settings of the Navy Captain character
    // Under Animation -> Events
    // Triggered when Player Attack begins
    public void AttackStartEvent()
    {
        // Hide display arrow            
        arrowDirection.gameObject.SetActive(false);

        if (!isServerOnly)
        {
            // Play appropriate sound effect
            ItemDefinition item = VoxelPlayEnvironment.GetItemDefinition(lastUsedItemName);
            if (item != null && item.useSound != null)
            {
                thisAudioSource.PlayOneShot(item.useSound);
            }
        }
    }

    // Triggered when Player Attack makes contact
    public void AttackHitEvent()
    {
    }

    // Triggered when Player Attack2 begins
    public void Attack2StartEvent()
    {
        // Hide display arrow            
        arrowDirection.gameObject.SetActive(false);

        if (!isServerOnly)
        {
            // Play appropriate sound effect
            ItemDefinition item = VoxelPlayEnvironment.GetItemDefinition(lastUsedItemName);
            if (item != null && item.useSound != null)
            {
                thisAudioSource.PlayOneShot(item.useSound);
            }
        }
    }

    // Triggered when Player Attack2 makes contact
    public void Attack2HitEvent()
    {
    }

    // Triggered when arrow is grabbed from behind the back
    public void LoadArrow()
    {
        // Show display arrow
        arrowDirection.gameObject.SetActive(true);
        thisAudioSource.PlayOneShot(BowReload);
    }

    // Triggered when Player gets Hit (receives a hit)
    public void HitEvent()
    {
        if (!isServerOnly)
        {
            thisAudioSource.PlayOneShot(hitSoundEffect);
        }
    }

    // Triggered when Player Death animation starts
    public void DeathEvent()
    {
        if (!isServerOnly)
        {
            thisAudioSource.PlayOneShot(deathSoundEffect);
        }
    }

    #endregion // animation events

}