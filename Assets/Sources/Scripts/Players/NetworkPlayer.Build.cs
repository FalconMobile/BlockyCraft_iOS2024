using UnityEngine;
using Mirror;
using VoxelPlay;


/// <summary>
/// Main player script, contains most methods needed for voxel placing
/// </summary>
public partial class NetworkPlayer : NetworkBehaviour
{

    [Header("Building")]
    public float buildMaxDistance = 5;

    /// <summary>
    /// Implements building stuff
    /// </summary>
    protected virtual void Build()
    {
        InventoryItem inventoryItem = VPPlayer.GetSelectedItem();
        ItemDefinition currentItem = inventoryItem.item;
        if (currentItem == null) return;

        Vector3 camPos = cam.position;
        Vector3 forward = cam.forward;

        switch (currentItem.category)
        {
            case ItemCategory.Voxel:

                // Basic placement rules
                bool canPlace = VPFPP.crosshairOnBlock;
                Voxel existingVoxel = VPFPP.crosshairHitInfo.voxel;
                VoxelDefinition existingVoxelType = existingVoxel.type;
                Vector3 placePos;

                if (currentItem.voxelType.renderType == RenderType.Water && !canPlace)
                {
                    canPlace = true; // water can be poured anywhere
                    placePos = camPos + forward * 3f;
                }
                else
                {
                    placePos = env.GetVoxelPosition(VPFPP.crosshairHitInfo.voxelCenter);
                    if (existingVoxelType.opaque > 5)
                    {
                        placePos += VPFPP.crosshairHitInfo.normal;
                    }
                    if (canPlace && VPFPP.crosshairHitInfo.normal.y > 0.9f)
                    {
                        // Make sure there's a valid voxel under position (ie. do not build a voxel on top of grass)
                        canPlace = (existingVoxelType != null && existingVoxelType.renderType != RenderType.CutoutCross && (existingVoxelType.renderType != RenderType.Water || currentItem.voxelType.renderType == RenderType.Water));
                    }
                }

                VoxelDefinition placeVoxelType = currentItem.voxelType;

                bool isPromoting = false;
                // Check voxel promotion
                if (canPlace && existingVoxelType == placeVoxelType && existingVoxelType.promotesTo != null)
                {
                    placePos = env.GetVoxelPosition(VPFPP.crosshairHitInfo.voxelCenter);
                    placeVoxelType = existingVoxelType.promotesTo;
                    isPromoting = true;
                }

                // Compute rotation
                int textureRotation = 0;
                if (placeVoxelType.placeFacingPlayer && placeVoxelType.renderType.supportsTextureRotation())
                {
                    // Orient voxel to player
                    if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
                    {
                        if (forward.x > 0)
                        {
                            textureRotation = 1;
                        }
                        else
                        {
                            textureRotation = 3;
                        }
                    }
                    else if (forward.z < 0)
                    {
                        textureRotation = 2;
                    }
                }

                // Final check, does it overlap existing geometry?
                if (canPlace && !isPromoting)
                {
                    Quaternion rotationQ = Quaternion.Euler(0, Voxel.GetTextureRotationDegrees(textureRotation), 0);
                    canPlace = !env.VoxelOverlaps(placePos, placeVoxelType, rotationQ, 1 << env.layerVoxels);
                    if (!canPlace)
                    {
                        VPFPP.PlayCancelSound();
                    }
                }
                // Finally place the voxel
                if (canPlace)
                {
                    // Consume item first
                    if (!env.buildMode)
                    {
                        VPPlayer.ConsumeItem();
                    }
                    // Place it
                    float amount = inventoryItem.quantity < 1f ? inventoryItem.quantity : 1f;

                    CmdPlaceVoxel(placePos, placeVoxelType.name, amount, textureRotation);
                    env.PlayBuildSound(placeVoxelType.buildSound, placePos);

                    // Moves back character controller if voxel is put just on its position
                    const float minDist = 0.5f;
                    float distSqr = Vector3.SqrMagnitude(camPos - placePos);
                    if (distSqr < minDist * minDist)
                    {
                        VPFPP.MoveTo(transform.position + VPFPP.crosshairHitInfo.normal);
                    }
                }
                break;
            case ItemCategory.Torch:
                // TODO: attach torches on server
                //if (VPFPP.crosshairOnBlock)
                //{
                //	GameObject torchAttached = env.TorchAttach(VPFPP.crosshairHitInfo, currentItem);
                //	if (!env.buildMode && torchAttached != null)
                //	{
                //		VPPlayer.ConsumeItem();
                //	}
                //}
                break;

            case ItemCategory.General:
                ThrowCurrentItem();
                break;
        }
    }

    [Command]
    void CmdPlaceVoxel(Vector3 placePos, string voxelTypeName, float amount, int textureRotation)
    {
        VoxelDefinition voxelDefinition = env.GetVoxelDefinition(voxelTypeName);
        if (voxelDefinition != null)
        {
            env.VoxelPlace(placePos, voxelDefinition, false, Color.white, amount, textureRotation);
        }
    }

}