using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vi.ProceduralAnimations;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    [DisallowMultipleComponent]
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
                    foreach (SkinnedMeshRenderer smr in wearableEquipmentInstances[wearableEquipmentOption.equipmentType].GetRenderList())
                    {
                        glowRenderer.UnregisterRenderer(smr);
                    }

                    if (wearableEquipmentInstances[wearableEquipmentOption.equipmentType].TryGetComponent(out PooledObject pooledObject))
                    {
                        ObjectPoolingManager.ReturnObjectToPool(ref pooledObject);
                    }
                    else
                    {
                        Destroy(wearableEquipmentInstances[wearableEquipmentOption.equipmentType].gameObject);
                    }
                }

                if (model)
                {
                    if (model.TryGetComponent(out PooledObject pooledObject))
                    {
                        wearableEquipmentInstances[wearableEquipmentOption.equipmentType] = ObjectPoolingManager.SpawnObject(pooledObject, transform).GetComponent<WearableEquipment>();
                    }
                    else
                    {
                        wearableEquipmentInstances[wearableEquipmentOption.equipmentType] = Instantiate(model.gameObject, transform).GetComponent<WearableEquipment>();
                    }

                    if (wearableEquipmentOption.equipmentType != CharacterReference.EquipmentType.Cape)
                    {
                        foreach (SkinnedMeshRenderer smr in wearableEquipmentInstances[wearableEquipmentOption.equipmentType].GetRenderList())
                        {
                            glowRenderer.RegisterNewRenderer(smr);
                        }
                    }
                }
                else
                    wearableEquipmentInstances.Remove(wearableEquipmentOption.equipmentType);
            }
            else if (model)
            {
                if (model.TryGetComponent(out PooledObject pooledObject))
                {
                    wearableEquipmentInstances.Add(wearableEquipmentOption.equipmentType, ObjectPoolingManager.SpawnObject(pooledObject, transform).GetComponent<WearableEquipment>());
                }
                else
                {
                    wearableEquipmentInstances.Add(wearableEquipmentOption.equipmentType, Instantiate(model.gameObject, transform).GetComponent<WearableEquipment>());
                }

                if (wearableEquipmentOption.equipmentType != CharacterReference.EquipmentType.Cape)
                {
                    foreach (SkinnedMeshRenderer smr in wearableEquipmentInstances[wearableEquipmentOption.equipmentType].GetRenderList())
                    {
                        glowRenderer.RegisterNewRenderer(smr);
                    }
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
            SetArmorType();

            if (wearableEquipmentInstances.ContainsKey(wearableEquipmentOption.equipmentType))
            {
                if (!wearableEquipmentInstances[wearableEquipmentOption.equipmentType].enabled)
                {
                    Debug.LogError(wearableEquipmentInstances[wearableEquipmentOption.equipmentType].name + " isn't enabled");
                }
                wearableEquipmentInstances[wearableEquipmentOption.equipmentType].enabled = true;
            }
            StartCoroutine(SetRendererStatusForCharacterCosmetics());
        }

        public void ClearWearableEquipment(CharacterReference.EquipmentType equipmentType)
        {
            if (wearableEquipmentInstances.ContainsKey(equipmentType))
            {
                foreach (SkinnedMeshRenderer smr in wearableEquipmentInstances[equipmentType].GetRenderList())
                {
                    glowRenderer.UnregisterRenderer(smr);
                }

                if (wearableEquipmentInstances[equipmentType].TryGetComponent(out PooledObject pooledObject))
                {
                    ObjectPoolingManager.ReturnObjectToPool(ref pooledObject);
                }
                else
                {
                    Destroy(wearableEquipmentInstances[equipmentType].gameObject);
                }
                
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
            SetArmorType();
            StartCoroutine(SetRendererStatusForCharacterCosmetics());
        }

        private IEnumerator SetRendererStatusForCharacterCosmetics()
        {
            yield return null;
            // Disable hair if we're wearing a helmet
            if (wearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Hair))
            {
                bool shouldRenderHair = !wearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Helm);
                foreach (SkinnedMeshRenderer smr in wearableEquipmentInstances[CharacterReference.EquipmentType.Hair].GetRenderList())
                {
                    smr.forceRenderingOff = !shouldRenderHair;
                }
            }
        }

        private void OnReturnToPool()
        {
            foreach (KeyValuePair<CharacterReference.EquipmentType, WearableEquipment> kvp in wearableEquipmentInstances)
            {
                foreach (SkinnedMeshRenderer smr in kvp.Value.GetRenderList())
                {
                    glowRenderer.UnregisterRenderer(smr);
                }

                if (kvp.Value.TryGetComponent(out PooledObject pooledObject))
                {
                    ObjectPoolingManager.ReturnObjectToPool(ref pooledObject);
                    kvp.Value.enabled = true;
                }
                else
                {
                    Destroy(kvp.Value);
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

        private Weapon.ArmorType armorType = Weapon.ArmorType.Flesh;

        private void SetArmorType()
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

            if (armorTypeCounts.Count == 0)
            {
                armorType = Weapon.ArmorType.Flesh;
            }
            else
            {
                armorType = armorTypeCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }
        }

        public Weapon.ArmorType GetArmorType() { return armorType; }

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

        private Vector3 accumulatedRootMotion;
        public Vector3 ApplyRootMotion()
        {
            Vector3 _ = accumulatedRootMotion;
            accumulatedRootMotion = Vector3.zero;
            return _;
        }

        private const string actionsLayerName = "Actions";
        private const string flinchLayerName = "Flinch";

        private int actionsLayerIndex;
        private int flinchLayerIndex;

        Animator animator;
        CombatAgent combatAgent;
        LimbReferences limbReferences;
        GlowRenderer glowRenderer;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.cullingMode = WebRequestManager.IsServerBuild() | NetworkManager.Singleton.IsServer ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.AlwaysAnimate;

            combatAgent = GetComponentInParent<CombatAgent>();
            limbReferences = GetComponent<LimbReferences>();
            glowRenderer = GetComponent<GlowRenderer>();

            actionsLayerIndex = animator.GetLayerIndex(actionsLayerName);
            flinchLayerIndex = animator.GetLayerIndex(flinchLayerName);

            GetComponent<PooledObject>().OnReturnToPool += OnReturnToPool;
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
            if (!animator) { return false; }
            if (animator.IsInTransition(animator.GetLayerIndex("Actions")))
            {
                return NextActionsAnimatorStateInfo.IsName("Empty");
            }
            else
            {
                return CurrentActionsAnimatorStateInfo.IsName("Empty");
            }
        }

        public bool IsAtRestIgnoringTransition()
        {
            if (!animator) { return false; }
            return CurrentActionsAnimatorStateInfo.IsName("Empty");
        }

        public bool ShouldApplyRootMotion()
        {
            if (!combatAgent.WeaponHandler) { return false; }
            if (!combatAgent.WeaponHandler.CurrentActionClip) { return false; }
            return combatAgent.WeaponHandler.CurrentActionClip.shouldApplyRootMotion & !IsAtRest();
        }

        private void Update()
        {
            limbReferences.SetRotationOffset(IsAtRest() ? 0 : combatAgent.WeaponHandler.CurrentActionClip.YAngleRotationOffset > 180 ? combatAgent.WeaponHandler.CurrentActionClip.YAngleRotationOffset - 360 : combatAgent.WeaponHandler.CurrentActionClip.YAngleRotationOffset);
        }

        public AnimatorStateInfo CurrentActionsAnimatorStateInfo { get; private set; }
        public AnimatorStateInfo NextActionsAnimatorStateInfo { get; private set; }
        public AnimatorStateInfo CurrentFlinchAnimatorStateInfo { get; private set; }
        public AnimatorStateInfo NextFlinchAnimatorStateInfo { get; private set; }

        private void OnAnimatorMove()
        {
            CurrentActionsAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(actionsLayerIndex);
            NextActionsAnimatorStateInfo = animator.GetNextAnimatorStateInfo(actionsLayerIndex);
            CurrentFlinchAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(flinchLayerIndex);
            NextFlinchAnimatorStateInfo = animator.GetNextAnimatorStateInfo(flinchLayerIndex);

            // Check if the current animator state is not "Empty" and update networkRootMotion and localRootMotion accordingly
            if (ShouldApplyRootMotion())
            {
                float normalizedTime = combatAgent.AnimationHandler.GetActionClipNormalizedTime(combatAgent.WeaponHandler.CurrentActionClip);
                bool shouldApplyCurves = combatAgent.AnimationHandler.IsActionClipPlaying(combatAgent.WeaponHandler.CurrentActionClip);

                if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack) { shouldApplyCurves = combatAgent.AnimationHandler.IsActionClipPlayingInCurrentState(combatAgent.WeaponHandler.CurrentActionClip); }

                Vector3 worldSpaceRootMotion = Quaternion.Inverse(transform.root.rotation) * animator.deltaPosition;
                if (shouldApplyCurves)
                {
                    if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HitReaction)
                    {
                        worldSpaceRootMotion.x *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionSidesMultiplier().Evaluate(normalizedTime) * combatAgent.WeaponHandler.CurrentActionClip.GetHitReactionRootMotionSidesMultiplier().Evaluate(normalizedTime);
                        worldSpaceRootMotion.y *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionVerticalMultiplier().Evaluate(normalizedTime) * combatAgent.WeaponHandler.CurrentActionClip.GetHitReactionRootMotionVerticalMultiplier().Evaluate(normalizedTime);
                        worldSpaceRootMotion.z *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionForwardMultiplier().Evaluate(normalizedTime) * combatAgent.WeaponHandler.CurrentActionClip.GetHitReactionRootMotionForwardMultiplier().Evaluate(normalizedTime);
                    }
                    else
                    {
                        worldSpaceRootMotion.x *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionSidesMultiplier().Evaluate(normalizedTime);
                        worldSpaceRootMotion.y *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionVerticalMultiplier().Evaluate(normalizedTime);
                        worldSpaceRootMotion.z *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionForwardMultiplier().Evaluate(normalizedTime);
                    }
                }
                accumulatedRootMotion += worldSpaceRootMotion / Time.fixedDeltaTime;
            }
        }
    }
}