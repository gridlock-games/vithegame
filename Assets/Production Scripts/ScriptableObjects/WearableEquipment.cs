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
        public bool shouldDisableCharSkinRenderer = true;

        private const bool shouldDebugWarnings = false;
        public const string equipmentBodyMaterialTag = "EquipmentMimicsBase";

        [SerializeField] private SkinnedMeshRenderer[] renderList = new SkinnedMeshRenderer[0];
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
            }
        }

        private void OnEnable()
        {
            Animator animator = GetComponentInParent<Animator>();
            if (!animator) { return; }

            NetworkObject networkObject = GetComponentInParent<NetworkObject>();

            Transform target = animator.avatarRoot;

            var boneMap = new Dictionary<string, Transform>();
            GetAllSkinnedMeshRenderers(ref boneMap, target);

            foreach (SkinnedMeshRenderer srenderer in renderList)
            {
                if (srenderer.TryGetComponent(out MagicaCloth magicaCloth))
                {
                    ClothSerializeData sdata = magicaCloth.SerializeData;
                    sdata.colliderCollisionConstraint.colliderList.Clear();
                    sdata.colliderCollisionConstraint.colliderList.AddRange(animator.GetComponentsInChildren<ColliderComponent>());

                    foreach (Transform potentialBone in animator.GetComponentsInChildren<Transform>())
                    {
                        bool shouldSkip = false;
                        foreach (KeyValuePair<Transform, Transform> kvp in boneMapToFollow)
                        {
                            if (kvp.Key.name == potentialBone.name) { shouldSkip = true; break; }
                        }

                        if (shouldSkip) { break; }

                        Transform boneToMap = System.Array.Find(srenderer.bones, item => item.name == potentialBone.name);
                        if (boneToMap)
                        {
                            boneMapToFollow.Add(potentialBone, boneToMap);
                        }
                    }
                    srenderer.updateWhenOffscreen = networkObject ? networkObject.IsLocalPlayer : true;
                }
                else // If this is not a cloth
                {
                    Transform[] newBones = new Transform[srenderer.bones.Length];

                    for (int i = 0; i < srenderer.bones.Length; ++i)
                    {
                        GameObject bone = srenderer.bones[i].gameObject;

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

        private bool bonesCanMove;
        private Dictionary<Transform, Transform> boneMapToFollow = new Dictionary<Transform, Transform>();
        private void LateUpdate()
        {
            if (bonesCanMove)
            {
                foreach (KeyValuePair<Transform, Transform> kvp in boneMapToFollow)
                {
                    kvp.Value.position = kvp.Key.position;
                    kvp.Value.rotation = kvp.Key.rotation;
                }
            }
            bonesCanMove = true;
        }
    }
}