using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;

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
                currentCharacter.level, currentCharacter.loadoutPreset1, currentCharacter.loadoutPreset2, currentCharacter.loadoutPreset3, currentCharacter.loadoutPreset4, currentCharacter.raceAndGender
            );
        }

        public void ApplyCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            //if (characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body)
            //{
            //    CharacterReference.CharacterMaterial headMaterial = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(characterMaterial.raceAndGender).Find(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Head & characterMaterial.material.name.Contains(string.Concat(item.material.name.Where(char.IsDigit))));
            //    ApplyCharacterMaterial(headMaterial);
            //}

            MaterialReplacementDefintion materialReplacementDefintion = System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == characterMaterial.materialApplicationLocation);
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in materialReplacementDefintion.skinnedMeshRenderers)
            {
                skinnedMeshRenderer.materials = new Material[] { characterMaterial.material };
            }
        }
        
        private Dictionary<CharacterReference.EquipmentType, WearableEquipment> wearableEquipmentInstances = new Dictionary<CharacterReference.EquipmentType, WearableEquipment>();
        public void ApplyWearableEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            WearableEquipment model = wearableEquipmentOption.GetModel(raceAndGender, PlayerDataManager.Singleton.GetCharacterReference().GetEmptyWearableEquipment());
            if (wearableEquipmentInstances.ContainsKey(wearableEquipmentOption.equipmentType))
            {
                if (wearableEquipmentInstances[wearableEquipmentOption.equipmentType])
                {
                    Destroy(wearableEquipmentInstances[wearableEquipmentOption.equipmentType].gameObject);
                }

                if (model)
                {
                    wearableEquipmentInstances[wearableEquipmentOption.equipmentType] = Instantiate(wearableEquipmentOption.GetModel(raceAndGender, PlayerDataManager.Singleton.GetCharacterReference().GetEmptyWearableEquipment()).gameObject, transform).GetComponent<WearableEquipment>();
                    SkinnedMeshRenderer[] equipmentSkinnedMeshRenderers = wearableEquipmentInstances[wearableEquipmentOption.equipmentType].GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer smr in equipmentSkinnedMeshRenderers)
                    {
                        glowRenderer.RegisterNewRenderer(smr);
                    }
                }
                else
                    wearableEquipmentInstances.Remove(wearableEquipmentOption.equipmentType);
            }
            else if (model)
            {
                wearableEquipmentInstances.Add(wearableEquipmentOption.equipmentType, Instantiate(wearableEquipmentOption.GetModel(raceAndGender, PlayerDataManager.Singleton.GetCharacterReference().GetEmptyWearableEquipment()).gameObject, transform).GetComponent<WearableEquipment>());
                SkinnedMeshRenderer[] equipmentSkinnedMeshRenderers = wearableEquipmentInstances[wearableEquipmentOption.equipmentType].GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer smr in equipmentSkinnedMeshRenderers)
                {
                    glowRenderer.RegisterNewRenderer(smr);
                }
            }

            WearableEquipmentRendererDefinition wearableEquipmentRendererDefinition = System.Array.Find(wearableEquipmentRendererDefinitions, item => item.equipmentType == wearableEquipmentOption.equipmentType);
            if (wearableEquipmentRendererDefinition != null)
            {
                if (model)
                {
                    for (int i = 0; i < wearableEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                    {
                        if (wearableEquipmentInstances.ContainsKey(wearableEquipmentOption.equipmentType))
                        {
                            SkinnedMeshRenderer[] equipmentSkinnedMeshRenderers = wearableEquipmentInstances[wearableEquipmentOption.equipmentType].GetComponentsInChildren<SkinnedMeshRenderer>();
                            wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = !model.shouldDisableCharSkinRenderer;
                        }
                        else
                        {
                            wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = true;
                        }
                    }
                }
                else // If there is no model, enable all the original skinned mesh renderers
                {
                    for (int i = 0; i < wearableEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                    {
                        wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = true;
                    }
                }
            }
        }

        public void ClearWearableEquipment(CharacterReference.EquipmentType equipmentType)
        {
            if (wearableEquipmentInstances.ContainsKey(equipmentType))
            {
                Destroy(wearableEquipmentInstances[equipmentType].gameObject);
                wearableEquipmentInstances.Remove(equipmentType);
            }

            WearableEquipmentRendererDefinition wearableEquipmentRendererDefinition = System.Array.Find(wearableEquipmentRendererDefinitions, item => item.equipmentType == equipmentType);
            if (wearableEquipmentRendererDefinition != null)
            {
                for (int i = 0; i < wearableEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                {
                    wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = true;
                }
            }
        }

        private readonly static List<CharacterReference.EquipmentType> equipmentTypesToEvaluateForArmorType = new List<CharacterReference.EquipmentType>()
        {
            CharacterReference.EquipmentType.Chest,
            CharacterReference.EquipmentType.Gloves,
            CharacterReference.EquipmentType.Helm,
            CharacterReference.EquipmentType.Pants,
            CharacterReference.EquipmentType.Shoulders,
            CharacterReference.EquipmentType.Boots
        };

        public Weapon.ArmorType GetArmorType()
        {
            Dictionary<Weapon.ArmorType, int> armorTypeCounts = new Dictionary<Weapon.ArmorType, int>();
            foreach (KeyValuePair<CharacterReference.EquipmentType, WearableEquipment> kvp in wearableEquipmentInstances)
            {
                if (!equipmentTypesToEvaluateForArmorType.Contains(kvp.Value.equipmentType)) { continue; }

                if (armorTypeCounts.ContainsKey(kvp.Value.armorType))
                {
                    armorTypeCounts[kvp.Value.armorType] += 1;
                }
                else
                {
                    armorTypeCounts.Add(kvp.Value.armorType, 1);
                }
            }
            return armorTypeCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
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
        Attributes attributes;
        MovementHandler movementHandler;
        LimbReferences limbReferences;
        GlowRenderer glowRenderer;
        AnimationHandler animationHandler;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.cullingMode = WebRequestManager.IsServerBuild() | NetworkManager.Singleton.IsServer ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.AlwaysAnimate;
            weaponHandler = GetComponentInParent<WeaponHandler>();
            animationHandler = weaponHandler.GetComponent<AnimationHandler>();
            attributes = weaponHandler.GetComponent<Attributes>();
            movementHandler = weaponHandler.GetComponent<MovementHandler>();
            limbReferences = GetComponent<LimbReferences>();
            glowRenderer = GetComponent<GlowRenderer>();
        }

        private void Start()
        {
            SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in smrs)
            {
                skinnedMeshRenderer.forceRenderingOff = true;
            }
            StartCoroutine(TurnRenderersBackOn());
        }

        private IEnumerator TurnRenderersBackOn()
        {
            yield return null;
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinnedMeshRenderer.forceRenderingOff = false;
            }
        }

        public bool IsAtRest()
        {
            if (animator.IsInTransition(animator.GetLayerIndex("Actions")))
            {
                return animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty");
            }
            else
            {
                return animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty");
            }
        }

        public bool ShouldApplyRootMotion()
        {
            if (!weaponHandler.CurrentActionClip) { return false; }
            return weaponHandler.CurrentActionClip.shouldApplyRootMotion & !IsAtRest();
        }

        private void Update()
        {
            limbReferences.SetRotationOffset(IsAtRest() ? 0 : weaponHandler.CurrentActionClip.YAngleRotationOffset);
        }

        private void OnAnimatorMove()
        {
            // Check if the current animator state is not "Empty" and update networkRootMotion and localRootMotion accordingly
            if (ShouldApplyRootMotion())
            {
                float normalizedTime = animationHandler.GetActionClipNormalizedTime(weaponHandler.CurrentActionClip);
                bool shouldApplyCurves = animationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip);

                if (weaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack) { shouldApplyCurves = animationHandler.IsActionClipPlayingInCurrentState(weaponHandler.CurrentActionClip); }

                Vector3 worldSpaceRootMotion = Quaternion.Inverse(transform.root.rotation) * animator.deltaPosition;
                if (shouldApplyCurves)
                {
                    worldSpaceRootMotion.x *= weaponHandler.CurrentActionClip.GetRootMotionSidesMultiplier().Evaluate(normalizedTime) * weaponHandler.CurrentActionClip.GetHitReactionRootMotionSidesMultiplier().Evaluate(normalizedTime);
                    worldSpaceRootMotion.y *= weaponHandler.CurrentActionClip.GetRootMotionVerticalMultiplier().Evaluate(normalizedTime) * weaponHandler.CurrentActionClip.GetHitReactionRootMotionVerticalMultiplier().Evaluate(normalizedTime);
                    worldSpaceRootMotion.z *= weaponHandler.CurrentActionClip.GetRootMotionForwardMultiplier().Evaluate(normalizedTime) * weaponHandler.CurrentActionClip.GetHitReactionRootMotionForwardMultiplier().Evaluate(normalizedTime);
                }

                Vector3 curveAdjustedLocalRootMotion;
                if (attributes.IsGrabbed() & animationHandler.IsActionClipPlayingInCurrentState(weaponHandler.CurrentActionClip))
                {
                    Transform grabAssailant = attributes.GetGrabAssailant().transform;
                    //movementHandler.AddForce(Vector3.ClampMagnitude(grabAssailant.position + (grabAssailant.forward * 1.2f) - transform.root.position, worldSpaceRootMotion.magnitude));
                    curveAdjustedLocalRootMotion = Vector3.ClampMagnitude(grabAssailant.position + (grabAssailant.forward * 1.2f) - transform.root.position, worldSpaceRootMotion.magnitude);
                }
                else if (attributes.IsPulled())
                {
                    //movementHandler.AddForce(Vector3.ClampMagnitude(attributes.GetPullAssailant().transform.position - transform.root.position, worldSpaceRootMotion.magnitude));
                    curveAdjustedLocalRootMotion = Vector3.ClampMagnitude(attributes.GetPullAssailant().transform.position - transform.root.position, worldSpaceRootMotion.magnitude);
                }
                else
                {
                    curveAdjustedLocalRootMotion = transform.root.rotation * worldSpaceRootMotion;
                }

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