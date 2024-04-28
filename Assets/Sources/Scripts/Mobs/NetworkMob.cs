using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using VoxelPlay;


    public enum MobState
    {
        Idle,
        EngagingPlayerMelee,
        EngagingPlayerRange,
        ReturnToStartPosition,
        Flying,
        Hunting,
        Fleeing
    }

    public static class MobStateExtensions
    {
        public static bool InCombat(this MobState state)
        {
            return state == MobState.EngagingPlayerMelee || state == MobState.EngagingPlayerRange;
        }
    }

/// <summary>
/// Base class for controlling any mob. All mob logic runs ONLY ON SERVER this script will be disabled when gameobject is started on a client.
/// </summary>
public abstract class NetworkMob : NetworkBehaviour, ILivingEntity
{

    public Transform GetTransform() => transform;

    /// <summary>
    /// Interval in seconds when this mob takes decisions
    /// </summary>
    public float updateInterval = 1f;

    public int maxHealth = 100;

    [Header("Scoring")]
    public int scoreByKill = 100;
    public int scoreByHit = 10;
    public int GetScoreByKill() { return scoreByKill; }
    public int GetScoreByHit() { return scoreByHit; }

    [SyncVar]
    int health = 100;

    public Loot[] loot;

    public float GetHealthPercentage() { return Mathf.Clamp01((float)health / maxHealth); }

    public string screenName = "Mob";

    public string GetScreenName() { return screenName; }

    public bool isAlive => health > 0;

    /// <summary>
    /// Current state of mob
    /// </summary>
    [NonSerialized] public MobState mobState = MobState.Idle;

    /// <summary>
    /// Target player
    /// </summary>
    [NonSerialized] public ILivingEntity target;

    // Since target is of type interface, C# will test this object for base null and won't use UnityEngine.Object == null override which checks if object is destroyed
    protected bool targetIsValid => ((UnityEngine.Object)target) != null;

    /// <summary>
    /// If player is further than this distance, mob will ignore it
    /// </summary>
    public float maxEngageDistance = 50;

    public float maxMeleeDamageDistance = 3.5f;

    /// <summary>
    /// Distance at which mob switches to long range weapon
    /// </summary>
    public float maxEngageMeleeDistance = 10;

    public bool usesNavMesh;


    /// <summary>
    /// Can be used to setup setuff when mob is initialized
    /// </summary>
    public virtual void InitMob() { }

    /// <summary>
    /// Mob classes need to implement this to perform logic
    /// </summary>
    public abstract void ManageState();

    /// <summary>
    /// Called when mob must disengage target (ie. target is destroyed)
    /// </summary>
    public virtual void Disengage(ILivingEntity target) { }

    /// <summary>
    /// Called when mob hits something
    /// </summary>
    public virtual void IncreaseScore(int amount) { }

    /// <summary>
    /// User defined tag for this mob
    /// </summary>
    public string userTag;


    protected Vector3 initialPosition;
    protected Animator thisAnimator;
    protected NetworkAnimator thisNetworkAnimator;
    protected Rigidbody thisRigidBody;
    protected NavMeshAgent agent;
    protected VoxelPlayEnvironment env;
    protected AudioSource thisAudioSource;
    protected bool isReady;
    float lastStateChange;

    IEnumerator Start()
    {

        env = VoxelPlayEnvironment.instance;
        health = maxHealth;
        thisAnimator = GetComponent<Animator>();
        thisNetworkAnimator = GetComponent<NetworkAnimator>();
        thisRigidBody = GetComponentInChildren<Rigidbody>();
        if (thisRigidBody)
        {
            thisRigidBody.isKinematic = true;
        }
        thisAudioSource = GetComponent<AudioSource>();
        initialPosition = transform.position;
        InitMob();

        if (!isServer)
        {
            enabled = false;
            yield break;
        }
        else
        {
            WorldState.Instance.RegisterMob(this);
        }

        if (thisRigidBody == null)
        {
            thisRigidBody = gameObject.AddComponent<Rigidbody>();
            thisRigidBody.isKinematic = true;
        }

        isReady = false;
        WaitForSeconds waitOneSecond = new WaitForSeconds(1f);
        if (usesNavMesh)
        {
            // wait for the chunk under mob to render
            VoxelChunk chunk = null;
            while (chunk == null || !chunk.isRendered || !env.ChunkHasNavMeshReady(chunk))
            {
                {
                    env.GetChunk(transform.position, out chunk, true);
                    yield return waitOneSecond;
                }
            }

            // wait for the navmesh to be ready
            agent = gameObject.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }
            while (!agent.isOnNavMesh)
            {
                yield return waitOneSecond;

            }
        }

        isReady = true;

        InvokeRepeating(nameof(DoSomething), UnityEngine.Random.value * updateInterval, updateInterval);
    }


    void OnDestroy()
    {
        if (WorldState.Instance != null)
        {
            WorldState.Instance.UnregisterMob(this);
        }
    }

    void DoSomething()
    {
        if (!isAlive)
        {
            CancelInvoke();
            return;
        }

        // Do something
        ManageState();

    }


    /// <summary>
    /// Switches mob state
    /// </summary>
    protected void SwitchState(MobState newState)
    {
        if (mobState == newState) return;

        float now = Time.time;
        if (now - lastStateChange < 1f) return;

        lastStateChange = now;
        mobState = newState;
        switch (mobState)
        {
            case MobState.Idle:
                thisAnimator.SetFloat(AnimationKeyword.Speed, 0);
                break;
            case MobState.Fleeing:
                thisAnimator.SetFloat(AnimationKeyword.Speed, 1f);
                break;
            case MobState.EngagingPlayerMelee:
                thisAnimator.SetFloat(AnimationKeyword.Speed, 1f);
                break;
            case MobState.EngagingPlayerRange:
                thisAnimator.SetFloat(AnimationKeyword.Speed, 0.5f);
                break;
            case MobState.ReturnToStartPosition:
                thisAnimator.SetFloat(AnimationKeyword.Speed, 0.5f);
                break;
            case MobState.Hunting:
                thisAnimator.SetFloat(AnimationKeyword.Speed, 0.5f);
                break;
        }

        if (agent != null)
        {
            // reset default values
            agent.speed = 3.5f;
            agent.angularSpeed = 120;
        }

        // Quickly react to the new state. To prevent recursive loops we only allow this if the last state change was 1 second or older
        ManageState();
    }


    /// <summary>
    /// Called when this mob receives damage
    /// </summary>
    public virtual void ReceiveDamage(ILivingEntity attacker, int damage)
    {
        if (health <= 0) return;

        health -= damage;
        if (!isReady)
        {
            health = 0;
        }
        if (health <= 0)
        {
            // trigger death animation
            thisNetworkAnimator.SetTrigger(AnimationKeyword.Death);

            if (thisRigidBody) thisRigidBody.isKinematic = false;

            // stop agent and any current movement
            if (agent != null)
            {
                agent.enabled = false;
            }

            DropLoot();

            StartCoroutine(BecomeDust());
        }
        else
        {
            thisNetworkAnimator.SetTrigger(AnimationKeyword.Hit);
            target = attacker;
            SwitchState(MobState.EngagingPlayerMelee);
        }
    }

    protected bool CheckDistance()
    {
        Vector3 targetPosition = target.GetTransform().position + Vector3.up;
        float distanceToTargetSqr = FastVector.SqrDistanceByValue(transform.position, targetPosition);

        if (distanceToTargetSqr > maxMeleeDamageDistance * maxMeleeDamageDistance)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Called to trigger the NavMeshAgent to move to a destination
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }


    /// <summary>
    /// When mob dies, it drops loot
    /// </summary>
    public void DropLoot()
    {
        if (loot == null) return;
        for (int i = 0; i < loot.Length; i++)
        {
            if (UnityEngine.Random.value < loot[i].probability)
            {
                int quantity = UnityEngine.Random.Range(loot[i].minAmount, loot[i].maxAmount + 1);
                if (quantity > 0)
                {

                    ItemDefinition item = loot[i].item;
                    if (item == null) continue;

                    // Get a forward vector and multiply by 2 so we can spawn 2 meters away from player
                    Vector3 positionToDrop = Vector3.forward * 2;

                    // Rotate the vector around the Up axis, by a fraction of 360
                    positionToDrop = Quaternion.AngleAxis(UnityEngine.Random.value * 360f, Vector3.up) * positionToDrop;

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

        }
    }

    [Server]
    public virtual void NotifyLoudSound(Vector3 soundSourcePosition, ILivingEntity shooter)
    {
    }

    IEnumerator BecomeDust()
    {
        yield return new WaitForSeconds(10);
        if (thisRigidBody != null)
        {
            thisRigidBody.isKinematic = true;
            for (int k = 0; k < 100; k++)
            {
                thisRigidBody.position -= new Vector3(0, Time.deltaTime, 0);
                yield return null;
            }
            NetworkServer.Destroy(gameObject);
        }
    }


}