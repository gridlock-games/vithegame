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
        private List<GameObject> weaponInstances = new List<GameObject>();

        public Weapon GetWeapon() { return weaponInstance; }

        public override void OnNetworkSpawn()
        {
            isBlocking.OnValueChanged += OnIsBlockingChanged;
        }

        public override void OnNetworkDespawn()
        {
            isBlocking.OnValueChanged -= OnIsBlockingChanged;
        }

        private void OnIsBlockingChanged(bool prev, bool current)
        {
            animationHandler.Animator.SetBool("Blocking", current);
        }

        private Weapon weaponInstance;
        private Attributes attributes;
        private AnimationHandler animationHandler;

        private void Awake()
        {
            animationHandler = GetComponent<AnimationHandler>();
            attributes = GetComponent<Attributes>();
        }

        public void SetNewWeapon(Weapon weapon, GameObject skinPrefab)
        {
            weaponInstance = Instantiate(weapon);
            EquipWeapon(skinPrefab);
        }

        private void EquipWeapon(GameObject skinPrefab)
        {
            List<GameObject> instances = new List<GameObject>();

            bool broken = false;
            foreach (Weapon.WeaponModelData data in weaponInstance.GetWeaponModelData())
            {
                if (data.skinPrefab.name == skinPrefab.name)
                {
                    foreach (Weapon.WeaponModelData.Data modelData in data.data)
                    {
                        GameObject instance = Instantiate(modelData.weaponPrefab);
                        instances.Add(instance);
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
                    }
                    broken = true;
                    break;
                }
            }

            if (!broken)
            {
                Debug.LogError("Could not find a weapon model data element for this skin: " + GetComponentInChildren<LimbReferences>().name + " on this melee weapon: " + this);
            }

            weaponInstances = instances;
        }

        public ActionClip CurrentActionClip { get; private set; }

        public void SetActionClip(ActionClip actionClip)
        {
            CurrentActionClip = actionClip;
            foreach (GameObject weaponInstance in weaponInstances)
            {
                weaponInstance.GetComponent<RuntimeWeapon>().ResetHitCounter();
            }

            if (CurrentActionClip.GetClipType() == ActionClip.ClipType.Ability)
            {
                weaponInstance.StartAbilityCooldown(CurrentActionClip);
            }

            actionVFXTracker.Clear();

            if (IsServer)
            {
                foreach (ActionClip.StatusPayload status in CurrentActionClip.statusesToApplyToSelfOnActivate)
                {
                    attributes.TryAddStatus(status.status, status.value, status.duration, status.delay);
                }
            }
        }

        private List<ActionVFX> actionVFXTracker = new List<ActionVFX>();
        public void SpawnActionVFX(ActionVFX actionVFXPrefab, Transform attackerTransform, Transform victimTransform = null)
        {
            if (actionVFXTracker.Contains(actionVFXPrefab)) { return; }
            GameObject vfxInstance = null;
            switch (actionVFXPrefab.transformType)
            {
                case ActionVFX.TransformType.Stationary:
                    vfxInstance = Instantiate(actionVFXPrefab.gameObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset));
                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.ParentToOriginator:
                    vfxInstance = Instantiate(actionVFXPrefab.gameObject, attackerTransform.position, attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset), transform);
                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFXPrefab.vfxPositionOffset;
                    break;
                //case ActionVFX.TransformType.OriginatorAndTarget:
                //    break;
                //case ActionVFX.TransformType.Projectile:
                //    break;
                case ActionVFX.TransformType.ConformToGround:
                    Vector3 startPos = attackerTransform.position + attackerTransform.rotation * actionVFXPrefab.raycastOffset;
                    startPos.y += actionVFXPrefab.raycastOffset.y;
                    RaycastHit[] allHits = Physics.RaycastAll(startPos, Vector3.down, 50, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                    Debug.DrawRay(startPos, Vector3.down * 50, Color.red, 3);
                    System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

                    bool bHit = false;
                    RaycastHit floorHit = new RaycastHit();

                    foreach (RaycastHit hit in allHits)
                    {
                        if (hit.transform.GetComponentInParent<Attributes>()) { continue; }

                        bHit = true;
                        floorHit = hit;

                        break;
                    }

                    if (bHit)
                    {
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject,
                            floorHit.point + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                            Quaternion.LookRotation(Vector3.Cross(floorHit.normal, actionVFXPrefab.crossProductDirection), actionVFXPrefab.lookRotationUpDirection) * attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset)
                        );
                    }
                    else
                    {
                        vfxInstance = Instantiate(actionVFXPrefab.gameObject,
                            attackerTransform.position + attackerTransform.rotation * actionVFXPrefab.vfxPositionOffset,
                            attackerTransform.rotation * Quaternion.Euler(actionVFXPrefab.vfxRotationOffset)
                        );
                    }
                    break;
                default:
                    Debug.LogError(actionVFXPrefab.transformType + " has not been implemented yet!");
                    break;
            }

            if (actionVFXPrefab.vfxSpawnType == ActionVFX.VFXSpawnType.OnActivate) { actionVFXTracker.Add(actionVFXPrefab); }

            if (vfxInstance)
            {
                if (vfxInstance.TryGetComponent(out ActionVFXParticleSystem actionVFXParticleSystem))
                {
                    actionVFXParticleSystem.InitializeVFX(attributes, CurrentActionClip);
                }

                StartCoroutine(DestroyVFXWhenFinishedPlaying(vfxInstance));
            }
            else
            {
                Debug.LogError("No vfx instance spawned for this prefab! " + actionVFXPrefab);
            }
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

        private void Update()
        {
            if (!CurrentActionClip) { CurrentActionClip = ScriptableObject.CreateInstance<ActionClip>(); }

            if (CurrentActionClip.isUninterruptable)
            {
                if (animationHandler.Animator.GetCurrentAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).IsName(CurrentActionClip.name)
                    | animationHandler.Animator.GetNextAnimatorStateInfo(animationHandler.Animator.GetLayerIndex("Actions")).IsName(CurrentActionClip.name))
                {
                    attributes.SetUninterruptable(Time.deltaTime * 2);
                }
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

            ActionClip.ClipType[] attackClipTypes = new ActionClip.ClipType[] { ActionClip.ClipType.LightAttack, ActionClip.ClipType.HeavyAttack, ActionClip.ClipType.Ability };
            if (attackClipTypes.Contains(CurrentActionClip.GetClipType()))
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
                            SpawnActionVFX(actionVFX, transform);
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
                            SpawnActionVFX(actionVFX, transform);
                        }
                    }
                }
                else
                {
                    IsInAnticipation = false;
                    IsAttacking = false;
                    IsInRecovery = false;
                }

                if (IsAttacking & !lastIsAttacking)
                {
                    AudioManager.Singleton.PlayClipAtPoint(weaponInstance.GetAttackSoundEffect(CurrentActionClip.weaponBone), transform.position);
                }
            }
            else
            {
                IsInAnticipation = false;
                IsAttacking = false;
                IsInRecovery = false;
            }
        }

        void OnLightAttack()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.LightAttack, animationHandler.Animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnHeavyAttack()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.HeavyAttack, animationHandler.Animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnAbility1()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.Ability1, animationHandler.Animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnAbility2()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.Ability2, animationHandler.Animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnAbility3()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.Ability3, animationHandler.Animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnAbility4()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.Ability4, animationHandler.Animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnReload()
        {

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
    }
}