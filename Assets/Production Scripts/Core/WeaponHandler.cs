using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using System.Linq;
using UnityEngine.InputSystem;
using Vi.Utility;
using Vi.Core.VFX;
using Vi.Core.CombatAgents;
using Vi.ProceduralAnimations;

namespace Vi.Core
{
    [DisallowMultipleComponent]
    public class WeaponHandler : NetworkBehaviour
    {
        private Dictionary<Weapon.WeaponBone, RuntimeWeapon> weaponInstances = new Dictionary<Weapon.WeaponBone, RuntimeWeapon>();
        private List<ShooterWeapon> shooterWeapons = new List<ShooterWeapon>();

        public Weapon GetWeapon() { return weaponInstance; }

        public Dictionary<Weapon.WeaponBone, RuntimeWeapon> GetWeaponInstances() { return weaponInstances; }

        private Weapon weaponInstance;
        private CombatAgent combatAgent;
        private MovementHandler movementHandler;
        private LoadoutManager loadoutManager;

        private void Awake()
        {
            combatAgent = GetComponent<CombatAgent>();
            movementHandler = GetComponent<MovementHandler>();
            loadoutManager = GetComponent<LoadoutManager>();
            weaponInstance = ScriptableObject.CreateInstance<Weapon>();
            CurrentActionClip = ScriptableObject.CreateInstance<ActionClip>();
            RefreshStatus();
        }

        public bool WeaponInitialized { get; private set; }
        public AnimatorOverrideController AnimatorOverrideControllerInstance { get; private set; }
        public void SetNewWeapon(Weapon weapon, AnimatorOverrideController animatorOverrideController)
        {
            if (IsOwner) { aiming.Value = false; }

            weaponInstance = weapon;
            AnimatorOverrideControllerInstance = Instantiate(animatorOverrideController);
            StartCoroutine(SwapAnimatorController());
            EquipWeapon();
            WeaponInitialized = true;
        }

        private IEnumerator SwapAnimatorController()
        {
            yield return new WaitUntil(() => !combatAgent.AnimationHandler.IsAiming());
            combatAgent.AnimationHandler.Animator.runtimeAnimatorController = AnimatorOverrideControllerInstance;
        }

        private List<PooledObject> stowedWeaponInstances = new List<PooledObject>();
        public void SetStowedWeapon(Weapon weapon)
        {
            if (!weapon) { return; }
            foreach (PooledObject g in stowedWeaponInstances)
            {
                ObjectPoolingManager.ReturnObjectToPool(g);
            }
            stowedWeaponInstances.Clear();

            foreach (Weapon.WeaponModelData data in weapon.GetWeaponModelData())
            {
                if (data.skinPrefab.name == combatAgent.AnimationHandler.LimbReferences.name.Replace("(Clone)", ""))
                {
                    foreach (Weapon.WeaponModelData.Data modelData in data.data)
                    {
                        PooledObject instance = ObjectPoolingManager.SpawnObject(modelData.weaponPrefab.GetComponent<PooledObject>(), combatAgent.AnimationHandler.LimbReferences.GetStowedWeaponParent());
                        instance.GetComponent<RuntimeWeapon>().SetIsStowed(true);
                        instance.transform.localPosition = modelData.stowedWeaponPositionOffset;
                        instance.transform.localRotation = Quaternion.Euler(modelData.stowedWeaponRotationOffset);
                        stowedWeaponInstances.Add(instance);
                    }
                }
            }
        }

        public bool ShouldUseAmmo()
        {
            if (weaponInstance != null) { return weaponInstance.ShouldUseAmmo(); }
            return false;
        }

        public int GetAmmoCount()
        {
            return loadoutManager.GetAmmoCount(weaponInstance);
        }

        public int GetMaxAmmoCount()
        {
            if (weaponInstance != null) { return weaponInstance.GetMaxAmmoCount(); }
            return 0;
        }

        public void UseAmmo()
        {
            loadoutManager.UseAmmo(weaponInstance);
        }

        private void EquipWeapon()
        {
            foreach (KeyValuePair<Weapon.WeaponBone, RuntimeWeapon> kvp in weaponInstances)
            {
                ObjectPoolingManager.ReturnObjectToPool(kvp.Value.GetComponent<PooledObject>());
            }
            weaponInstances.Clear();
            shooterWeapons.Clear();

            CanAim = false;
            CanADS = false;

            bool broken = false;
            foreach (Weapon.WeaponModelData data in weaponInstance.GetWeaponModelData())
            {
                if (data.skinPrefab.name == combatAgent.AnimationHandler.LimbReferences.name.Replace("(Clone)", ""))
                {
                    foreach (Weapon.WeaponModelData.Data modelData in data.data)
                    {
                        Transform bone = null;
                        switch (modelData.weaponBone)
                        {
                            case Weapon.WeaponBone.Root:
                                bone = transform;
                                break;
                            default:
                                if (combatAgent.AnimationHandler.Animator.avatar.isHuman)
                                {
                                    bone = combatAgent.AnimationHandler.Animator.GetBoneTransform((HumanBodyBones)modelData.weaponBone);
                                }
                                else
                                {
                                    bone = combatAgent.AnimationHandler.LimbReferences.GetBoneTransform(modelData.weaponBone);
                                }
                                break;
                        }

                        RuntimeWeapon instance = null;
                        if (modelData.weaponPrefab.TryGetComponent(out PooledObject pooledObject))
                        {
                            instance = ObjectPoolingManager.SpawnObject(pooledObject, bone).GetComponent<RuntimeWeapon>();
                        }

                        if (!instance)
                        {
                            Debug.LogError(modelData.weaponPrefab + " does not have a runtime weapon component!");
                            continue;
                        }

                        instance.SetWeaponBone(modelData.weaponBone);
                        weaponInstances.Add(modelData.weaponBone, instance);
                        //instance.transform.localScale = modelData.weaponPrefab.transform.localScale;

                        instance.transform.localPosition = modelData.weaponPositionOffset;
                        instance.transform.localRotation = Quaternion.Euler(modelData.weaponRotationOffset);

                        if (instance.TryGetComponent(out ShooterWeapon shooterWeapon))
                        {
                            shooterWeapons.Add(shooterWeapon);
                            CanAim = true;
                            CanADS = shooterWeapon.CanADS() | CanADS;
                        }
                    }
                    broken = true;
                    break;
                }
            }

            if (!broken)
            {
                Debug.LogError("Could not find a weapon model data element for this skin: " + GetComponentInChildren<LimbReferences>().name + " on this melee weapon: " + this);
            }

            if (!CanAim & combatAgent.AnimationHandler.IsAiming()) { combatAgent.AnimationHandler.LimbReferences.OnCannotAim(); }

            combatAgent.AnimationHandler.LimbReferences.SetMeleeVerticalAimEnabled(!CanAim);

            List<RuntimeWeapon> runtimeWeaponList = weaponInstances.Values.ToList();
            foreach (RuntimeWeapon runtimeWeapon in runtimeWeaponList)
            {
                runtimeWeapon.SetAssociatedRuntimeWeapons(runtimeWeaponList);
            }
        }

        public void PlayFlashAttack()
        {
            ActionClip flashAttack = weaponInstance.GetFlashAttack();
            if (flashAttack != null)
            {
                if (flashAttack.GetClipType() == ActionClip.ClipType.FlashAttack)
                {
                    combatAgent.AnimationHandler.PlayAction(flashAttack);
                }
                else
                {
                    Debug.LogError("Attempting to play a flash attack, but the clip isn't set to be a flash attack! " + flashAttack);
                }
            }
        }

        public ActionClip CurrentActionClip { get; private set; }
        private string currentActionClipWeapon;

        private const float abilityCancelledDuringAnticipationCooldownReductionPercent = 0.1f;

        private List<int> actionSoundEffectIdTracker = new List<int>();

        public void SetActionClip(ActionClip actionClip, string weaponName)
        {
            if (actionClip.GetClipType() == ActionClip.ClipType.Flinch) { return; }

            if (IsInAnticipation)
            {
                if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Ability)
                {
                    weaponInstance.ReduceAbilityCooldownTime(CurrentActionClip, abilityCancelledDuringAnticipationCooldownReductionPercent);
                }
            }

            CurrentActionClip = actionClip;
            currentActionClipWeapon = weaponName;
            foreach (KeyValuePair<Weapon.WeaponBone, RuntimeWeapon> kvp in weaponInstances)
            {
                kvp.Value.ResetHitCounter();
            }

            if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Ability)
            {
                weaponInstance.StartAbilityCooldown(CurrentActionClip);
            }
            else if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Dodge)
            {
                weaponInstance.StartDodgeCooldown();
            }

            actionVFXTracker.Clear();
            actionSoundEffectIdTracker.Clear();

            // Action sound effect logic here for normalized time of 0
            foreach (ActionClip.ActionSoundEffect actionSoundEffect in CurrentActionClip.GetActionClipSoundEffects(combatAgent.GetRaceAndGender(), actionSoundEffectIdTracker))
            {
                if (Mathf.Approximately(0, actionSoundEffect.normalizedPlayTime))
                {
                    AudioManager.Singleton.PlayClipOnTransform(transform, actionSoundEffect.audioClip, false, ActionClip.actionClipSoundEffectVolume);
                    actionSoundEffectIdTracker.Add(actionSoundEffect.id);
                }
            }

            if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Dodge | CurrentActionClip.GetClipType() == ActionClip.ClipType.HitReaction)
            {
                ResetComboSystem();
            }
            else if (CurrentActionClip.GetClipType() == ActionClip.ClipType.LightAttack)
            {
                inputHistory.Add(Weapon.InputAttackType.LightAttack);
            }
            else if (CurrentActionClip.GetClipType() == ActionClip.ClipType.FlashAttack)
            {
                ResetComboSystem();
                inputHistory.Add(Weapon.InputAttackType.LightAttack);
            }
            else if (CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                inputHistory.Add(Weapon.InputAttackType.HeavyAttack);
            }
            else if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Ability)
            {
                if (CurrentActionClip == weaponInstance.GetAbility1())
                {
                    inputHistory.Add(Weapon.InputAttackType.Ability1);
                }
                else if (CurrentActionClip == weaponInstance.GetAbility2())
                {
                    inputHistory.Add(Weapon.InputAttackType.Ability2);
                }
                else if (CurrentActionClip == weaponInstance.GetAbility3())
                {
                    inputHistory.Add(Weapon.InputAttackType.Ability3);
                }
                else if (CurrentActionClip == weaponInstance.GetAbility4())
                {
                    inputHistory.Add(Weapon.InputAttackType.Ability4);
                }
            }

            if (IsServer)
            {
                foreach (ActionClip.StatusPayload status in CurrentActionClip.statusesToApplyToSelfOnActivate)
                {
                    combatAgent.StatusAgent.TryAddStatus(status.status, status.value, status.duration, status.delay, true);
                }
            }
        }

        public GameObject SpawnPreviewVFX(ActionClip actionClip, ActionVFXPreview actionPreviewVFXPrefab, Transform attackerTransform, Transform victimTransform = null)
        {
            if (actionClip.actionVFXList.Count == 0) { Debug.LogError("Trying to spawn a preview VFX with no action clip referneces! " + actionClip); return null; }

            (SpawnPoints.TransformData orientation, Transform parent) = GetActionVFXOrientation(actionClip, actionClip.actionVFXList[0], true, attackerTransform, victimTransform);

            ActionVFXPreview previewInstance;
            if (actionPreviewVFXPrefab.TryGetComponent(out PooledObject pooledObject))
            {
                previewInstance = ObjectPoolingManager.SpawnObject(pooledObject, orientation.position, orientation.rotation, parent).GetComponent<ActionVFXPreview>();
            }
            else
            {
                previewInstance = Instantiate(pooledObject, orientation.position, orientation.rotation, parent).GetComponent<ActionVFXPreview>();
                Debug.LogError("No pooled object on preview VFX object!" + actionPreviewVFXPrefab);
            }
            previewInstance.SetActionVFX(actionClip.actionVFXList[0]);
            return previewInstance.gameObject;
        }

        private (SpawnPoints.TransformData, Transform) GetActionVFXOrientation(ActionClip actionClip, ActionVFX actionVFX, bool isPreviewVFX, Transform attackerTransform, Transform victimTransform = null)
        {
            SpawnPoints.TransformData orientation = new SpawnPoints.TransformData();
            Transform parent = null;

            switch (actionVFX.transformType)
            {
                case ActionVFX.TransformType.Stationary:
                    orientation.position = attackerTransform.position;
                    orientation.rotation = attackerTransform.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset);
                    orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                    parent = isPreviewVFX ? attackerTransform : null;
                    break;
                case ActionVFX.TransformType.ParentToOriginator:
                    orientation.position = attackerTransform.position;
                    orientation.rotation = attackerTransform.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset);
                    orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                    parent = attackerTransform;
                    break;
                case ActionVFX.TransformType.SpawnAtWeaponPoint:
                    orientation.position = weaponInstances[actionVFX.weaponBone].transform.position;
                    orientation.rotation = weaponInstances[actionVFX.weaponBone].transform.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset);
                    orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                    parent = isPreviewVFX ? weaponInstances[actionVFX.weaponBone].transform : null;
                    break;
                case ActionVFX.TransformType.Projectile:
                    if (isPreviewVFX)
                    {
                        orientation.position = attackerTransform.position;
                        orientation.rotation = attackerTransform.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset);
                        orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                        parent = attackerTransform;
                    }
                    else
                    {
                        foreach (Weapon.WeaponBone weaponBone in actionClip.effectedWeaponBones)
                        {
                            if (weaponInstances[weaponBone].TryGetComponent(out ShooterWeapon shooterWeapon))
                            {
                                orientation.position = shooterWeapon.GetProjectileSpawnPoint().position;
                                orientation.rotation = shooterWeapon.GetProjectileSpawnPoint().rotation * Quaternion.Euler(actionVFX.vfxRotationOffset);
                                orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                                parent = isPreviewVFX ? shooterWeapon.GetProjectileSpawnPoint() : null;
                                break;
                            }
                        }
                    }
                    break;
                case ActionVFX.TransformType.ConformToGround:
                    Vector3 startPos = attackerTransform.position + attackerTransform.rotation * actionVFX.raycastOffset;
                    Vector3 fartherStartPos = attackerTransform.position + attackerTransform.rotation * actionVFX.fartherRaycastOffset;
                    bool bHit = Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, actionVFX.raycastMaxDistance, LayerMask.GetMask(ActionVFX.layersToAccountForInRaycasting), QueryTriggerInteraction.Ignore);
                    bool fartherBHit = Physics.Raycast(fartherStartPos, Vector3.down, out RaycastHit fartherHit, actionVFX.raycastMaxDistance * 2, LayerMask.GetMask(ActionVFX.layersToAccountForInRaycasting), QueryTriggerInteraction.Ignore);

                    # if UNITY_EDITOR
                    if (bHit) { Debug.DrawLine(startPos, hit.point, Color.red, 2); }
                    if (fartherBHit) { Debug.DrawLine(fartherStartPos, fartherHit.point, Color.magenta, 2); }
                    # endif

                    if (bHit & fartherBHit)
                    {
                        Vector3 offset = attackerTransform.rotation * actionVFX.vfxPositionOffset;
                        Vector3 spawnPosition = hit.point + offset;
                        Vector3 rel = fartherHit.point + offset - spawnPosition;
                        Quaternion groundRotation = rel == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(rel, actionVFX.lookRotationUpDirection);

                        orientation.position = hit.point + offset;
                        orientation.rotation = groundRotation * Quaternion.Euler(actionVFX.vfxRotationOffset);
                        orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                        parent = isPreviewVFX ? attackerTransform : null;
                    }
                    else
                    {
                        orientation.position = attackerTransform.position + attackerTransform.rotation * actionVFX.vfxPositionOffset;
                        orientation.rotation = attackerTransform.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset);
                        orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                        parent = isPreviewVFX ? attackerTransform : null;
                    }
                    break;
                case ActionVFX.TransformType.ParentToVictim:
                    if (!victimTransform) { Debug.LogError("VFX has transform type Parent To Victim, but there was no victim transform provided!" + actionVFX); break; }
                    orientation.position = victimTransform.position;
                    orientation.rotation = victimTransform.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset);
                    orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                    parent = victimTransform;
                    break;
                case ActionVFX.TransformType.StationaryOnVictim:
                    if (!victimTransform) { Debug.LogError("VFX has transform type Parent To Victim, but there was no victim transform provided!" + actionVFX); break; }
                    orientation.position = victimTransform.position;
                    orientation.rotation = victimTransform.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset);
                    orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                    parent = null;
                    break;
                case ActionVFX.TransformType.AimAtTarget:
                    orientation.position = attackerTransform.position;
                    orientation.rotation = attackerTransform.rotation;
                    orientation.position += orientation.rotation * actionVFX.vfxPositionOffset;
                    orientation.rotation = Quaternion.LookRotation(combatAgent.AnimationHandler.GetAimPoint() - orientation.position);
                    orientation.rotation *= Quaternion.Euler(actionVFX.vfxRotationOffset);
                    parent = isPreviewVFX ? attackerTransform : null;
                    break;
                default:
                    Debug.LogError(actionVFX.transformType + " has not been implemented yet!");
                    break;
            }

            return (orientation, parent);
        }

        private List<ActionVFX> actionVFXTracker = new List<ActionVFX>();
        public GameObject SpawnActionVFX(ActionClip actionClip, ActionVFX actionVFXPrefab, Transform attackerTransform, Transform victimTransform = null)
        {
            if (!IsServer) { Debug.LogError("Trying to spawn an action vfx when we aren't the server! " + actionClip); return null; }

            if (actionVFXTracker.Contains(actionVFXPrefab)) { return null; }

            GameObject vfxInstance;
            (SpawnPoints.TransformData orientation, Transform parent) = GetActionVFXOrientation(actionClip, actionVFXPrefab, false, attackerTransform, victimTransform);
            if (actionVFXPrefab.TryGetComponent(out PooledObject pooledObject))
            {
                vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, orientation.position, orientation.rotation, parent).gameObject;
            }
            else
            {
                vfxInstance = Instantiate(pooledObject, orientation.position, orientation.rotation, parent).gameObject;
                Debug.LogError("ActionVFX doesn't have a pooled object! " + actionVFXPrefab);
            }

            if (vfxInstance)
            {
                if (!IsServer) { Debug.LogError("Why the fuck are we not the server here!?"); return null; }
                NetworkObject netObj = vfxInstance.GetComponent<NetworkObject>();
                netObj.Spawn(true);
                //netObj.TrySetParent(parent);
                if (vfxInstance.TryGetComponent(out GameInteractiveActionVFX gameInteractiveActionVFX))
                {
                    gameInteractiveActionVFX.InitializeVFX(combatAgent, CurrentActionClip);
                }
            }
            else if (actionVFXPrefab.transformType != ActionVFX.TransformType.ConformToGround)
            {
                Debug.LogError("No vfx instance spawned for this prefab! " + actionVFXPrefab);
            }

            if (actionVFXPrefab.vfxSpawnType == ActionVFX.VFXSpawnType.OnActivate) { actionVFXTracker.Add(actionVFXPrefab); }

            return vfxInstance;
        }

        public bool IsInAnticipation { get; private set; }
        private bool isAboutToAttack;
        public bool IsAttacking { get; private set; }
        public bool IsInRecovery { get; private set; }

        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!combatAgent.AnimationHandler.Animator) { return; }

            if (combatAgent.AnimationHandler.IsAtRest() | CurrentActionClip.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                IsBlocking = combatAgent.GetSpirit() > 0 | combatAgent.GetStamina() / combatAgent.GetMaxStamina() > CombatAgent.minStaminaPercentageToBeAbleToBlock && isBlocking.Value;
            }
            else
            {
                IsBlocking = false;
            }

            if (combatAgent.AnimationHandler.IsActionClipPlaying(CurrentActionClip))
            {
                float normalizedTime = combatAgent.AnimationHandler.GetActionClipNormalizedTime(CurrentActionClip);
                // Action VFX spawning here
                if (IsServer)
                {
                    foreach (ActionVFX actionVFX in CurrentActionClip.actionVFXList)
                    {
                        if (actionVFX.vfxSpawnType != ActionVFX.VFXSpawnType.OnActivate) { continue; }
                        if (normalizedTime >= actionVFX.onActivateVFXSpawnNormalizedTime)
                        {
                            SpawnActionVFX(CurrentActionClip, actionVFX, transform);
                        }
                    }
                }

                // Action sound effect logic here
                foreach (ActionClip.ActionSoundEffect actionSoundEffect in CurrentActionClip.GetActionClipSoundEffects(combatAgent.GetRaceAndGender(), actionSoundEffectIdTracker))
                {
                    if (Mathf.Approximately(0, actionSoundEffect.normalizedPlayTime))
                    {
                        AudioManager.Singleton.PlayClipOnTransform(transform, actionSoundEffect.audioClip, false, ActionClip.actionClipSoundEffectVolume);
                        actionSoundEffectIdTracker.Add(actionSoundEffect.id);
                    }
                }
            }

            if (currentActionClipWeapon != weaponInstance.name)
            {
                IsInAnticipation = false;
                isAboutToAttack = false;
                IsAttacking = false;
                IsInRecovery = false;
            }
            else if (CurrentActionClip.IsAttack())
            {
                bool lastIsAttacking = IsAttacking;
                if (combatAgent.AnimationHandler.IsActionClipPlaying(CurrentActionClip))
                {
                    float normalizedTime = combatAgent.AnimationHandler.GetActionClipNormalizedTime(CurrentActionClip);
                    IsInRecovery = normalizedTime >= CurrentActionClip.recoveryNormalizedTime;
                    IsAttacking = normalizedTime >= CurrentActionClip.attackingNormalizedTime & !IsInRecovery;
                    IsInAnticipation = !IsAttacking & !IsInRecovery;

                    isAboutToAttack = false;
                    if (IsInAnticipation)
                    {
                        isAboutToAttack = normalizedTime >= CurrentActionClip.attackingNormalizedTime - isAboutToAttackNormalizedTimeOffset;
                    }
                }
                else
                {
                    IsInAnticipation = false;
                    isAboutToAttack = false;
                    IsAttacking = false;
                    IsInRecovery = false;
                }

                // If we started attacking on this fixedUpdate
                if (IsAttacking & !lastIsAttacking)
                {
                    foreach (Weapon.WeaponBone weaponBone in CurrentActionClip.effectedWeaponBones)
                    {
                        if (weaponInstances.TryGetValue(weaponBone, out RuntimeWeapon runtimeWeapon))
                        {
                            if (runtimeWeapon)
                            {
                                // Don't play sound effects for shooter weapons here
                                if (runtimeWeapon is ShooterWeapon) { continue; }

                                AudioClip attackSoundEffect = weaponInstance.GetAttackSoundEffect(weaponBone);
                                if (attackSoundEffect)
                                {
                                    AudioSource audioSource = AudioManager.Singleton.PlayClipOnTransform(runtimeWeapon.transform, attackSoundEffect, false, Weapon.attackSoundEffectVolume);
                                    audioSource.maxDistance = Weapon.attackSoundEffectMaxDistance;
                                }
                                else if (Application.isEditor)
                                    Debug.LogWarning("No attack sound effect for weapon " + weaponInstance.name + " on bone - " + weaponBone);
                            }
                            else
                            {
                                Debug.LogError("Affected weapon bone " + weaponBone + " but there isn't a weapon instance");
                            }
                        }
                        else
                        {
                            Debug.LogError("Affected weapon bone " + weaponBone + " but there isn't a weapon instance");
                        }
                    }
                }

                // Check if flash attack had no hits
                if (IsServer & CurrentActionClip.GetClipType() == ActionClip.ClipType.FlashAttack)
                {
                    // If we stopped attacking on this fixedUpdate
                    if (!IsAttacking & lastIsAttacking & IsInRecovery)
                    {
                        bool wasThereAHit = false;
                        foreach (Weapon.WeaponBone weaponBone in CurrentActionClip.effectedWeaponBones)
                        {
                            wasThereAHit = weaponInstances[weaponBone].GetComponent<RuntimeWeapon>().GetHitCounter().Count > 0;
                            if (wasThereAHit) { break; }
                        }

                        if (!wasThereAHit)
                        {
                            combatAgent.ProcessEnvironmentDamage(-CurrentActionClip.healthPenaltyOnMiss, NetworkObject);
                            combatAgent.AddStamina(-CurrentActionClip.staminaPenaltyOnMiss);
                            combatAgent.AddRage(-CurrentActionClip.ragePenaltyOnMiss);
                        }
                    }
                }
            }
            else
            {
                IsInAnticipation = false;
                isAboutToAttack = false;
                IsAttacking = false;
                IsInRecovery = false;
            }

            if (CanAim)
            {
                if (IsInAnticipation)
                {
                    Aim(CurrentActionClip.aimDuringAnticipation ? IsInAnticipation : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction);

                    if (CurrentActionClip.aimDuringAttack & isAboutToAttack)
                    {
                        Aim(CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction);
                    }
                }
                else if (IsAttacking)
                {
                    Aim(CurrentActionClip.aimDuringAttack ? IsAttacking : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction);
                }
                else if (IsInRecovery)
                {
                    Aim(CurrentActionClip.aimDuringRecovery ? IsInRecovery : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction);
                }
                else
                {
                    Aim(aiming.Value & combatAgent.AnimationHandler.CanAim());
                }
            }
            else
            {
                Aim(false);
            }
        }

        private const float isAboutToAttackNormalizedTimeOffset = 0.2f;

        [HideInInspector] public float lastMeleeHitTime = Mathf.NegativeInfinity;

        public bool CanActivateFlashSwitch()
        {
            return IsInRecovery & CurrentActionClip.canFlashAttack;
        }

        void OnLightAttack(InputValue value)
        {
            LightAttack(value.isPressed);
        }

        public void LightAttack(bool isPressed)
        {
            if (isPressed)
            {
                ActionClip actionClip = GetAttack(Weapon.InputAttackType.LightAttack);
                if (actionClip != null)
                    combatAgent.AnimationHandler.PlayAction(actionClip);
            }
        }

        private NetworkVariable<bool> lightAttackIsPressed = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private Coroutine lightAttackHoldCoroutine;
        public void LightAttackHold(bool isPressed)
        {
            lightAttackIsPressed.Value = isPressed;
        }

        public override void OnNetworkSpawn()
        {
            lightAttackIsPressed.OnValueChanged += OnLightAttackHoldChange;
            reloadingAnimParameterValue.OnValueChanged += OnReloadAnimParameterChange;
        }

        public override void OnNetworkDespawn()
        {
            lightAttackIsPressed.OnValueChanged -= OnLightAttackHoldChange;
            reloadingAnimParameterValue.OnValueChanged -= OnReloadAnimParameterChange;
        }

        void OnLightAttackHold(InputValue value)
        {
            lightAttackIsPressed.Value = value.isPressed;
        }

        private void OnLightAttackHoldChange(bool prev, bool current)
        {
            if (IsServer)
            {
                if (lightAttackHoldCoroutine != null) { StopCoroutine(lightAttackHoldCoroutine); }
                if (current) { lightAttackHoldCoroutine = StartCoroutine(LightAttackHold()); }
                else { LightAttack(false); }
            }
        }

        private IEnumerator LightAttackHold()
        {
            while (true)
            {
                yield return null;
                LightAttack(true);
            }
        }

        private void OnReloadAnimParameterChange(bool prev, bool current)
        {
            if (current)
            {
                AudioClip reloadSoundEffect = GetWeapon().GetReloadSoundEffect();
                if (reloadSoundEffect) { StartCoroutine(PlayReloadSoundEffect(reloadSoundEffect)); }
            }
        }

        private IEnumerator PlayReloadSoundEffect(AudioClip reloadSoundEffect)
        {
            AudioSource audioSource = AudioManager.Singleton.PlayClipAtPoint(gameObject, reloadSoundEffect, transform.position, Weapon.reloadSoundEffectVolume);
            while (true)
            {
                if (!reloadingAnimParameterValue.Value) { break; }
                if (!audioSource) { break; }
                if (!audioSource.isPlaying) { break; }
                yield return null;
            }
        }

        public bool CanAim { get; private set; }
        public bool CanADS { get; private set; }

        private NetworkVariable<bool> aiming = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public void OnDeath()
        {
            if (IsServer)
            {
                loadoutManager.ReloadAllWeapons();
            }

            if (IsOwner)
            {
                aiming.Value = false;
            }
        }

        void OnHeavyAttack(InputValue value)
        {
            HeavyAttack(value.isPressed);
        }

        public void HeavyAttack(bool isPressed)
        {
            combatAgent.AnimationHandler.SetHeavyAttackPressedState(isPressed);

            if (CanADS)
            {
                if (NetworkObject.IsPlayerObject)
                {
                    if (zoomMode == "TOGGLE")
                    {
                        if (isPressed) { aiming.Value = !aiming.Value; }
                    }
                    else if (zoomMode == "HOLD")
                    {
                        aiming.Value = isPressed;
                    }
                    else
                    {
                        Debug.LogError("Not sure how to handle player prefs ZoomMode - " + zoomMode);
                    }
                }
                else
                {
                    aiming.Value = isPressed;
                }
            }
            else if (isPressed)
            {
                if (!IsBlocking)
                {
                    ActionClip actionClip = GetAttack(Weapon.InputAttackType.HeavyAttack);
                    if (actionClip != null)
                        combatAgent.AnimationHandler.PlayAction(actionClip);
                }
            }
        }

        public void HeavyAttackHold(bool isPressed)
        {
            if (heavyAttackHoldCoroutine != null) { StopCoroutine(heavyAttackHoldCoroutine); }

            if (CanAim)
            {
                HeavyAttack(isPressed);
            }
            else
            {
                if (isPressed) { heavyAttackHoldCoroutine = StartCoroutine(HeavyAttackHold()); }
                else { HeavyAttack(false); }
            }
        }

        private Coroutine heavyAttackHoldCoroutine;
        void OnHeavyAttackHold(InputValue value)
        {
            HeavyAttackHold(value.isPressed);
        }

        private IEnumerator HeavyAttackHold()
        {
            while (true)
            {
                yield return null;
                HeavyAttack(true);
            }
        }

        public void ClearActionVFXInstances()
        {
            if (ability1PreviewInstance)
            {
                ObjectPoolingManager.ReturnObjectToPool(ability1PreviewInstance.GetComponent<PooledObject>());
                ability1PreviewInstance = null;
            }

            if (ability2PreviewInstance)
            {
                ObjectPoolingManager.ReturnObjectToPool(ability2PreviewInstance.GetComponent<PooledObject>());
                ability2PreviewInstance = null;
            }

            if (ability3PreviewInstance)
            {
                ObjectPoolingManager.ReturnObjectToPool(ability3PreviewInstance.GetComponent<PooledObject>());
                ability3PreviewInstance = null;
            }

            if (ability4PreviewInstance)
            {
                ObjectPoolingManager.ReturnObjectToPool(ability4PreviewInstance.GetComponent<PooledObject>());
                ability4PreviewInstance = null;
            }
        }

        void OnAbility1(InputValue value)
        {
            Ability1(value.isPressed);
        }

        private GameObject ability1PreviewInstance;
        public void Ability1(bool isPressed)
        {
            ActionClip actionClip = weaponInstance.GetAbility1();
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer)
                {
                    if (isPressed) // If we are holding down the key
                    {
                        ability1PreviewInstance = SpawnPreviewVFX(actionClip, actionClip.previewActionVFX.GetComponent<ActionVFXPreview>(), transform);
                    }
                    else // If we have released the key
                    {
                        if (ability1PreviewInstance)
                        {
                            if (ability1PreviewInstance.TryGetComponent(out ActionVFXPreview preview))
                            {
                                if (preview.CanCast & GetAttack(Weapon.InputAttackType.Ability1)) { combatAgent.AnimationHandler.PlayAction(actionClip); }
                            }
                            ObjectPoolingManager.ReturnObjectToPool(ability1PreviewInstance.GetComponent<PooledObject>());
                            ability1PreviewInstance = null;
                        }
                    }
                }
                else if (GetAttack(Weapon.InputAttackType.Ability1) & isPressed) // If there is no preview VFX
                {
                    combatAgent.AnimationHandler.PlayAction(actionClip);
                }
            }
        }

        void OnAbility2(InputValue value)
        {
            Ability2(value.isPressed);
        }

        private GameObject ability2PreviewInstance;
        public void Ability2(bool isPressed)
        {
            ActionClip actionClip = weaponInstance.GetAbility2();
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer)
                {
                    if (isPressed) // If we are holding down the key
                    {
                        ability2PreviewInstance = SpawnPreviewVFX(actionClip, actionClip.previewActionVFX.GetComponent<ActionVFXPreview>(), transform);
                    }
                    else // If we have released the key
                    {
                        if (ability2PreviewInstance)
                        {
                            if (ability2PreviewInstance.TryGetComponent(out ActionVFXPreview preview))
                            {
                                if (preview.CanCast & GetAttack(Weapon.InputAttackType.Ability2)) { combatAgent.AnimationHandler.PlayAction(actionClip); }
                            }
                            ObjectPoolingManager.ReturnObjectToPool(ability2PreviewInstance.GetComponent<PooledObject>());
                            ability2PreviewInstance = null;
                        }
                    }
                }
                else if (GetAttack(Weapon.InputAttackType.Ability2) & isPressed) // If there is no preview VFX
                {
                    combatAgent.AnimationHandler.PlayAction(actionClip);
                }
            }
        }

        void OnAbility3(InputValue value)
        {
            Ability3(value.isPressed);
        }

        private GameObject ability3PreviewInstance;
        public void Ability3(bool isPressed)
        {
            ActionClip actionClip = weaponInstance.GetAbility3();
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer)
                {
                    if (isPressed) // If we are holding down the key
                    {
                        ability3PreviewInstance = SpawnPreviewVFX(actionClip, actionClip.previewActionVFX.GetComponent<ActionVFXPreview>(), transform);
                    }
                    else // If we have released the key
                    {
                        if (ability3PreviewInstance)
                        {
                            if (ability3PreviewInstance.TryGetComponent(out ActionVFXPreview preview))
                            {
                                if (preview.CanCast & GetAttack(Weapon.InputAttackType.Ability3)) { combatAgent.AnimationHandler.PlayAction(actionClip); }
                            }
                            ObjectPoolingManager.ReturnObjectToPool(ability3PreviewInstance.GetComponent<PooledObject>());
                            ability3PreviewInstance = null;
                        }
                    }
                }
                else if (GetAttack(Weapon.InputAttackType.Ability3) & isPressed) // If there is no preview VFX
                {
                    combatAgent.AnimationHandler.PlayAction(actionClip);
                }
            }
        }

        void OnAbility4(InputValue value)
        {
            Ability4(value.isPressed);
        }

        private GameObject ability4PreviewInstance;
        public void Ability4(bool isPressed)
        {
            ActionClip actionClip = weaponInstance.GetAbility4();
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer)
                {
                    if (isPressed) // If we are holding down the key
                    {
                        ability4PreviewInstance = SpawnPreviewVFX(actionClip, actionClip.previewActionVFX.GetComponent<ActionVFXPreview>(), transform);
                    }
                    else // If we have released the key
                    {
                        if (ability4PreviewInstance)
                        {
                            if (ability4PreviewInstance.TryGetComponent(out ActionVFXPreview preview))
                            {
                                if (preview.CanCast & GetAttack(Weapon.InputAttackType.Ability4)) { combatAgent.AnimationHandler.PlayAction(actionClip); }
                            }
                            ObjectPoolingManager.ReturnObjectToPool(ability4PreviewInstance.GetComponent<PooledObject>());
                            ability4PreviewInstance = null;
                        }
                    }
                }
                else if (GetAttack(Weapon.InputAttackType.Ability4) & isPressed) // If there is no preview VFX
                {
                    combatAgent.AnimationHandler.PlayAction(actionClip);
                }
            }
        }

        public bool IsNextProjectileDamageMultiplied()
        {
            foreach (ShooterWeapon shooterWeapon in shooterWeapons)
            {
                return shooterWeapon.GetNextDamageMultiplier() > 1;
            }
            return false;
        }

        public bool IsAiming(LimbReferences.Hand hand) { return combatAgent.AnimationHandler.LimbReferences.IsAiming(hand) & combatAgent.AnimationHandler.CanAim(); }

        public bool IsAiming() { return aiming.Value & combatAgent.AnimationHandler.CanAim(); }

        private void Aim(bool isAiming)
        {
            combatAgent.AnimationHandler.Animator.SetBool("Aiming", isAiming);
            foreach (ShooterWeapon shooterWeapon in shooterWeapons)
            {
                CharacterReference.RaceAndGender raceAndGender = combatAgent.GetRaceAndGender();
                combatAgent.AnimationHandler.LimbReferences.AimHand(shooterWeapon.GetAimHand(), shooterWeapon.GetAimHandIKOffset(raceAndGender), isAiming & !combatAgent.AnimationHandler.IsReloading(), combatAgent.AnimationHandler.IsAtRest() || CurrentActionClip.shouldAimBody, shooterWeapon.GetBodyAimIKOffset(raceAndGender), shooterWeapon.GetBodyAimType());
                ShooterWeapon.OffHandInfo offHandInfo = shooterWeapon.GetOffHandInfo();
                combatAgent.AnimationHandler.LimbReferences.ReachHand(offHandInfo.offHand, offHandInfo.offHandTarget, (combatAgent.AnimationHandler.IsAtRest() ? isAiming : CurrentActionClip.shouldAimOffHand & isAiming) & !combatAgent.AnimationHandler.IsReloading());
            }
        }

        private string zoomMode = "TOGGLE";
        private string blockingMode = "HOLD";
        private bool disableBots;
        private void RefreshStatus()
        {
            zoomMode = FasterPlayerPrefs.Singleton.GetString("ZoomMode");
            blockingMode = FasterPlayerPrefs.Singleton.GetString("BlockingMode");
            disableBots = bool.Parse(FasterPlayerPrefs.Singleton.GetString("DisableBots"));
        }

        private NetworkVariable<bool> reloadingAnimParameterValue = new NetworkVariable<bool>();
        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (!combatAgent.AnimationHandler.Animator) { return; }

            combatAgent.AnimationHandler.Animator.SetBool("Blocking", IsBlocking);

            if (IsServer)
            {
                reloadingAnimParameterValue.Value = combatAgent.AnimationHandler.Animator.GetBool("Reloading");

                if (ShouldUseAmmo())
                {
                    if (GetAmmoCount() == 0)
                    {
                        if (movementHandler.GetMoveInput() == Vector2.zero) { OnReload(); }
                    }
                }
            }
            else
            {
                combatAgent.AnimationHandler.Animator.SetBool("Reloading", reloadingAnimParameterValue.Value);
            }

            foreach (KeyValuePair<Weapon.WeaponBone, RuntimeWeapon> kvp in weaponInstances)
            {
                kvp.Value.SetActive(!CurrentActionClip.weaponBonesToHide.Contains(kvp.Key) | combatAgent.AnimationHandler.IsAtRest());
            }
        }

        public void Reload()
        {
            OnReload();
        }

        void OnReload()
        {
            if (IsServer)
            {
                ReloadOnServer();
            }
            else
            {
                ReloadServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void ReloadServerRpc()
        {
            ReloadOnServer();
        }

        private void ReloadOnServer()
        {
            if (reloadRunning) { return; }
            if (combatAgent.AnimationHandler.IsReloading()) { return; }
            if (loadoutManager.GetAmmoCount(weaponInstance) == weaponInstance.GetMaxAmmoCount()) { return; }
            if (!combatAgent.AnimationHandler.IsAtRest()) { return; }

            foreach (ShooterWeapon shooterWeapon in shooterWeapons)
            {
                StartCoroutine(Reload(shooterWeapon));
                break;
            }
        }

        private bool reloadRunning;
        private IEnumerator Reload(ShooterWeapon shooterWeapon)
        {
            reloadRunning = true;
            combatAgent.AnimationHandler.Animator.SetBool("Reloading", true);
            yield return new WaitUntil(() => combatAgent.AnimationHandler.IsFinishingReload());
            combatAgent.AnimationHandler.Animator.SetBool("Reloading", false);
            loadoutManager.Reload(weaponInstance);
            reloadRunning = false;
        }

        public bool IsBlocking { get; private set; }
        private NetworkVariable<bool> isBlocking = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        void OnBlock(InputValue value)
        {
            Block(value.isPressed);
        }

        public void Block(bool isPressed)
        {
            if (blockingMode == "TOGGLE")
            {
                if (isPressed) { isBlocking.Value = !isBlocking.Value; }
            }
            else if (blockingMode == "HOLD")
            {
                isBlocking.Value = isPressed;
            }
            else
            {
                Debug.LogError("Not sure how to handle player prefs BlockingMode - " + blockingMode);
            }
        }

        void OnTimeScaleChange()
        {
            if (!Application.isEditor) { return; }

            if (Time.timeScale == 1)
            {
                Time.timeScale = 0.1f;
            }
            else
            {
                Time.timeScale = 1;
            }
        }

        void OnDisableBots()
        {
            if (!Application.isEditor) { return; }
            FasterPlayerPrefs.Singleton.SetString("DisableBots", (!disableBots).ToString());

            if (disableBots)
            {
                Debug.Log("Enabled Bot AI");
            }
            else
            {
                Debug.Log("Disabled Bot AI");
            }
        }

        private List<Weapon.InputAttackType> inputHistory = new List<Weapon.InputAttackType>();
        private ActionClip GetAttack(Weapon.InputAttackType inputAttackType)
        {
            if (combatAgent.AnimationHandler.WaitingForActionToPlay) { return null; }
            if (combatAgent.AnimationHandler.IsReloading()) { return null; }

            // If we are in recovery
            if (IsInRecovery)
            {
                ActionClip actionClip = null;
                // If not transitioning to a different action
                if (!combatAgent.AnimationHandler.Animator.IsInTransition(combatAgent.AnimationHandler.Animator.GetLayerIndex("Actions")))
                {
                    actionClip = SelectAttack(inputAttackType, inputHistory);
                }

                if (actionClip)
                {
                    if (ShouldUseAmmo())
                    {
                        if (actionClip.requireAmmo)
                        {
                            if (GetAmmoCount() < actionClip.requiredAmmoAmount)
                            {
                                OnReload();
                                return null;
                            }
                        }
                    }
                }
                else // If action clip is null
                {
                    if (CurrentActionClip.canBeCancelledByLightAttacks & inputAttackType == Weapon.InputAttackType.LightAttack)
                    {
                        actionClip = SelectAttack(inputAttackType, new List<Weapon.InputAttackType>());
                        if (actionClip != null) { ResetComboSystem(); }
                    }

                    if (CurrentActionClip.canBeCancelledByHeavyAttacks & inputAttackType == Weapon.InputAttackType.HeavyAttack)
                    {
                        actionClip = SelectAttack(inputAttackType, new List<Weapon.InputAttackType>());
                        if (actionClip != null) { ResetComboSystem(); }
                    }

                    if (CurrentActionClip.canBeCancelledByAbilities &
                        (inputAttackType == Weapon.InputAttackType.Ability1
                        | inputAttackType == Weapon.InputAttackType.Ability2
                        | inputAttackType == Weapon.InputAttackType.Ability3
                        | inputAttackType == Weapon.InputAttackType.Ability4))
                    {
                        actionClip = SelectAttack(inputAttackType, new List<Weapon.InputAttackType>());
                        if (actionClip != null) { ResetComboSystem(); }
                    }
                }
                return actionClip;
            }
            else if ((combatAgent.AnimationHandler.IsAtRest() & !combatAgent.AnimationHandler.IsFlinching()) | combatAgent.AnimationHandler.IsDodging() | combatAgent.AnimationHandler.IsPlayingBlockingHitReaction())
            {
                ResetComboSystem();
                ActionClip actionClip = SelectAttack(inputAttackType, inputHistory);
                if (actionClip)
                {
                    if (ShouldUseAmmo())
                    {
                        if (actionClip.requireAmmo)
                        {
                            if (GetAmmoCount() < actionClip.requiredAmmoAmount)
                            {
                                OnReload();
                                return null;
                            }
                        }
                    }
                }
                return actionClip;
            }
            else // If we are not at rest and not recovering
            {
                return null;
            }
        }

        private void ResetComboSystem()
        {
            inputHistory.Clear();
        }

        private ActionClip SelectAttack(Weapon.InputAttackType inputAttackType, List<Weapon.InputAttackType> inputHistory)
        {
            switch (inputAttackType)
            {
                case Weapon.InputAttackType.Ability1:
                    return weaponInstance.GetAbility1();
                case Weapon.InputAttackType.Ability2:
                    return weaponInstance.GetAbility2();
                case Weapon.InputAttackType.Ability3:
                    return weaponInstance.GetAbility3();
                case Weapon.InputAttackType.Ability4:
                    return weaponInstance.GetAbility4();
            }

            List<Weapon.InputAttackType> cachedInputHistory = new List<Weapon.InputAttackType>(inputHistory);
            cachedInputHistory.Add(inputAttackType);

            List<Weapon.Attack> potentialAttacks = weaponInstance.GetAttackList().FindAll(item => item.inputs.SequenceEqual(cachedInputHistory));

            Weapon.Attack selectedAttack = potentialAttacks.Find(item => item.inputs.SequenceEqual(cachedInputHistory) & item.comboCondition == Weapon.ComboCondition.None & !item.attackClip.mustBeAiming);
            if (aiming.Value)
            {
                selectedAttack = potentialAttacks.Find(item => item.inputs.SequenceEqual(cachedInputHistory) & item.comboCondition == Weapon.ComboCondition.None & item.attackClip.mustBeAiming);
            }

            foreach (Weapon.Attack attack in potentialAttacks)
            {
                bool conditionMet = false;
                switch (attack.comboCondition)
                {
                    case Weapon.ComboCondition.None:
                        break;
                    case Weapon.ComboCondition.InputForward:
                        conditionMet = movementHandler.GetMoveInput().y > 0.7f;
                        break;
                    case Weapon.ComboCondition.InputBackwards:
                        conditionMet = movementHandler.GetMoveInput().y < -0.7f;
                        break;
                    case Weapon.ComboCondition.InputLeft:
                        conditionMet = movementHandler.GetMoveInput().x < -0.7f;
                        break;
                    case Weapon.ComboCondition.InputRight:
                        conditionMet = movementHandler.GetMoveInput().x > 0.7f;
                        break;
                    default:
                        Debug.Log(attack.comboCondition + " has not been implemented yet!");
                        break;
                }

                if (conditionMet)
                {
                    selectedAttack = attack;
                    break;
                }
            }

            if (inputAttackType == Weapon.InputAttackType.HeavyAttack)
            {
                if (selectedAttack == null)
                {
                    selectedAttack = weaponInstance.GetAttackList().Find(item => item.inputs.SequenceEqual(new List<Weapon.InputAttackType>() { Weapon.InputAttackType.HeavyAttack }) & item.comboCondition == Weapon.ComboCondition.None & !item.attackClip.mustBeAiming);
                }
            }

            if (selectedAttack == null) { return null; }
            return selectedAttack.attackClip;
        }
    }
}