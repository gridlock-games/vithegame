using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using System.Linq;
using UnityEngine.InputSystem;

namespace Vi.Core
{
    public class WeaponHandler : NetworkBehaviour
    {
        [SerializeField] private Weapon weapon;

        private List<GameObject> weaponInstances = new List<GameObject>();

        Animator animator;
        AnimationHandler animationHandler;
        
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
            animator.SetBool("Blocking", current);
        }

        private Weapon weaponInstance;
        private Attributes attributes;

        private void Start()
        {
            weaponInstance = Instantiate(weapon);

            attributes = GetComponent<Attributes>();
            animator = GetComponentInChildren<Animator>();
            animationHandler = GetComponentInChildren<AnimationHandler>();
            EquipWeapon();
        }

        private void EquipWeapon()
        {
            List<GameObject> instances = new List<GameObject>();

            bool broken = false;
            foreach (Weapon.WeaponModelData data in weaponInstance.GetWeaponModelData())
            {
                if (data.skinPrefab.name == GetComponentInChildren<LimbReferences>().name)
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
                                bone = animator.GetBoneTransform((HumanBodyBones)modelData.weaponBone);
                                break;
                        }

                        instance.transform.SetParent(bone);
                        instance.transform.localPosition = modelData.weaponPositionOffset;
                        instance.transform.localRotation = Quaternion.Euler(modelData.weaponRotationOffset);

                        instance.GetComponent<RuntimeWeapon>().SetWeaponBone(modelData.weaponBone);
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

            if (IsServer)
            {
                foreach (ActionClip.StatusPayload status in CurrentActionClip.statusesToApplyOnActivate)
                {
                    attributes.TryAddStatus(status.status, status.value, status.duration, status.delay);
                }
            }
        }

        private void SpawnActionVFX(ActionVFX actionVFX)
        {
            switch (actionVFX.transformType)
            {
                case ActionVFX.TransformType.Stationary:
                    GameObject vfxInstance = Instantiate(actionVFX.gameObject, transform.position, transform.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset));
                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFX.vfxPositionOffset;
                    break;
                case ActionVFX.TransformType.ParentToOriginator:
                    vfxInstance = Instantiate(actionVFX.gameObject, transform.position, transform.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset), transform);
                    vfxInstance.transform.position += vfxInstance.transform.rotation * actionVFX.vfxPositionOffset;
                    break;
                //case ActionVFX.TransformType.OriginatorAndTarget:
                //    break;
                //case ActionVFX.TransformType.Projectile:
                //    break;
                //case ActionVFX.TransformType.ConformToGround:
                //    break;
                default:
                    Debug.LogError(actionVFX.transformType + " has not been implemented yet!");
                    break;
            }
        }

        public bool IsInAnticipation { get; private set; }
        public bool IsAttacking { get; private set; }
        public bool IsInRecovery { get; private set; }

        private bool onActivateVFXPlayed;

        private void Update()
        {
            if (!CurrentActionClip) { CurrentActionClip = ScriptableObject.CreateInstance<ActionClip>(); }

            if ((animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty") & !animator.IsInTransition(animator.GetLayerIndex("Actions")))
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
                if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(CurrentActionClip.name))
                {
                    float normalizedTime = animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime;
                    IsInRecovery = normalizedTime >= CurrentActionClip.recoveryNormalizedTime;
                    IsAttacking = normalizedTime >= CurrentActionClip.attackingNormalizedTime & !IsInRecovery;
                    IsInAnticipation = !IsAttacking & !IsInRecovery;

                    if (!onActivateVFXPlayed)
                    {
                        if (normalizedTime >= CurrentActionClip.onActivateVFXSpawnNormalizedTime)
                        {
                            foreach (ActionVFX actionVFX in CurrentActionClip.actionVFXList)
                            {
                                if (actionVFX.vfxSpawnType != ActionVFX.VFXSpawnType.OnActivate) { continue; }
                                SpawnActionVFX(actionVFX);
                            }
                            onActivateVFXPlayed = true;
                        }
                    }
                }
                else if (animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(CurrentActionClip.name))
                {
                    float normalizedTime = animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime;
                    IsInRecovery = normalizedTime >= CurrentActionClip.recoveryNormalizedTime;
                    IsAttacking = normalizedTime >= CurrentActionClip.attackingNormalizedTime & !IsInRecovery;
                    IsInAnticipation = !IsAttacking & !IsInRecovery;

                    if (!onActivateVFXPlayed)
                    {
                        if (normalizedTime >= CurrentActionClip.onActivateVFXSpawnNormalizedTime)
                        {
                            foreach (ActionVFX actionVFX in CurrentActionClip.actionVFXList)
                            {
                                if (actionVFX.vfxSpawnType != ActionVFX.VFXSpawnType.OnActivate) { continue; }
                                SpawnActionVFX(actionVFX);
                            }
                            onActivateVFXPlayed = true;
                        }
                    }
                }
                else
                {
                    IsInAnticipation = false;
                    IsAttacking = false;
                    IsInRecovery = false;
                    onActivateVFXPlayed = false;
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
                onActivateVFXPlayed = false;
            }
        }

        void OnLightAttack()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.LightAttack, animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnHeavyAttack()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.HeavyAttack, animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnAbility1()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.Ability1, animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnAbility2()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.Ability2, animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnAbility3()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.Ability3, animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnAbility4()
        {
            ActionClip actionClip = weaponInstance.GetAttack(Weapon.InputAttackType.Ability4, animator);
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