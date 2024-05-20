using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using Vi.Utility;

namespace Vi.Core
{
    public class WeaponHandler : NetworkBehaviour
    {
        private Dictionary<Weapon.WeaponBone, GameObject> weaponInstances = new Dictionary<Weapon.WeaponBone, GameObject>();

        public Weapon GetWeapon()
        {
            return weaponInstance;
        }

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
        }

        public bool WeaponInitialized { get; private set; }
        public void SetNewWeapon(Weapon weapon, RuntimeAnimatorController runtimeAnimatorController)
        {
            if (IsOwner) { aiming.Value = false; }

            weaponInstance = weapon;
            StartCoroutine(SwapAnimatorController(runtimeAnimatorController));
            EquipWeapon();
            WeaponInitialized = true;
        }

        private IEnumerator SwapAnimatorController(RuntimeAnimatorController runtimeAnimatorController)
        {
            yield return new WaitUntil(() => !animationHandler.IsAiming());
            animationHandler.Animator.runtimeAnimatorController = runtimeAnimatorController;
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
            foreach (KeyValuePair<Weapon.WeaponBone, GameObject> kvp in weaponInstances)
            {
                Destroy(kvp.Value);
            }
            weaponInstances.Clear();

            CanAim = false;
            Dictionary<Weapon.WeaponBone, GameObject> instances = new Dictionary<Weapon.WeaponBone, GameObject>();

            List<RuntimeWeapon> runtimeWeapons = new List<RuntimeWeapon>();
            bool broken = false;
            foreach (Weapon.WeaponModelData data in weaponInstance.GetWeaponModelData())
            {
                if (data.skinPrefab.name == animationHandler.LimbReferences.name.Replace("(Clone)", ""))
                {
                    foreach (Weapon.WeaponModelData.Data modelData in data.data)
                    {
                        GameObject instance = Instantiate(modelData.weaponPrefab);
                        runtimeWeapons.Add(instance.GetComponent<RuntimeWeapon>());
                        instances.Add(modelData.weaponBone, instance);
                        instance.transform.localScale = modelData.weaponPrefab.transform.localScale;

                        Transform bone = null;
                        switch (modelData.weaponBone)
                        {
                            case Weapon.WeaponBone.Root:
                                bone = transform;
                                break;
                            case Weapon.WeaponBone.Camera:
                                bone = Camera.main.transform;
                                break;
                            default:
                                bone = animationHandler.Animator.GetBoneTransform((HumanBodyBones)modelData.weaponBone);
                                break;
                        }

                        instance.transform.SetParent(bone);
                        instance.transform.localPosition = modelData.weaponPositionOffset;
                        instance.transform.localRotation = Quaternion.Euler(modelData.weaponRotationOffset);

                        if (instance.TryGetComponent(out RuntimeWeapon runtimeWeapon))
                        {
                            runtimeWeapon.SetWeaponBone(modelData.weaponBone);
                        }
                        else
                        {
                            Debug.LogWarning(instance + " does not have a runtime weapon component!");
                        }
                        CanAim = instance.GetComponent<ShooterWeapon>() | CanAim;
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

            weaponInstances = instances;

            foreach (KeyValuePair<Weapon.WeaponBone, GameObject> kvp in weaponInstances)
            {
                if (kvp.Value.TryGetComponent(out RuntimeWeapon runtimeWeapon))
                {
                    runtimeWeapon.SetAssociatedRuntimeWeapons(runtimeWeapons);
                }
                else
                {
                    Debug.LogError(kvp.Key + " has no runtime weapon component!");
                }
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
            foreach (KeyValuePair<Weapon.WeaponBone, GameObject> weaponInstance in weaponInstances)
            {
                weaponInstance.Value.GetComponent<RuntimeWeapon>().ResetHitCounter();
            }

            if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Ability)
            {
                weaponInstance.StartAbilityCooldown(CurrentActionClip);
            }

            actionVFXTracker.Clear();
            if (actionVFXPreviewInstance) { Destroy(actionVFXPreviewInstance.gameObject); }

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
                    attributes.TryAddStatus(status.status, status.value, status.duration, status.delay);
                }
            }
        }

        public void SpawnPreviewVFX(ActionClip actionClip, ActionVFXPreview actionPreviewVFXPrefab, Transform attackerTransform, Transform victimTransform = null)
        {
            actionPreviewVFXPrefab.vfxPositionOffset = actionClip.actionVFXList[0].vfxPositionOffset;
            actionPreviewVFXPrefab.vfxRotationOffset = actionClip.actionVFXList[0].vfxRotationOffset;
            actionPreviewVFXPrefab.vfxSpawnType = actionClip.actionVFXList[0].vfxSpawnType;
            actionPreviewVFXPrefab.transformType = actionClip.actionVFXList[0].transformType;
            actionPreviewVFXPrefab.onActivateVFXSpawnNormalizedTime = actionClip.actionVFXList[0].onActivateVFXSpawnNormalizedTime;
            actionPreviewVFXPrefab.raycastOffset = actionClip.actionVFXList[0].raycastOffset;
            actionPreviewVFXPrefab.raycastMaxDistance = actionClip.actionVFXList[0].raycastMaxDistance;
            actionPreviewVFXPrefab.crossProductDirection = actionClip.actionVFXList[0].crossProductDirection;
            actionPreviewVFXPrefab.lookRotationUpDirection = actionClip.actionVFXList[0].lookRotationUpDirection;
            actionPreviewVFXPrefab.weaponBone = actionClip.actionVFXList[0].weaponBone;

            SpawnActionVFX(actionClip, actionPreviewVFXPrefab, attackerTransform, victimTransform);
        }

        private ActionVFXPreview actionVFXPreviewInstance;
        private List<ActionVFX> actionVFXTracker = new List<ActionVFX>();
        public GameObject SpawnActionVFX(ActionClip actionClip, ActionVFX actionVFXPrefab, Transform attackerTransform, Transform victimTransform = null)
        {
            bool isPreviewVFX = actionVFXPrefab.GetComponent<ActionVFXPreview>();

            if (actionVFXTracker.Contains(actionVFXPrefab)) { return null; }
            GameObject vfxInstance = null;
            switch (actionVFXPrefab.transformType)
            {
                case ActionVFX.TransformType.Stationary:
                    vfxInstance = Instantiate(actionVFXPrefab.gameObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? attackerTransform : null);
                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.ParentToOriginator:
                    vfxInstance = Instantiate(actionVFXPrefab.gameObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), attackerTransform);
                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.SpawnAtWeaponPoint:
                    vfxInstance = Instantiate(actionVFXPrefab.gameObject, weaponInstances[actionVFXPrefab.weaponBone].transform.position, weaponInstances[actionVFXPrefab.weaponBone].transform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? weaponInstances[actionVFXPrefab.weaponBone].transform : null);
                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.Projectile:
                    if (isPreviewVFX)
                    {
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), attackerTransform);
                        vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                        break;
                    }

                    foreach (Weapon.WeaponBone weaponBone in actionClip.effectedWeaponBones)
                    {
                        if (weaponInstances[weaponBone].TryGetComponent(out ShooterWeapon shooterWeapon))
                        {
                            vfxInstance = Instantiate(actionVFXPrefab.gameObject, shooterWeapon.GetProjectileSpawnPoint().position, shooterWeapon.GetProjectileSpawnPoint().rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? shooterWeapon.GetProjectileSpawnPoint() : null);
                            vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                        }
                        else
                        {
                            vfxInstance = Instantiate(actionVFXPrefab.gameObject, weaponInstances[weaponBone].transform.position, Quaternion.LookRotation(animationHandler.GetAimPoint() - weaponInstances[weaponBone].transform.position) * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), isPreviewVFX ? weaponInstances[weaponBone].transform : null);
                            vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                        }
                    }
                    break;
                case ActionVFX.TransformType.ConformToGround:
                    Vector3 startPos = attackerTransform.position + attackerTransform.rotation * actionVFXPrefab.raycastOffset;
                    bool bHit = Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, actionVFXPrefab.raycastMaxDistance, LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
                    Debug.DrawRay(startPos, Vector3.down * actionVFXPrefab.raycastMaxDistance, Color.red, 3);

                    if (bHit)
                    {
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject,
                            hit.point + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                            Quaternion.LookRotation(Vector3.Cross(hit.normal, actionVFXPrefab.crossProductDirection), actionVFXPrefab.lookRotationUpDirection) * attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset),
                            isPreviewVFX ? attackerTransform : null
                        );
                    }
                    else if (isPreviewVFX)
                    {
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject,
                            attackerTransform.position + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                            attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset),
                            isPreviewVFX ? attackerTransform : null
                        );
                    }
                    break;
                case ActionVFX.TransformType.ParentToVictim:
                    if (!victimTransform) { Debug.LogError("VFX has transform type Parent To Victim, but there was no victim transform provided!" + actionVFXPrefab); break; }
                    vfxInstance = Instantiate(actionVFXPrefab.gameObject, victimTransform.position, victimTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), victimTransform);
                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.StationaryOnVictim:
                    if (!victimTransform) { Debug.LogError("VFX has transform type Parent To Victim, but there was no victim transform provided!" + actionVFXPrefab); break; }
                    vfxInstance = Instantiate(actionVFXPrefab.gameObject, victimTransform.position, victimTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset));
                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.AimAtTarget:
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

            if (vfxInstance)
            {
                if (vfxInstance.TryGetComponent(out ActionVFXParticleSystem actionVFXParticleSystem))
                {
                    actionVFXParticleSystem.InitializeVFX(attributes, CurrentActionClip);
                    StartCoroutine(DestroyVFXWhenFinishedPlaying(vfxInstance));
                }
                else if (vfxInstance.TryGetComponent(out ActionVFXPhysicsProjectile actionVFXPhysicsProjectile))
                {
                    actionVFXPhysicsProjectile.InitializeVFX(attributes, CurrentActionClip);
                }
                else if (vfxInstance.TryGetComponent(out ActionVFXPreview actionVFXPreview))
                {
                    actionVFXPreviewInstance = actionVFXPreview;
                }
                else
                {
                    StartCoroutine(DestroyVFXWhenFinishedPlaying(vfxInstance));
                }

                if (isPreviewVFX) { vfxInstance.transform.localScale = actionClip.previewActionVFXScale; }
            }
            else if (!isPreviewVFX & actionVFXPrefab.transformType != ActionVFX.TransformType.ConformToGround)
            {
                Debug.LogError("No vfx instance spawned for this prefab! " + actionVFXPrefab);
            }

            if (actionVFXPrefab.vfxSpawnType == ActionVFX.VFXSpawnType.OnActivate & !isPreviewVFX) { actionVFXTracker.Add(actionVFXPrefab); }

            return vfxInstance;
        }

        public static IEnumerator DestroyVFXWhenFinishedPlaying(GameObject vfxInstance)
        {
            ParticleSystem particleSystem = vfxInstance.GetComponentInChildren<ParticleSystem>();
            if (particleSystem)
            {
                while (true)
                {
                    yield return null;
                    if (!vfxInstance) { yield break; }
                    if (!particleSystem.isPlaying) { break; }
                }
            }

            AudioSource audioSource = vfxInstance.GetComponentInChildren<AudioSource>();
            if (audioSource)
            {
                while (true)
                {
                    yield return null;
                    if (!vfxInstance) { yield break; }
                    if (!audioSource.isPlaying) { break; }
                }
            }

            VisualEffect visualEffect = vfxInstance.GetComponentInChildren<VisualEffect>();
            if (visualEffect)
            {
                while (true)
                {
                    yield return null;
                    if (!vfxInstance) { yield break; }
                    if (!visualEffect.HasAnySystemAwake()) { break; }
                }
            }
            
            Destroy(vfxInstance);
        }

        public bool IsInAnticipation { get; private set; }
        public bool IsAttacking { get; private set; }
        public bool IsInRecovery { get; private set; }

        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!animationHandler.Animator) { return; }

            if (animationHandler.IsActionClipPlaying(CurrentActionClip))
            {
                if (CurrentActionClip.isUninterruptable) { attributes.SetUninterruptable(Time.deltaTime * 2); }
                if (CurrentActionClip.isInvincible) { attributes.SetInviniciblity(Time.deltaTime * 2); }
            }

            if (animationHandler.IsAtRest() | CurrentActionClip.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                IsBlocking = attributes.GetDefense() > 0 | attributes.GetStamina() / attributes.GetMaxStamina() > Attributes.minStaminaPercentageToBeAbleToBlock && isBlocking.Value;
            }
            else
            {
                IsBlocking = false;
            }

            if (animationHandler.IsActionClipPlaying(CurrentActionClip))
            {
                float normalizedTime = animationHandler.GetActionClipNormalizedTime(CurrentActionClip);
                foreach (ActionVFX actionVFX in CurrentActionClip.actionVFXList)
                {
                    if (actionVFX.vfxSpawnType != ActionVFX.VFXSpawnType.OnActivate) { continue; }
                    if (normalizedTime >= actionVFX.onActivateVFXSpawnNormalizedTime)
                    {
                        SpawnActionVFX(CurrentActionClip, actionVFX, transform);
                    }
                }
            }

            if (currentActionClipWeapon != weaponInstance.name)
            {
                IsInAnticipation = false;
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
                }
                else
                {
                    IsInAnticipation = false;
                    IsAttacking = false;
                    IsInRecovery = false;
                }

                // If we started attacking on this fixedUpdate
                if (IsAttacking & !lastIsAttacking)
                {
                    foreach (Weapon.WeaponBone weaponBone in CurrentActionClip.effectedWeaponBones)
                    {
                        if (weaponInstances[weaponBone])
                        {
                            // Don't play sound effects for shooter weapons here
                            if (weaponInstances[weaponBone].GetComponent<ShooterWeapon>()) { continue; }

                            AudioClip attackSoundEffect = weaponInstance.GetAttackSoundEffect(weaponBone);
                            if (attackSoundEffect)
                                AudioManager.Singleton.PlayClipAtPoint(gameObject, attackSoundEffect, weaponInstances[weaponBone].transform.position);
                            else if (Application.isEditor)
                                Debug.LogWarning("No attack sound effect for weapon " + weaponInstance.name + " on bone - " + weaponBone);
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
                IsAttacking = false;
                IsInRecovery = false;
            }

            if (CanAim)
            {
                if (IsInAnticipation)
                {
                    Aim(CurrentActionClip.aimDuringAnticipation ? IsInAnticipation : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction);
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
                if (!IsBlocking)
                {
                    ActionClip actionClip = GetAttack(Weapon.InputAttackType.LightAttack);
                    if (actionClip != null)
                        animationHandler.PlayAction(actionClip);
                }
            }
        }

        private Coroutine lightAttackHoldCoroutine;
        public void LightAttackHold(bool isPressed)
        {
            if (lightAttackHoldCoroutine != null) { StopCoroutine(lightAttackHoldCoroutine); }
            if (isPressed) { lightAttackHoldCoroutine = StartCoroutine(LightAttackHold()); }
            else { LightAttack(false); }
        }

        void OnLightAttackHold(InputValue value)
        {
            LightAttackHold(value.isPressed);
        }

        private IEnumerator LightAttackHold()
        {
            while (true)
            {
                yield return null;
                LightAttack(true);
            }
        }

        public bool CanAim { get; private set; }

        private NetworkVariable<bool> aiming = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void OnHeavyAttack(InputValue value)
        {
            HeavyAttack(value.isPressed);
        }

        private bool lastHeavyAttackPressedState;

        public void HeavyAttack(bool isPressed)
        {
            if (isPressed)
            {
                if (actionVFXPreviewInstance)
                {
                    Destroy(actionVFXPreviewInstance.gameObject);
                    return;
                }

                if (isPressed != lastHeavyAttackPressedState) { animationHandler.HeavyAttackPressedServerRpc(); }
            }
            else
            {
                if (isPressed != lastHeavyAttackPressedState) { animationHandler.HeavyAttackReleasedServerRpc(); }
            }

            if (CanAim)
            {
                if (NetworkObject.IsPlayerObject)
                {
                    if (FasterPlayerPrefs.Singleton.GetString("ZoomMode") == "TOGGLE")
                    {
                        if (isPressed) { aiming.Value = !aiming.Value; }
                    }
                    else if (FasterPlayerPrefs.Singleton.GetString("ZoomMode") == "HOLD")
                    {
                        aiming.Value = isPressed;
                    }
                    else
                    {
                        Debug.LogError("Not sure how to handle player prefs ZoomMode - " + FasterPlayerPrefs.Singleton.GetString("ZoomMode"));
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

            lastHeavyAttackPressedState = isPressed;
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

        void OnAbility1(InputValue value)
        {
            Ability1(value.isPressed);
        }

        public void Ability1(bool isPressed)
        {
            ActionClip actionClip = GetAttack(Weapon.InputAttackType.Ability1);
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer)
                {
                    if (isPressed) // If we are holding down the key
                    {
                        SpawnPreviewVFX(actionClip, actionClip.previewActionVFX.GetComponent<ActionVFXPreview>(), transform);
                    }
                    else // If we have released the key
                    {
                        if (actionVFXPreviewInstance)
                        {
                            animationHandler.PlayAction(actionClip);
                            Destroy(actionVFXPreviewInstance.gameObject);
                        }
                    }
                }
                else // If there is no preview VFX
                {
                    animationHandler.PlayAction(actionClip);
                }
            }
        }

        void OnAbility2(InputValue value)
        {
            Ability2(value.isPressed);
        }

        public void Ability2(bool isPressed)
        {
            ActionClip actionClip = GetAttack(Weapon.InputAttackType.Ability2);
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer)
                {
                    if (isPressed) // If we are holding down the key
                    {
                        SpawnPreviewVFX(actionClip, actionClip.previewActionVFX.GetComponent<ActionVFXPreview>(), transform);
                    }
                    else // If we have released the key
                    {
                        if (actionVFXPreviewInstance)
                        {
                            animationHandler.PlayAction(actionClip);
                            Destroy(actionVFXPreviewInstance.gameObject);
                        }
                    }
                }
                else // If there is no preview VFX
                {
                    animationHandler.PlayAction(actionClip);
                }
            }
        }

        void OnAbility3(InputValue value)
        {
            Ability3(value.isPressed);
        }

        public void Ability3(bool isPressed)
        {
            ActionClip actionClip = GetAttack(Weapon.InputAttackType.Ability3);
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer)
                {
                    if (isPressed) // If we are holding down the key
                    {
                        SpawnPreviewVFX(actionClip, actionClip.previewActionVFX.GetComponent<ActionVFXPreview>(), transform);
                    }
                    else // If we have released the key
                    {
                        if (actionVFXPreviewInstance)
                        {
                            animationHandler.PlayAction(actionClip);
                            Destroy(actionVFXPreviewInstance.gameObject);
                        }
                    }
                }
                else // If there is no preview VFX
                {
                    animationHandler.PlayAction(actionClip);
                }
            }
        }

        void OnAbility4(InputValue value)
        {
            Ability4(value.isPressed);
        }

        public void Ability4(bool isPressed)
        {
            ActionClip actionClip = GetAttack(Weapon.InputAttackType.Ability4);
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX & IsLocalPlayer)
                {
                    if (isPressed) // If we are holding down the key
                    {
                        SpawnPreviewVFX(actionClip, actionClip.previewActionVFX.GetComponent<ActionVFXPreview>(), transform);
                    }
                    else // If we have released the key
                    {
                        if (actionVFXPreviewInstance)
                        {
                            animationHandler.PlayAction(actionClip);
                            Destroy(actionVFXPreviewInstance.gameObject);
                        }
                    }
                }
                else // If there is no preview VFX
                {
                    animationHandler.PlayAction(actionClip);
                }
            }
        }

        public bool IsNextProjectileDamageMultiplied()
        {
            foreach (KeyValuePair<Weapon.WeaponBone, GameObject> instance in weaponInstances)
            {
                if (instance.Value.TryGetComponent(out ShooterWeapon shooterWeapon))
                {
                    return shooterWeapon.GetNextDamageMultiplier() > 1;
                }
            }
            return false;
        }

        public bool IsAiming(LimbReferences.Hand hand) { return animationHandler.LimbReferences.IsAiming(hand) & animationHandler.CanAim(); }

        public bool IsAiming() { return aiming.Value & animationHandler.CanAim(); }

        private void Aim(bool isAiming)
        {
            animationHandler.Animator.SetBool("Aiming", isAiming);
            foreach (KeyValuePair<Weapon.WeaponBone, GameObject> instance in weaponInstances)
            {
                if (instance.Value.TryGetComponent(out ShooterWeapon shooterWeapon))
                {
                    CharacterReference.RaceAndGender raceAndGender = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).character.raceAndGender;
                    animationHandler.LimbReferences.AimHand(shooterWeapon.GetAimHand(), shooterWeapon.GetAimHandIKOffset(raceAndGender), isAiming & !animationHandler.IsReloading(), animationHandler.IsAtRest() || CurrentActionClip.shouldAimBody, shooterWeapon.GetBodyAimIKOffset(raceAndGender), shooterWeapon.GetBodyAimType());
                    ShooterWeapon.OffHandInfo offHandInfo = shooterWeapon.GetOffHandInfo();
                    animationHandler.LimbReferences.ReachHand(offHandInfo.offHand, offHandInfo.offHandTarget, (animationHandler.IsAtRest() ? isAiming : CurrentActionClip.shouldAimOffHand & isAiming) & !animationHandler.IsReloading());
                }
            }
        }

        private NetworkVariable<bool> reloadingAnimParameterValue = new NetworkVariable<bool>();
        private void Update()
        {
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

            foreach (KeyValuePair<Weapon.WeaponBone, GameObject> kvp in weaponInstances)
            {
                if (kvp.Value.TryGetComponent(out RuntimeWeapon runtimeWeapon))
                {
                    runtimeWeapon.SetActive(!CurrentActionClip.weaponBonesToHide.Contains(kvp.Key) | animationHandler.IsAtRest());
                }
                else
                {
                    Debug.LogError(kvp.Key + " has no runtime weapon component!");
                }
            }
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

        [ServerRpc]
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

            foreach (KeyValuePair<Weapon.WeaponBone, GameObject> instance in weaponInstances)
            {
                if (instance.Value.TryGetComponent(out ShooterWeapon shooterWeapon))
                {
                    StartCoroutine(Reload(shooterWeapon));
                    break;
                }
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
            bool isPressed = value.isPressed;
            if (FasterPlayerPrefs.Singleton.GetString("BlockingMode") == "TOGGLE")
            {
                if (isPressed) { isBlocking.Value = !isBlocking.Value; }
            }
            else if (FasterPlayerPrefs.Singleton.GetString("BlockingMode") == "HOLD")
            {
                isBlocking.Value = isPressed;
            }
            else
            {
                Debug.LogError("Not sure how to handle player prefs BlockingMode - " + FasterPlayerPrefs.Singleton.GetString("BlockingMode"));
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
            FasterPlayerPrefs.Singleton.SetString("DisableBots", (!bool.Parse(FasterPlayerPrefs.Singleton.GetString("DisableBots"))).ToString());

            if (bool.Parse(FasterPlayerPrefs.Singleton.GetString("DisableBots")))
            {
                Debug.Log("Disabled Bot AI");
            }
            else
            {
                Debug.Log("Enabled Bot AI");
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
            else if (animationHandler.IsAtRest() | animationHandler.IsDodging() | animationHandler.IsPlayingBlockingHitReaction())
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