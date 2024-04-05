using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

namespace Vi.Core
{
    public class WeaponHandler : NetworkBehaviour
    {
        private Dictionary<Weapon.WeaponBone, GameObject> weaponInstances = new Dictionary<Weapon.WeaponBone, GameObject>();

        public Weapon GetWeapon() { return weaponInstance; }

        public override void OnNetworkSpawn()
        {
            isBlocking.OnValueChanged += OnIsBlockingChange;
        }

        public override void OnNetworkDespawn()
        {
            isBlocking.OnValueChanged -= OnIsBlockingChange;
        }

        private void OnIsBlockingChange(bool prev, bool current)
        {
            animationHandler.Animator.SetBool("Blocking", current);
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
        }

        public void SetNewWeapon(Weapon weapon, RuntimeAnimatorController runtimeAnimatorController)
        {
            if (IsOwner & aiming.Value) { aiming.Value = false; return; }

            weaponInstance = weapon;
            animationHandler.Animator.runtimeAnimatorController = runtimeAnimatorController;
            EquipWeapon();
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

            bool broken = false;
            foreach (Weapon.WeaponModelData data in weaponInstance.GetWeaponModelData())
            {
                if (data.skinPrefab.name == animationHandler.LimbReferences.name.Replace("(Clone)", ""))
                {
                    foreach (Weapon.WeaponModelData.Data modelData in data.data)
                    {
                        GameObject instance = Instantiate(modelData.weaponPrefab);
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

            animationHandler.LimbReferences.SetMeleeVerticalAimEnabled(!CanAim);

            weaponInstances = instances;
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

        public void SetActionClip(ActionClip actionClip, string weaponName)
        {
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
            actionPreviewVFXPrefab.crossProductDirection = actionClip.actionVFXList[0].crossProductDirection;
            actionPreviewVFXPrefab.lookRotationUpDirection = actionClip.actionVFXList[0].lookRotationUpDirection;
            actionPreviewVFXPrefab.weaponBone = actionClip.actionVFXList[0].weaponBone;

            SpawnActionVFX(actionClip, actionPreviewVFXPrefab, attackerTransform, victimTransform);
        }

        private ActionVFXPreview actionVFXPreviewInstance;
        private List<ActionVFX> actionVFXTracker = new List<ActionVFX>();
        public void SpawnActionVFX(ActionClip actionClip, ActionVFX actionVFXPrefab, Transform attackerTransform, Transform victimTransform = null)
        {
            bool isPreviewVFX = actionVFXPrefab.GetComponent<ActionVFXPreview>();

            if (actionVFXTracker.Contains(actionVFXPrefab)) { return; }
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
                    //startPos.y += actionVFXPrefab.raycastOffset.y;
                    bool bHit = Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, 50, LayerMask.GetMask(new string[] { "Default" }), QueryTriggerInteraction.Ignore);
                    Debug.DrawRay(startPos, Vector3.down * 50, Color.red, 3);

                    if (bHit)
                    {
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject,
                            hit.point + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                            Quaternion.LookRotation(Vector3.Cross(hit.normal, actionVFXPrefab.crossProductDirection), actionVFXPrefab.lookRotationUpDirection) * attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset),
                            isPreviewVFX ? attackerTransform : null
                        );
                    }
                    else
                    {
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject,
                            attackerTransform.position + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                            attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset),
                            isPreviewVFX ? attackerTransform : null
                        );
                    }
                    break;
                //case ActionVFX.TransformType.OriginatorAndTarget:
                //    break;
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
            else
            {
                Debug.LogError("No vfx instance spawned for this prefab! " + actionVFXPrefab);
            }

            if (actionVFXPrefab.vfxSpawnType == ActionVFX.VFXSpawnType.OnActivate & !isPreviewVFX) { actionVFXTracker.Add(actionVFXPrefab); }
        }

        public IEnumerator DestroyVFXWhenFinishedPlaying(GameObject vfxInstance)
        {
            ParticleSystem particleSystem = vfxInstance.GetComponentInChildren<ParticleSystem>();
            if (particleSystem) { yield return new WaitUntil(() => !particleSystem.isPlaying); }

            AudioSource audioSource = vfxInstance.GetComponentInChildren<AudioSource>();
            if (audioSource) { yield return new WaitUntil(() => !audioSource.isPlaying); }

            VisualEffect visualEffect = vfxInstance.GetComponentInChildren<VisualEffect>();
            if (visualEffect) { yield return new WaitUntil(() => !visualEffect.HasAnySystemAwake()); }

            Destroy(vfxInstance);
        }

        public bool IsInAnticipation { get; private set; }
        public bool IsAttacking { get; private set; }
        public bool IsInRecovery { get; private set; }

        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!animationHandler.Animator) { return; }
            if (!CurrentActionClip) { CurrentActionClip = ScriptableObject.CreateInstance<ActionClip>(); }

            if (animationHandler.Animator.GetCurrentAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).IsName(CurrentActionClip.name)
                    | animationHandler.Animator.GetNextAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).IsName(CurrentActionClip.name))
            {
                if (CurrentActionClip.isUninterruptable) { attributes.SetUninterruptable(Time.deltaTime * 2); }
                if (CurrentActionClip.isInvincible) { attributes.SetInviniciblity(Time.deltaTime * 2); }
            }

            if ((animationHandler.Animator.GetCurrentAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).IsName("Empty") & !animationHandler.Animator.IsInTransition(animationHandler.Animator.GetLayerIndex("Actions")))
                | CurrentActionClip.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                IsBlocking = isBlocking.Value;
            }
            else
            {
                IsBlocking = false;
            }

            ActionClip.ClipType[] attackClipTypes = new ActionClip.ClipType[] { ActionClip.ClipType.LightAttack, ActionClip.ClipType.HeavyAttack, ActionClip.ClipType.Ability, ActionClip.ClipType.FlashAttack };
            if (currentActionClipWeapon != weaponInstance.name)
            {
                IsInAnticipation = false;
                IsAttacking = false;
                IsInRecovery = false;
            }
            else if (attackClipTypes.Contains(CurrentActionClip.GetClipType()))
            {
                bool lastIsAttacking = IsAttacking;
                if (animationHandler.Animator.GetCurrentAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).IsName(CurrentActionClip.name))
                {
                    float normalizedTime = animationHandler.Animator.GetCurrentAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).normalizedTime;
                    IsInRecovery = normalizedTime >= CurrentActionClip.recoveryNormalizedTime;
                    IsAttacking = normalizedTime >= CurrentActionClip.attackingNormalizedTime & !IsInRecovery;
                    IsInAnticipation = !IsAttacking & !IsInRecovery;

                    foreach (ActionVFX actionVFX in CurrentActionClip.actionVFXList)
                    {
                        if (actionVFX.vfxSpawnType != ActionVFX.VFXSpawnType.OnActivate) { continue; }
                        if (normalizedTime >= actionVFX.onActivateVFXSpawnNormalizedTime)
                        {
                            SpawnActionVFX(CurrentActionClip, actionVFX, transform);
                        }
                    }
                }
                else if (animationHandler.Animator.GetNextAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).IsName(CurrentActionClip.name))
                {
                    float normalizedTime = animationHandler.Animator.GetNextAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).normalizedTime;
                    IsInRecovery = normalizedTime >= CurrentActionClip.recoveryNormalizedTime;
                    IsAttacking = normalizedTime >= CurrentActionClip.attackingNormalizedTime & !IsInRecovery;
                    IsInAnticipation = !IsAttacking & !IsInRecovery;

                    foreach (ActionVFX actionVFX in CurrentActionClip.actionVFXList)
                    {
                        if (actionVFX.vfxSpawnType != ActionVFX.VFXSpawnType.OnActivate) { continue; }
                        if (normalizedTime >= actionVFX.onActivateVFXSpawnNormalizedTime)
                        {
                            SpawnActionVFX(CurrentActionClip, actionVFX, transform);
                        }
                    }
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
                            AudioManager.Singleton.PlayClipAtPoint(weaponInstance.GetAttackSoundEffect(weaponBone), weaponInstances[weaponBone].transform.position);
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
                            attributes.AddDefense(-CurrentActionClip.defensePenaltyOnMiss);
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

            if (IsInAnticipation)
            {
                Aim(CurrentActionClip.aimDuringAnticipation ? IsInAnticipation : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction, true);
            }
            else if (IsAttacking)
            {
                Aim(CurrentActionClip.aimDuringAttack ? IsAttacking : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction, true);
            }
            else if (IsInRecovery)
            {
                Aim(CurrentActionClip.aimDuringRecovery ? IsInRecovery : CurrentActionClip.mustBeAiming & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction, true);
            }
            else if (animationHandler.IsAtRest())
            {
                Aim(aiming.Value, IsServer);
            }
            else
            {
                Aim(aiming.Value & CurrentActionClip.GetClipType() != ActionClip.ClipType.Dodge & CurrentActionClip.GetClipType() != ActionClip.ClipType.HitReaction, IsServer);
            }

            if (shouldRepeatLightAttack) { OnLightAttack(); }
            if (shouldRepeatHeavyAttack) { HeavyAttack(true); }
        }

        [HideInInspector] public float lastMeleeHitTime = Mathf.NegativeInfinity;

        public bool CanActivateFlashSwitch()
        {
            return IsInRecovery & CurrentActionClip.canFlashAttack;
        }

        void OnLightAttack()
        {
            ActionClip actionClip = GetAttack(Weapon.InputAttackType.LightAttack);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        private bool shouldRepeatLightAttack;
        void OnLightAttackHold(InputValue value)
        {
            shouldRepeatLightAttack = value.isPressed;
        }

        public bool CanAim { get; private set; }

        private NetworkVariable<bool> aiming = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void OnHeavyAttack(InputValue value)
        {
            HeavyAttack(value.isPressed);
        }

        private void HeavyAttack(bool isPressed)
        {
            if (isPressed)
            {
                if (actionVFXPreviewInstance)
                {
                    Destroy(actionVFXPreviewInstance.gameObject);
                    return;
                }
            }

            if (CanAim)
            {
                if (PlayerPrefs.GetString("ZoomMode") == "TOGGLE")
                {
                    if (isPressed) { aiming.Value = !aiming.Value; }
                }
                else if (PlayerPrefs.GetString("ZoomMode") == "HOLD")
                {
                    aiming.Value = isPressed;
                }
                else
                {
                    Debug.LogError("Not sure how to handle player prefs ZoomMode - " + PlayerPrefs.GetString("ZoomMode"));
                }
            }
            else if (isPressed)
            {
                ActionClip actionClip = GetAttack(Weapon.InputAttackType.HeavyAttack);
                if (actionClip != null)
                    animationHandler.PlayAction(actionClip);
            }
        }

        private bool shouldRepeatHeavyAttack;
        void OnHeavyAttackHold(InputValue value)
        {
            if (CanAim)
            {
                HeavyAttack(value.isPressed);
            }
            else
            {
                shouldRepeatHeavyAttack = value.isPressed;
            }
        }

        void OnAbility1(InputValue value)
        {
            ActionClip actionClip = GetAttack(Weapon.InputAttackType.Ability1);
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX)
                {
                    if (value.isPressed) // If we are holding down the key
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
            ActionClip actionClip = GetAttack(Weapon.InputAttackType.Ability2);
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX)
                {
                    if (value.isPressed) // If we are holding down the key
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
            ActionClip actionClip = GetAttack(Weapon.InputAttackType.Ability3);
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX)
                {
                    if (value.isPressed) // If we are holding down the key
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
            ActionClip actionClip = GetAttack(Weapon.InputAttackType.Ability4);
            if (actionClip != null)
            {
                if (actionClip.previewActionVFX)
                {
                    if (value.isPressed) // If we are holding down the key
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

        public bool IsAiming(LimbReferences.Hand hand) { return animationHandler.LimbReferences.IsAiming(hand); }

        public bool IsAiming() { return aiming.Value; }

        private void Aim(bool isAiming, bool instantAim)
        {
            foreach (KeyValuePair<Weapon.WeaponBone, GameObject> instance in weaponInstances)
            {
                if (instance.Value.TryGetComponent(out ShooterWeapon shooterWeapon))
                {
                    CharacterReference.RaceAndGender raceAndGender = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).character.raceAndGender;
                    animationHandler.LimbReferences.AimHand(shooterWeapon.GetAimHand(), shooterWeapon.GetAimHandIKOffset(raceAndGender), isAiming & !animationHandler.IsReloading(), instantAim, animationHandler.IsAtRest() || CurrentActionClip.shouldAimBody, shooterWeapon.GetBodyAimIKOffset(raceAndGender), shooterWeapon.GetBodyAimType());
                    ShooterWeapon.OffHandInfo offHandInfo = shooterWeapon.GetOffHandInfo();
                    animationHandler.LimbReferences.ReachHand(offHandInfo.offHand, offHandInfo.offHandTarget, (animationHandler.IsAtRest() ? isAiming : CurrentActionClip.shouldAimOffHand & isAiming) & !animationHandler.IsReloading(), instantAim);
                }
            }
        }

        private NetworkVariable<bool> reloadingAnimParameterValue = new NetworkVariable<bool>();
        private void Update()
        {
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

        public void SetIsBlocking(bool isBlocking)
        {
            this.isBlocking.Value = isBlocking;
        }

        public bool IsBlocking { get; private set; }
        private NetworkVariable<bool> isBlocking = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        void OnBlock(InputValue value)
        {
            isBlocking.Value = value.isPressed;
            //if (Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer)
            //{
            //    if (value.isPressed) { isBlocking.Value = !isBlocking.Value; }
            //}
            //else
            //{
            //    isBlocking.Value = value.isPressed;
            //}
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
            PlayerPrefs.SetString("DisableBots", (!bool.Parse(PlayerPrefs.GetString("DisableBots"))).ToString());

            if (bool.Parse(PlayerPrefs.GetString("DisableBots")))
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

            // If we are in recovery, and not transitioning to a different action
            if (IsInRecovery & !animationHandler.Animator.IsInTransition(animationHandler.Animator.GetLayerIndex("Actions")))
            {
                ActionClip actionClip = SelectAttack(inputAttackType, inputHistory);
                if (actionClip)
                {
                    if (ShouldUseAmmo())
                    {
                        if (actionClip.requireAmmo)
                        {
                            if (GetAmmoCount() < actionClip.maxHitLimit)
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
            else if (animationHandler.IsAtRest()) // If we are at rest
            {
                ResetComboSystem();
                ActionClip actionClip = SelectAttack(inputAttackType, inputHistory);
                if (actionClip)
                {
                    if (ShouldUseAmmo())
                    {
                        if (actionClip.requireAmmo)
                        {
                            if (GetAmmoCount() < actionClip.maxHitLimit)
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
            Weapon.Attack selectedAttack = potentialAttacks.Find(item => item.inputs.SequenceEqual(cachedInputHistory) & item.comboCondition == Weapon.ComboCondition.None);
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
                Debug.Log(movementHandler.GetMoveInput() + " " + conditionMet);

                if (conditionMet)
                {
                    selectedAttack = attack;
                    break;
                }
            }
            if (selectedAttack == null) { return null; }
            return selectedAttack.attackClip;
        }

    }
}