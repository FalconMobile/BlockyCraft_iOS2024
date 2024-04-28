using UnityEngine;
using Mirror;
using VoxelPlay;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Main player script, contains most methods needed for inventory management
/// </summary>
public partial class NetworkPlayer : NetworkBehaviour
{
    [Header("Initial inventory")]
    public int defaultSlot = 1;
    public List<InventoryItem> initialItems;
    public List<InventoryItem> quickItems;

    Renderer currentItemRenderer;
    Dictionary<string, GameObject> weaponsGameObjects = new Dictionary<string, GameObject>();
    GameObject currentItemGameObject;
    ItemDefinition currentItem;
    ItemDefinition handVoxel;

    [SyncVar] ItemCategory currentItemCategory;
    [SyncVar] string currentItemName;

    private void OnItemSelectedChanged(int selectedItemIndex, int prevSelectedItemIndex)
    {
        // this event is called when user selects an item from the inventory
        ItemDefinition item = VPPlayer.GetSelectedItem().item;

        // show the item in the player hand
        SetHandItem(item);
    }


    void UnSelectItem()
    {
        // clears selection on the inventory
        VPPlayer.UnSelectItem();

        // hides hand item
        SetHandItem(null);
    }

    void SetHandItem(ItemDefinition item)
    {
        // Hide current weapons & arrow
        if (currentItemGameObject != null)
        {
            currentItemGameObject.SetActive(false);
        }
        arrowDirection.gameObject.SetActive(false);

        // If no weapon or no weapon object found in character hierarchy: hide all weapons and go back to normal
        if (item == null)
        {
            VPPlayer.hitRange = meleeRange;
            VPPlayer.hitDamage = meleeBluntDamage;
            VPPlayer.hitDelay = meleeHitDelay;
            thisAnimator.SetInteger(AnimationKeyword.Weapon, 0);
            thisAnimator.SetInteger(AnimationKeyword.Weapon, 0);
            spineAdjustment = new Vector3(0, -90, -90);

            currentItem = null;
            currentItemName = "";
            currentItemCategory = ItemCategory.General;
            currentWeaponType = WeaponType.None;

            return;
        }

        // Sets new weapon or item
        currentItem = item;
        currentItemName = item.name;
        currentItemCategory = item.category;
        currentWeaponType = Weapon.GetWeaponType(currentItem);

        if (currentWeaponType == WeaponType.Voxel)
        {
            VPPlayer.hitRange = 3;
            VPPlayer.hitDamage = meleeBluntDamage;
            VPPlayer.hitDelay = meleeHitDelay;
        }
        else
        {
            VPPlayer.hitRange = item.GetPropertyValue<float>("hitRange");
            VPPlayer.hitDamage = item.GetPropertyValue<int>("hitDamage");
            VPPlayer.hitDelay = item.GetPropertyValue<float>("hitDelay");
        }

        // Pick Animation layer based on weapon name, set range and damage accordingly
        thisAnimator.SetInteger(AnimationKeyword.Weapon, (int)Weapon.GetWeaponAnimationId(currentWeaponType));

        // Show the selected weapon
        ShowHandGameObject();

        // Set spine rotation adjustment so when aiming and rotation changes, the spine is correctly orientated
        switch (currentWeaponType)
        {
            case WeaponType.Bone:
                spineAdjustment = new Vector3(0, -90, -90);
                break;

            case WeaponType.Fork:
                spineAdjustment = new Vector3(0, -90, -90);
                break;

            case WeaponType.SwordCurved:
                spineAdjustment = new Vector3(0, -90, -90);
                break;

            case WeaponType.SwordStraight:
                spineAdjustment = new Vector3(0, -90, -90);
                break;

            case WeaponType.Pistol:
                spineAdjustment = new Vector3(0, -65, -70);
                break;

            case WeaponType.Musket:
            case WeaponType.MusketBlade:
                spineAdjustment = new Vector3(0, -45, -90);
                break;

            case WeaponType.Bow:
                spineAdjustment = new Vector3(5, -30, -90);
                break;

            default:
                spineAdjustment = new Vector3(0, -90, -90);
                break;
        }

        // Send this same message to the Server (who in turn will send it to clients)
        CmdSetHandItem(currentItem.category, currentItem.name);
    }

    void ShowHandGameObject()
    {
        if (currentWeaponType.IsBow())
        {
            currentItemGameObject = bowGameObject;
            bowGameObject.SetActive(true);
            arrowDirection.gameObject.SetActive(true);
            return;
        }

        if (!weaponsGameObjects.TryGetValue(currentItem.name, out currentItemGameObject))
        {
            currentItemGameObject = Instantiate(currentItem.category == ItemCategory.Voxel ? handVoxel.iconPrefab : currentItem.iconPrefab);
            currentItemGameObject.transform.SetParent(rightHand.transform, false);
            currentItemGameObject.transform.localPosition = Vector3.zero;
            currentItemGameObject.transform.localRotation = Quaternion.identity;
            weaponsGameObjects[currentItem.name] = currentItemGameObject;
        }
        else
        {
            currentItemGameObject.gameObject.SetActive(true);
        }

        weaponTip = currentItemGameObject.transform.Find("PistolTip");
        if (weaponTip == null)
        {
            weaponTip = currentItemGameObject.transform.Find("MusketTip");
            if (weaponTip == null)
            {
                weaponTip = currentItemGameObject.transform;
            }
        }

        currentItemRenderer = currentItemGameObject.GetComponentInChildren<Renderer>();

        if (currentItem.category == ItemCategory.Voxel)
        {
            UpdateHandVoxelTextures(currentItem.name);
        }
    }

    /// <summary>
    /// Called on Start or when Player changes a weapon to display the correct weapon
    /// </summary>
    void ShowCorrentHandItem(ItemCategory itemCategory, string itemObjName)
    {
        if (currentItemGameObject != null)
        {
            currentItemGameObject.SetActive(false);
        }
        arrowDirection.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(itemObjName))
        {
            ItemDefinition item = env.GetItemDefinition(itemCategory, itemObjName);
            currentItem = item;
            currentWeaponType = Weapon.GetWeaponType(item);
            currentItemName = currentItem.name;
            currentItemCategory = currentItem.category;
            ShowHandGameObject();
        }
    }

    /// <summary>
    /// Called on Server when player sets a weapon
    /// </summary>
    [Command]
    void CmdSetHandItem(ItemCategory itemCategory, string itemObjName)
    {
        // This run on the server: annotate new selected weapon;
        // As this run on the server, here we could check if this weapon is actually in the player inventory to avoid cheating
        currentItem = env.GetItemDefinition(itemCategory, itemObjName);
        if (currentItem == null)
        {
            Debug.LogError($"NetworkPlayer CmdSetHandItem itemCategory: {itemCategory} itemObjName: {itemObjName}");
        }
        currentWeaponType = Weapon.GetWeaponType(currentItem);
        currentItemName = currentItem.name;
        currentItemCategory = currentItem.category;
        lastUsedItemName = currentItemName;

        // Inform the instances of this player object on all clients of the change so their appearance is updated on all clients
        RpcSetHandItem(itemCategory, itemObjName);

        // Also update the dedicated server
        if (isServerOnly)
        {
            ShowCorrentHandItem(itemCategory, itemObjName);
        }
    }

    /// <summary>
    /// Called on clients when player sets a weapon (this message is sent to all client objects of same player)
    /// </summary>
#if MIRROR_32_1_2_OR_NEWER
    [ClientRpc(includeOwner = false)]
#endif
    void RpcSetHandItem(ItemCategory itemCategory, string itemObjName)
    {
        ShowCorrentHandItem(itemCategory, itemObjName);
    }

    /// <summary>
    /// Updates the texture of the hand voxel with the icon texture of the voxel definition
    /// </summary>
    void UpdateHandVoxelTextures(string voxelTypeName)
    {
        if (currentItemRenderer == null || string.IsNullOrEmpty(voxelTypeName)) return;

        VoxelDefinition voxelDefinition = env.GetVoxelDefinition(voxelTypeName);

        // we use ".material" instead of ".sharedMaterial" to ensure this change do not affect other player materials (this material will get instantiated only once)
        currentItemRenderer.material.mainTexture = voxelDefinition.GetIcon();
    }

    /// <summary>  
    /// Called when the player dies,
    /// goes through all items and tell the server to drop them
    /// In the end it removes them from the Inventory
    /// </summary>
    void DropInventory()
    {
        UnSelectItem();

        for (int i = 0; i < VPPlayer.items.Count; i++)
        {
            InventoryItem inventoryItem = VPPlayer.items[i];
            CmdDropItem(inventoryItem.item.category, inventoryItem.item.category == ItemCategory.Voxel ? inventoryItem.item.voxelType.name : inventoryItem.item.name, inventoryItem.quantity, i * (360 / VPPlayer.items.Count));
        }

        VPPlayer.ConsumeAllItems();
    }

    [Command]
    void CmdDropItem(ItemCategory itemCategory, string itemObjName, float quantity, int rotation)
    {
        ItemDefinition item;
        if (itemCategory == ItemCategory.Voxel)
        {
            VoxelDefinition voxelDefinition = env.GetVoxelDefinition(itemObjName);
            item = env.GetItemDefinition(ItemCategory.Voxel, voxelDefinition);
        }
        else
        {
            item = VoxelPlayEnvironment.GetItemDefinition(itemObjName);
        }
        if (item != null)
        {

            // Get a forward vector and multiply by 2 so we can spawn 2 meters away from player
            Vector3 positionToDrop = Vector3.forward * 2;

            // Rotate the vector around the Up axis, by a fraction of 360
            positionToDrop = Quaternion.AngleAxis(rotation, Vector3.up) * positionToDrop;

            // Create the object
            GameObject droppedItem = WorldState.Instance.DropItem(item, transform.position + positionToDrop, transform.rotation);
            if (droppedItem == null) return;

            // Set quantity, so that for example we don't spawn 30 arrows, but just 1 with quantity 30
            NetworkItem networkitem = droppedItem.GetComponent<NetworkItem>();
            networkitem.quantity = quantity;

            // Spawn Gameobject on Server
            NetworkServer.Spawn(droppedItem);
        }
    }


    /// <summary>
    /// Called by Item when close enough to pickup
    /// This should only happen on the server
    /// </summary>
    public void AddItem(int amount, ItemCategory category, string objName)
    {
        RpcAddItem(amount, category, objName);
    }


    [ClientRpc]
    void RpcAddItem(int amount, ItemCategory category, string objName)
    {
        if (VPPlayer == null) return;

        ItemDefinition item;
        // item can be a voxel or prefab
        if (category == ItemCategory.Voxel)
        {
            VoxelDefinition voxelDefinition = env.GetVoxelDefinition(objName);
            item = env.GetItemDefinition(ItemCategory.Voxel, voxelDefinition);
        }
        else
        {
            item = VoxelPlayEnvironment.GetItemDefinition(objName);
        }
        if (item == null) return;

        // play pickup sound
        if (thisAudioSource != null)
        {
            if (item.pickupSound != null)
            {
                thisAudioSource.PlayOneShot(item.pickupSound);
            }
            else if (VoxelPlayEnvironment.instance.defaultPickupSound != null)
            {
                thisAudioSource.PlayOneShot(VoxelPlayEnvironment.instance.defaultPickupSound);
            }
        }

        // add item to player inventory
        VPPlayer.AddInventoryItem(item, amount);

        // refresh inventory UI
        VoxelPlayUI ui = VoxelPlayUI.instance;
        string itemName = item.category == ItemCategory.Voxel ? item.title : item.name;
        ui.AddGetItemsMessage(amount, itemName, 2);
        if (ui != null)
        {
            ui.RefreshInventoryContents();
        }
    }


    public void ThrowCurrentItem()
    {
        InventoryItem inventoryItem = VPPlayer.ConsumeItem();
        if (inventoryItem == InventoryItem.Null)
            return;

        if (VPPlayer.GetSelectedItem() == InventoryItem.Null)
        {
            SetHandItem(null);
        }

        ItemDefinition item = inventoryItem.item;
        if (item == null) return;
        CmdItemThrow(transform.position + Vector3.up * 0.5f, Camera.main.transform.forward, item.category, item.category == ItemCategory.Voxel ? item.voxelType.name : item.name);
    }


    [Command]
    void CmdItemThrow(Vector3 position, Vector3 direction, ItemCategory category, string objName)
    {
        ItemDefinition item;
        GameObject itemObj;

        // An item can be a voxel or a prefab
        if (category == ItemCategory.Voxel)
        {
            VoxelDefinition voxelDefinition = env.GetVoxelDefinition(objName);
            item = env.GetItemDefinition(category, voxelDefinition);
            itemObj = WorldState.Instance.DropItem(item, position, Quaternion.identity);
        }
        else
        {
            item = VoxelPlayEnvironment.GetItemDefinition(objName);
            if (item == null || item.prefab == null) return;
            itemObj = WorldState.Instance.DropItem(item, position, Quaternion.identity);
        }

        NetworkItem networkItem = itemObj.GetComponent<NetworkItem>();
        networkItem.initialVelocity = direction * 15;

        NetworkServer.Spawn(itemObj);
    }


}