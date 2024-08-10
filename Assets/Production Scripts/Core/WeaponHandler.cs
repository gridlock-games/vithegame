using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using System.Linq;
using UnityEngine.InputSystem;
using Vi.Utility;
using Vi.Core.VFX;

namespace Vi.Core
{
    public class WeaponHandler : NetworkBehaviour
    {
        private Dictionary<Weapon.WeaponBone, RuntimeWeapon> weaponInstances = new Dictionary<Weapon.WeaponBone, RuntimeWeapon>();
        private List<ShooterWeapon> shooterWeapons = new List<ShooterWeapon>();

        public Weapon GetWeapon() { return weaponInstance; }

        public Dictionary<Weapon.WeaponBone, RuntimeWeapon> GetWeaponInstances() { return weaponInstances; }

        private Weapon weaponInstance;
        private Attributes attributes;
        private AnimationHandler animationHandler;
        private MovementHandler movementHandler;
        private LoadoutManager loadoutManager;

        private void Awake()
        {
            animationHandler = GetComponent<AnimationHandler>();
            attributes = GetComponent<Attributes>();
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
            yield return new WaitUntil(() => !animationHandler.IsAiming());
            animationHandler.Animator.runtimeAnimatorController = AnimatorOverrideControllerInstance;
        }

        private List<GameObject> stowedWeaponInstances = new List<GameObject>();
        public void SetStowedWeapon(Weapon weapon)
        {
            if (!weapon) { return; }
            foreach (GameObject g in stowedWeaponInstances)
            {
                Destroy(g);
            }
            stowedWeaponInstances.Clear();

            foreach (Weapon.WeaponModelData data in weapon.GetWeaponModelData())
            {
                if (data.skinPrefab.name == animationHandler.LimbReferences.name.Replace("(Clone)", ""))
                {
                    foreach (Weapon.WeaponModelData.Data modelData in data.data)
                    {
                        GameObject instance = Instantiate(modelData.weaponPrefab, animationHandler.LimbReferences.GetStowedWeaponParent(), true);
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
                Destroy(kvp.Value.gameObject);
            }
            weaponInstances.Clear();
            shooterWeapons.Clear();

            CanAim = false;
            CanADS = false;

            bool broken = false;
            foreach (Weapon.WeaponModelData data in weaponInstance.GetWeaponModelData())
            {
                if (data.skinPrefab.name == animationHandler.LimbReferences.name.Replace("(Clone)", ""))
                {
                    foreach (Weapon.WeaponModelData.Data modelData in data.data)
                    {
                        RuntimeWeapon instance = Instantiate(modelData.weaponPrefab).GetComponent<RuntimeWeapon>();

                        if (!instance)
                        {
                            Debug.LogError(instance + " does not have a runtime weapon component!");
                            continue;
                        }

                        instance.SetWeaponBone(modelData.weaponBone);
                        weaponInstances.Add(modelData.weaponBone, instance);
                        instance.transform.localScale = modelData.weaponPrefab.transform.localScale;

                        Transform bone = null;
                        switch (modelData.weaponBone)
                        {
                            case Weapon.WeaponBone.Root:
                                bone = transform;
                                break;
                            default:
                                bone = animationHandler.Animator.GetBoneTransform((HumanBodyBones)modelData.weaponBone);
                                break;
                        }

                        instance.transform.SetParent(bone);
                        instance.transform.localPosition = modelData.weaponPositionOffset;
                        instance.transform.localRotation = Quaternion.Euler(modelData.weaponRotationOffset);

                        ShooterWeapon shooterWeapon = instance.GetComponent<ShooterWeapon>();
                        if (shooterWeapon)
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

            if (!CanAim & animationHandler.IsAiming()) { animationHandler.LimbReferences.OnCannotAim(); }

            animationHandler.LimbReferences.SetMeleeVerticalAimEnabled(!CanAim);

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
                    animationHandler.PlayAction(flashAttack);
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
            foreach (ActionClip.ActionSoundEffect actionSoundEffect in CurrentActionClip.GetActionClipSoundEffects(attributes.GetRaceAndGender(), actionSoundEffectIdTracker))
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
                    attributes.TryAddStatus(status.status, status.value, status.duration, status.delay, true);
                }
            }
        }

        public GameObject SpawnPreviewVFX(ActionClip actionClip, ActionVFXPreview actionPreviewVFXPrefab, Transform attackerTransform, Transform victimTransform = null)
        {
            actionPreviewVFXPrefab.vfxPositionOffset = actionClip.actionVFXList[0].vfxPositionOffset + actionClip.previewActionVFXPositionOffset;
            actionPreviewVFXPrefab.vfxRotationOffset = (Quaternion.Euler(actionClip.actionVFXList[0].vfxRotationOffset) * Quaternion.Euler(actionClip.previewActionVFXRotationOffset)).eulerAngles;
            actionPreviewVFXPrefab.vfxSpawnType = actionClip.actionVFXList[0].vfxSpawnType;
            actionPreviewVFXPrefab.transformType = actionClip.actionVFXList[0].transformType;
            actionPreviewVFXPrefab.onActivateVFXSpawnNormalizedTime = actionClip.actionVFXList[0].onActivateVFXSpawnNormalizedTime;
            actionPreviewVFXPrefab.raycastOffset = actionClip.actionVFXList[0].raycastOffset;
            actionPreviewVFXPrefab.fartherRaycastOffset = actionClip.actionVFXList[0].fartherRaycastOffset;
            actionPreviewVFXPrefab.raycastMaxDistance = actionClip.actionVFXList[0].raycastMaxDistance;
            actionPreviewVFXPrefab.lookRotationUpDirection = actionClip.actionVFXList[0].lookRotationUpDirection;
            actionPreviewVFXPrefab.weaponBone = actionClip.actionVFXList[0].weaponBone;

            return SpawnActionVFX(actionClip, actionPreviewVFXPrefab, attackerTransform, victimTransform);
        }

        private List<ActionVFX> actionVFXTracker = new List<ActionVFX>();
        public GameObject SpawnActionVFX(ActionClip actionClip, ActionVFX actionVFXPrefab, Transform attackerTransform, Transform victimTransform = null)
        {
            bool isPreviewVFX = actionVFXPrefab.GetComponent<ActionVFXPreview>();
            if (!IsServer & !isPreviewVFX) { Debug.LogError("Trying to spawn an action vfx when we aren't the server! " + actionClip); return null; }

            if (actionVFXTracker.Contains(actionVFXPrefab)) { return null; }
            GameObject vfxInstance = null;
            switch (actionVFXPrefab.transformType)
            {
                case ActionVFX.TransformType.Stationary:
                    if (actionVFXPrefab.TryGetComponent(out PooledObject pooledObject))
                        vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? attackerTransform : null).gameObject;
                    else
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? attackerTransform : null);

                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.ParentToOriginator:
                    if (actionVFXPrefab.TryGetComponent(out pooledObject))
                        vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), attackerTransform).gameObject;
                    else
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), attackerTransform);

                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.SpawnAtWeaponPoint:
                    if (actionVFXPrefab.TryGetComponent(out pooledObject))
                        vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, weaponInstances[actionVFXPrefab.weaponBone].transform.position, weaponInstances[actionVFXPrefab.weaponBone].transform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? weaponInstances[actionVFXPrefab.weaponBone].transform : null).gameObject;
                    else
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject, weaponInstances[actionVFXPrefab.weaponBone].transform.position, weaponInstances[actionVFXPrefab.weaponBone].transform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? weaponInstances[actionVFXPrefab.weaponBone].transform : null);

                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.Projectile:
                    if (actionVFXPrefab.TryGetComponent(out pooledObject))
                    {
                        vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), attackerTransform).gameObject;
                        vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                        break;
                    }

                    foreach (Weapon.WeaponBone weaponBone in actionClip.effectedWeaponBones)
                    {
                        if (weaponInstances[weaponBone].TryGetComponent(out ShooterWeapon shooterWeapon))
                        {
                            if (actionVFXPrefab.TryGetComponent(out pooledObject))
                                vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, shooterWeapon.GetProjectileSpawnPoint().position, shooterWeapon.GetProjectileSpawnPoint().rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? shooterWeapon.GetProjectileSpawnPoint() : null).gameObject;
                            else
                                vfxInstance = Instantiate(actionVFXPrefab.gameObject, shooterWeapon.GetProjectileSpawnPoint().position, shooterWeapon.GetProjectileSpawnPoint().rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? shooterWeapon.GetProjectileSpawnPoint() : null);

                            vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                        }
                        else
                        {
                            if (actionVFXPrefab.TryGetComponent(out pooledObject))
                                vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, weaponInstances[weaponBone].transform.position, Quaternion.LookRotation(animationHandler.GetAimPoint() - weaponInstances[weaponBone].transform.position) * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? weaponInstances[weaponBone].transform : null).gameObject;
                            else
                                vfxInstance = Instantiate(actionVFXPrefab.gameObject, weaponInstances[weaponBone].transform.position, Quaternion.LookRotation(animationHandler.GetAimPoint() - weaponInstances[weaponBone].transform.position) * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? weaponInstances[weaponBone].transform : null);
                            
                            vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                        }
                    }
                    break;
                case ActionVFX.TransformType.ConformToGround:
                    Vector3 startPos = attackerTransform.position + attackerTransform.rotation * actionVFXPrefab.raycastOffset;
                    Vector3 fartherStartPos = attackerTransform.position + attackerTransform.rotation * actionVFXPrefab.fartherRaycastOffset;
                    bool bHit = Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, actionVFXPrefab.raycastMaxDistance, LayerMask.GetMask(ActionVFX.layersToAccountForInRaycasting), QueryTriggerInteraction.Ignore);
                    bool fartherBHit = Physics.Raycast(fartherStartPos, Vector3.down, out RaycastHit fartherHit, actionVFXPrefab.raycastMaxDistance * 2, LayerMask.GetMask(ActionVFX.layersToAccountForInRaycasting), QueryTriggerInteraction.Ignore);

                    if (Application.isEditor)
                    {
                        if (bHit) { Debug.DrawLine(startPos, hit.point, Color.red, 2); }
                        if (fartherBHit) { Debug.DrawLine(fartherStartPos, fartherHit.point, Color.magenta, 2); }
                    }

                    if (bHit & fartherBHit)
                    {
                        Vector3 spawnPosition = hit.point + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset;
                        Vector3 rel = fartherHit.point - spawnPosition;
                        Quaternion groundRotation = rel == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(rel, actionVFXPrefab.lookRotationUpDirection);

                        if (actionVFXPrefab.TryGetComponent(out pooledObject))
                        {
                            vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject,
                                hit.point + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                                groundRotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset),
                                isPreviewVFX ? attackerTransform : null
                            ).gameObject;
                        }
                        else
                        {
                            vfxInstance = Instantiate(actionVFXPrefab.gameObject,
                                hit.point + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                                groundRotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset),
                                isPreviewVFX ? attackerTransform : null
                            );
                        }
                    }
                    else
                    {
                        if (actionVFXPrefab.TryGetComponent(out pooledObject))
                        {
                            vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject,
                                attackerTransform.position + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                                attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset),
                                isPreviewVFX ? attackerTransform : null
                            ).gameObject;
                        }
                        else
                        {
                            vfxInstance = Instantiate(actionVFXPrefab.gameObject,
                                attackerTransform.position + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                                attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset),
                                isPreviewVFX ? attackerTransform : null
                            );
                        }
                    }
                    break;
                case ActionVFX.TransformType.ParentToVictim:
                    if (!victimTransform) { Debug.LogError("VFX has transform type Parent To Victim, but there was no victim transform provided!" + actionVFXPrefab); break; }
                    
                    if (actionVFXPrefab.TryGetComponent(out pooledObject))
                        vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, victimTransform.position, victimTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), victimTransform).gameObject;
                    else
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject, victimTransform.position, victimTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), victimTransform);

                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.StationaryOnVictim:
                    if (!victimTransform) { Debug.LogError("VFX has transform type Parent To Victim, but there was no victim transform provided!" + actionVFXPrefab); break; }
                    
                    if (actionVFXPrefab.TryGetComponent(out pooledObject))
                        vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, victimTransform.position, victimTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset)).gameObject;
                    else
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject, victimTransform.position, victimTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset));

                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.AimAtTarget:
                    if (actionVFXPrefab.TryGetComponent(out pooledObject))
                        vfxInstance = ObjectPoolingManager.SpawnObject(pooledObject, attackerTransform.position, attackerTransform.rotation, isPreviewVFX ? attackerTransform : null).gameObject;
                    else
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject, attackerTransform.position, attackerTransform.rotation, isPreviewVFX ? attackerTransform : null);

                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;

                    // Look at point then apply rotation offset
                    vfxInstance.transform.LookAt(animationHandler.GetAimPoint());
                    vfxInstance.transform.rotation *= Quaternion.Euler(actionVFXPrefab.vfxRotationOffset);
                    break;
                default:
                    Debug.LogError(actionVFXPrefab.transformType + " has not been implemented yet!");
                    break;
            }

            if (isPreviewVFX)
            {
                ActionVFXPreview previewInstance = vfxInstance.GetComponent<ActionVFXPreview>();
                previewInstance.vfxPositionOffset = actionClip.actionVFXList[0].vfxPositionOffset;
                previewInstance.vfxRotationOffset = actionClip.actionVFXList[0].vfxRotationOffset;
                previewInstance.vfxSpawnType = actionClip.actionVFXList[0].vfxSpawnType;
                previewInstance.transformType = actionClip.actionVFXList[0].transformType;
                previewInstance.onActivateVFXSpawnNormalizedTime = actionClip.actionVFXList[0].onActivateVFXSpawnNormalizedTime;
                previewInstance.raycastOffset = actionClip.actionVFXList[0].raycastOffset;
                previewInstance.fartherRaycastOffset = actionClip.actionVFXList[0].fartherRaycastOffset;
                previewInstance.raycastMaxDistance = actionClip.actionVFXList[0].raycastMaxDistance;
                previewInstance.lookRotationUpDirection = actionClip.actionVFXList[0].lookRotationUpDirection;
                previewInstance.weaponBone = actionClip.actionVFXList[0].weaponBone;
            }

            if (vfxInstance)
            {
                if (isPreviewVFX)
                {
                    vfxInstance.transform.localScale = actionClip.previewActionVFXScale;
                }
                else
                {
                    if (!IsServer) { Debug.LogError("Why the fuck are we not the server here!?"); return null; }
                    NetworkObject netObj = vfxInstance.GetComponent<NetworkObject>();
                    Transform parent = netObj.transform.parent;
                    netObj.transform.parent = null;
                    netObj.SpawnWithOwnership(OwnerClientId, true);
                    netObj.TrySetParent(parent);
                    if (vfxInstance.TryGetComponent(out GameInteractiveActionVFX gameInteractiveActionVFX))
                    {
                        gameInteractiveActionVFX.InitializeVFX(attributes, CurrentActionClip);
                    }
                }
            }
            else if (!isPreviewVFX & actionVFXPrefab.transformType != ActionVFX.TransformType.ConformToGround)
            {
                Debug.LogError("No vfx instance spawned for this prefab! " + actionVFXPrefab);
            }

            if (actionVFXPrefab.vfxSpawnType == ActionVFX.VFXSpawnType.OnActivate & !isPreviewVFX) { actionVFXTracker.Add(actionVFXPrefab); }

            return vfxInstance;
        }

        public bool IsInAnticipation { get; private set; }
        private bool isAboutToAttack;
        public bool IsAttacking { get; private set; }
        public bool IsInRecovery { get; private set; }

        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!animationHandler.Animator) { return; }

            if (animationHandler.IsAtRest() | CurrentActionClip.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                IsBlocking = attributes.GetSpirit() > 0 | attributes.GetStamina() / attributes.GetMaxStamina() > Attributes.minStaminaPercentageToBeAbleToBlock && isBlocking.Value;
            }
            else
            {
                IsBlocking = false;
            }

            if (animationHandler.IsActionClipPlaying(CurrentActionClip))
            {
                float normalizedTime = animationHandler.GetActionClipNormalizedTime(CurrentActionClip);
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
                foreach (ActionClip.ActionSoundEffect actionSoundEffect in CurrentActionClip.GetActionClipSoundEffects(attributes.GetRaceAndGender(), actionSoundEffectIdTracker))
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
            else if (CurrentActionClip.IsAttack())
            {
                bool lastIsAttacking = IsAttacking;
                if (animationHandler.IsActionClipPlaying(CurrentActionClip))
                {
                    float normalizedTime = animationHandler.GetActionClipNormalizedTime(CurrentActionClip);
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
                            attributes.ProcessEnvironmentDamage(-CurrentActionClip.healthPenaltyOnMiss, NetworkObject);
                            attributes.AddStamina(-CurrentActionClip.staminaPenaltyOnMiss);
                            attributes.AddRage(-CurrentActionClip.ragePenaltyOnMiss);
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
                    Aim(aiming.Value & animationHandler.CanAim());
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
                    animationHandler.PlayAction(actionClip);
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
            animationHandler.SetHeavyAttackPressedState(isPressed);

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
                        animationHandler.PlayAction(actionClip);
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
                                if (preview.CanCast & GetAttack(Weapon.InputAttackType.Ability1)) { animationHandler.PlayAction(actionClip); }
                            }
                            ObjectPoolingManager.ReturnObjectToPool(ability1PreviewInstance.GetComponent<PooledObject>());
                            ability1PreviewInstance = null;
                        }
                    }
                }
                else if (GetAttack(Weapon.InputAttackType.Ability1) & isPressed) // If there is no preview VFX
                {
                    animationHandler.PlayAction(actionClip);
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
                                if (preview.CanCast & GetAttack(Weapon.InputAttackType.Ability2)) { animationHandler.PlayAction(actionClip); }
                            }
                            ObjectPoolingManager.ReturnObjectToPool(ability2PreviewInstance.GetComponent<PooledObject>());
                            ability2PreviewInstance = null;
                        }
                    }
                }
                else if (GetAttack(Weapon.InputAttackType.Ability2) & isPressed) // If there is no preview VFX
                {
                    animationHandler.PlayAction(actionClip);
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
                                if (preview.CanCast & GetAttack(Weapon.InputAttackType.Ability3)) { animationHandler.PlayAction(actionClip); }
                            }
                            ObjectPoolingManager.ReturnObjectToPool(ability3PreviewInstance.GetComponent<PooledObject>());
                            ability3PreviewInstance = null;
                        }
                    }
                }
                else if (GetAttack(Weapon.InputAttackType.Ability3) & isPressed) // If there is no preview VFX
                {
                    animationHandler.PlayAction(actionClip);
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
                                if (preview.CanCast & GetAttack(Weapon.InputAttackType.Ability4)) { animationHandler.PlayAction(actionClip); }
                            }
                            ObjectPoolingManager.ReturnObjectToPool(ability4PreviewInstance.GetComponent<PooledObject>());
                            ability4PreviewInstance = null;
                        }
                    }
                }
                else if (GetAttack(Weapon.InputAttackType.Ability4) & isPressed) // If there is no preview VFX
                {
                    animationHandler.PlayAction(actionClip);
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

        public bool IsAiming(LimbReferences.Hand hand) { return animationHandler.LimbReferences.IsAiming(hand) & animationHandler.CanAim(); }

        public bool IsAiming() { return aiming.Value & animationHandler.CanAim(); }

        private void Aim(bool isAiming)
        {
            animationHandler.Animator.SetBool("Aiming", isAiming);
            foreach (ShooterWeapon shooterWeapon in shooterWeapons)
            {
                CharacterReference.RaceAndGender raceAndGender = attributes.CachedPlayerData.character.raceAndGender;
                animationHandler.LimbReferences.AimHand(shooterWeapon.GetAimHand(), shooterWeapon.GetAimHandIKOffset(raceAndGender), isAiming & !animationHandler.IsReloading(), animationHandler.IsAtRest() || CurrentActionClip.shouldAimBody, shooterWeapon.GetBodyAimIKOffset(raceAndGender), shooterWeapon.GetBodyAimType());
                ShooterWeapon.OffHandInfo offHandInfo = shooterWeapon.GetOffHandInfo();
                animationHandler.LimbReferences.ReachHand(offHandInfo.offHand, offHandInfo.offHandTarget, (animationHandler.IsAtRest() ? isAiming : CurrentActionClip.shouldAimOffHand & isAiming) & !animationHandler.IsReloading());
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

            if (!animationHandler.Animator) { return; }

            animationHandler.Animator.SetBool("Blocking", IsBlocking);

            if (IsServer)
            {
                reloadingAnimParameterValue.Value = animationHandler.Animator.GetBool("Reloading");

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
                animationHandler.Animator.SetBool("Reloading", reloadingAnimParameterValue.Value);
            }

            foreach (KeyValuePair<Weapon.WeaponBone, RuntimeWeapon> kvp in weaponInstances)
            {
                kvp.Value.SetActive(!CurrentActionClip.weaponBonesToHide.Contains(kvp.Key) | animationHandler.IsAtRest());
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
            if (animationHandler.IsReloading()) { return; }
            if (loadoutManager.GetAmmoCount(weaponInstance) == weaponInstance.GetMaxAmmoCount()) { return; }
            if (!animationHandler.IsAtRest()) { return; }

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
            animationHandler.Animator.SetBool("Reloading", true);
            yield return new WaitUntil(() => animationHandler.IsFinishingReload());
            animationHandler.Animator.SetBool("Reloading", false);
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
            if (animationHandler.WaitingForActionToPlay) { return null; }
            if (animationHandler.IsReloading()) { return null; }

            // If we are in recovery
            if (IsInRecovery)
            {
                ActionClip actionClip = null;
                // If not transitioning to a different action
                if (!animationHandler.Animator.IsInTransition(animationHandler.Animator.GetLayerIndex("Actions")))
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
            else if ((animationHandler.IsAtRest() & !animationHandler.IsFlinching()) | animationHandler.IsDodging() | animationHandler.IsPlayingBlockingHitReaction())
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