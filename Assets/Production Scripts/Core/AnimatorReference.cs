using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(LimbReferences))]
    [RequireComponent(typeof(GlowRenderer))]
    public class AnimatorReference : MonoBehaviour
    {
        [SerializeField] private MaterialReplacementDefintion[] materialReplacementDefintions;
        [SerializeField] private WearableEquipmentRendererDefinition[] wearableEquipmentRendererDefinitions;

        public void ApplyCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            MaterialReplacementDefintion materialReplacementDefintion = System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == characterMaterial.materialApplicationLocation);
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in materialReplacementDefintion.skinnedMeshRenderers)
            {
                skinnedMeshRenderer.materials = new Material[] { characterMaterial.material };
            }
        }
        
        private Dictionary<CharacterReference.EquipmentType, GameObject> wearableEquipmentInstances = new Dictionary<CharacterReference.EquipmentType, GameObject>();
        public void ApplyWearableEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption)
        {
            if (wearableEquipmentInstances.ContainsKey(wearableEquipmentOption.equipmentType))
            {
                if (wearableEquipmentInstances[wearableEquipmentOption.equipmentType])
                {
                    Destroy(wearableEquipmentInstances[wearableEquipmentOption.equipmentType]);
                }

                if (wearableEquipmentOption.wearableEquipmentPrefab)
                    wearableEquipmentInstances[wearableEquipmentOption.equipmentType] = Instantiate(wearableEquipmentOption.wearableEquipmentPrefab.gameObject, transform);
            }
            else if (wearableEquipmentOption.wearableEquipmentPrefab)
            {
                wearableEquipmentInstances.Add(wearableEquipmentOption.equipmentType, Instantiate(wearableEquipmentOption.wearableEquipmentPrefab.gameObject, transform));
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = wearableEquipmentInstances[wearableEquipmentOption.equipmentType].GetComponentsInChildren<SkinnedMeshRenderer>();
            WearableEquipmentRendererDefinition wearableEquipmentRendererDefinition = System.Array.Find(wearableEquipmentRendererDefinitions, item => item.equipmentType == wearableEquipmentOption.equipmentType);
            if (wearableEquipmentRendererDefinition != null)
            {
                for (int i = 0; i < wearableEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                {
                    if (skinnedMeshRenderers.Length > 1)
                        skinnedMeshRenderers[1].materials = wearableEquipmentRendererDefinition.skinnedMeshRenderers[0].materials;
                    wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = !wearableEquipmentInstances[wearableEquipmentOption.equipmentType];
                }
            }
        }

        [System.Serializable]
        private class MaterialReplacementDefintion
        {
            public CharacterReference.MaterialApplicationLocation characterMaterialType;
            public SkinnedMeshRenderer[] skinnedMeshRenderers;
        }

        [System.Serializable]
        private class WearableEquipmentRendererDefinition
        {
            public CharacterReference.EquipmentType equipmentType;
            public SkinnedMeshRenderer[] skinnedMeshRenderers;
        }

        // Variable to store network root motion
        private Vector3 networkRootMotion;
        // Method to apply network root motion
        public Vector3 ApplyNetworkRootMotion()
        {
            Vector3 _ = networkRootMotion;
            networkRootMotion = Vector3.zero;
            return _;
        }

        // Variable to store local root motion
        private Vector3 localRootMotion;
        // Method to apply local root motion
        public Vector3 ApplyLocalRootMotion()
        {
            Vector3 _ = localRootMotion;
            localRootMotion = Vector3.zero;
            return _;
        }

        Animator animator;
        WeaponHandler weaponHandler;
        LimbReferences limbReferences;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            weaponHandler = GetComponentInParent<WeaponHandler>();
            limbReferences = GetComponent<LimbReferences>();
        }

        public bool ShouldApplyRootMotion()
        {
            if (!weaponHandler.CurrentActionClip) { return false; }
            return weaponHandler.CurrentActionClip.shouldApplyRootMotion & !animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty");
        }

        private void OnAnimatorMove()
        {
            // Check if the current animator state is not "Empty" and update networkRootMotion and localRootMotion accordingly
            if (ShouldApplyRootMotion())
            {
                //networkRootMotion += animator.deltaPosition * weaponHandler.CurrentActionClip.rootMotionMulitplier;
                //localRootMotion += animator.deltaPosition * weaponHandler.CurrentActionClip.rootMotionMulitplier;
                float normalizedTime = 0;
                if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(weaponHandler.CurrentActionClip.name))
                {
                    normalizedTime = animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime;
                }
                else if (animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(weaponHandler.CurrentActionClip.name))
                {
                    normalizedTime = animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime;
                }

                Vector3 worldSpaceRootMotion = transform.TransformDirection(animator.deltaPosition);
                worldSpaceRootMotion.x *= weaponHandler.CurrentActionClip.rootMotionSidesMultiplier.Evaluate(normalizedTime);
                worldSpaceRootMotion.y *= weaponHandler.CurrentActionClip.rootMotionVerticalMultiplier.Evaluate(normalizedTime);
                worldSpaceRootMotion.z *= weaponHandler.CurrentActionClip.rootMotionForwardMultiplier.Evaluate(normalizedTime);
                Vector3 curveAdjustedLocalRootMotion = transform.InverseTransformDirection(worldSpaceRootMotion);
                
                networkRootMotion += curveAdjustedLocalRootMotion;
                localRootMotion += curveAdjustedLocalRootMotion;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (limbReferences.RightHandFollowTarget)
            {
                if (limbReferences.RightHandFollowTarget.target)
                {
                    animator.SetIKPosition(AvatarIKGoal.RightHand, limbReferences.RightHandFollowTarget.target.position);
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, limbReferences.GetRightHandReachRig().weight);
                }
            }

            if (limbReferences.LeftHandFollowTarget)
            {
                if (limbReferences.LeftHandFollowTarget.target)
                {
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, limbReferences.LeftHandFollowTarget.target.position);
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, limbReferences.GetLeftHandReachRig().weight);
                }
            }
        }
    }
}