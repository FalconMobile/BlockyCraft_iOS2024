using UnityEngine;
using VoxelPlay;
using Mirror;
using System.Collections.Generic;

namespace IslandAdventureBattleRoyale
{

    /// <summary>
    /// Controls skeleton. Check NetworkMob base class for more details.
    /// </summary>
    public class NetworkSkeleton : NetworkMob
    {
        // Weapons
        public ItemDefinition[] allowedMeleeWeapons;
        public ItemDefinition rangedWeapon;
        ItemDefinition meleeWeaponItem;

        ItemDefinition currentItem;
        GameObject currentItemGameObject;
        WeaponType currentWeaponType;

        [SyncVar] ItemCategory currentItemCategory;
        [SyncVar] string currentItemName;

        Transform rightHand;
        readonly Dictionary<string, GameObject> weaponsGameObjects = new Dictionary<string, GameObject>();
        GameObject bowGameObject;

        public int meleeBluntDamage = 20;
        public float meleeAttackDelay = 2;


        // Arrow
        public GameObject Arrow;
        public GameObject ArrowDisplay;
        Transform ArrowDirTransform;
        Transform ArrowDirection;

        // Audio Effects
        public AudioClip hitSoundEffect;
        public AudioClip deathSoundEffect;

        float lastAttackTime;
        int currentWeaponDamage = 10;
        float currentWeaponHitDelay;
        bool isSwimming;
        Vector3 lastSwimPosition;

        readonly static RaycastHit[] hits = new RaycastHit[20];

        public override void InitMob()
        {
            thisAnimator.applyRootMotion = false;

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

            if (rangedWeapon == null)
            {
                rangedWeapon = VoxelPlayEnvironment.GetItemDefinition("Bow");
            }
            if (allowedMeleeWeapons.Length > 0)
            {
                meleeWeaponItem = allowedMeleeWeapons[Random.Range(0, allowedMeleeWeapons.Length)];
            }

            // Arrow setup
            ArrowDirection = Instantiate(ArrowDisplay).transform;
            ArrowDirection.SetParent(rightHand, false);
            ArrowDirection.position = rightHand.position;
            ArrowDirection.rotation = rightHand.rotation;
            ArrowDirection.Rotate(0, -105, 0);
            ArrowDirection.gameObject.SetActive(false);

            ArrowDirTransform = new GameObject("ArrowPointer").transform;
            ArrowDirTransform.SetParent(ArrowDirection, false);
            ArrowDirTransform.position = ArrowDirection.position;
            ArrowDirTransform.rotation = ArrowDirection.rotation;

            SetHandItem(null);
        }


        public override void Disengage(ILivingEntity target)
        {
            if (this.target == target)
            {
                this.target = null;
                SwitchState(MobState.ReturnToStartPosition);
            }
        }

        public override void IncreaseScore(int amount)
        {
        }


        public override void ManageState()
        {
            CheckSwimming();

            switch (mobState)
            {
                case MobState.Idle:
                    {
                        NetworkPlayer nearest = WorldState.Instance.GetNearestPlayer(transform.position);
                        if (nearest != null)
                        {
                            // if player is nearer than 5 meters to cannibal, it will attack him
                            float distance = FastVector.SqrDistanceByValue(transform.position, nearest.transform.position);
                            if (distance < 5 * 5)
                            {
                                target = nearest;
                                SwitchState(MobState.EngagingPlayerMelee);
                                return;
                            }
                        }
                        // If a player is in LOS and within 70 meters, attack him
                        WorldState worldstate = WorldState.Instance;
                        int playersCount = worldstate.NetworkPlayers.Count;
                        for (int k = 0; k < playersCount; k++)
                        {
                            NetworkPlayer player = worldstate.NetworkPlayers[k];
                            if (PlayerInLOS(player, maxEngageDistance))
                            {
                                float distanceSqr = FastVector.SqrDistanceByValue(player.transform.position, transform.position);
                                target = nearest;
                                if (distanceSqr > maxEngageMeleeDistance * maxEngageMeleeDistance)
                                {
                                    SwitchState(MobState.EngagingPlayerRange);
                                }
                                else
                                {
                                    SwitchState(MobState.EngagingPlayerMelee);
                                }
                                return;
                            }
                        }
                    }
                    break;
                case MobState.ReturnToStartPosition:
                    {
                        float distanceSqr = FastVector.SqrDistanceByValue(initialPosition, transform.position);
                        if (distanceSqr > 1)
                        {
                            MoveTo(initialPosition);
                        }
                        else
                        {
                            SwitchState(MobState.Idle);
                        }
                    }
                    break;
                case MobState.EngagingPlayerMelee:
                    if (targetIsValid)
                    {
                        Vector3 targetPosition = target.GetTransform().position + Vector3.up;
                        float distanceToTargetSqr = FastVector.SqrDistanceByValue(transform.position, targetPosition);

                        if (distanceToTargetSqr > maxEngageDistance * maxEngageDistance || !target.isAlive)
                        {
                            SwitchState(MobState.ReturnToStartPosition);
                            return;
                        }

                        if (distanceToTargetSqr > maxEngageMeleeDistance * maxEngageMeleeDistance)
                        {
                            SwitchState(MobState.EngagingPlayerRange);
                            return;
                        }

                        if (currentItem != meleeWeaponItem && meleeWeaponItem != null)
                        {
                            SetHandItem(meleeWeaponItem);
                        }

                        if (distanceToTargetSqr < 8)
                        { // melee attack
                            float now = Time.time;
                            if (now > currentWeaponHitDelay)
                            {
                                thisAnimator.SetFloat(AnimationKeyword.Speed, 0.1f);
                                thisNetworkAnimator.SetTrigger(AnimationKeyword.Attack);
                                Invoke(nameof(DamageTarget), 0.5f);
                            }
                        }
                        else
                        {
                            thisAnimator.SetFloat(AnimationKeyword.Speed, 1f);
                            MoveTo(targetPosition + (transform.position - targetPosition).normalized * 2f);
                        }
                    }
                    break;
                case MobState.EngagingPlayerRange:
                    if (targetIsValid)
                    {
                        Vector3 targetPosition = target.GetTransform().position + Vector3.up;
                        float distanceToTargetSqr = FastVector.SqrDistanceByValue(transform.position, targetPosition);

                        if (distanceToTargetSqr > maxEngageDistance * maxEngageDistance || !target.isAlive)
                        {
                            SwitchState(MobState.ReturnToStartPosition);
                            return;
                        }

                        if (isSwimming || distanceToTargetSqr < maxEngageMeleeDistance * maxEngageMeleeDistance)
                        {
                            SwitchState(MobState.EngagingPlayerMelee);
                            return;
                        }

                        if (currentItem != rangedWeapon)
                        {
                            SetHandItem(rangedWeapon);
                        }

                        MoveTo(targetPosition);
                        float now = Time.time;
                        if (now - lastAttackTime > currentWeaponHitDelay)
                        {
                            lastAttackTime = now;
                            // attack with bow
                            thisNetworkAnimator.SetTrigger(AnimationKeyword.Attack);

                            // Point arrow direction to target and a bit up to compensate gravity
                            ArrowDirTransform.LookAt(targetPosition + Vector3.up);

                            GameObject instancedArrow = Instantiate(Arrow, ArrowDirTransform.position, ArrowDirTransform.rotation);
                            instancedArrow.GetComponent<NetworkArrow>().owner = this;

                            NetworkServer.Spawn(instancedArrow);
                        }
                    }
                    break;
            }
        }

        void CheckSwimming()
        {

            Vector3 currentPosition = transform.position;
            float dist = FastVector.SqrDistanceByValue(lastSwimPosition, currentPosition);
            if (dist < 1) return;
            lastSwimPosition = currentPosition;

            // Check water & swimming state
            if (env.IsWaterAtPosition(transform.position))
            {
                if (!isSwimming)
                {
                    isSwimming = true;
                    thisAnimator.SetBool(AnimationKeyword.Swimming, true);

                }
            }
            else if (isSwimming)
            {
                isSwimming = false;
                thisAnimator.SetBool(AnimationKeyword.Swimming, false);
            }
        }

        bool PlayerInLOS(NetworkPlayer player, float maxDistance)
        {
            Vector3 origin = transform.position + Vector3.up * 1.8f;
            Vector3 targetPos = player.transform.position + Vector3.up;
            Vector3 dir = (targetPos - origin).normalized;
            // check orientation
            float d = Vector3.Angle(dir, transform.forward);
            if (d > 80) return false;
            // check light of sight (LOS)
            Ray ray = new Ray(origin, dir);
            int hitCount = Physics.SphereCastNonAlloc(ray, 1f, hits, maxDistance, 1 << player.gameObject.layer);
            for (int k = 0; k < hitCount; k++)
            {
                NetworkDamageTaker damageTaker = hits[k].collider.GetComponent<NetworkDamageTaker>();
                if (damageTaker != null && damageTaker.entity.GetTransform() == player.transform)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Damages player
        /// </summary>
        void DamageTarget()
        {
            // Pick a random limb to damage
            NetworkDamageTaker damageTaker = target.GetTransform().GetComponentInChildren<NetworkDamageTaker>();
            if (damageTaker != null)
            {
                WorldState.Instance.MobDamagesPlayer(damageTaker, this, currentWeaponDamage);
            }
        }

        void SetHandItem(ItemDefinition item)
        {

            // Set last attack time to now so it can't attack immediately after switching weapon
            lastAttackTime = Time.time;

            // Hide all weapons & arrow
            if (currentItemGameObject != null)
            {
                currentItemGameObject.SetActive(false);
            }
            ArrowDirection.gameObject.SetActive(false);

            // If no weapon name: hide all weapons and go back to normal
            if (item == null)
            {
                currentWeaponDamage = meleeBluntDamage;
                currentWeaponHitDelay = meleeAttackDelay;
                thisAnimator.SetInteger(AnimationKeyword.Weapon, 0);

                currentItemName = null;
                currentItemCategory = ItemCategory.General;
                currentWeaponType = WeaponType.None;

                return;
            }

            currentItem = item;
            currentItemName = item.name;
            currentItemCategory = item.category;
            WeaponType weaponType = Weapon.GetWeaponType(currentItem);

            currentWeaponDamage = currentItem.GetPropertyValue<int>("hitDamage", 1);
            currentWeaponHitDelay = currentItem.GetPropertyValue<float>("hitDelay", 1);

            // Pick Animation layer based on weapon name, set range and damage accordingly
            thisAnimator.SetInteger(AnimationKeyword.Weapon, (int)Weapon.GetWeaponAnimationId(weaponType));

            // Show the selected weapon
            ShowHandGameObject();

            // Send this same message to the clients (mobs are managed on the server)
            RpcShowWeapon(item.category, item.name);
        }

        [ClientRpc]
        void RpcShowWeapon(ItemCategory itemCategory, string itemObjName)
        {
            // Hide all weapons & arrow on client
            if (currentItemGameObject != null)
            {
                currentItemGameObject.SetActive(false);
            }
            ArrowDirection.gameObject.SetActive(false);

            ItemDefinition item = env.GetItemDefinition(itemCategory, itemObjName);
            currentItem = item;
            currentWeaponType = Weapon.GetWeaponType(item);
            ShowHandGameObject();
        }

        void ShowHandGameObject()
        {
            if (currentWeaponType.IsBow())
            {
                currentItemGameObject = bowGameObject;
                currentItemGameObject.SetActive(true);
                ArrowDirection.gameObject.SetActive(true);
                return;
            }

            if (currentItem == null)
            {
                currentItemGameObject.gameObject.SetActive(true);
                return;
            }
            
            if (!weaponsGameObjects.TryGetValue(currentItem.name, out currentItemGameObject))
            {
                currentItemGameObject = Instantiate(currentItem.iconPrefab);
                currentItemGameObject.transform.SetParent(rightHand.transform, false);
                currentItemGameObject.transform.localPosition = Vector3.zero;
                currentItemGameObject.transform.localRotation = Quaternion.identity;
                weaponsGameObjects[currentItem.name] = currentItemGameObject;
            }
            else
            {
                currentItemGameObject.gameObject.SetActive(true);
            }
        }


        /// <summary>
        /// Animation events, called by the animator
        /// </summary>
        // Triggered when Mob grabs arrow
        public void LoadArrow()
        {
            if (ArrowDirection != null)
            {
                ArrowDirection.gameObject.SetActive(true);
            }
        }

        // Triggered when Mob's Attack begins                   
        public void AttackStartEvent()
        {   // Hide display arrow 
            if (ArrowDirection != null)
            {
                ArrowDirection.gameObject.SetActive(false);
            }
            if (!isServerOnly)
            {
                ItemDefinition item = VoxelPlayEnvironment.GetItemDefinition(currentItemName);
                if (item != null && item.useSound != null)
                {
                    thisAudioSource.PlayOneShot(item.useSound);
                }
            }
        }

        // Triggered when Mob's Hit Animation starts (receives a hit)
        public void AttackHitEvent()
        {

        }

        // Triggered when Mob's Hit Animation starts (receives a hit)
        public void HitEvent()
        {
            if (!isServerOnly)
            {
                thisAudioSource.PlayOneShot(hitSoundEffect);
            }
        }

        // Triggered when Mob's Death Animation starts
        public void DeathEvent()
        {
            if (!isServerOnly)
            {
                thisAudioSource.PlayOneShot(deathSoundEffect);
            }
        }

        // Let the cannibal hear the sound if player shoots near
        public override void NotifyLoudSound(Vector3 soundSourcePosition, ILivingEntity shooter)
        {
            if (mobState.InCombat()) return;

            if (shooter is NetworkPlayer)
            {
                const float AWARENESS_DISTANCE_SQR = 50 * 50;
                float sqrDistance = (transform.position - soundSourcePosition).sqrMagnitude;
                if (sqrDistance < AWARENESS_DISTANCE_SQR)
                {
                    target = shooter;
                    SwitchState(MobState.EngagingPlayerMelee);
                }
            }
        }
    }

}
