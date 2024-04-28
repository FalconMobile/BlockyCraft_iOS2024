using System;
using System.Collections;
using System.Collections.Generic;
using Sources.Scripts.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoxelPlay
{

    [ExecuteInEditMode]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001854-voxel-play-fps-controller")]
    public partial class VoxelPlayFirstPersonController : VoxelPlayCharacterControllerBase
    {

        [Header("Movement")]
        public float walkSpeed = 5f;
        public float runSpeed = 10f;
        public float flySpeed = 20f;
        public float swimSpeed = 3.7f;
        public float jumpSpeed = 10f;
        public float stickToGroundForce = 10f;
        public float gravityMultiplier = 2f;

        [Header("Smooth Climb")]
        public bool smoothClimb = true;
        public float climbYThreshold = 0.5f;
        public float climbSpeed = 4f;

        [Header("Thrust")]
        public float thrustPower = 23f;
        public float thrustMaxSpeed = 2f;
        public float thrustMaxAltitude = 100f;

        [Header("Aiming & HeadBob")]
        public MouseLook mouseLook;
        public bool useFovKick = true;
        [SerializeField] private FOVKick m_FovKick = new FOVKick(); 
        
        [Header("TeleportMap")]
        [SerializeField] private float heightTeleport;
        [SerializeField] private float heightRaycastHit;

        public override float GetCharacterHeight()
        {
            return hasCharacterController ? m_CharacterController.height : _characterHeight;
        }

        [Header("Orbit")]
        public bool orbitMode;
        public Vector3 lookAt;
        public float minDistance = 1f;
        public float maxDistance = 100f;

        // internal fields
        protected Camera m_Camera;

        bool isJumpActivated;
        bool isJumpInProgress;
        bool isPreviouslyGrounded;
        
        Vector3 m_Input;
        Vector3 m_MoveDir = Misc.vector3zero;
        CharacterController m_CharacterController;
        CollisionFlags m_CollisionFlags;
        float prevCrouchYPos;
        float prevCrouchTime;
        bool movingSmooth;
        float thrustAmount;

        protected float lastHitButtonPressed;
        GameObject underwaterPanel;
        Material underWaterMat;
        Transform crouch;

        int lastNearClipPosX, lastNearClipPosY, lastNearClipPosZ;
        Vector3 curPos;
        float waterLevelTop;

        const float switchDuration = 2f;
        bool firePressed;
        bool switching;
        float switchingStartTime;
        float switchingLapsed;

        float lastUserCameraNearClipPlane;

        static VoxelPlayFirstPersonController _firstPersonController;
        public bool hasCharacterController;
        bool hasThrusted;

        Vector3 externalForce;
        /// <summary>
        /// Used to provide a temporary push in some direction
        /// </summary>
        public void AddExternalForce(Vector3 force)
        {
            externalForce = force;
        }

        /// <summary>
        /// This method will check if a character controller is attached to the gameobject and update the public "hasCharacterController" field.
        /// </summary>
        /// <returns></returns>
        public bool CheckCharacterController()
        {
            if (this == null) return false;
            m_CharacterController = GetComponent<CharacterController>();
            hasCharacterController = !useThirdPartyController && m_CharacterController != null;
            return hasCharacterController;
        }

        public CharacterController characterController
        {
            get { return m_CharacterController; }
        }

        public static VoxelPlayFirstPersonController instance
        {
            get
            {
                if (_firstPersonController == null)
                {
                    _firstPersonController = VoxelPlayEnvironment.instance.characterController as VoxelPlayFirstPersonController;
                }
                return _firstPersonController;
            }
        }

        public override bool isReady
        {
            get
            {
                CheckCharacterController();
                return m_CharacterController != null && m_CharacterController.enabled;
            }
        }

        void OnEnable()
        {
            CheckCharacterController();
            env = VoxelPlayEnvironment.instance;
            if (env != null)
            {
                env.characterController = this;
            }
            crouch = transform.Find("Crouch");
            if (crouch == null)
            {
                GameObject crouchGO = new GameObject("Crouch");
                crouch = crouchGO.transform;
                crouch.transform.SetParent(transform, false);
            }
        }

        void Start()
        {
            Init();
            m_Camera = GetComponentInChildren<Camera>();
            if (m_Camera == null)
            {
                // cover the case where the camera is not part of this prefab but it's in the scene. In this case, we'll steal the camera and put it as a children
                m_Camera = Camera.main;
                if (m_Camera == null) m_Camera = FindObjectOfType<Camera>();
                if (m_Camera != null)
                {
                    m_Camera.transform.SetParent(crouch, false);
                    //m_Camera.transform.localPosition = new Vector3(0, 0.8f, 0f);
                    m_Camera.transform.localRotation = Misc.quaternionZero;
                }
            }
            if (m_Camera != null)
            {
                if (env != null)
                {
                    env.cameraMain = m_Camera;
                }
            }

            if (env == null || !env.applicationIsPlaying)
                return;

            InitUnderwaterEffect();

            ToggleCharacterController(false);

            // Position character on ground
            if (!env.saveFileIsLoaded)
            {
                if (startOnFlat && env.world != null)
                {
                    float minAltitude = env.world.terrainGenerator.maxHeight;
                    Vector3 flatPos = transform.position;
                    Vector3 randomPos;
                    for (int k = 0; k < startOnFlatIterations; k++)
                    {
                        randomPos = Random.insideUnitSphere * 1000;
                        float alt = env.GetTerrainHeight(randomPos);
                        if (alt < minAltitude && alt >= env.waterLevel + 1)
                        {
                            minAltitude = alt;
                            randomPos.y = alt + GetCharacterHeight() + 1;
                            flatPos = randomPos;
                        }
                    }
                    transform.position = flatPos;
                }
            }

            InitCrosshair();

            if (env.initialized)
            {
                LateInit();
            }
            else
            {
                env.OnInitialized += () => LateInit();
            }
        }


        void LateInit()
        {
            if (hasCharacterController)
            {
                SetOrbitMode(orbitMode);
                mouseLook.Init(transform, m_Camera.transform, input);
            }
            if (useFovKick)
            {
                m_FovKick.ChangeCamera(m_Camera);
            }
            WaitForCurrentChunk();
            CoroutineContainer.Start(Teport());
        }

        private IEnumerator<WaitForSeconds> Teport()
        {
            yield return Yielders.WaitForSeconds(2f);
            // Bit shift the index of the layer (8) to get a bit mask
            int layerMask = 1 << 8;

            // This would cast rays only against colliders in layer 8.
            // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
            layerMask = ~layerMask;
            
            RaycastHit hit;
            // Пересекает ли луч какие-либо объекты, кроме слоя игрока
            if (Physics.Raycast(new Vector3(transform.position.x,transform.position.y + heightRaycastHit ,transform.position.z), transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity))
            {
                gameObject.transform.position = new Vector3(hit.point.x, hit.point.y + heightTeleport, hit.point.z);
                Debug.Log($"Попал : {hit.collider.name}");
            }
            else
            {
                Debug.Log("Не попал");
            }
        }

        void InitUnderwaterEffect()
        {
            underwaterPanel = Instantiate(Resources.Load<GameObject>("VoxelPlay/Prefabs/UnderwaterPanel"), m_Camera.transform);
            underwaterPanel.name = "UnderwaterPanel";
            Renderer underwaterRenderer = underwaterPanel.GetComponent<Renderer>();
            underWaterMat = underwaterRenderer.sharedMaterial;
            underWaterMat = Instantiate<Material>(underWaterMat);
            underwaterRenderer.sharedMaterial = underWaterMat;

            underwaterPanel.transform.localPosition = new Vector3(0, 0, m_Camera.nearClipPlane + 0.001f);
            underwaterPanel.SetActive(false);
        }


        public override void UpdateLook()
        {
            // Pass initial rotation to mouseLook script
            if (m_Camera != null)
            {
                mouseLook.Init(characterController.transform, m_Camera.transform, null);
            }
        }


        /// <summary>
        /// Disables character controller until chunk is ready
        /// </summary>
        public void WaitForCurrentChunk()
        {
            ToggleCharacterController(false);
            StartCoroutine(WaitForCurrentChunkCoroutine());
        }

        /// <summary>
        /// Enables/disables character controller
        /// </summary>
        /// <param name="state">If set to <c>true</c> state.</param>
        public void ToggleCharacterController(bool state)
        {
            if (hasCharacterController)
            {
                m_CharacterController.enabled = state;
            }
            enabled = state;
        }

        /// <summary>
        /// Ensures player chunk is finished before allow player movement / interaction with colliders
        /// </summary>
        IEnumerator WaitForCurrentChunkCoroutine()
        {
            // Wait until current player chunk is rendered
            WaitForSeconds w = new WaitForSeconds(0.2f);
            for (int k = 0; k < 20; k++)
            {
                VoxelChunk chunk = env.GetCurrentChunk();
                if (chunk != null && chunk.isRendered)
                {
                    break;
                }
                yield return w;
            }
            Unstuck(true);
            prevCrouchYPos = crouch.position.y;
            ToggleCharacterController(true);
            if (!hasCharacterController)
            {
                switchingLapsed = 1f;
            }
        }

        void Update()
        {
            UpdateImpl();
        }

        protected virtual void UpdateImpl()
        {
            if (env == null || !env.applicationIsPlaying || !env.initialized || input == null)
                return;

            curPos = transform.position;

            if (hasCharacterController)
            {
                UpdateWithCharacterController();
                if (smoothClimb)
                {
                    SmoothClimb();
                }
            }
            else
            {
                UpdateSimple();
            }

            ControllerUpdate();
        }

        protected virtual void UpdateWithCharacterController()
        {
            CheckFootfalls();
            RotateView();

            if (orbitMode)
                isFlying = true;

            
            // the jump state needs to read here to make sure it is not missed
            if (!isJumpActivated && !isFlying && manageJump)
            {
                isJumpActivated = input.GetButtonDown(InputButtonNames.Jump);
            }

            bool isNowGrounded = m_CharacterController.isGrounded;
            if (isNowGrounded && !isPreviouslyGrounded)
            {
                PlayLandingSound();
                m_MoveDir.y = 0f;
                
                isJumpInProgress = false;
                isJumpActivated = false;
            }
            
            if (!isNowGrounded && isPreviouslyGrounded && !isJumpInProgress)
            {
                m_MoveDir.y = 0f;
            }

            isPreviouslyGrounded = isNowGrounded;

            // Process click events
            if (input.focused && input.enabled)
            {
                bool leftAltPressed = input.GetButton(InputButtonNames.LeftAlt);
                bool leftShiftPressed = input.GetButton(InputButtonNames.LeftShift);
                bool leftControlPressed = input.GetButton(InputButtonNames.LeftControl);
                
                bool isAttackClicked = input.GetButtonClick(InputButtonNames.Button1) || input.GetButtonClick(InputButtonNames.Destroy);
                bool isAttack = input.GetButton(InputButtonNames.Button1) || input.GetButton(InputButtonNames.Destroy);
                bool isAttackDown = input.GetButtonDown(InputButtonNames.Button1) || input.GetButtonDown(InputButtonNames.Destroy);
                
                bool fire1Clicked = manageAttack && isAttackDown;
                bool fire2Clicked = manageBuild && input.GetButtonDown(InputButtonNames.Button2);

                if (crosshairOnBlock && isAttackClicked)
                {
                    env.TriggerVoxelClickEvent(_crosshairHitInfo.chunk, _crosshairHitInfo.voxelIndex, 0);
                }
                else if (crosshairOnBlock && input.GetButtonClick(InputButtonNames.Button2))
                {
                    env.TriggerVoxelClickEvent(_crosshairHitInfo.chunk, _crosshairHitInfo.voxelIndex, 1);
                }
                else if (crosshairOnBlock && input.GetButtonClick(InputButtonNames.MiddleButton))
                {
                    env.TriggerVoxelClickEvent(_crosshairHitInfo.chunk, _crosshairHitInfo.voxelIndex, 2);
                }

                if (fire1Clicked)
                {
                    firePressed = true;
                    if (ModelPreviewCancel())
                    {
                        firePressed = false;
                        lastHitButtonPressed = Time.time + 0.5f;
                    }
                }
                else if (!isAttack)
                {
                    firePressed = false;
                }

                if (!leftShiftPressed && !leftAltPressed && !leftControlPressed)
                {
                    if (Time.time - lastHitButtonPressed > player.GetHitDelay())
                    {
                        if (firePressed)
                        {
                            if (_crosshairHitInfo.item != null)
                            {
                                _crosshairHitInfo.item.PickItem();
                                crosshairOnBlock = false;
                                firePressed = false;
                            }
                            else
                            {
                                DoHit(env.buildMode ? 255 : player.GetHitDamage());
                            }
                        }
                    }
                }

                if (crosshairOnBlock && input.GetButtonDown(InputButtonNames.MiddleButton))
                {
                    if (_crosshairHitInfo.voxel.type.allowUpsideDownVoxel && _crosshairHitInfo.voxel.type.upsideDownVoxel != null)
                    {
                        player.SetSelectedItem(_crosshairHitInfo.voxel.type.hidden ? _crosshairHitInfo.voxel.type.upsideDownVoxel : _crosshairHitInfo.voxel.type);
                    }
                    else
                    {
                        player.SetSelectedItem(_crosshairHitInfo.voxel.type);
                    }
                }

                if (manageBuild)
                {
                    if (input.GetButtonDown(InputButtonNames.Build))
                    {
                        env.SetBuildMode(!env.buildMode);
                        if (env.buildMode)
                        {
                            env.ShowMessage("<color=green>Entered <color=yellow>Build Mode</color>. Press <color=white>B</color> to cancel.</color>");
                        }
                        else
                        {
                            env.ShowMessage("<color=green>Back to <color=yellow>Normal Mode</color>.</color>");
                        }
                    }
                    else if (input.GetButtonDown(InputButtonNames.Rotate))
                    {
                        if (_crosshairHitInfo.voxel.type.allowsTextureRotation)
                        {
                            int rotation = env.GetVoxelTexturesRotation(_crosshairHitInfo.chunk, _crosshairHitInfo.voxelIndex);
                            rotation = (rotation + 1) % 4;
                            env.VoxelSetTexturesRotation(_crosshairHitInfo.chunk, _crosshairHitInfo.voxelIndex, rotation);
                        }
                    }
                }

                if (fire2Clicked && !leftAltPressed && !leftShiftPressed)
                {
#if UNITY_EDITOR
                    DoBuild(m_Camera.transform.position, m_Camera.transform.forward, voxelHighlightBuilder != null ? (Vector3d)voxelHighlightBuilder.transform.position : Vector3d.zero);
#else
                    DoBuild (m_Camera.transform.position, m_Camera.transform.forward, Vector3d.zero);
#endif
                }

                // Toggles Flight mode
                if (manageFly && input.GetButtonDown(InputButtonNames.Fly))
                {
                    isFlying = !isFlying;
                    if (isFlying)
                    {
                        isJumpInProgress = false;
                        env.ShowMessage("<color=green>Flying <color=yellow>ON</color></color>");
                    }
                    else
                    {
                        env.ShowMessage("<color=green>Flying <color=yellow>OFF</color></color>");
                    }
                }

                if (isGrounded && !isCrouched && input.GetButtonDown(InputButtonNames.LeftControl))
                {
                    isCrouched = true;
                }
                else if (isGrounded && isCrouched && input.GetButtonUp(InputButtonNames.LeftControl))
                {
                    isCrouched = false;
                }
                else if (isGrounded && manageCrouch && input.GetButtonDown(InputButtonNames.Crouch))
                {
                    isCrouched = !isCrouched;
                    if (isCrouched)
                    {
                        env.ShowMessage("<color=green>Crouching <color=yellow>ON</color></color>");
                    }
                    else
                    {
                        env.ShowMessage("<color=green>Crouching <color=yellow>OFF</color></color>");
                    }
                }
                else if (input.GetButtonDown(InputButtonNames.Light))
                {
                    ToggleCharacterLight();
                }
                else if (input.GetButtonDown(InputButtonNames.ThrowItem))
                {
                    ThrowCurrentItem(m_Camera.transform.position, m_Camera.transform.forward);
                }
            }

            // Check water
            if (!movingSmooth)
            {
                CheckWaterStatus();

                // Check crouch status
                if (!isInWater)
                {
                    UpdateCrouch();
                }
            }

#if UNITY_EDITOR
            UpdateConstructor();
#endif

        }

        protected virtual void UpdateSimple()
        {
            // Check water
            CheckWaterStatus();

        }

        public void SetOrbitMode(bool enableOrbitMode)
        {
            if (orbitMode != enableOrbitMode)
            {
                orbitMode = enableOrbitMode;
                switching = true;
                switchingStartTime = Time.time;
                freeMode = orbitMode;
            }
        }


        void UpdateCrouch()
        {
            if (isCrouched && crouch.localPosition.y == 0)
            {
                crouch.transform.localPosition = Misc.vector3down;
                m_CharacterController.stepOffset = 0.6f;
            }
            else if (!isCrouched && crouch.localPosition.y != 0)
            {
                crouch.transform.localPosition = Misc.vector3zero;
                m_CharacterController.stepOffset = 1.1f;
            }
        }

        void CheckWaterStatus()
        {

            Vector3 nearClipPos = m_Camera.transform.position + m_Camera.transform.forward * (m_Camera.nearClipPlane + 0.001f);
            if (nearClipPos.x == lastNearClipPosX && nearClipPos.y == lastNearClipPosY && nearClipPos.z == lastNearClipPosZ)
                return;

            lastNearClipPosX = (int)nearClipPos.x;
            lastNearClipPosY = (int)nearClipPos.y;
            lastNearClipPosZ = (int)nearClipPos.z;

            bool wasInWater = isInWater;

            isInWater = false;
            isSwimming = false;
            isUnderwater = false;

            // Check water on character controller position
            Voxel voxelCh;
            if (env.GetVoxelIndex(curPos, out VoxelChunk chunk, out int voxelIndex, false))
            {
                voxelCh = chunk.voxels[voxelIndex];
            }
            else
            {
                voxelCh = Voxel.Empty;
            }
            VoxelDefinition voxelChType = env.voxelDefinitions[voxelCh.typeIndex];
            if (voxelCh.hasContent == 1)
            {
                CheckEnterTrigger(chunk, voxelIndex);
                CheckDamage(voxelChType);
            }

            // Safety check; if voxel at character position is solid, move character on top of terrain
            if (voxelCh.isSolid)
            {
                Unstuck(false);
            }
            else
            {
                AnnotateNonCollidingPosition(curPos);
                // Check if water surrounds camera
                Voxel voxelCamera = env.GetVoxel(nearClipPos, false);
                VoxelDefinition voxelCameraType = env.voxelDefinitions[voxelCamera.typeIndex];
                if (voxelCamera.hasContent == 1)
                {
                    CheckEnterTrigger(chunk, voxelIndex);
                    CheckDamage(voxelCameraType);
                }

                if (voxelCamera.GetWaterLevel() > 7)
                {
                    // More water on top?
                    Vector3 pos1Up = nearClipPos;
                    pos1Up.y += 1f;
                    Voxel voxel1Up = env.GetVoxel(pos1Up);
                    if (voxel1Up.GetWaterLevel() > 0)
                    {
                        isUnderwater = true;
                        waterLevelTop = nearClipPos.y + 1f;
                    }
                    else
                    {
                        waterLevelTop = FastMath.FloorToInt(nearClipPos.y) + 0.9f;
                        isUnderwater = nearClipPos.y < waterLevelTop;
                        isSwimming = !isUnderwater;
                    }
                    underWaterMat.color = voxelCameraType.diveColor;
                }
                else if (voxelCh.GetWaterLevel() > 7)
                {
                    isSwimming = true;
                    waterLevelTop = FastMath.FloorToInt(curPos.y) + 0.9f;
                    underWaterMat.color = voxelChType.diveColor;

                }
                underWaterMat.SetFloat("_WaterLevel", waterLevelTop);
            }

            isInWater = isSwimming || isUnderwater;
            if (crouch != null)
            {
                // move camera a bit down to simulate swimming position
                if (!wasInWater && isInWater)
                {
                    PlayWaterSplashSound();
                    crouch.localPosition = Misc.vector3down * 0.6f; // crouch
                }
                else if (wasInWater && !isInWater)
                {
                    crouch.localPosition = Misc.vector3zero;
                }
            }

            // Show/hide underwater panel
            if (isInWater && !underwaterPanel.activeSelf)
            {
                underwaterPanel.SetActive(true);
            }
            else if (!isInWater && underwaterPanel.activeSelf)
            {
                underwaterPanel.SetActive(false);
            }

        }

        protected override void CharacterChangedXZPosition(Vector3 newPosition)
        {
            // Check if underground and adjust camera near clip plane
            float alt = env.GetTerrainHeight(newPosition);
            if (newPosition.y >= alt)
            {
                alt = env.GetTopMostHeight(newPosition);
            }
            isUnderground = newPosition.y < alt;
            if (isUnderground)
            {
                if (env.cameraMain.nearClipPlane > 0.081f)
                {
                    lastUserCameraNearClipPlane = env.cameraMain.nearClipPlane;
                    env.cameraMain.nearClipPlane = 0.08f;
                }
            }
            else if (env.cameraMain.nearClipPlane < lastUserCameraNearClipPlane)
            {
                env.cameraMain.nearClipPlane = lastUserCameraNearClipPlane;
            }

        }

        protected virtual void DoHit(int damage)
        {
            lastHitButtonPressed = Time.time;

            // Check item sound
            InventoryItem inventoryItem = player.GetSelectedItem();
            if (inventoryItem != InventoryItem.Null)
            {
                ItemDefinition currentItem = inventoryItem.item;
                PlayCustomSound(currentItem.useSound);
            }

            Ray ray = GetCameraRay();
            float maxDistance = player.GetHitRange();
            if (env.buildMode)
            {
                maxDistance = Mathf.Max(crosshairMaxDistance, maxDistance);
            }
            env.RayHit(ray, damage, maxDistance, player.GetHitDamageRadius());
        }


        private void FixedUpdate()
        {
            FixedUpdateImpl();
        }

        protected virtual void FixedUpdateImpl()
        {

            if (!hasCharacterController)
                return;

            GetInput(out float speed);

            Vector3 pos = transform.position;

            m_MoveDir += externalForce;
            externalForce = Misc.vector3zero;

            if (thrustAmount > 0.001f)
            {
                hasThrusted = true;
                Vector3 impulseVector = transform.forward * m_Input.y + transform.right * m_Input.x + transform.up * thrustAmount;
                impulseVector.x *= thrustAmount;
                impulseVector.z *= thrustAmount;
                impulseVector += Physics.gravity * gravityMultiplier;
                m_MoveDir += impulseVector * Time.fixedDeltaTime;
                float velocity = m_MoveDir.magnitude;
                if (velocity > thrustMaxSpeed)
                {
                    m_MoveDir = m_MoveDir.normalized * thrustMaxSpeed;
                }
            }
            else if (isFlying || isInWater)
            {
                Transform camTransform = m_Camera.transform;
                m_MoveDir = camTransform.forward * m_Input.y + camTransform.right * m_Input.x + camTransform.up * m_Input.z;
                m_MoveDir *= speed;
                
                if (isInWater) //todo improve jumps in water
                {
                    if (isJumpActivated)
                    {
                        // Check if player is next to terrain 
                        if (env.CheckCollision(new Vector3(pos.x + camTransform.forward.x, pos.y, pos.z + camTransform.forward.z)))
                        {
                            m_MoveDir.y = jumpSpeed * 0.5f;
                            isJumpInProgress = true;
                        }
                        isJumpActivated = false;
                    }
                    else
                    {
                        m_MoveDir += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime * 0.5f;
                    }

                    if (m_MoveDir.y <= 0)
                    {
                        m_MoveDir.y += 0.2f * Time.fixedDeltaTime;
                    }
                    else if (pos.y > waterLevelTop)
                    {
                        m_MoveDir.y = 0; // do not exit water
                    }
                    
                    ProgressSwimCycle(m_CharacterController.velocity, swimSpeed);
                }
            }
            else
            {
                // always move along the camera forward as it is the direction that it being aimed at
                Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

                // get a normal for the surface that is being touched to move along it
                Physics.SphereCast(pos, m_CharacterController.radius, Misc.vector3down, out RaycastHit hitInfo,
                    GetCharacterHeight() / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

                if (!hasThrusted)
                {
                    m_MoveDir.x = desiredMove.x * speed;
                    m_MoveDir.z = desiredMove.z * speed;
                }
                
                if (m_CharacterController.isGrounded)
                {
                    hasThrusted = false;
                    m_MoveDir.y = -stickToGroundForce;
                    if (isJumpActivated)
                    {
                        PlayJumpSound();
                        m_MoveDir.y = jumpSpeed;
                        
                        isJumpActivated = false;
                        isJumpInProgress = true;
                    }
                }
                else
                {
                    m_MoveDir += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;
                }

                ProgressStepCycle(m_CharacterController.velocity, speed);
            }


            Vector3 finalMove = m_MoveDir * Time.fixedDeltaTime;
            Vector3 newPos = pos + finalMove;
            bool canMove = true;
            
            if (isPreviouslyGrounded && !isFlying && isCrouched)
            {
                // check if player is beyond the edge
                Ray ray = new Ray(newPos, Misc.vector3down);
                canMove = Physics.SphereCast(ray, 0.3f, 1f);
                // if player can't move, clamp movement along the edge and check again
                if (!canMove)
                {
                    if (Mathf.Abs(m_MoveDir.z) > Mathf.Abs(m_MoveDir.x))
                    {
                        m_MoveDir.x = 0;
                    }
                    else
                    {
                        m_MoveDir.z = 0;
                    }
                    finalMove = m_MoveDir * Time.fixedDeltaTime;
                    newPos = pos + finalMove;
                    ray.origin = newPos;
                    canMove = Physics.SphereCast(ray, 0.3f, 1f);
                }
            }

            // if constructor is enabled, disable any movement if control key is pressed (reserved for special constructor actions)
            if (env.constructorMode && input.GetButton(InputButtonNames.LeftControl))
            {
                canMove = false;
            }
            else if (!m_CharacterController.enabled)
            {
                canMove = false;
            }
            if (canMove && isActiveAndEnabled)
            {
                m_CollisionFlags = m_CharacterController.Move(finalMove);
                // check limits
                if (limitBoundsEnabled)
                {
                    pos = m_CharacterController.transform.position;
                    bool clamp = false;
                    if (pos.x > limitBounds.max.x) { pos.x = limitBounds.max.x; clamp = true; } else if (pos.x < limitBounds.min.x) { pos.x = limitBounds.min.x; clamp = true; }
                    if (pos.y > limitBounds.max.y) { pos.y = limitBounds.max.y; clamp = true; } else if (pos.y < limitBounds.min.y) { pos.y = limitBounds.min.y; clamp = true; }
                    if (pos.z > limitBounds.max.z) { pos.z = limitBounds.max.z; clamp = true; } else if (pos.z < limitBounds.min.z) { pos.z = limitBounds.min.z; clamp = true; }
                    if (clamp)
                    {
                        MoveTo(pos);
                    }

                }

            }
            isGrounded = m_CharacterController.isGrounded;

            // Check limits
            if (orbitMode)
            {
                if (FastVector.ClampDistance(ref lookAt, ref pos, minDistance, maxDistance))
                {
                    m_CharacterController.transform.position = pos;
                }
            }

            mouseLook.UpdateCursorLock();

            if (!isGrounded && !isFlying)
            {
                // Check current chunk
                VoxelChunk chunk = env.GetCurrentChunk();
                if (chunk != null && !chunk.isRendered)
                {
                    WaitForCurrentChunk();
                    return;
                }
            }
        }

        protected Ray GetCameraRay()
        {
            Ray ray;
            if (freeMode || switching)
            {
                ray = m_Camera.ScreenPointToRay(input.screenPos);
            }
            else
            {
                ray = m_Camera.ViewportPointToRay(Misc.vector2half);
            }
            ray.origin = m_Camera.transform.position + ray.direction * 0.3f;
            return ray;
        }


        void SmoothClimb()
        {
            if (!movingSmooth)
            {
                if (crouch.position.y - prevCrouchYPos >= climbYThreshold && !isFlying && !isThrusting)
                {
                    prevCrouchTime = Time.time;
                    movingSmooth = true;
                }
                else
                {
                    prevCrouchYPos = crouch.position.y;
                }
            }

            if (movingSmooth)
            {
                float t = (Time.time - prevCrouchTime) * climbSpeed;
                if (t > 1f)
                {
                    t = 1f;
                    movingSmooth = false;
                    prevCrouchYPos = crouch.position.y;
                }
                UpdateCrouch();
                Vector3 pos = crouch.position;
                pos.y = prevCrouchYPos * (1f - t) + crouch.position.y * t;
                crouch.position = pos;

            }
        }

        protected virtual void GetInput(out float speed)
        {
            float up = 0;
            bool wasRunning = isRunning;
            if (input == null || !input.enabled)
            {
                speed = 0;
                return;
            }

            if (input.GetButton(InputButtonNames.Up))
            {
                up = 1f;
            }
            else if (input.GetButton(InputButtonNames.Down))
            {
                up = -1f;
            }

            bool leftShiftPressed = input.GetButton(InputButtonNames.LeftShift);

            // set the desired speed to be walking or running
            if (isFlying)
            {
                speed = leftShiftPressed ? flySpeed * 2 : flySpeed;
            }
            else if (isInWater)
            {
                speed = swimSpeed;
            }
            else if (isCrouched)
            {
                speed = walkSpeed * 0.25f;
            }
            else if (!leftShiftPressed)
            {
                speed = walkSpeed;
            }
            else
            {
                speed = runSpeed;
            }
            m_Input = new Vector3(input.horizontalAxis, input.verticalAxis, up);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            isMoving = m_CharacterController.velocity.sqrMagnitude > 0;

            isPressingMoveKeys = input.anyAxisButtonPressed;
            if (isPressingMoveKeys)
            {
                isRunning = leftShiftPressed && isMoving;
            }
            else
            {
                isRunning = false;
                if (isGrounded)
                {
                    speed = 0;
                }
            }

            // thrust
            if (manageThrust && input.GetButton(InputButtonNames.Thrust))
            {
                float atmos = 1f / (1.0f + Mathf.Max(0, transform.position.y - thrustMaxAltitude));
                thrustAmount = thrustPower * atmos;
                isThrusting = true;
            }
            else
            {
                thrustAmount = 0;
                isThrusting = false;
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used

            if (useFovKick && isRunning != wasRunning && (isMoving || m_FovKick.isFOVUp))
            {
                StopAllCoroutines();
                StartCoroutine(isRunning ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown(speed == 0 ? 5f : 1f));
            }

        }


        private void RotateView()
        {
            if (switching)
            {
                switchingLapsed = (Time.time - switchingStartTime) / switchDuration;
                if (switchingLapsed > 1f)
                {
                    switchingLapsed = 1f;
                    switching = false;
                }
            }
            else
            {
                switchingLapsed = 1;
            }

            if (input.enabled)
            {
#if UNITY_EDITOR
                if (Input.GetMouseButtonUp(0))
                {
                    mouseLook.SetCursorLock(true);
                    input.focused = true;
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    input.focused = false;
                }
#endif
                if (input.focused)
                {
                    mouseLook.LookRotation(transform, m_Camera.transform, orbitMode, lookAt, switchingLapsed);
                }
            }
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {

            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }
            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }

        /// <summary>
        /// Moves character controller to a new position. Use this method instead of changing the transform position
        /// </summary>
        public override void MoveTo(Vector3 newPosition)
        {
            CheckCharacterController();
            m_CharacterController.enabled = false;
            transform.position = newPosition;
            m_CharacterController.enabled = true;
        }

        /// <summary>
        /// Moves character controller by a distance. Use this method instead of changing the transform position
        /// </summary>
        public override void Move(Vector3 deltaPosition)
        {
            CheckCharacterController();
            m_CharacterController.enabled = false;
            transform.position += deltaPosition;
            m_CharacterController.enabled = true;
        }


    }
}
