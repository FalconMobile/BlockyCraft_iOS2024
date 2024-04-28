using UnityEngine;
using System.Collections.Generic;
using Mirror;
using VoxelPlay;

public partial class WorldState : NetworkBehaviour
{
    /// <summary>
    /// For voxel types, we ensure there's a "cube" prefab with correct textures attached to it
    /// </summary>
    /// <param name="item"></param>
    public GameObject DropItem(ItemDefinition item, Vector3 position, Quaternion rotation)
    {
        if (item == null)
        {
            return null;
        }
        if (item.category == ItemCategory.Voxel && item.voxelType != null && item.voxelType.dropItem != null)
        {
            item = item.voxelType.dropItem;
        }

        GameObject go;
        if (item.category == ItemCategory.Voxel)
        {
            go = Instantiate(VoxelPrefab);
        }
        else
        {
            if (item.prefab == null)
            {
                return null;
            }
            go = Instantiate(item.prefab);
        }

        go.transform.position = position;
        go.transform.rotation = rotation;

        NetworkItem networkItem = go.GetComponent<NetworkItem>();
        if (networkItem != null)
        {
            networkItem.itemDefinition = item;

            // Also set voxel type (if it's a voxel), so it gets proper textures on clients
            if (item.category == ItemCategory.Voxel)
            {
                networkItem.voxelTypeName = item.name;
            }
        }
        return go;
    }

    /// <summary>
    /// Creates a cube gameobject representing a collectable voxel
    /// </summary>
    /// <param name="voxelDefinition"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public GameObject DropVoxel(VoxelDefinition voxelDefinition, Vector3 position)
    {
        if (voxelDefinition.dropItem != null)
        {
            return DropItem(voxelDefinition.dropItem, position, Quaternion.identity);
        }
        GameObject dropVoxel = Instantiate(VoxelPrefab);
        dropVoxel.transform.position = position;
        NetworkItem networkItem = dropVoxel.GetComponent<NetworkItem>();
        networkItem.voxelTypeName = voxelDefinition.name;
        return dropVoxel;
    }
}