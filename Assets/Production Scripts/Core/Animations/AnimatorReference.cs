using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Vi.ProceduralAnimations;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.MeshSlicing;

namespace Vi.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(LimbReferences))]
    [RequireComponent(typeof(GlowRenderer))]
    [RequireComponent(typeof(ExplodableMeshController))]
    public class AnimatorReference : MonoBehaviour
    {
        [SerializeField] private MaterialReplacementDefintion[] materialReplacementDefintions;
        [SerializeField] private WearableEquipmentRendererDefinition[] wearableEquipmentRendererDefinitions;

        public CharacterManager.Character GetCharacterWebInfo(CharacterManager.Character currentCharacter)
        {
            MaterialReplacementDefintion browsReplacementDefinition = System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == CharacterReference.MaterialApplicationLocation.Brows);

            CharacterManager.Character newChar = new CharacterManager.Character(currentCharacter._id.ToString(), name.Replace("(Clone)", ""), currentCharacter.name.ToString(), currentCharacter.experience,
                System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == CharacterReference.MaterialApplicationLocation.Body).skinnedMeshRenderers[0].material.name.Replace(" (Instance)", ""),
                System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == CharacterReference.MaterialApplicationLocation.Eyes).skinnedMeshRenderers[0].material.name.Replace(" (Instance)", ""),
                WearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Beard) ? WearableEquipmentInstances[CharacterReference.EquipmentType.Beard].name.Replace("(Clone)", "") : "",
                browsReplacementDefinition == null ? (WearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Brows) ? WearableEquipmentInstances[CharacterReference.EquipmentType.Brows].name.Replace("(Clone)", "") : "") : browsReplacementDefinition.skinnedMeshRenderers[0].material.name.Replace(" (Instance)", ""),
                WearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Hair) ? WearableEquipmentInstances[CharacterReference.EquipmentType.Hair].name.Replace("(Clone)", "") : "",
                currentCharacter.level, currentCharacter.attributes, currentCharacter.loadoutPreset1, currentCharacter.loadoutPreset2, currentCharacter.loadoutPreset3, currentCharacter.loadoutPreset4, currentCharacter.raceAndGender
            );

            if (newChar.beard == "EmptyWearableEquipment") { newChar.beard = "null"; }
            if (newChar.brows == "EmptyWearableEquipment") { newChar.brows = "null"; }
            if (newChar.hair == "EmptyWearableEquipment") { newChar.hair = "null"; }

            return newChar;
        }

        private Dictionary<CharacterReference.MaterialApplicationLocation, Material> appliedCharacterMaterials = new Dictionary<CharacterReference.MaterialApplicationLocation, Material>();
        public void ApplyCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            if (characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body)
            {
                CharacterReference.CharacterMaterial headMaterial = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(characterMaterial.raceAndGender).Find(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Head & characterMaterial.material.name.Contains(string.Concat(item.material.name.Where(char.IsDigit))));
                ApplyCharacterMaterial(headMaterial);
            }

            MaterialReplacementDefintion materialReplacementDefintion = System.Array.Find(materialReplacementDefintions, item => item.characterMaterialType == characterMaterial.materialApplicationLocation);
            Material newMat = new Material(characterMaterial.material);
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in materialReplacementDefintion.skinnedMeshRenderers)
            {
                skinnedMeshRenderer.materials = new Material[] { newMat };
            }
            
            if (appliedCharacterMaterials.ContainsKey(characterMaterial.materialApplicationLocation))
            {
                appliedCharacterMaterials[characterMaterial.materialApplicationLocation] = newMat;
            }
            else
            {
                appliedCharacterMaterials.Add(characterMaterial.materialApplicationLocation, newMat);
            }

            if (characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body)
            {
                foreach (var kvp in WearableEquipmentInstances)
                {
                    SetBodyMaterialsOnEquipmentInstance(kvp.Value);
                }
            }
        }

        private void SetBodyMaterialsOnEquipmentInstance(WearableEquipment wearableEquipment)
        {
            if (!wearableEquipment) { return; }

            if (wearableEquipment.equipmentType == CharacterReference.EquipmentType.Pants)
            {
                if (WearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Boots))
                {
                    if (!WearableEquipmentInstances[CharacterReference.EquipmentType.Boots].isShort)
                    {
                        SkinnedMeshRenderer pantsRenderer = wearableEquipment.GetRenderList().FirstOrDefault(item => !item.CompareTag(WearableEquipment.equipmentBodyMaterialTag));
                        if (pantsRenderer)
                        {
                            foreach (SkinnedMeshRenderer bootsRenderer in WearableEquipmentInstances[CharacterReference.EquipmentType.Boots].GetRenderList())
                            {
                                if (bootsRenderer.CompareTag(WearableEquipment.equipmentBodyMaterialTag))
                                {
                                    bootsRenderer.material = pantsRenderer.material;
                                }
                            }
                        }
                    }
                }
            }

            foreach (SkinnedMeshRenderer smr in wearableEquipment.GetRenderList())
            {
                if (smr.CompareTag(WearableEquipment.equipmentBodyMaterialTag))
                {
                    if (wearableEquipment.equipmentType == CharacterReference.EquipmentType.Boots & !wearableEquipment.isShort)
                    {
                        if (WearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Pants))
                        {
                            SkinnedMeshRenderer pantsRenderer = WearableEquipmentInstances[CharacterReference.EquipmentType.Pants].GetRenderList().FirstOrDefault(item => !item.CompareTag(WearableEquipment.equipmentBodyMaterialTag));
                            if (pantsRenderer)
                            {
                                smr.material = pantsRenderer.material;
                                break;
                            }
                        }
                    }

                    if (appliedCharacterMaterials.ContainsKey(CharacterReference.MaterialApplicationLocation.Body))
                    {
                        smr.material = appliedCharacterMaterials[CharacterReference.MaterialApplicationLocation.Body];
                    }
                }
            }
        }

        public Dictionary<CharacterReference.EquipmentType, WearableEquipment> WearableEquipmentInstances { get; private set; } = new Dictionary<CharacterReference.EquipmentType, WearableEquipment>();
        public void ApplyWearableEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            WearableEquipment model = wearableEquipmentOption.GetModel(raceAndGender, PlayerDataManager.Singleton.GetCharacterReference().EmptyWearableEquipment);
            if (WearableEquipmentInstances.ContainsKey(wearableEquipmentOption.equipmentType))
            {
                if (WearableEquipmentInstances[wearableEquipmentOption.equipmentType])
                {
                    foreach (SkinnedMeshRenderer smr in WearableEquipmentInstances[wearableEquipmentOption.equipmentType].GetRenderList())
                    {
                        glowRenderer.UnregisterRenderer(smr);
                    }

                    if (WearableEquipmentInstances[wearableEquipmentOption.equipmentType].TryGetComponent(out PooledObject pooledObject))
                    {
                        ObjectPoolingManager.ReturnObjectToPool(ref pooledObject);
                    }
                    else
                    {
                        Destroy(WearableEquipmentInstances[wearableEquipmentOption.equipmentType].gameObject);
                    }
                }

                if (model)
                {
                    if (model.TryGetComponent(out PooledObject pooledObject))
                    {
                        WearableEquipmentInstances[wearableEquipmentOption.equipmentType] = ObjectPoolingManager.SpawnObject(pooledObject, transform).GetComponent<WearableEquipment>();
                    }
                    else
                    {
                        WearableEquipmentInstances[wearableEquipmentOption.equipmentType] = Instantiate(model.gameObject, transform).GetComponent<WearableEquipment>();
                    }
                    SetBodyMaterialsOnEquipmentInstance(WearableEquipmentInstances[wearableEquipmentOption.equipmentType]);
                }
                else
                    WearableEquipmentInstances.Remove(wearableEquipmentOption.equipmentType);
            }
            else if (model)
            {
                if (model.TryGetComponent(out PooledObject pooledObject))
                {
                    WearableEquipmentInstances.Add(wearableEquipmentOption.equipmentType, ObjectPoolingManager.SpawnObject(pooledObject, transform).GetComponent<WearableEquipment>());
                }
                else
                {
                    WearableEquipmentInstances.Add(wearableEquipmentOption.equipmentType, Instantiate(model.gameObject, transform).GetComponent<WearableEquipment>());
                }
                SetBodyMaterialsOnEquipmentInstance(WearableEquipmentInstances[wearableEquipmentOption.equipmentType]);
            }

            if (WearableEquipmentInstances.TryGetValue(wearableEquipmentOption.equipmentType, out WearableEquipment instance))
            {
                foreach (SkinnedMeshRenderer smr in instance.GetRenderList())
                {
                    glowRenderer.RegisterRenderer(smr);
                }
            }
            
            WearableEquipmentRendererDefinition wearableEquipmentRendererDefinition = System.Array.Find(wearableEquipmentRendererDefinitions, item => item.equipmentType == wearableEquipmentOption.equipmentType);
            if (wearableEquipmentRendererDefinition != null)
            {
                if (model)
                {
                    for (int i = 0; i < wearableEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                    {
                        if (WearableEquipmentInstances.ContainsKey(wearableEquipmentOption.equipmentType))
                        {
                            wearableEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = !model.shouldDisableCharSkinRenderer | (i > 0 ? model.isShort : false);
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

            if (WearableEquipmentInstances.ContainsKey(wearableEquipmentOption.equipmentType))
            {
                if (!WearableEquipmentInstances[wearableEquipmentOption.equipmentType].enabled)
                {
                    Debug.LogError(WearableEquipmentInstances[wearableEquipmentOption.equipmentType].name + " isn't enabled");
                }
                WearableEquipmentInstances[wearableEquipmentOption.equipmentType].enabled = true;
            }
            StartCoroutine(SetRendererStatusForCharacterCosmetics());

            if (wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Chest
                | wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Gloves)
            {
                RefreshChestSleevesRendererDisplay();
            }
        }

        public void ClearWearableEquipment(CharacterReference.EquipmentType equipmentType)
        {
            if (WearableEquipmentInstances.ContainsKey(equipmentType))
            {
                foreach (SkinnedMeshRenderer smr in WearableEquipmentInstances[equipmentType].GetRenderList())
                {
                    glowRenderer.UnregisterRenderer(smr);
                    smr.enabled = true;
                    smr.forceRenderingOff = false;
                }

                if (WearableEquipmentInstances[equipmentType].TryGetComponent(out PooledObject pooledObject))
                {
                    ObjectPoolingManager.ReturnObjectToPool(ref pooledObject);
                }
                else
                {
                    Destroy(WearableEquipmentInstances[equipmentType].gameObject);
                }

                WearableEquipmentInstances.Remove(equipmentType);
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
            if (gameObject.activeSelf) { StartCoroutine(SetRendererStatusForCharacterCosmetics()); }
            
            if (equipmentType == CharacterReference.EquipmentType.Chest
                | equipmentType == CharacterReference.EquipmentType.Gloves)
            {
                RefreshChestSleevesRendererDisplay();
            }
        }

        private IEnumerator SetRendererStatusForCharacterCosmetics()
        {
            yield return null;
            // Disable hair if we're wearing a helmet
            if (WearableEquipmentInstances.ContainsKey(CharacterReference.EquipmentType.Hair))
            {
                bool shouldRenderHair;
                if (WearableEquipmentInstances.TryGetValue(CharacterReference.EquipmentType.Helm, out WearableEquipment helm))
                {
                    shouldRenderHair = !WearableEquipmentInstances[CharacterReference.EquipmentType.Helm].shouldDisableCharSkinRenderer;
                }
                else
                {
                    shouldRenderHair = true;
                }

                foreach (SkinnedMeshRenderer smr in WearableEquipmentInstances[CharacterReference.EquipmentType.Hair].GetRenderList())
                {
                    smr.forceRenderingOff = !shouldRenderHair;
                }
            }
        }

        private void RefreshChestSleevesRendererDisplay()
        {
            if (WearableEquipmentInstances.TryGetValue(CharacterReference.EquipmentType.Chest, out WearableEquipment chestInstance))
            {
                WearableEquipmentRendererDefinition glovesEquipmentRendererDefinition = System.Array.Find(wearableEquipmentRendererDefinitions, item => item.equipmentType == CharacterReference.EquipmentType.Gloves);
                if (glovesEquipmentRendererDefinition != null)
                {
                    if (chestInstance.sleevesRenderer)
                    {
                        if (WearableEquipmentInstances.TryGetValue(CharacterReference.EquipmentType.Gloves, out WearableEquipment glovesInstance))
                        {
                            for (int i = 0; i < glovesEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                            {
                                glovesEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = !glovesInstance.shouldDisableCharSkinRenderer | (i > 0 ? glovesInstance.isShort : false);
                            }
                            chestInstance.sleevesRenderer.enabled = false;
                        }
                        else
                        {
                            for (int i = 0; i < glovesEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                            {
                                glovesEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = false;
                            }
                            chestInstance.sleevesRenderer.enabled = true;
                        }
                    }
                    else
                    {
                        if (WearableEquipmentInstances.TryGetValue(CharacterReference.EquipmentType.Gloves, out WearableEquipment glovesInstance))
                        {
                            for (int i = 0; i < glovesEquipmentRendererDefinition.skinnedMeshRenderers.Length; i++)
                            {
                                glovesEquipmentRendererDefinition.skinnedMeshRenderers[i].enabled = !glovesInstance.shouldDisableCharSkinRenderer | (i > 0 ? glovesInstance.isShort : false);
                            }
                        }
                    }
                }
            }
        }

        private void OnSpawnFromPool()
        {
            glowRenderer.RegisterChildRenderers();
            animator.enabled = true;
            SetRagdollActive(false);
        }

        private void OnDestroy()
        {
            ExplodableMeshController.ClearInstances();
        }

        private void OnReturnToPool()
        {
            ExplodableMeshController.ClearInstances();

            foreach (KeyValuePair<CharacterReference.EquipmentType, WearableEquipment> kvp in new Dictionary<CharacterReference.EquipmentType, WearableEquipment>(WearableEquipmentInstances))
            {
                foreach (SkinnedMeshRenderer smr in kvp.Value.GetRenderList())
                {
                    glowRenderer.UnregisterRenderer(smr);
                }
                ClearWearableEquipment(kvp.Key);
            }

            armorType = Weapon.ArmorType.Flesh;
            accumulatedRootMotion = Vector3.zero;
            combatAgent = null;
            animationHandler = null;
            CurrentActionsAnimatorStateInfo = default;
            NextActionsAnimatorStateInfo = default;
            CurrentFlinchAnimatorStateInfo = default;
            NextFlinchAnimatorStateInfo = default;
            IsAtRest = true;

            appliedCharacterMaterials.Clear();

            glowRenderer.UnregisterAllRenderers();
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
            foreach (KeyValuePair<CharacterReference.EquipmentType, WearableEquipment> kvp in WearableEquipmentInstances)
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
        LimbReferences limbReferences;
        GlowRenderer glowRenderer;
        public Renderer[] Renderers { get; private set; } = new Renderer[0];

        public ExplodableMeshController ExplodableMeshController { get; private set; }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.cullingMode = FasterPlayerPrefs.IsServerPlatform | NetworkManager.Singleton.IsServer ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.AlwaysAnimate;

            limbReferences = GetComponent<LimbReferences>();
            glowRenderer = GetComponent<GlowRenderer>();
            ExplodableMeshController = GetComponent<ExplodableMeshController>();

            actionsLayerIndex = animator.GetLayerIndex(actionsLayerName);
            flinchLayerIndex = animator.GetLayerIndex(flinchLayerName);

            PooledObject pooledObject = GetComponentInParent<PooledObject>();
            if (pooledObject)
            {
                pooledObject.OnSpawnFromPool += OnSpawnFromPool;
                pooledObject.OnReturnToPool += OnReturnToPool;
            }

            ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            Renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer r in Renderers)
            {
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = true;
                }
            }
        }

        public void OnNetworkSpawn()
        {
            foreach (Renderer r in Renderers)
            {
                r.gameObject.layer = LayerMask.NameToLayer(combatAgent.IsSpawned ? "Character" : "Preview");
            }
        }

        CombatAgent combatAgent;
        AnimationHandler animationHandler;
        private void OnEnable()
        {
            combatAgent = GetComponentInParent<CombatAgent>();
            if (combatAgent)
            {
                animationHandler = combatAgent.GetComponent<AnimationHandler>();
                foreach (Renderer r in Renderers)
                {
                    r.gameObject.layer = LayerMask.NameToLayer(combatAgent.IsSpawned ? "Character" : "Preview");
                }
            }

            foreach (Renderer r in Renderers)
            {
                r.forceRenderingOff = true;
            }
            StartCoroutine(TurnRenderersBackOn());
        }

        private IEnumerator TurnRenderersBackOn()
        {
            yield return null;
            if (combatAgent) { yield return new WaitUntil(() => combatAgent.WeaponHandler.WeaponInitialized); }

            foreach (Renderer r in Renderers)
            {
                r.forceRenderingOff = false;
            }
        }

        public bool IsAtRest { get; private set; } = true;

        private bool RefreshIsAtRest()
        {
            if (!animator) { return false; }
            if (animator.IsInTransition(actionsLayerIndex))
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
            if (!combatAgent) { return false; }
            if (!combatAgent.WeaponHandler) { return false; }
            if (!combatAgent.WeaponHandler.CurrentActionClip) { return false; }
            return combatAgent.WeaponHandler.CurrentActionClip.shouldApplyRootMotion & !IsAtRestIgnoringTransition();
        }

        private void Update()
        {
            IsAtRest = RefreshIsAtRest();
            limbReferences.SetHeadIKWeight(!combatAgent.WeaponHandler.IsAiming() & IsAtRest & combatAgent.IsSpawned ? 1 : 0);

            if (animationHandler)
            {
                if (!animator.enabled) { animationHandler.ProcessNextActionClip(); }
            }

            if (IsAtRest)
            {
                limbReferences.SetRotationOffset(0, 0, 0);
            }
            else
            {
                limbReferences.SetRotationOffset(
                    combatAgent.WeaponHandler.CurrentActionClip.XAngleRotationOffset > 180 ? combatAgent.WeaponHandler.CurrentActionClip.XAngleRotationOffset - 360 : combatAgent.WeaponHandler.CurrentActionClip.XAngleRotationOffset,
                    combatAgent.WeaponHandler.CurrentActionClip.YAngleRotationOffset > 180 ? combatAgent.WeaponHandler.CurrentActionClip.YAngleRotationOffset - 360 : combatAgent.WeaponHandler.CurrentActionClip.YAngleRotationOffset,
                    combatAgent.WeaponHandler.CurrentActionClip.ZAngleRotationOffset > 180 ? combatAgent.WeaponHandler.CurrentActionClip.ZAngleRotationOffset - 360 : combatAgent.WeaponHandler.CurrentActionClip.ZAngleRotationOffset
                );
            }
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
                bool shouldApplyMultiplierCurves = combatAgent.AnimationHandler.IsActionClipPlaying(combatAgent.WeaponHandler.CurrentActionClip);
                if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack) { shouldApplyMultiplierCurves = combatAgent.AnimationHandler.IsActionClipPlayingInCurrentState(combatAgent.WeaponHandler.CurrentActionClip); }

                accumulatedRootMotion += ProcessMotionData(Quaternion.Inverse(animator.rootRotation) * animator.deltaPosition,
                    combatAgent.AnimationHandler.GetActionClipNormalizedTime(combatAgent.WeaponHandler.CurrentActionClip),
                    shouldApplyMultiplierCurves);
            }

            if (animationHandler) { animationHandler.ProcessNextActionClip(); }
        }

        public Vector3 ProcessMotionData(Vector3 worldSpaceRootMotion, float normalizedTime, bool shouldApplyMultiplierCurves)
        {
            if (shouldApplyMultiplierCurves)
            {
                if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HitReaction)
                {
                    worldSpaceRootMotion.x *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionSidesMultiplier().EvaluateNormalizedTime(normalizedTime) * combatAgent.WeaponHandler.CurrentActionClip.GetHitReactionRootMotionSidesMultiplier().EvaluateNormalizedTime(normalizedTime);
                    worldSpaceRootMotion.y *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionVerticalMultiplier().EvaluateNormalizedTime(normalizedTime) * combatAgent.WeaponHandler.CurrentActionClip.GetHitReactionRootMotionVerticalMultiplier().EvaluateNormalizedTime(normalizedTime);
                    worldSpaceRootMotion.z *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionForwardMultiplier().EvaluateNormalizedTime(normalizedTime) * combatAgent.WeaponHandler.CurrentActionClip.GetHitReactionRootMotionForwardMultiplier().EvaluateNormalizedTime(normalizedTime);
                }
                else
                {
                    worldSpaceRootMotion.x *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionSidesMultiplier().EvaluateNormalizedTime(normalizedTime);
                    worldSpaceRootMotion.y *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionVerticalMultiplier().EvaluateNormalizedTime(normalizedTime);
                    worldSpaceRootMotion.z *= combatAgent.WeaponHandler.CurrentActionClip.GetRootMotionForwardMultiplier().EvaluateNormalizedTime(normalizedTime);
                }
            }
            return new Vector3(worldSpaceRootMotion.x / transform.lossyScale.x, worldSpaceRootMotion.y / transform.lossyScale.y, worldSpaceRootMotion.z / transform.lossyScale.z) / Time.fixedDeltaTime;
        }

        private Rigidbody[] ragdollRigidbodies = new Rigidbody[0];
        public void SetRagdollActive(bool isActive)
        {
            if (!combatAgent) { return; }
            foreach (Rigidbody rb in ragdollRigidbodies)
            {
                rb.isKinematic = !isActive;
                rb.interpolation = combatAgent.IsClient ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            }
            animator.enabled = !isActive;
        }

        [System.Serializable]
        public struct WorldSpaceLabelTransformInfo
        {
            public Renderer rendererToFollow;
            public Vector3 positionOffsetFromRenderer;
            public float scaleMultiplier;

            public WorldSpaceLabelTransformInfo(Renderer rendererToFollow, Vector3 positionOffsetFromRenderer, float scaleMultiplier)
            {
                this.rendererToFollow = rendererToFollow;
                this.positionOffsetFromRenderer = positionOffsetFromRenderer;
                this.scaleMultiplier = scaleMultiplier;
            }

            public static WorldSpaceLabelTransformInfo GetDefaultWorldSpaceLabelTransformInfo()
            {
                return new WorldSpaceLabelTransformInfo(null, new Vector3(0, 0, -0.25f), 1);
            }
        }

        public WorldSpaceLabelTransformInfo GetWorldSpaceLabelTransformInfo() { return worldSpaceLabelTransformInfo; }
        [SerializeField] private WorldSpaceLabelTransformInfo worldSpaceLabelTransformInfo = WorldSpaceLabelTransformInfo.GetDefaultWorldSpaceLabelTransformInfo();

        private void OnValidate()
        {
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length == 0) { return; }
            Vector3 highestPoint = renderers[0].bounds.center;
            Renderer rendererToFollow = renderers[0];
            foreach (Renderer renderer in renderers)
            {
                if (renderer.bounds.center.y > highestPoint.y)
                {
                    rendererToFollow = renderer;
                    highestPoint = renderer.bounds.center;
                }
            }
            worldSpaceLabelTransformInfo.rendererToFollow = rendererToFollow;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) { return; }
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length == 0) { return; }
            Vector3 highestPoint = renderers[0].bounds.center;
            Renderer rendererToFollow = renderers[0];
            foreach (Renderer renderer in renderers)
            {
                if (renderer.bounds.center.y > highestPoint.y)
                {
                    rendererToFollow = renderer;
                    highestPoint = renderer.bounds.center;
                }
            }
            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(rendererToFollow.bounds.center + rendererToFollow.transform.rotation * worldSpaceLabelTransformInfo.positionOffsetFromRenderer, new Vector3(worldSpaceLabelTransformInfo.scaleMultiplier, 0.1f, 0.1f));
        }
    }
}