using UnityEngine;
using Mirror;
using VoxelPlay;
using UnityEngine.UI;
using System;
using System.Collections;
using Sources.Scripts;
using TMPro;


/// <summary>
/// Main player script, contains most methods needed for player movement and interaction
/// </summary>
public partial class NetworkPlayer : NetworkBehaviour, ILivingEntity
{
    public static event Action<NetworkPlayer> PlayerDead = delegate { };

    // Game state
    [SyncVar] public string playerName = "";

    public string GetScreenName()
    {
        return playerName;
    }

    // Voxel Play
    VoxelPlayEnvironment env;
    IVoxelPlayPlayer VPPlayer;
    VoxelPlayFirstPersonController VPFPP;

    // Animation
    [Header("Character")] public Transform head;
    public Transform hat;
    public Transform hair;
    public Transform cameraRotationAnchor, cameraAnchor;
    Animator thisAnimator;
    NetworkAnimator thisNetworkAnimator;
    public Transform GetTransform() => transform;

    // Movement
    float inpVer;
    float sprint = 0.5f;

    // Audio
    [Header("Sounds")] public AudioClip hitSoundEffect;
    public AudioClip deathSoundEffect;
    public AudioClip BowReload;
    AmbientSoundMixer ambientSoundMixer;
    AudioSource thisAudioSource;

    // Prefabs
    [Header("Prefabs")] public GameObject MuzzleFlash;
    public GameObject BloodParticles;

    // Arrow
    public GameObject Arrow;
    public GameObject ArrowDisplay;
    Transform arrowDirTransform;

    // Spine
    Vector3 targetPos;
    [NonSerialized] public Vector3 spineAdjustment;
    [NonSerialized] public Transform spine;
    Transform spineRotationSync;

    // Other
    Transform rightHand;
    Transform cam;
    CharacterController thisCharacterController;
    NetworkManager networkManager;
    float respawnTime;
    bool isSwimming;
    Vector3 lastSwimPosition;
    bool gameOver;

    // Indicators
    GameObject targetLifebarIndicatorPanel, targetLifebarAmountPanel;
    GameObject headshotTextGO;
    Text targetNameText;

    public static int devRespawnTime = 15;


    #region Gameloop main events

    /// <summary>
    /// Register player in WorldState manager
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        WorldState.Instance.RegisterPlayer(this);
    }

    /// <summary>
    /// Unregister player from WorldState
    /// </summary>
    public override void OnStopServer()
    {
        if (WorldState.Instance != null)
        {
            WorldState.Instance.UnregisterPlayer(this);
        }

        base.OnStopServer();
    }


    private void Start()
    {
        // Get Camera for raycasting
        cam = Camera.main.transform;

        // Get First Person Controller
        VPFPP = GetComponentInChildren<VoxelPlayFirstPersonController>();

        // Get network manager reference
        networkManager = FindObjectOfType<NetworkManager>();

        // Get AudioSource
        thisAudioSource = GetComponent<AudioSource>();
        if (thisAudioSource == null)
        {
            thisAudioSource = gameObject.AddComponent<AudioSource>();
            thisAudioSource.spatialBlend = 1;
        }

        // VoxelPlay Environment instance
        env = VoxelPlayEnvironment.instance;

        // Fetch hand voxel item definition
        handVoxel = VoxelPlayEnvironment.GetItemDefinition("HandVoxel");

        // Find weapons attached to character, hide them and put them in an array
        InitializeCharacter();

        // If this is the local player, use the VoxelPlay controller
        if (isLocalPlayer)
        {
            VPFPP.enabled = true;

            // Prepare screen damage indicator (used when receiving damage)
            Transform ui = GameObject.Find("GameUICanvas").transform;
            damageIndicator = ui.Find("DamageIndicator").GetComponent<Image>();
            damageMaterial = Instantiate(damageIndicator.material); // avoid modifying the material asset
            damageIndicator.material = damageMaterial;
            tempDamageColor = Color.white;
            tempDamageColor.a = 0;
            damageMaterial.SetColor("_Color", tempDamageColor);
            damageIndicator.gameObject.SetActive(true);

            // Show player name
            Transform playerNameDisplay = ui.Find("PlayerName");
            playerNameDisplay.GetComponent<Text>().text = playerName;

            // Activates Health and Score screen indicators
            Transform heart = ui.Find("Health");
            heart.gameObject.SetActive(true);
            healthDisplay = heart.Find("HealthAmount").GetComponent<Text>();
            healthDisplay.text = health.ToString();
            scoreDisplay = ui.Find("Score").GetComponentInChildren<TextMeshProUGUI>();
            scoreDisplay.gameObject.SetActive(true);
            topPlayersDisplay = ui.Find("TopPlayers").GetComponent<Text>();
            CmdResetScore();

            // Prepares target remaining life screen indicator
            targetLifebarIndicatorPanel = ui.Find("LifebarIndicator").gameObject;
            targetLifebarAmountPanel = targetLifebarIndicatorPanel.transform.Find("Lifebar/LifebarAmount").gameObject;
            targetNameText = targetLifebarIndicatorPanel.transform.Find("TargetName").GetComponent<Text>();
            headshotTextGO = targetLifebarIndicatorPanel.transform.Find("Headshot").gameObject;

            //gameOverMenu = FindObjectOfType<IngameMenu>(); TO DO

            // Initializes player inventory
            VPPlayer = VoxelPlayPlayer.instance;
            VPPlayer.ConsumeAllItems();
            
            env.OnInitialized += () =>
            {
                if (RandomWorld.isNewGame)
                {
                    SaveAndLoadingInventory.LoadInitial(VPPlayer, quickItems, initialItems);
                }
                else
                {
                    SaveAndLoadingInventory.Loading(VPPlayer, quickItems, initialItems);
                }

                VoxelPlayUI.instance.RefreshInventoryContents();

                VPPlayer.SetSelectedItem(defaultSlot);
                // Sets starting weapon
                VPPlayer.OnItemSelectedChanged += OnItemSelectedChanged;
                SetHandItem(VPPlayer.GetSelectedItem().item);

                // Initializes other manager references
                ambientSoundMixer = FindObjectOfType<AmbientSoundMixer>();
                ambientSoundMixer.PlayAmbience(transform);

                // disabled the character controller until chunk is ready
                StartCoroutine(WaitForChunk());


                // Capture commands on client console
                VoxelPlayUI.instance.OnConsoleNewCommand += OnConsoleNewCommand;
            };
        }
        else
        {
            ShowCorrentHandItem(currentItemCategory, currentItemName);
        }

        thisCharacterController = GetComponent<CharacterController>();

        ShowTargetDamageIndicator();
    }


    IEnumerator WaitForChunk()
    {
        CharacterController cc = GetComponentInChildren<CharacterController>();
        cc.enabled = false;

        VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
        VoxelChunk chunk = env.GetChunk(transform.position, true);
        WaitForSeconds w = new WaitForSeconds(0.5f);
        do
        {
            yield return w;
        } while (!chunk.isRendered);

        cc.enabled = true;
    }


    void LateUpdate()
    {
        // If we aren't the local player do nothing
        if (!isLocalPlayer)
        {
            spine.rotation = spineRotationSync.rotation;
            return;
        }

        if (isAlive)
        {
            if (gameOver)
            {
                gameOver = false;

                //gameOverMenu.ReSpawn(); TO DO
                RespawnCharacter();
                thisNetworkAnimator.SetTrigger(AnimationKeyword.Respawn);
                return;
            }

            CheckSwimming();

            // Spine rotation must be performed before calling other Commands to server so this rotation is performed before the commands actually execute on server
            // Moving this code after the command for firing weapon will cause sync issues with firing anchor for the muzzle
            spine.rotation = cam.rotation;

            // When looking at a target, the rotation of the spine is altered - we fix it so the orientation looks correct after setting the aim target
            spine.Rotate(spineAdjustment);

            // Keep spine sync node rotation same so spine rotation propagates to other clients correctly
            spineRotationSync.rotation = spine.rotation;

            // Adjust camera position so it's aligned with head by rotating its rotation anchor
            Vector3 camAnchorPosition = cameraAnchor.position;
            Vector3 rotationAnchorPosition = cameraRotationAnchor.position;
            Quaternion rot = cam.rotation;
            var v = camAnchorPosition - rotationAnchorPosition;
            v = rot * v;
            cam.position = v + rotationAnchorPosition;
        }
        else
        {
            if (!gameOver)
            {
                // Disable Damage Effect
                damageIndicator.enabled = false;

                // Animate death
                thisNetworkAnimator.SetTrigger(AnimationKeyword.Death);

                // Zero out health display
                healthDisplay.text = "0";

                // Set Respawn time
                respawnTime = 15;
#if UNITY_EDITOR
                respawnTime = devRespawnTime;
#endif

                // Make sure this is only called once
                gameOver = true;

                // Disable VPFPP so playe can't move dead
                VPFPP.enabled = false;

                // Drop weapons
                if (WorldState.Instance.DropInventoryOnDeath)
                {
                    DropInventory();
                }

                PlayerDead?.Invoke(this);
                return;
            }

            // Countdown time to respawn - this is local
            respawnTime = Mathf.Max(0, respawnTime - Time.deltaTime);

            // Update time to respawn display
            //gameOverMenu.UpdateCountdown(respawnTime); TO DO

            // When time reaches zero, respawn
            return;
        }

        // If hit display damage indicator, fading with time
        if (showingDamageEffect)
        {
            tempDamageColor.a -= Time.deltaTime;
            damageMaterial.SetColor("_Color", tempDamageColor);
            damageTimer -= Time.deltaTime;
            if (damageTimer <= 0)
            {
                damageIndicator.enabled = false;
                showingDamageEffect = false;
            }
        }

        // Show enemy remaining life indicator
        ShowTargetDamageIndicator();

        ManageInput();

        // This gets automatically synchronized
        thisAnimator.SetFloat(AnimationKeyword.Speed, inpVer * sprint);
    }

    void OnDestroy()
    {
        if (VPPlayer != null)
        {
            VPPlayer.OnItemSelectedChanged -= OnItemSelectedChanged;
        }
    }

    #endregion // gameloop


    #region Input handling

    void ManageInput()
    {
        VoxelPlayInputController input = env.input;
        if (!input.enabled)
        {
            return;
        }

        // Unarm
        if (Input.GetKeyDown(KeyCode.Z))
        {
            UnSelectItem();
        }

        // Get input to apply to animator for animation display
        inpVer = input.verticalAxis;

        // Smooth sprint
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (sprint < 1) sprint += Time.deltaTime;
        }
        else
        {
            if (sprint > 0.5f) sprint -= Time.deltaTime;
        }

        if (input.GetButtonDown(InputButtonNames.Jump))
        {
            // This only triggers the flailing like animation, need to extend it and make it loop
            // then stop it upon landing
            thisNetworkAnimator.SetTrigger(AnimationKeyword.Jump);
        }

        if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(-1))
        {
            // Primary Attack
            if (input.GetButtonDown(InputButtonNames.Button1))
            {
                ExecutePrimaryAttack();
            }

            // Throw items
            if (input.GetButtonDown(InputButtonNames.Button2))
            {
                ThrowCurrentItem();

                //Build(); wtf legacy shit
            }

            // Destroy blocks
            if (input.GetButtonDown(InputButtonNames.Destroy))
            {
                ExecuteEntityRaycast();
            }
        }
    }

    #endregion // input


    // Finds all weapons, hides them and puts them in an array
    void InitializeCharacter()
    {
        // Animator controls animations, but we also need NetworkAnimator for Trigger synchronization since Mirror requires SetTrigger to be called on NetworkAnimator
        thisAnimator = GetComponentInChildren<Animator>();
        thisNetworkAnimator = GetComponent<NetworkAnimator>();

        // We use the motion from VoxelPlay controller, we don't want the animator to affect this
        thisAnimator.applyRootMotion = false;

        // Audiosource for SoundEffects
        thisAudioSource = GetComponent<AudioSource>();

        // Fetch weapons from model (they're child gameobjects)
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (rightHand == null && "Biped R Hand".Equals(allTransforms[i].name))
            {
                rightHand = allTransforms[i];
            }

            if (bowGameObject == null && "Bow".Equals(allTransforms[i].name))
            {
                bowGameObject = allTransforms[i].gameObject;
                bowGameObject.SetActive(false);
            }
        }

        // If we still don't have a right hand, assing the Humanoid.RightHand so at least we can run
        if (rightHand == null) rightHand = thisAnimator.GetBoneTransform(HumanBodyBones.RightHand);

        // Arrow setup
        arrowDirection = Instantiate(ArrowDisplay).transform;
        arrowDirection.SetParent(rightHand, false);
        arrowDirection.position = rightHand.position;
        arrowDirection.rotation = rightHand.rotation;
        arrowDirection.Rotate(0, -105, 0);
        arrowDirection.gameObject.SetActive(false);

        arrowDirTransform = new GameObject("ArrowPointer").transform;
        arrowDirTransform.SetParent(arrowDirection, false);
        arrowDirTransform.position = arrowDirection.position;
        arrowDirTransform.rotation = arrowDirection.rotation;

        // If we are the local player hide the head and hat
        for (int i = 0; i < allTransforms.Length; i++)
        {
            string objName = allTransforms[i].name;
            if ("Biped Spine1".Equals(objName))
            {
                spine = allTransforms[i];
            }
        }

        // If we still don't have a Spine transform, assing the Humanoid.Chest so at least we can run
        if (spine == null) spine = thisAnimator.GetBoneTransform(HumanBodyBones.Chest);

        // Our character animates but we also modify the spine rotation so he aims targets
        // this works in local mode as usual but Mirror has conflicts with NetworkTransformChild and Animator so we have to sync the rotation of the spine using an object outside the animation skeleton
        NetworkTransformChild ntc = GetComponent<NetworkTransformChild>();
        if (ntc != null)
        {
            spineRotationSync = ntc.target;
        }
        else
        {
            spineRotationSync = spine;
        }


        // Setup head and other utility animation stuff
        if (isLocalPlayer)
        {
            if (head != null)
            {
                head.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            if (hat != null)
            {
                hat.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            if (hair != null)
            {
                hair.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            if (cameraAnchor != null)
            {
                cam.transform.SetParent(cameraAnchor, false);
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localRotation = Quaternion.identity;
            }

            thisAnimator.SetBool(AnimationKeyword.IsFPS, true);
        }
    }


    void CheckSwimming()
    {
        // Check water & swimming state
        if (VPFPP.isSwimming || VPFPP.isUnderwater)
        {
            if (!isSwimming)
            {
                isSwimming = true;
                thisAnimator.SetBool(AnimationKeyword.Swimming, true);
                UnSelectItem();
            }
        }
        else if (isSwimming)
        {
            isSwimming = false;
            thisAnimator.SetBool(AnimationKeyword.Swimming, false);
            UnSelectItem();
        }
    }
}