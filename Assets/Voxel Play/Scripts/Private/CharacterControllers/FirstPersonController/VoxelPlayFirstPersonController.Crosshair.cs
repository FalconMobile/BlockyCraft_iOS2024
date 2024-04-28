using UnityEngine;

namespace VoxelPlay
{
    public partial class VoxelPlayFirstPersonController : VoxelPlayCharacterControllerBase
    {
        Transform crosshair;
        const string CROSSHAIR_NAME = "Crosshair";
        Material crosshairMat;
        bool forceUpdateCrosshair;

        void InitCrosshair()
        {
            if (env.crosshairPrefab == null)
            {
                Debug.LogError("Crosshair prefab not assigned to this world.");
                return;
            }

            GameObject obj = Instantiate<GameObject>(env.crosshairPrefab);
            obj.name = CROSSHAIR_NAME;
            crosshair = obj.transform;
            crosshair.SetParent(m_Camera.transform, false);
            if (autoInvertColors && !env.isMobilePlatform)
            {
                crosshairMat = Resources.Load<Material>("VoxelPlay/Materials/VP Crosshair");
            }
            else
            {
                crosshairMat = Resources.Load<Material>("VoxelPlay/Materials/VP Crosshair No GrabPass");
            }

            crosshairMat = Instantiate(crosshairMat);
            crosshairMat.hideFlags = HideFlags.DontSave;
            obj.GetComponent<Renderer>().sharedMaterial = crosshairMat;
            if (env.crosshairTexture != null)
            {
                crosshairMat.mainTexture = env.crosshairTexture;
            }

            ResetCrosshairPosition();
            if (!enableCrosshair)
            {
                crosshair.gameObject.SetActive(false);
            }

            // ensure crosshair gets updated when a chunk changes on screen (including custom voxels which are created when rendering the chunk)
            env.OnChunkRender += (VoxelChunk chunk) => { forceUpdateCrosshair = true; };
        }

        public void ResetCrosshairPosition()
        {
            UpdateCrosshairScreenPosition();
            crosshair.localRotation = Misc.quaternionZero;
            crosshair.localScale = Misc.vector3one * crosshairScale;
            crosshairMat.color = crosshairNormalColor;
        }

        void UpdateCrosshairScreenPosition()
        {
            if (freeMode)
            {
                Vector3 scrPos = input.screenPos;
                scrPos.x = Mathf.Clamp(scrPos.x, 0, Screen.width);
                scrPos.y = Mathf.Clamp(scrPos.y, 0, Screen.height);
                scrPos.z = 1f;
                Vector3 newPosition = m_Camera.ScreenToWorldPoint(scrPos);
                if (switchingLapsed < 1f)
                {
                    crosshair.position = Vector3.Lerp(crosshair.position, newPosition, switchingLapsed);
                }
                else
                {
                    crosshair.position = newPosition;
                }
            }
            else
            {
                if (switchingLapsed < 1f)
                {
                    crosshair.localPosition =
                        Vector3.Lerp(crosshair.localPosition, Misc.vector3forward, switchingLapsed);
                }
                else
                {
                    crosshair.localPosition = Misc.vector3forward;
                }
            }
        }


        void LateUpdate()
        {
            if (env == null || !env.applicationIsPlaying)
                return;

            if (freeMode || switching || VoxelPlayPlayer.instance.isSelectedItem)
            {
                UpdateCrosshairScreenPosition();
                forceUpdateCrosshair = true;
                VoxelPlayPlayer.instance.isSelectedItem = false;
            }

            if (env.cameraHasMoved || forceUpdateCrosshair)
            {
                forceUpdateCrosshair = false;

                Ray ray = GetCameraRay();

                // Check if there's a voxel in range
                float hitRange = player.GetHitRange();
                if (env.buildMode) hitRange = Mathf.Max(crosshairMaxDistance, hitRange);
                crosshairOnBlock =
                    env.RayCast(ray, out _crosshairHitInfo, hitRange, colliderTypes: ColliderTypes.IgnorePlayer,
                        layerMask: crosshairHitLayerMask) && _crosshairHitInfo.voxelIndex >= 0;
                if (changeOnBlock)
                {
                    if (crosshairOnBlock)
                    {
                        // Puts crosshair over the voxel but do it only if crosshair won't disappear because of the angle or it's switching from orbit to free mode (or viceversa)
                        float d = -1;
                        if (_crosshairHitInfo.sqrDistance > 6f)
                        {
                            d = Vector3.Dot(ray.direction, _crosshairHitInfo.normal);
                        }

                        if (d < -0.2f && switchingLapsed >= 1f)
                        {
                            Vector3 crosshairPosition = _crosshairHitInfo.point;
                            crosshair.position = crosshairPosition;
                            crosshair.LookAt(crosshairPosition + _crosshairHitInfo.normal);
                        }
                        else
                        {
                            crosshair.localRotation = Misc.quaternionZero;
                        }

                        crosshairMat.color = crosshairOnTargetColor;
                    }
                    else
                    {
                        ResetCrosshairPosition();
                    }
                }
            }

            if (crosshairOnBlock)
            {
                crosshair.localScale = Misc.vector3one * (crosshairScale * (1f - targetAnimationScale * 0.5f + Mathf.PingPong(Time.time * targetAnimationSpeed, targetAnimationScale)));
                if (voxelHighlight)
                {
                    env.VoxelHighlight(_crosshairHitInfo, voxelHighlightColor, voxelHighlightEdge);
                }
            }
            else
            {
                env.VoxelHighlight(false);
            }

            if (voxelHighlight)
            {
                env.VoxelHighlight(_crosshairHitInfo, voxelHighlightColor, voxelHighlightEdge);
            }
        }
    }
}