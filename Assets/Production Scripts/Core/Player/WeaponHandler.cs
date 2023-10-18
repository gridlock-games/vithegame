using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using UnityEngine.InputSystem;

namespace Vi.Player
{
    public class WeaponHandler : NetworkBehaviour
    {
        [SerializeField] private Weapon weapon;

        private List<GameObject> weaponInstances = new List<GameObject>();

        Animator animator;
        AnimationHandler animationHandler;
        
        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            animationHandler = GetComponentInChildren<AnimationHandler>();
            EquipWeapon();
        }

        private void EquipWeapon()
        {
            List<GameObject> instances = new List<GameObject>();

            bool broken = false;
            foreach (Weapon.WeaponModelData data in weapon.GetWeaponModelData())
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

        void OnLightAttack()
        {
            ActionClip actionClip = weapon.GetAttack(Weapon.InputAttackType.LightAttack, animator);
            if (actionClip != null)
                animationHandler.PlayAction(actionClip);
        }

        void OnHeavyAttack()
        {
            //ActionClip actionClip = weapon.GetAttack(Weapon.InputAttackType.HeavyAttack);
            //animationHandler.PlayAction(actionClip);
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

        void OnBlock(InputValue value)
        {
            animator.SetBool("Blocking", value.isPressed);
        }
    }
}