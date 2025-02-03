using MagicaCloth2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core.GameModeManagers;
using Vi.Core.VFX;
using Vi.Core.Weapons;
using Vi.ProceduralAnimations;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.CombatAgents;

namespace Vi.Core
{
    [DisallowMultipleComponent]
    public class WeaponHandler : NetworkBehaviour
    {
        public Dictionary<Weapon.WeaponBone, RuntimeWeapon> WeaponInstances { get { return weaponInstances; } }
        private Dictionary<Weapon.WeaponBone, RuntimeWeapon> weaponInstances = new Dictionary<Weapon.WeaponBone, RuntimeWeapon>();

        private List<ShooterWeapon> shooterWeapons = new List<ShooterWeapon>();

        private List<PooledObject> equippedPersistentNonWeapons = new List<PooledObject>();
        private List<PooledObject> stowedPersistentNonWeapons = new List<PooledObject>();

        public Weapon GetWeapon() { return weaponInstance; }

        private Weapon weaponInstance;
        private CombatAgent combatAgent;

        private void Awake()
        {
            combatAgent = GetComponent<CombatAgent>();
            RefreshStatus();

            if (TryGetComponent(out PooledObject pooledObject))
            {
                pooledObject.OnReturnToPool += OnReturnToPool;
            }
        }

        private void OnEnable()
        {
            weaponInstance = ScriptableObject.CreateInstance<Weapon>();
            CurrentActionClip = ScriptableObject.CreateInstance<ActionClip>();
            RefreshStatus();
        }

        public bool WeaponInitialized { get; private set; }
        public AnimatorOverrideController AnimatorOverrideControllerInstance { get; private set; }
        public void SetNewWeapon(Weapon weapon, AnimatorOverrideController animatorOverrideController)
        {
            if (!weapon) { Debug.LogWarning("Weapon is null!"); return; }
            if (IsOwner & IsSpawned) { aiming.Value = false; }

            CurrentActionClip = ScriptableObject.CreateInstance<ActionClip>();
            weaponInstance = weapon;
            AnimatorOverrideControllerInstance = Instantiate(animatorOverrideController);

            if (combatAgent.AnimationHandler.IsAiming())
            {
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(SwapAnimatorControllerAfterNotAiming());
                }
                else
                {
                    Debug.LogError("Cannot swap animator controller in coroutine because object is inactive");
                }
            }
            else
            {
                combatAgent.AnimationHandler.Animator.runtimeAnimatorController = AnimatorOverrideControllerInstance;
                
                if (combatAgent is Attributes)
                {
                    combatAgent.AnimationHandler.Animator.SetFloat("MVPSpeed", weaponInstance.GetMVPAnimationSpeed());
                }
            }
            
            EquipWeapon();
            WeaponInitialized = true;
        }

        private IEnumerator SwapAnimatorControllerAfterNotAiming()
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
                if (g.TryGetComponent(out MagicaCapsuleCollider weaponCapsuleCollider))
                {
                    combatAgent.AnimationHandler.RemoveClothCapsuleCollider(weaponCapsuleCollider);
                }
                ObjectPoolingManager.ReturnObjectToPool(g);
            }
            stowedWeaponInstances.Clear();

            foreach (PooledObject pooledObject in stowedPersistentNonWeapons)
            {
                ObjectPoolingManager.ReturnObjectToPool(pooledObject);
            }
            stowedPersistentNonWeapons.Clear();

            foreach (Weapon.WeaponModelData data in weapon.GetWeaponModelData())
            {
                if (data.skinPrefab.name == combatAgent.AnimationHandler.LimbReferences.name.Replace("(Clone)", ""))
                {
                    foreach (Weapon.WeaponModelData.Data modelData in data.data)
                    {
                        PooledObject instance = ObjectPoolingManager.SpawnObject(modelData.weaponPrefab.GetComponent<PooledObject>(),
                            combatAgent.AnimationHandler.LimbReferences.GetStowedWeaponParent(modelData.stowedParentType));
                        instance.GetComponent<RuntimeWeapon>().SetIsStowed(true);
                        instance.transform.localPosition = modelData.stowedWeaponPositionOffset;
                        instance.transform.localRotation = Quaternion.Euler(modelData.stowedWeaponRotationOffset);
                        stowedWeaponInstances.Add(instance);

                        if (instance.TryGetComponent(out MagicaCapsuleCollider weaponCapsuleCollider))
                        {
                            combatAgent.AnimationHandler.AddClothCapsuleCollider(weaponCapsuleCollider);
                        }

                        if (modelData.persistentNonWeaponPrefabs != null)
                        {
                            foreach (Weapon.WeaponModelData.PersistentNonWeaponData persistentNonWeaponData in modelData.persistentNonWeaponPrefabs)
                            {
                                LoadoutManager.WeaponSlotType weaponSlotType = LoadoutManager.WeaponSlotType.Primary;
                                switch (combatAgent.LoadoutManager.GetEquippedSlotType())
                                {
                                    case LoadoutManager.WeaponSlotType.Primary:
                                        weaponSlotType = LoadoutManager.WeaponSlotType.Secondary;
                                        break;
                                    case LoadoutManager.WeaponSlotType.Secondary:
                                        weaponSlotType = LoadoutManager.WeaponSlotType.Primary;
                                        break;
                                    default:
                                        Debug.LogError("Unsure how to handle weapon slot type " + combatAgent.LoadoutManager.GetEquippedSlotType());
                                        break;
                                }
                                stowedPersistentNonWeapons.Add(CreatePersistentNonWeapons(weaponSlotType, persistentNonWeaponData));
                            }
                        }
                    }
                }
            }
        }

        private PooledObject CreatePersistentNonWeapons(LoadoutManager.WeaponSlotType weaponSlotType, Weapon.WeaponModelData.PersistentNonWeaponData persistentNonWeaponData)
        {
            PooledObject nonWeapon = ObjectPoolingManager.SpawnObject(persistentNonWeaponData.prefab,
                combatAgent.AnimationHandler.LimbReferences.GetStowedWeaponParent(persistentNonWeaponData.parentType));

            PersistentLocalObjects.Singleton.StartCoroutine(InitializeNonWeaponRenderers(nonWeapon.GetComponentsInChildren<Renderer>()));

            if (nonWeapon.TryGetComponent(out Quiver quiver))
            {
                quiver.Initialize(combatAgent, combatAgent.LoadoutManager.GetWeaponInSlot(weaponSlotType));
            }

            return nonWeapon;
        }

        private IEnumerator InitializeNonWeaponRenderers(Renderer[] renderers)
        {
            foreach (Renderer r in renderers)
            {
                r.forceRenderingOff = true;
                r.gameObject.layer = LayerMask.NameToLayer(IsSpawned ? "NetworkPrediction" : "Preview");
            }
            yield return null;
            foreach (Renderer r in renderers)
            {
                r.forceRenderingOff = false;
            }
        }

        public bool ShouldUseAmmo()
        {
            if (weaponInstance != null) { return weaponInstance.ShouldUseAmmo(); }
            return false;
        }

        public int GetAmmoCount()
        {
            return combatAgent.LoadoutManager.GetAmmoCount(weaponInstance);
        }

        public int GetMaxAmmoCount()
        {
            if (weaponInstance != null) { return weaponInstance.GetMaxAmmoCount(); }
            return 0;
        }

        public void UseAmmo()
        {
            combatAgent.LoadoutManager.UseAmmo(weaponInstance);
        }

        private void OnReturnToPool()
        {
            DespawnActionVFXInstances();
        }

        private void OnDisable()
        {
            WeaponInitialized = default;
            AnimatorOverrideControllerInstance = default;

            foreach (KeyValuePair<Weapon.WeaponBone, RuntimeWeapon> kvp in weaponInstances)
            {
                if (kvp.Value.TryGetComponent(out MagicaCapsuleCollider weaponCapsuleCollider))
                {
                    combatAgent.AnimationHandler.RemoveClothCapsuleCollider(weaponCapsuleCollider);
                }
                ObjectPoolingManager.ReturnObjectToPool(kvp.Value.GetComponent<PooledObject>());
            }
            weaponInstances.Clear();

            foreach (PooledObject pooledObject in equippedPersistentNonWeapons)
            {
                ObjectPoolingManager.ReturnObjectToPool(pooledObject);
            }
            equippedPersistentNonWeapons.Clear();

            foreach (PooledObject g in stowedWeaponInstances)
            {
                if (g.TryGetComponent(out MagicaCapsuleCollider weaponCapsuleCollider))
                {
                    combatAgent.AnimationHandler.RemoveClothCapsuleCollider(weaponCapsuleCollider);
                }
                ObjectPoolingManager.ReturnObjectToPool(g);
            }
            stowedWeaponInstances.Clear();

            foreach (PooledObject pooledObject in stowedPersistentNonWeapons)
            {
                ObjectPoolingManager.ReturnObjectToPool(pooledObject);
            }
            stowedPersistentNonWeapons.Clear();

            inputHistory.Clear();

            CanAim = false;
            CanADS = false;

            currentActionClipWeapon = default;

            actionSoundEffectIdTracker.Clear();

            actionVFXPrefabTracker.Clear();

            IsInAnticipation = false;
            isAboutToAttack = false;
            IsAttacking = false;
            IsInRecovery = false;

            ClearPreviewActionVFXInstances();

            reloadFinished = false;

            lastLightAttackPressNetworkLatencyWait = Mathf.NegativeInfinity;
        }

        private void EquipWeapon()
        {
            foreach (KeyValuePair<Weapon.WeaponBone, RuntimeWeapon> kvp in weaponInstances)
            {
                if (kvp.Value.TryGetComponent(out MagicaCapsuleCollider weaponCapsuleCollider))
                {
                    combatAgent.AnimationHandler.RemoveClothCapsuleCollider(weaponCapsuleCollider);
                }
                ObjectPoolingManager.ReturnObjectToPool(kvp.Value.GetComponent<PooledObject>());
            }
            weaponInstances.Clear();
            shooterWeapons.Clear();

            foreach (PooledObject pooledObject in equippedPersistentNonWeapons)
            {
                ObjectPoolingManager.ReturnObjectToPool(pooledObject);
            }
            equippedPersistentNonWeapons.Clear();

            CanAim = false;
            CanADS = false;

            bool broken = false;
            foreach (Weapon.WeaponModelData data in weaponInstance.GetWeaponModelData())
            {
                if (data.skinPrefab.name == combatAgent.AnimationHandler.LimbReferences.name.Replace("(Clone)", "") | data.skinPrefab.name == name.Replace("(Clone)", ""))
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
                                bone = combatAgent.AnimationHandler.LimbReferences.GetBoneTransform(modelData.weaponBone);
                                break;
                        }

                        RuntimeWeapon instance = null;
                        if (modelData.weaponPrefab.TryGetComponent(out PooledObject pooledObject))
                        {
                            instance = ObjectPoolingManager.SpawnObject(pooledObject, bone).GetComponent<RuntimeWeapon>();
                        }
                        else
                        {
                            Debug.LogError(modelData.weaponPrefab + " does not have a pooled object component!");
                        }

                        if (!instance)
                        {
                            Debug.LogError(modelData.weaponPrefab + " does not have a runtime weapon component!");
                            continue;
                        }

                        instance.SetWeaponBone(modelData.weaponBone);
                        instance.SetIsStowed(false);
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

                        if (instance.TryGetComponent(out MagicaCapsuleCollider weaponCapsuleCollider))
                        {
                            combatAgent.AnimationHandler.AddClothCapsuleCollider(weaponCapsuleCollider);
                        }

                        if (modelData.persistentNonWeaponPrefabs != null)
                        {
                            foreach (Weapon.WeaponModelData.PersistentNonWeaponData persistentNonWeaponData in modelData.persistentNonWeaponPrefabs)
                            {
                                equippedPersistentNonWeapons.Add(CreatePersistentNonWeapons(combatAgent.LoadoutManager.GetEquippedSlotType(), persistentNonWeaponData));
                            }
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

            foreach (RuntimeWeapon runtimeWeapon in weaponInstances.Values)
            {
                runtimeWeapon.SetBoxColliderMultiplier(actionClip.bladeSizeMultiplier);
            }

            thisClipSummonablesCount = 0;
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

            actionVFXPrefabTracker.Clear();
            actionVFXInstanceTracker.Clear();
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

            if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Reload)
            {
                ResetComboSystem();
                reloadFinished = false;
                AudioClip reloadSoundEffect = GetWeapon().GetReloadSoundEffect();
                if (reloadSoundEffect) { AudioManager.Singleton.PlayClipOnTransform(transform, reloadSoundEffect, false, Weapon.reloadSoundEffectVolume); }
            }
            else if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Dodge | CurrentActionClip.GetClipType() == ActionClip.ClipType.HitReaction)
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
                    if (!status.associatedWithCurrentWeapon)
                    {
                        Debug.LogWarning("You're applying a status to self on activate but it's not associated with the current weapon " + GetWeapon() + " " + CurrentActionClip + " " + status.status);
                    }
                    combatAgent.StatusAgent.TryAddStatus(status);
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
            previewInstance.transform.rotation *= Quaternion.Euler(actionClip.previewActionVFXRotationOffset);
            previewInstance.transform.position += previewInstance.transform.rotation * actionClip.previewActionVFXPositionOffset;
            previewInstance.transform.localScale = actionClip.previewActionVFXScale;
            previewInstance.Initialize(actionClip, actionClip.actionVFXList[0]);
            return previewInstance.gameObject;
        }

        public (SpawnPoints.TransformData, Transform) GetActionVFXOrientation(ActionClip actionClip, ActionVFX actionVFX, bool isPreviewVFX, Transform attackerTransform, Transform victimTransform = null)
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
                    orientation.rotation = (actionVFX.baseRotationOnRoot ? attackerTransform.rotation : weaponInstances[actionVFX.weaponBone].transform.rotation) * Quaternion.Euler(actionVFX.vfxRotationOffset);
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
                                orientation.rotation = shooterWeapon.GetProjectileSpawnRotation() * Quaternion.Euler(actionVFX.vfxRotationOffset);
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

            if (actionVFX.offsetByTargetBodyHeight)
            {
                orientation.position += combatAgent.MovementHandler.BodyHeightOffset;
            }

            return (orientation, parent);
        }

        private List<ActionVFX> actionVFXPrefabTracker = new List<ActionVFX>();
        private List<ActionVFX> actionVFXInstanceTracker = new List<ActionVFX>();
        public GameObject SpawnActionVFX(ActionClip actionClip, ActionVFX actionVFXPrefab, Transform attackerTransform, Transform victimTransform = null)
        {
            if (!IsServer) { Debug.LogError("Trying to spawn an action vfx when we aren't the server! " + actionClip); return null; }

            if (actionVFXPrefabTracker.Contains(actionVFXPrefab)) { return null; }

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
                if (!IsServer) { Debug.LogError("You can only spawn action VFX on server!"); return null; }
                
                if (attackerTransform)
                {
                    if (attackerTransform.TryGetComponent(out CombatAgent attacker))
                    {
                        if (vfxInstance.TryGetComponent(out GameInteractiveActionVFX gameInteractiveActionVFX))
                        {
                            gameInteractiveActionVFX.InitializeVFX(attacker, actionClip);
                        }
                    }
                }
                
                if (vfxInstance.TryGetComponent(out NetworkObject netObj))
                {
                    if (netObj.IsSpawned)
                    {
                        Debug.LogError("Trying to spawn an action VFX instance that is already spawned " + vfxInstance);
                    }
                    else
                    {
                        netObj.Spawn(true);
                    }
                }
                else
                {
                    Debug.LogError("VFX Instance doesn't have a network object component! " + vfxInstance);
                }
            }
            else if (actionVFXPrefab.transformType != ActionVFX.TransformType.ConformToGround)
            {
                Debug.LogError("No vfx instance spawned for this prefab! " + actionVFXPrefab);
            }

            if (actionVFXPrefab.vfxSpawnType == ActionVFX.VFXSpawnType.OnActivate) { actionVFXPrefabTracker.Add(actionVFXPrefab); }

            actionVFXInstanceTracker.Add(vfxInstance.GetComponent<ActionVFX>());
            return vfxInstance;
        }

        public bool IsInAnticipation { get; private set; }
        private bool isAboutToAttack;
        public bool IsAttacking { get; private set; }
        public bool IsInRecovery { get; private set; }

        private bool reloadFinished;

        private int thisClipSummonablesCount;

        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!combatAgent.AnimationHandler.Animator) { return; }

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

                    if (thisClipSummonablesCount < CurrentActionClip.summonableCount)
                    {
                        if (CurrentActionClip.summonables.Length > 0)
                        {
                            if (combatAgent.GetSlaves().Count(item => item.GetAilment() != ActionClip.Ailment.Death) < ActionClip.maxLivingSummonables)
                            {
                                if (normalizedTime >= CurrentActionClip.normalizedSummonTime)
                                {
                                    CombatAgent summonedAgent = ObjectPoolingManager.SpawnObject(CurrentActionClip.summonables[Random.Range(0, CurrentActionClip.summonables.Length)].GetComponent<PooledObject>(), combatAgent.MovementHandler.GetPosition() + combatAgent.MovementHandler.GetRotation() * CurrentActionClip.summonPositionOffset, combatAgent.MovementHandler.GetRotation()).GetComponent<CombatAgent>();
                                    summonedAgent.SetMaster(combatAgent);
                                    summonedAgent.NetworkObject.Spawn(true);
                                    thisClipSummonablesCount++;
                                }
                            }
                        }
                    }
                }

                // Action sound effect logic here
                foreach (ActionClip.ActionSoundEffect actionSoundEffect in CurrentActionClip.GetActionClipSoundEffects(combatAgent.GetRaceAndGender(), actionSoundEffectIdTracker))
                {
                    if (normalizedTime >= actionSoundEffect.normalizedPlayTime)
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
            else if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Reload)
            {
                IsInAnticipation = false;
                isAboutToAttack = false;
                IsAttacking = false;
                IsInRecovery = false;

                if (IsServer)
                {
                    if (!reloadFinished)
                    {
                        float normalizedTime = combatAgent.AnimationHandler.GetActionClipNormalizedTime(CurrentActionClip);
                        if (normalizedTime >= CurrentActionClip.reloadNormalizedTime) { combatAgent.LoadoutManager.Reload(weaponInstance); reloadFinished = true; }
                    }
                }
            }
            else if (CurrentActionClip.IsAttack())
            {
                bool lastIsAttacking = IsAttacking;
                if (combatAgent.AnimationHandler.IsActionClipPlaying(CurrentActionClip))
                {
                    float normalizedTime = combatAgent.AnimationHandler.GetActionClipNormalizedTime(CurrentActionClip);
                    if (CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
                    {
                        float floor = Mathf.FloorToInt(normalizedTime);
                        if (!Mathf.Approximately(floor, normalizedTime)) { normalizedTime -= floor; }
                    }

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
                                    if (audioSource) { audioSource.maxDistance = Weapon.attackSoundEffectMaxDistance; }
                                }
                                else if (Application.isEditor)
                                    Debug.LogWarning("No attack sound effect for weapon " + weaponInstance.name + " on bone - " + weaponBone);
                            }
                            else
                            {
                                Debug.LogError("Weapon instance was destroyed for affected bone " + weaponBone + " for weapon " + weaponInstance.name + " for action clip " + CurrentActionClip.name);
                            }
                        }
                        else
                        {
                            Debug.LogError("No weapon instance for affected bone " + weaponBone + " for weapon " + weaponInstance.name + " for action clip " + CurrentActionClip.name);
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
                    AimCharacter(CurrentActionClip.aimDuringAnticipation ? IsInAnticipation : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction);

                    if (CurrentActionClip.aimDuringAttack & isAboutToAttack)
                    {
                        AimCharacter(CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction);
                    }
                }
                else if (IsAttacking)
                {
                    AimCharacter(CurrentActionClip.aimDuringAttack ? IsAttacking : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction);
                }
                else if (IsInRecovery)
                {
                    AimCharacter(CurrentActionClip.aimDuringRecovery ? IsInRecovery : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction);
                }
                else
                {
                    AimCharacter(aiming.Value & combatAgent.AnimationHandler.CanAim());
                }
            }
            else
            {
                AimCharacter(false);
            }
        }

        private const float isAboutToAttackNormalizedTimeOffset = 0.2f;

        public static List<string> GetHoldToggleOptions() { return holdToggleOptions.ToList(); }
        private static readonly List<string> holdToggleOptions = new List<string>() { "HOLD", "TOGGLE" };
        public static List<string> GetAttackModeOptions() { return attackModeOptions.ToList(); }
        private static readonly List<string> attackModeOptions = new List<string>() { "PRESS", "HOLD" };

        public bool CanActivateFlashSwitch() { return IsInRecovery & CurrentActionClip.canFlashAttack; }

        public override void OnNetworkSpawn()
        {
            lightAttackIsPressed.OnValueChanged += OnLightAttackIsPressedChanged;
        }

        public override void OnNetworkDespawn()
        {
            lightAttackIsPressed.OnValueChanged -= OnLightAttackIsPressedChanged;
            DespawnActionVFXInstances();
        }

        private void DespawnActionVFXInstances()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                foreach (ActionVFX actionVFX in actionVFXInstanceTracker)
                {
                    if (!actionVFX) { continue; }
                    if (actionVFX.IsSpawned) { actionVFX.NetworkObject.Despawn(true); }
                }
            }
            actionVFXInstanceTracker.Clear();
        }

        public void LightAttack(bool isPressed)
        {
            if (IsLocalPlayer & !FasterPlayerPrefs.IsAutomatedClient)
            {
                if (lightAttackMode == "PRESS")
                {
                    if (isPressed) { LightAttackHold(isPressed, true); }
                }
                else if (lightAttackMode == "HOLD")
                {
                    LightAttackHold(isPressed, false);
                }
                else
                {
                    Debug.LogError("Not sure how to handle player prefs Light Attack Mode - " + lightAttackMode);
                }
            }
            else
            {
                ExecuteLightAttack(isPressed);
            }
        }

        private void OnLightAttack(InputValue value) { LightAttack(value.isPressed); }

        private void ExecuteLightAttack(bool isPressed)
        {
            if (isPressed)
            {
                ActionClip actionClip = GetAttack(Weapon.InputAttackType.LightAttack);
                if (actionClip != null)
                {
                    combatAgent.AnimationHandler.PlayAction(actionClip);
                }
            }
        }

        public bool LightAttackIsPressed { get { return lightAttackIsPressed.Value | lastLightAttackPressNetworkLatencyWait > 0; } }
        private NetworkVariable<bool> lightAttackIsPressed = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private float lastLightAttackPressNetworkLatencyWait = Mathf.NegativeInfinity;
        private Coroutine lightAttackHoldCoroutine;
        private void LightAttackHold(bool isPressed, bool resetAfterSent)
        {
            if (!IsOwner) { Debug.LogError("LightAttackHold() should only be called on the owner!"); return; }
            lightAttackIsPressed.Value = isPressed;

            if (!IsServer)
            {
                if (isPressed) { lastLightAttackPressNetworkLatencyWait = NetworkManager.LocalTime.TimeAsFloat - NetworkManager.ServerTime.TimeAsFloat; }
            }

            if (resetAfterSent)
            {
                if (!resetLightAttackIsRunning) { StartCoroutine(ResetLightAttackIsPressedAfterNotDirty()); }
            }
        }

        private void LateUpdate()
        {
            lastLightAttackPressNetworkLatencyWait -= Time.unscaledDeltaTime;
        }

        private bool resetLightAttackIsRunning;
        private IEnumerator ResetLightAttackIsPressedAfterNotDirty()
        {
            resetLightAttackIsRunning = true;
            yield return new WaitUntil(() => !lightAttackIsPressed.IsDirty());
            lightAttackIsPressed.Value = false;
            resetLightAttackIsRunning = false;
        }

        private void OnLightAttackIsPressedChanged(bool prev, bool current)
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
                ExecuteLightAttack(true);
                yield return null;
            }
        }

        public void HeavyAttack(bool isPressed)
        {
            combatAgent.AnimationHandler.SetHeavyAttackPressedState(isPressed);
            if (isPressed)
            {
                ActionClip actionClip = GetAttack(Weapon.InputAttackType.HeavyAttack);
                if (actionClip != null)
                    combatAgent.AnimationHandler.PlayAction(actionClip);
            }
        }

        private void OnHeavyAttack(InputValue value) { HeavyAttack(value.isPressed); }

        public bool CanAim { get; private set; }
        public bool CanADS { get; private set; }

        private NetworkVariable<bool> aiming = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void OnAim(InputValue value) { if (CanADS) { AimDownSights(value.isPressed); } }

        public void AimDownSights(bool isPressed)
        {
            if (!CanADS) { Debug.LogWarning("Calling AimDownSights() but we can't ADS!"); return; }
            if (IsLocalPlayer & !FasterPlayerPrefs.IsAutomatedClient)
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

        public void OnDeath()
        {
            if (IsServer)
            {
                combatAgent.LoadoutManager.ReloadAllWeapons();
            }

            if (IsOwner)
            {
                aiming.Value = false;
            }

            if (IsLocalPlayer)
            {
                if (deathVibrationsEnabled)
                {
#if UNITY_IOS || UNITY_ANDROID
                    CandyCoded.HapticFeedback.HapticFeedback.HeavyFeedback();
#endif
                }
            }
        }

        public void ClearPreviewActionVFXInstances()
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

        void OnCancelAbility()
        {
            ClearPreviewActionVFXInstances();
        }

        private bool upgradePressed;
        void OnUpgrade(InputValue value)
        {
            upgradePressed = value.isPressed;
        }

        void OnAbility1(InputValue value)
        {
            Ability1(value.isPressed);
        }

        private GameObject ability1PreviewInstance;
        public void Ability1(bool isPressed)
        {
            ActionClip actionClip = weaponInstance.GetAbility1();
            if (GameModeManager.Singleton.LevelingEnabled)
            {
                if (upgradePressed & combatAgent.SessionProgressionHandler.CanUpgradeAbility(actionClip, GetWeapon()))
                {
                    if (isPressed) { combatAgent.SessionProgressionHandler.UpgradeAbility(GetWeapon(), actionClip); }
                    return;
                }
                else if (combatAgent.SessionProgressionHandler.GetAbilityLevel(GetWeapon(), actionClip) == -1)
                {
                    return;
                }
            }

            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer & !FasterPlayerPrefs.IsAutomatedClient)
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
            if (GameModeManager.Singleton.LevelingEnabled)
            {
                if (upgradePressed & combatAgent.SessionProgressionHandler.CanUpgradeAbility(actionClip, GetWeapon()))
                {
                    if (isPressed) { combatAgent.SessionProgressionHandler.UpgradeAbility(GetWeapon(), actionClip); }
                    return;
                }
                else if (combatAgent.SessionProgressionHandler.GetAbilityLevel(GetWeapon(), actionClip) == -1)
                {
                    return;
                }
            }

            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer & !FasterPlayerPrefs.IsAutomatedClient)
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
            if (GameModeManager.Singleton.LevelingEnabled)
            {
                if (upgradePressed & combatAgent.SessionProgressionHandler.CanUpgradeAbility(actionClip, GetWeapon()))
                {
                    if (isPressed) { combatAgent.SessionProgressionHandler.UpgradeAbility(GetWeapon(), actionClip); }
                    return;
                }
                else if (combatAgent.SessionProgressionHandler.GetAbilityLevel(GetWeapon(), actionClip) == -1)
                {
                    return;
                }
            }

            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer & !FasterPlayerPrefs.IsAutomatedClient)
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
            if (GameModeManager.Singleton.LevelingEnabled)
            {
                if (upgradePressed & combatAgent.SessionProgressionHandler.CanUpgradeAbility(actionClip, GetWeapon()))
                {
                    if (isPressed) { combatAgent.SessionProgressionHandler.UpgradeAbility(GetWeapon(), actionClip); }
                    return;
                }
                else if (combatAgent.SessionProgressionHandler.GetAbilityLevel(GetWeapon(), actionClip) == -1)
                {
                    return;
                }
            }

            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer & !FasterPlayerPrefs.IsAutomatedClient)
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

        private void AimCharacter(bool isAiming)
        {
            if (GameModeManager.Singleton)
            {
                if (GameModeManager.Singleton.IsGameOver()) { isAiming = false; }
            }

            combatAgent.AnimationHandler.Animator.SetBool("Aiming", isAiming);
            foreach (ShooterWeapon shooterWeapon in shooterWeapons)
            {
                CharacterReference.RaceAndGender raceAndGender = combatAgent.GetRaceAndGender();

                combatAgent.AnimationHandler.LimbReferences.AimHand(shooterWeapon.GetAimHand(), shooterWeapon.GetAimHandIKOffset(raceAndGender),
                    isAiming & !combatAgent.AnimationHandler.IsReloading(), combatAgent.AnimationHandler.IsAtRest() || CurrentActionClip.shouldAimBody,
                    shooterWeapon.GetBodyAimIKOffset(raceAndGender), shooterWeapon.GetBodyAimType(), shooterWeapon.GetOffHandIKOffset(raceAndGender));

                ShooterWeapon.OffHandInfo offHandInfo = shooterWeapon.GetOffHandInfo();
                combatAgent.AnimationHandler.LimbReferences.ReachHand(offHandInfo.offHand, offHandInfo.offHandTarget, (combatAgent.AnimationHandler.IsAtRest() ? isAiming : CurrentActionClip.shouldAimOffHand & isAiming) & !combatAgent.AnimationHandler.IsReloading());
            }
        }

        private string zoomMode = "TOGGLE";
        private string blockingMode = "HOLD";
        private string lightAttackMode = Application.isMobilePlatform ? "HOLD" : "PRESS";
        private bool disableBots;
        private bool deathVibrationsEnabled;
        private void RefreshStatus()
        {
            zoomMode = FasterPlayerPrefs.Singleton.GetString("ZoomMode");
            blockingMode = FasterPlayerPrefs.Singleton.GetString("BlockingMode");
            lightAttackMode = FasterPlayerPrefs.Singleton.GetString("LightAttackMode");
            disableBots = FasterPlayerPrefs.Singleton.GetBool("DisableBots");
            deathVibrationsEnabled = FasterPlayerPrefs.Singleton.GetBool("DeathVibrationEnabled");
        }

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (!combatAgent.AnimationHandler.Animator) { return; }

            if (IsServer & IsSpawned)
            {
                if (ShouldUseAmmo())
                {
                    if (GetAmmoCount() == 0)
                    {
                        if (combatAgent.MovementHandler.GetPlayerMoveInput() == Vector2.zero) { OnReload(); }
                    }
                }
            }

            foreach (KeyValuePair<Weapon.WeaponBone, RuntimeWeapon> kvp in weaponInstances)
            {
                kvp.Value.SetActive(!CurrentActionClip.weaponBonesToHide.Contains(kvp.Key) | combatAgent.AnimationHandler.IsAtRest());
            }
        }

        public void Reload() { OnReload(); }

        void OnReload()
        {
            if (combatAgent.LoadoutManager.GetAmmoCount(weaponInstance) == weaponInstance.GetMaxAmmoCount()) { return; }
            if (combatAgent.AnimationHandler.IsReloading()) { return; }
            if (!combatAgent.AnimationHandler.IsAtRest()) { return; }

            ActionClip reloadClip = GetWeapon().GetReloadClip();
            if (reloadClip) { combatAgent.AnimationHandler.PlayAction(reloadClip); }
        }

#if UNITY_EDITOR
        void OnTimeScaleChange()
        {
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
            FasterPlayerPrefs.Singleton.SetBool("DisableBots", !disableBots);
            if (disableBots)
            {
                Debug.Log("Enabled Bot AI");
            }
            else
            {
                Debug.Log("Disabled Bot AI");
            }
        }

        void OnAddForce()
        {
            combatAgent.MovementHandler.Rigidbody.AddForce((transform.forward + Vector3.up) * 50);
        }
#endif
        public List<Weapon.InputAttackType> GetInputHistory() { return inputHistory.ToList(); }
        private List<Weapon.InputAttackType> inputHistory = new List<Weapon.InputAttackType>();
        private ActionClip GetAttack(Weapon.InputAttackType inputAttackType)
        {
            if (combatAgent.AnimationHandler.WaitingForActionClipToPlay) { return null; }
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
            else if (combatAgent.AnimationHandler.IsAtRest()
                | combatAgent.AnimationHandler.IsDodging()
                | combatAgent.AnimationHandler.IsPlayingBlockingHitReaction())
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
            else if (combatAgent.AnimationHandler.IsFlinching())
            {
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

        private void ResetComboSystem() { inputHistory.Clear(); }

        public ActionClip SelectAttack(Weapon.InputAttackType inputAttackType, List<Weapon.InputAttackType> inputHistory)
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
                        conditionMet = combatAgent.MovementHandler.GetPlayerMoveInput().y > 0.7f;
                        break;
                    case Weapon.ComboCondition.InputBackwards:
                        conditionMet = combatAgent.MovementHandler.GetPlayerMoveInput().y < -0.7f;
                        break;
                    case Weapon.ComboCondition.InputLeft:
                        conditionMet = combatAgent.MovementHandler.GetPlayerMoveInput().x < -0.7f;
                        break;
                    case Weapon.ComboCondition.InputRight:
                        conditionMet = combatAgent.MovementHandler.GetPlayerMoveInput().x > 0.7f;
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