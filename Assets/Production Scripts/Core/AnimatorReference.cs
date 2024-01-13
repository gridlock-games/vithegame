using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public WebRequestManager.Character GetCharacterWebInfo(WebRequestManager.Character currentCharacter)
        {
            MaterialReplacementDefintion browsReplacementDefinition = System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == CharacterReference.MaterialApplicationLocation.Brows);

            return new WebRequestManager.Character(currentCharacter._id.ToString(), name.Replace("(Clone)", ""), currentCharacter.name.ToString(), currentCharacter.experience,
                System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == CharacterReference.MaterialApplicationLocation.Body).skinnedMeshRenderers[0].material.name.Replace(" (Instance)", ""),
                //System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == CharacterReference.MaterialApplicationLocation.Head).skinnedMeshRenderers[0].material.name.Replace(" (Instance)", ""),
                System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == CharacterReference.MaterialApplicationLocation.Eyes).skinnedMeshRenderers[0].material.name.Replace(" (Instance)", ""),
                wearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Beard) ? wearableEquipmentInstances[CharacterReference.EquipmentType.Beard].name.Replace("(Clone)", "") : "",
                browsReplacementDefinition == null ? (wearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Brows) ? wearableEquipmentInstances[CharacterReference.EquipmentType.Brows].name.Replace("(Clone)", "") : "") : browsReplacementDefinition.skinnedMeshRenderers[0].material.name.Replace(" (Instance)", ""),
                wearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Hair) ? wearableEquipmentInstances[CharacterReference.EquipmentType.Hair].name.Replace("(Clone)", "") : "",
                currentCharacter.level, currentCharacter.loadoutPreset1
            );
        }

        public void ApplyCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            if (characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body)
            {
                CharacterReference.CharacterMaterial headMaterial = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(characterMaterial.raceAndGender).Find(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Head & characterMaterial.material.name.Contains(string.Concat(item.material.name.Where(char.IsDigit))));
                ApplyCharacterMaterial(headMaterial);
            }

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
                else
                    wearableEquipmentInstances.Remove(wearableEquipmentOption.equipmentType);
            }
            else if (wearableEquipmentOption.wearableEquipmentPrefab)
            {
                wearableEquipmentInstances.Add(wearableEquipmentOption.equipmentType, Instantiate(wearableEquipmentOption.wearableEquipmentPrefab.gameObject, transform));
            }

            WearableEquipmentRendererDefinition wearableEquipmentRendererDefinition = System.Array.Find(wearableEquipmentRendererDefinitions, item => item.equipmentType == wearableEquipmentOption.equipmentType);
            if (wearableEquipmentRendererDefinition != null)
            {
                for (int i = 0; i < wearableEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                {
                    if (wearableEquipmentInstances.ContainsKey(wearableEquipmentOption.equipmentType))
                    {
                        SkinnedMeshRenderer[] equipmentSkinnedMeshRenderers = wearableEquipmentInstances[wearableEquipmentOption.equipmentType].GetComponentsInChildren<SkinnedMeshRenderer>();
                        if (equipmentSkinnedMeshRenderers.Length > 1)
                            equipmentSkinnedMeshRenderers[1].materials = wearableEquipmentRendererDefinition.skinnedMeshRenderers[0].materials;
                        wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = !wearableEquipmentInstances[wearableEquipmentOption.equipmentType];

                        glowRenderer.RegisterNewRenderer(equipmentSkinnedMeshRenderers[i]);
                    }
                    else
                    {
                        wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = true;
                    }
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
        GlowRenderer glowRenderer;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            weaponHandler = GetComponentInParent<WeaponHandler>();
            limbReferences = GetComponent<LimbReferences>();
            glowRenderer = GetComponent<GlowRenderer>();
        }

        private void Start()
        {
            SkinnedMeshRenderer[] skinnedMeshRenderersToEvaluate = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderersToEvaluate)
            {
                skinnedMeshRenderer.enabled = false;
            }
            StartCoroutine(DisplayRenderersBasedOnEquipment(skinnedMeshRenderersToEvaluate));
        }

        private IEnumerator DisplayRenderersBasedOnEquipment(SkinnedMeshRenderer[] skinnedMeshRenderersToEvaluate)
        {
            yield return null;
            List<SkinnedMeshRenderer> renderersAlreadyEvaluated = new List<SkinnedMeshRenderer>();
            foreach (CharacterReference.EquipmentType equipmentType in System.Enum.GetValues(typeof(CharacterReference.EquipmentType)))
            {
                WearableEquipmentRendererDefinition wearableEquipmentRendererDefinition = System.Array.Find(wearableEquipmentRendererDefinitions, item => item.equipmentType == equipmentType);
                if (wearableEquipmentRendererDefinition != null)
                {
                    for (int i = 0; i < wearableEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                    {
                        if (wearableEquipmentInstances.ContainsKey(equipmentType))
                        {
                            SkinnedMeshRenderer[] skinnedMeshRenderers = wearableEquipmentInstances[equipmentType].GetComponentsInChildren<SkinnedMeshRenderer>();
                            if (skinnedMeshRenderers.Length > 1)
                                skinnedMeshRenderers[1].materials = wearableEquipmentRendererDefinition.skinnedMeshRenderers[0].materials;
                            wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = !wearableEquipmentInstances[equipmentType];
                        }
                        else
                        {
                            wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = true;
                        }
                        renderersAlreadyEvaluated.Add(wearableEquipmentRendererDefinition.skinnedMeshRenderers[i]);
                    }
                }
            }

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderersToEvaluate)
            {
                if (renderersAlreadyEvaluated.Contains(skinnedMeshRenderer)) { continue; }
                skinnedMeshRenderer.enabled = true;
            }
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