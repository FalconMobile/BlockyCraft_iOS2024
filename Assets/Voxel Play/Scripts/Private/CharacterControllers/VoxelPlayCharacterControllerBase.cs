using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoxelPlay
{

    public abstract partial class VoxelPlayCharacterControllerBase : MonoBehaviour, IVoxelPlayCharacterController
    {

        [HideInInspector]
        public bool useThirdPartyController;

        [Header ("Start Position")]
        [Tooltip ("Places player on a random position in world which is flat. If this option is not enabled, the current gameobject transform position will be used.")]
        public bool startOnFlat = true;

        [Tooltip ("Number of terrain checks to determine a flat position. The more iterations the lower the resulting starting position.")]
        [Range (1, 100)]
        public int startOnFlatIterations = 50;

        [Header ("State Flags (Informative)")]
        [Tooltip ("Player is flying - can go up/down with E and Q keys.")]
        public bool isFlying;

        [Tooltip ("Player is thrusting - can apply vertical thrust using X key.")]
        public bool isThrusting;

        [Tooltip ("Player is moving (walk or run)")]
        public bool isMoving;

        [Tooltip ("Player is pressing any move key")]
        public bool isPressingMoveKeys;

        [Tooltip ("Player is running")]
        public bool isRunning;

        [Tooltip ("Player is either on water surface or under water")]
        public bool isInWater;

        [Tooltip ("Player is on water surface.")]
        public bool isSwimming;

        [Tooltip ("Player is below water surface.")]
        public bool isUnderwater;

        [Tooltip ("Player is on ground.")]
        public bool isGrounded;

        [Tooltip ("Player is crouched.")]
        public bool isCrouched;

        [Tooltip ("Player is underground or undercover.")]
        public bool isUnderground;

        [Tooltip ("Voxel under character feet")]
        public VoxelDefinition voxelUnder;

        [Header ("Managed Actions By This Controller")]
        [Tooltip ("Allows this character controller to attack (defaults to left mouse button or tap")]
        public bool manageAttack = true;
        [Tooltip ("Allows this character controller to crouch (defaults to C key)")]
        public bool manageCrouch = true;
        [Tooltip ("Allows this character controller to jump (defaults to Space key)")]
        public bool manageJump = true;
        [Tooltip ("Allows this character controller to build (defaults to B key)")]
        public bool manageBuild = true;
        [Tooltip ("Allows this character controller to fly (defaults to F key)")]
        public bool manageFly = true;
        [Tooltip ("Allows this character controller to activate vertical propulsion (defaults to X key)")]
        public bool manageThrust;

        [Header ("Sounds")]
        [SerializeField] AudioSource m_AudioSource;

        // the sound played when character enters water.
        public AudioClip waterSplash;
        // an array of swim stroke sounds that will be randomly selected from.
        public AudioClip [] swimStrokeSounds;
        public float swimStrokeInterval = 8;

        // an array of footstep sounds that will be randomly selected from.
        public AudioClip [] footstepSounds;

        [Range (0f, 1f)] public float runstepLenghten = 0.7f;
        public float footStepInterval = 5;

        // the sound played when character leaves the ground.
        public AudioClip jumpSound;

        // the sound played when character touches back on ground.
        public AudioClip landSound;

        public AudioClip cancelSound;

        [Header ("World Limits")]
        public bool limitBoundsEnabled;
        public Bounds limitBounds;
        [Tooltip ("Detects collisions and moves player back or ontop a safe position")]
        public bool unstuck = true;

        [Header ("Crosshair")]
        [Tooltip ("Note: crosshair is currently disabled in VR")]
        public bool enableCrosshair = true;
        [Tooltip ("Max distance from character to selection")]
        public float crosshairMaxDistance = 30f;
        public float crosshairScale = 0.1f;
        public float targetAnimationSpeed = 0.75f;
        public float targetAnimationScale = 0.2f;
        public Color crosshairOnTargetColor = Color.yellow;
        public Color crosshairNormalColor = Color.white;
        public LayerMask crosshairHitLayerMask = -1;
        [Tooltip ("Crosshair will change over a reachable voxel.")]
        public bool changeOnBlock = true;
        [Tooltip ("Enable move crosshair on screen")]
        public bool freeMode;
        [Tooltip ("When enabled, crosshair colors invert according to background color to enhance visibility. This feature uses GrabPass which can be expensive on mobile.")]
        public bool autoInvertColors = true;
        public float wheelSensibility = 10f;

        [Header ("Voxel Highlight")]
        public bool voxelHighlight = true;
        public Color voxelHighlightColor = Color.yellow;
        [Range (1f, 100f)]
        public float voxelHighlightEdge = 20f;

        /// <summary>
        /// Triggered when player enters a voxel if that voxel definition has triggerEnterEvent = true
        /// </summary>
        public event VoxelEvent OnVoxelEnter;

        /// <summary>
        /// Triggered when a player walks over a voxel if that voxel definition has triggerWalkEvent = true
        /// </summary>
        public event VoxelEvent OnVoxelWalk;


        // internal fields
        int lastPosX, lastPosY, lastPosZ;
        int lastVoxelTypeIndex;
        float nextPlayerDamageTime;
        float lastDiveTime;
        float m_StepCycle;
        float m_NextStep;
        bool modelBuildPreview;
        ModelDefinition modelBuildItem;
        bool modelBuildInProgress;
        Vector3d modelBuildPreviewPosition;
        int buildRotationDegrees;
        GameObject modelBuildPreviewGO;
        float modelBuildPreviewOffset;


        protected VoxelHitInfo _crosshairHitInfo;
        public VoxelHitInfo crosshairHitInfo => _crosshairHitInfo;

        [NonSerialized]
        public bool crosshairOnBlock;
        Vector3 m_LastNonCollidingCharacterPos;
        protected IVoxelPlayPlayer _player;
        [SerializeField, HideInInspector] protected float _characterHeight = 1.8f;
        protected VoxelPlayInputController input;

        int lastPositionX, lastPositionZ;

        public virtual float GetCharacterHeight ()
        {
            return _characterHeight;
        }


        public IVoxelPlayPlayer player {
            get {
                return VoxelPlayPlayer.instance;
            }
        }

        [NonSerialized]
        public VoxelPlayEnvironment env;



        protected void Init ()
        {
            m_AudioSource = GetComponent<AudioSource> ();
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;

            env = VoxelPlayEnvironment.instance;
            if (env == null) {
                Debug.LogError ("Voxel Play Environment must be added first.");
            } else {
                env.characterController = this;
            }
            m_LastNonCollidingCharacterPos = Misc.vector3max;

            // Check player can collide with voxels
            if (env != null) {
                env.OnInitialized += () => input = env.input;
#if UNITY_EDITOR
                if (Physics.GetIgnoreLayerCollision(gameObject.layer, env.layerVoxels)) {
                    Debug.LogError("Player currently can't collide with voxels. Please check physics collision matrix in Project settings or change Voxels Layer in VoxelPlayEnvironment component.");
                }
#endif
            }
        }

        /// <summary>
        /// Updates internal rotation variables based on current character and camera transforms
        /// </summary>
        public abstract void UpdateLook ();

        public abstract bool isReady { get; }

        private void OnApplicationFocus (bool focus)
        {
            if (input != null) {
                input.focused = focus;
            }
        }
        /// <summary>
        /// Toggles on/off character light
        /// </summary>
        public void ToggleCharacterLight ()
        {
            Light light = GetComponentInChildren<Light> ();
            if (light != null) {
                ToggleCharacterLight (!light.enabled);
            }
        }

        /// <summary>
        /// Toggles on/off character light
        /// </summary>
        public void ToggleCharacterLight (bool state)
        {
            Light light = GetComponentInChildren<Light> ();
            if (light != null && light.enabled != state) {
                light.enabled = state;
                env.UpdateLights ();
                if (light.enabled) {
                    env.ShowMessage ("<color=green>Player torch <color=yellow>ON</color></color>");
                } else {
                    env.ShowMessage ("<color=green>Player torch <color=yellow>OFF</color></color>");
                }
            }
        }

        protected void CheckFootfalls ()
        {
            Vector3 curPos = transform.position;
            int x = (int)curPos.x;
            int y = (int)curPos.y;
            int z = (int)curPos.z;
            if (x != lastPosX || y != lastPosY || z != lastPosZ || voxelUnder == null) {
                lastPosX = x;
                lastPosY = y;
                lastPosZ = z;
                curPos.y = FastMath.FloorToInt (curPos.y) - 0.5f;
                VoxelIndex index = env.GetVoxelUnderIndex (curPos, true, ColliderTypes.IgnorePlayer);
                if (index.typeIndex != lastVoxelTypeIndex || voxelUnder == null) {
                    lastVoxelTypeIndex = index.typeIndex;
                    if (lastVoxelTypeIndex != 0) {
                        voxelUnder = index.type;
                        if (isGrounded) {
                            SetFootstepSounds (voxelUnder.footfalls, voxelUnder.landingSound, voxelUnder.jumpSound);
                            if (voxelUnder.triggerWalkEvent && OnVoxelWalk != null) {
                                OnVoxelWalk (index.chunk, index.voxelIndex);
                            }
                        }
                    }
                }
            }
            if (isGrounded || isInWater) {
                CheckDamage (voxelUnder);
            }
        }

        protected void CheckDamage (VoxelDefinition voxelType)
        {
            if (voxelType == null)
                return;
            int playerDamage = voxelType.playerDamage;
            if (playerDamage > 0 && Time.time > nextPlayerDamageTime) {
                nextPlayerDamageTime = Time.time + voxelType.playerDamageDelay;
                player.DamageToPlayer (playerDamage);
            }
        }

        protected void CheckEnterTrigger (VoxelChunk chunk, int voxelIndex)
        {
            if (chunk != null && env.voxelDefinitions [chunk.voxels [voxelIndex].typeIndex].triggerEnterEvent && OnVoxelEnter != null) {
                OnVoxelEnter (chunk, voxelIndex);
            }
        }

        public void SetFootstepSounds (AudioClip [] footStepsSounds, AudioClip jumpSound, AudioClip landSound)
        {
            this.footstepSounds = footStepsSounds;
            this.jumpSound = jumpSound;
            this.landSound = landSound;
        }

        public void PlayLandingSound ()
        {
            if (isInWater || m_AudioSource == null)
                return;
            m_AudioSource.clip = landSound;
            m_AudioSource.Play ();
            m_NextStep = m_StepCycle + .5f;
        }



        public void PlayJumpSound ()
        {
            if (isInWater || isFlying || m_AudioSource == null)
                return;
            m_AudioSource.clip = jumpSound;
            m_AudioSource.Play ();
        }


        public void PlayCancelSound ()
        {
            if (m_AudioSource == null)
                return;
            m_AudioSource.clip = cancelSound;
            m_AudioSource.Play ();
        }


        public void PlayWaterSplashSound ()
        {
            if (Time.time - lastDiveTime < 1f)
                return;
            lastDiveTime = Time.time;
            m_NextStep = m_StepCycle + swimStrokeInterval;
            if (waterSplash != null && m_AudioSource != null) {
                m_AudioSource.clip = waterSplash;
                m_AudioSource.Play ();
            }
        }

        /// <summary>
        /// Plays a sound at character position
        /// </summary>
        public void PlayCustomSound (AudioClip sound)
        {
            if (sound != null && m_AudioSource != null) {
                m_AudioSource.clip = sound;
                m_AudioSource.Play ();
            }
        }


        protected void ProgressStepCycle (Vector3 velocity, float speed)
        {
            if (velocity.sqrMagnitude > 0 && isPressingMoveKeys) {
                m_StepCycle += (velocity.magnitude + (speed * (isMoving ? 1f : runstepLenghten))) * Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep)) {
                return;
            }

            m_NextStep = m_StepCycle + footStepInterval;

            PlayFootStepAudio ();
        }



        private void PlayFootStepAudio ()
        {
            if (!isGrounded || m_AudioSource == null) {
                return;
            }
            if (footstepSounds == null || footstepSounds.Length == 0)
                return;
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n;
            if (footstepSounds.Length == 1) {
                n = 0;
            } else {
                n = Random.Range (1, footstepSounds.Length);
            }
            m_AudioSource.clip = footstepSounds [n];
            m_AudioSource.PlayOneShot (m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            footstepSounds [n] = footstepSounds [0];
            footstepSounds [0] = m_AudioSource.clip;
        }


        protected void ProgressSwimCycle (Vector3 velocity, float speed)
        {
            if (velocity.sqrMagnitude > 0 && isPressingMoveKeys) {
                m_StepCycle += (velocity.magnitude + speed) * Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep)) {
                return;
            }

            m_NextStep = m_StepCycle + swimStrokeInterval;

            if (!isUnderwater) {
                PlaySwimStrokeAudio ();
            }
        }


        private void PlaySwimStrokeAudio ()
        {
            if (swimStrokeSounds == null || swimStrokeSounds.Length == 0 || m_AudioSource == null)
                return;
            // pick & play a random swim stroke sound from the array,
            // excluding sound at index 0
            int n;
            if (swimStrokeSounds.Length == 1) {
                n = 0;
            } else {
                n = Random.Range (1, swimStrokeSounds.Length);
            }
            m_AudioSource.clip = swimStrokeSounds [n];
            m_AudioSource.PlayOneShot (m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            swimStrokeSounds [n] = swimStrokeSounds [0];
            swimStrokeSounds [0] = m_AudioSource.clip;
        }


        /// <summary>
        /// Moves character controller to a new position. Use this method instead of changing the transform position
        /// </summary>
        public abstract void MoveTo (Vector3 newPosition);


        /// <summary>
        /// Moves character controller by a distance. Use this method instead of changing the transform position
        /// </summary>
        public virtual void Move (Vector3 newPosition) { }


        protected void ControllerUpdate ()
        {
            if (input.GetButtonDown (InputButtonNames.Rotate)) {
                buildRotationDegrees = (buildRotationDegrees + 90) % 360;
            }
            ModelBuildPreviewUpdate ();

            // Check if character has changed X/Z voxel position
            Vector3 pos = transform.position;
            int posX = (int)pos.x;
            int posZ = (int)pos.z;
            if (posX != lastPositionX || posZ != lastPositionZ) {
                lastPositionZ = posZ;
                lastPositionX = posX;
                CharacterChangedXZPosition (pos);
            }
        }

        protected virtual void CharacterChangedXZPosition (Vector3 newPosition) { }

        /// <summary>
        /// Implements building stuff
        /// </summary>
        /// <param name="camPos">The camera position OR the character position in a 3rd person controller</param>">
        protected virtual void DoBuild (Vector3 camPos, Vector3 forward, Vector3d hintedPlacePos)
        {
            if (player.selectedItemIndex < 0 || player.selectedItemIndex >= player.items.Count)
                return;

            InventoryItem inventoryItem = player.GetSelectedItem ();
            ItemDefinition currentItem = inventoryItem.item;
            switch (currentItem.category) {
            case ItemCategory.Voxel:

                // Basic placement rules
                bool canPlace = crosshairOnBlock;
                Voxel existingVoxel = _crosshairHitInfo.voxel;
                VoxelDefinition existingVoxelType = existingVoxel.type;
                Vector3d placePos;

                if (currentItem.voxelType.renderType == RenderType.Water && !canPlace) {
                    canPlace = true; // water can be poured anywhere
                    placePos = camPos + forward * 3f;
                } else {
                    placePos = env.GetVoxelPosition (_crosshairHitInfo.voxelCenter);
                    if (existingVoxelType.renderType != RenderType.CutoutCross) {
                        placePos += _crosshairHitInfo.normal;
                    }
                    if (canPlace && _crosshairHitInfo.normal.y > 0.9f) {
                        // Make sure there's a valid voxel under position (ie. do not build a voxel on top of grass)
                        canPlace = (existingVoxelType != null && existingVoxelType.renderType != RenderType.CutoutCross && (existingVoxelType.renderType != RenderType.Water || currentItem.voxelType.renderType == RenderType.Water));
                    }
                }

                VoxelDefinition existingVoxelOnPlacePos = env.GetVoxel (placePos).type;
                VoxelDefinition placeVoxelType = currentItem.voxelType;
                float distanceFromCenter = (float)(_crosshairHitInfo.point.y - env.GetVoxelPosition (_crosshairHitInfo.voxelCenter).y);
                bool isOverCenter = false;

                if (placeVoxelType.allowUpsideDownVoxel && placeVoxelType.upsideDownVoxel != null) {
                    isOverCenter = distanceFromCenter > 0 ? true : false;

                    isOverCenter = _crosshairHitInfo.normal.y > 0.9f ? false : isOverCenter;
                    isOverCenter = _crosshairHitInfo.normal.y < -0.9f ? true : isOverCenter;

                    if (isOverCenter) {
                        placeVoxelType = placeVoxelType.upsideDownVoxel;
                    }
                }

                bool isPromoting = false;
                // Check voxel promotion
                if (canPlace) {
                    if (existingVoxelType == placeVoxelType || existingVoxelType == placeVoxelType.upsideDownVoxel) {
                        if (existingVoxelType.promotesTo != null) {
                            if (existingVoxelType.isUpsideDown) {
                                if (_crosshairHitInfo.normal.y < -0.9f) {
                                    placePos = env.GetVoxelPosition (_crosshairHitInfo.voxelCenter);
                                    placeVoxelType = existingVoxelType.promotesTo;
                                    isPromoting = true;
                                }
                            } else {
                                if (_crosshairHitInfo.normal.y > 0.9f) {
                                    placePos = env.GetVoxelPosition (_crosshairHitInfo.voxelCenter);
                                    placeVoxelType = existingVoxelType.promotesTo;
                                    isPromoting = true;
                                }
                            }
                        }
                    }
                    if ((existingVoxelOnPlacePos == placeVoxelType || existingVoxelOnPlacePos == placeVoxelType.upsideDownVoxel) && !isPromoting) {
                        if (existingVoxelOnPlacePos.promotesTo != null) {
                            if (existingVoxelOnPlacePos.isUpsideDown) {
                                if (distanceFromCenter < 0f || _crosshairHitInfo.normal.y > 0.9f) {
                                    placeVoxelType = existingVoxelOnPlacePos.promotesTo;
                                    isPromoting = true;
                                }
                            } else {
                                if (distanceFromCenter > 0f || _crosshairHitInfo.normal.y < -0.9f) {
                                    placeVoxelType = existingVoxelOnPlacePos.promotesTo;
                                    isPromoting = true;
                                }
                            }
                        }
                    }
                }

                // Compute rotation
                int textureRotation = 0;
                if (placeVoxelType.placeOnWall) {
                    if (!existingVoxelType.supportsDecorations) {
                        canPlace = false;
                    } else if (_crosshairHitInfo.normal.z > 0) {
                        textureRotation = 2;
                    } else if (_crosshairHitInfo.normal.z < 0) {
                        textureRotation = 0;
                    } else if (_crosshairHitInfo.normal.x > 0) {
                        textureRotation = 3;
                    } else if (_crosshairHitInfo.normal.x < 0) {
                        textureRotation = 1;
                    } else {
                        canPlace = false;
                    }
                    if (canPlace) {
                        if (existingVoxelOnPlacePos.hasContent == 1) {
                            env.VoxelDestroy (placePos);
                        }
                    } else {
                        PlayCancelSound ();
                    }
                } else if (placeVoxelType.placeFacingPlayer && placeVoxelType.renderType.supportsTextureRotation ()) {
                    // Orient voxel to player
                    if (Mathf.Abs (forward.x) > Mathf.Abs (forward.z)) {
                        if (forward.x > 0) {
                            textureRotation = 1;
                        } else {
                            textureRotation = 3;
                        }
                    } else if (forward.z < 0) {
                        textureRotation = 2;
                    }
                }

                // Final check, does it overlap existing geometry?
                if (canPlace && !isPromoting) {
                    Quaternion rotationQ = Quaternion.Euler (0, Voxel.GetTextureRotationDegrees (textureRotation), 0);
                    canPlace = !env.VoxelOverlaps (placePos, placeVoxelType, rotationQ, 1 << env.layerVoxels);
                    if (!canPlace) {
                        PlayCancelSound ();
                    }
                }

#if UNITY_EDITOR
                else if (env.constructorMode) {
                    placePos = hintedPlacePos;
                    placeVoxelType = currentItem.voxelType;
                    canPlace = true;
                }
#endif

                // Finally place the voxel
                if (canPlace) {
                    // Consume item first
                    if (!env.buildMode) {
                        player.ConsumeItem ();
                    }
                    // Place it
                    float amount = inventoryItem.quantity < 1f ? inventoryItem.quantity : 1f;
                    env.VoxelPlace (placePos, placeVoxelType, true, placeVoxelType.tintColor, amount, textureRotation);

                    // Moves back character controller if voxel is put just on its position
                    const float minDist = 0.5f;
                    float distSqr = Vector3.SqrMagnitude (camPos - placePos);
                    if (distSqr < minDist * minDist) {
                        MoveTo (transform.position + _crosshairHitInfo.normal);
                    }
                }
                break;
            case ItemCategory.Torch:
                if (crosshairOnBlock) {
                    GameObject torchAttached = env.TorchAttach (_crosshairHitInfo, currentItem);
                    if (!env.buildMode && torchAttached != null) {
                        player.ConsumeItem ();
                    }
                }
                break;
            case ItemCategory.Model:
                if (!modelBuildInProgress) {
                    if (modelBuildPreview) {
                        ModelPreviewCancel ();
                        // check if building position is in frustum, otherwise cancel building
                        Vector3 viewportPos = env.cameraMain.WorldToViewportPoint (modelBuildPreviewPosition);
                        if (viewportPos.x < 0 || viewportPos.x > 1f || viewportPos.y < 0 || viewportPos.y > 1f || viewportPos.z < 0) {
                            return;
                        }
                        if (currentItem.model.buildDuration > 0) {
                            modelBuildInProgress = true;
                            env.ModelPlace (modelBuildPreviewPosition, currentItem.model, currentItem.model.buildDuration, buildRotationDegrees, 1f, true, FinishBuilding);
                        } else {
                            env.ModelPlace (modelBuildPreviewPosition, currentItem.model, buildRotationDegrees, colorBrightness: 1f, fitTerrain: true);
                        }
                        player.ConsumeItem ();
                    } else {
                        modelBuildPreview = true;
                        modelBuildItem = currentItem.model;
                        ModelBuildPreviewUpdate ();
                    }
                }
                break;
            case ItemCategory.General:
                ThrowCurrentItem (camPos, forward);
                break;
            }
        }


        void ModelBuildPreviewUpdate ()
        {
            if (!modelBuildPreview || !crosshairOnBlock || modelBuildItem == null)
                return;

            modelBuildPreviewPosition = _crosshairHitInfo.voxelCenter;

            modelBuildPreviewOffset += Input.GetAxis ("Mouse ScrollWheel") * wheelSensibility;
            if (modelBuildPreviewOffset < 0) modelBuildPreviewOffset = 0;

            modelBuildPreviewPosition.y += (int)modelBuildPreviewOffset;

            int halfSizeX = modelBuildItem.sizeX / 2;
            int halfSizeZ = modelBuildItem.sizeZ / 2;

            Vector3d corner1 = Quaternion.Euler (0, buildRotationDegrees, 0) * new Vector3 (-halfSizeX, 0, -halfSizeZ);
            Vector3d corner2 = corner1 + Quaternion.Euler (0, buildRotationDegrees, 0) * new Vector3 (modelBuildItem.sizeX - 1, 0, modelBuildItem.sizeZ - 1);
            Vector3d previewPos = new Vector3d ((corner1.x + corner2.x) * 0.5f + modelBuildPreviewPosition.x, modelBuildPreviewPosition.y - 0.5f, (corner1.z + corner2.z) * 0.5f + modelBuildPreviewPosition.z);
            modelBuildPreviewGO = env.ModelHighlight (modelBuildItem, previewPos);
            modelBuildPreviewGO.transform.localRotation = Quaternion.Euler (0, buildRotationDegrees, 0);
        }


        /// <summary>
        /// Removes an unit from current item in player inventory and throws it into the scene
        /// </summary>
        public void ThrowCurrentItem (Vector3 throwPosition, Vector3 direction)
        {
            InventoryItem inventoryItem = player.ConsumeItem ();
            if (inventoryItem == InventoryItem.Null)
                return;

            if (inventoryItem.item.category == ItemCategory.Voxel) {
                env.VoxelThrow (throwPosition, direction, 15f, inventoryItem.item.voxelType, Misc.color32White);
            } else if (inventoryItem.item.category == ItemCategory.General) {
                env.ItemThrow (throwPosition, direction, 15f, inventoryItem.item);
            }
        }


        void FinishBuilding (ModelDefinition modelDefinition, Vector3d position)
        {
            modelBuildInProgress = false;
        }

        public bool ModelPreviewCancel ()
        {
            if (modelBuildPreview) {
                modelBuildPreview = false;
                if (modelBuildPreviewGO != null) {
                    modelBuildPreviewGO.SetActive (false);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ensures player is above terrain
        /// </summary>
        public virtual void Unstuck (bool toSurface = true)
        {
#if UNITY_EDITOR
            if (env.constructorMode) return;
#endif

            if (!unstuck) return;

            if (env.CheckCollision (env.cameraMain.transform.position) || env.CheckCollision (transform.position)) {
                // try to move to last good position
                if (m_LastNonCollidingCharacterPos.y < float.MaxValue && !env.CheckCollision (m_LastNonCollidingCharacterPos)) {
                    MoveTo (m_LastNonCollidingCharacterPos);
                    return;
                }
                // try up or surface
                float minAltitude = Mathf.FloorToInt (transform.position.y) + 1.1f;
                if (toSurface) {
                    minAltitude = Mathf.Max (env.GetTerrainHeight (transform.position), minAltitude);
                }
                Vector3 pos = new Vector3 (transform.position.x, minAltitude + GetCharacterHeight () * 0.5f, transform.position.z);
                MoveTo (pos);
            }
        }

        public void AnnotateNonCollidingPosition (Vector3 position)
        {
            m_LastNonCollidingCharacterPos = position;
        }

    }
}
