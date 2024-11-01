using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Utility;
using UnityEngine.Assertions;
using MagicaCloth2;
using System.Linq;

namespace Vi.ScriptableObjects
{
    [DisallowMultipleComponent]
    public class WearableEquipment : MonoBehaviour
    {
        public CharacterReference.EquipmentType equipmentType;
        public Weapon.ArmorType armorType = Weapon.ArmorType.Cloth;
        [Header("Renderer Presentation")]
        public bool shouldDisableCharSkinRenderer;
        [Tooltip("Only relevant for boots")]
        public bool isShort;
        [Tooltip("Only relevant for chest")]
        public SkinnedMeshRenderer sleevesRenderer;

        private const bool shouldDebugWarnings = true;
        public const string equipmentBodyMaterialTag = "EquipmentMimicsBase";

        [SerializeField] private SkinnedMeshRenderer[] renderList = new SkinnedMeshRenderer[0];

        public List<MagicaCloth> ClothInstances { get { return _clothInstances; } }
        private List<MagicaCloth> _clothInstances = new List<MagicaCloth>();
        
        private List<(Transform, Transform[])> originalRenderData = new List<(Transform, Transform[])>();

        public SkinnedMeshRenderer[] GetRenderList() { return renderList; }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) { return; }
            SkinnedMeshRenderer[] changes = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (!changes.SequenceEqual(renderList))
            {
                renderList = changes;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        private void Awake()
        {
            foreach (SkinnedMeshRenderer srenderer in renderList)
            {
                originalRenderData.Add((srenderer.rootBone, srenderer.bones));

                if (srenderer.TryGetComponent(out MagicaCloth cloth))
                {
                    _clothInstances.Add(cloth);
                }
            }
        }

        private void OnEnable()
        {
            Animator animator = GetComponentInParent<Animator>();
            if (!animator) { return; }

            NetworkObject networkObject = GetComponentInParent<NetworkObject>();

            var boneMap = new Dictionary<string, Transform>();
            GetAllSkinnedMeshRenderers(ref boneMap, animator.avatarRoot);

            foreach (SkinnedMeshRenderer srenderer in renderList)
            {
                if (srenderer.TryGetComponent(out MagicaCloth magicaCloth))
                {
                    ClothSerializeData sdata = magicaCloth.SerializeData;
                    sdata.colliderCollisionConstraint.colliderList.Clear();
                    sdata.colliderCollisionConstraint.colliderList.AddRange(animator.GetComponentsInChildren<ColliderComponent>());
                    magicaCloth.SetParameterChange();

                    List<Transform> bonesReservedForClothSimulation = new List<Transform>();
                    foreach (Transform rootBone in magicaCloth.SerializeData.rootBones)
                    {
                        bonesReservedForClothSimulation.AddRange(rootBone.GetComponentsInChildren<Transform>());
                    }

                    for (int i = 0; i < srenderer.bones.Length; i++)
                    {
                        Transform clothBone = srenderer.bones[i];
                        if (boneMap.TryGetValue(clothBone.name, out Transform animatorBone))
                        {
                            if (!bonesReservedForClothSimulation.Contains(clothBone))
                            {
                                boneMapToFollow.Add(animatorBone, clothBone);
                            }
                        }
                        else if (Application.isEditor & shouldDebugWarnings)
                        {
                            Debug.LogWarning(name + " Unable to map bone \"" + clothBone.name + "\" to target skeleton.");
                        }
                    }
                    srenderer.updateWhenOffscreen = networkObject ? networkObject.IsLocalPlayer : true;
                }
                else // If this is not a cloth
                {
                    Transform[] newBones = new Transform[srenderer.bones.Length];
                    for (int i = 0; i < srenderer.bones.Length; ++i)
                    {
                        Transform bone = srenderer.bones[i];
                        if (!boneMap.TryGetValue(bone.name, out newBones[i]))
                        {
                            if (Application.isEditor & shouldDebugWarnings) { Debug.LogWarning(name + " Unable to map bone \"" + bone.name + "\" to target skeleton."); }
                        }
                    }
                    srenderer.bones = newBones;
                    srenderer.rootBone = FindBoundByName(srenderer.rootBone.name, boneMap);
                    srenderer.updateWhenOffscreen = networkObject ? networkObject.IsLocalPlayer : true;
                }
            }

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in renderList)
            {
                skinnedMeshRenderer.gameObject.layer = LayerMask.NameToLayer(networkObject.IsSpawned ? "Character" : "Preview");
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < renderList.Length; i++)
            {
                (Transform originalRootBone, Transform[] originalBones) = originalRenderData[i];
                renderList[i].rootBone = originalRootBone;
                renderList[i].bones = originalBones;

                if (!renderList[i].enabled)
                {
                    Debug.LogWarning("Wearable equipment renderer is disabled on disable. Make sure this is intentional");
                    renderList[i].enabled = true;
                }

                if (!renderList[i].forceRenderingOff)
                {
                    Debug.LogWarning("Wearable equipment renderer has force rendering off set to true on disable. Make sure this is intentional");
                    renderList[i].forceRenderingOff = false;
                }
            }
            boneMapToFollow.Clear();
        }

        private void GetAllSkinnedMeshRenderers(ref Dictionary<string, Transform> map, Transform target)
        {
            if (!map.ContainsKey(target.name))
                map.Add(target.name, target);
            foreach (Transform child in target)
            {
                if (child.gameObject.GetComponent<WearableEquipment>() == null)
                    GetAllSkinnedMeshRenderers(ref map, child);
            }
        }

        private Transform FindBoundByName(string _name, Dictionary<string, Transform> boneMap)
        {
            if (!boneMap.TryGetValue(_name, out Transform _rootBone))
            {
                if (Application.isEditor & shouldDebugWarnings) { Debug.LogWarning(name + " Unable to map bone \"" + _name + "\" to target skeleton."); }
            }
            return _rootBone;
        }

        private Dictionary<Transform, Transform> boneMapToFollow = new Dictionary<Transform, Transform>();
        private void LateUpdate()
        {
            foreach (KeyValuePair<Transform, Transform> kvp in boneMapToFollow)
            {
                kvp.Value.position = kvp.Key.position;
                kvp.Value.rotation = kvp.Key.rotation;
            }
        }
    }
}