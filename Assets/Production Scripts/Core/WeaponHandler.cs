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

        private void Start()
        {
            weaponInstance = Instantiate(weapon);

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

        public ActionClip currentActionClip { get; private set; }

        public void SetActionClip(ActionClip actionClip)
        {
            currentActionClip = actionClip;
            foreach (GameObject weaponInstance in weaponInstances)
            {
                weaponInstance.GetComponent<RuntimeWeapon>().ResetHitCounter();
            }
        }

        public bool IsInAnticipation { get; private set; }
        public bool IsAttacking { get; private set; }
        public bool IsInRecovery { get; private set; }

        private void Update()
        {
            if (!currentActionClip) { currentActionClip = ScriptableObject.CreateInstance<ActionClip>(); }

            if ((animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty") & !animator.IsInTransition(animator.GetLayerIndex("Actions")))
                | currentActionClip.GetHitReactionType() == ActionClip.HitReactionType.Blocking)
            {
                IsBlocking = isBlocking.Value;
            }
            else
            {
                IsBlocking = false;
            }

            ActionClip.ClipType[] attackClipTypes = new ActionClip.ClipType[] { ActionClip.ClipType.LightAttack, ActionClip.ClipType.HeavyAttack };
            if (attackClipTypes.Contains(currentActionClip.GetClipType()))
            {
                if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(currentActionClip.name))
                {
                    float normalizedTime = animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime;
                    IsInRecovery = normalizedTime >= currentActionClip.recoveryNormalizedTime;
                    IsAttacking = normalizedTime >= currentActionClip.attackingNormalizedTime & !IsInRecovery;
                    IsInAnticipation = !IsAttacking & !IsInRecovery;
                }
                else if (animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(currentActionClip.name))
                {
                    float normalizedTime = animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime;
                    IsInRecovery = normalizedTime >= currentActionClip.recoveryNormalizedTime;
                    IsAttacking = normalizedTime >= currentActionClip.attackingNormalizedTime & !IsInRecovery;
                    IsInAnticipation = !IsAttacking & !IsInRecovery;
                }
                else
                {
                    IsInAnticipation = false;
                    IsAttacking = false;
                    IsInRecovery = false;
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

        }

        void OnAbility2()
        {

        }

        void OnAbility3()
        {

        }

        void OnAbility4()
        {

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
    }
}