using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;

namespace Vi.ScriptableObjects
{
    [DisallowMultipleComponent]
    public class WearableEquipment : MonoBehaviour
    {
        public CharacterReference.EquipmentType equipmentType;
        public bool shouldDisableCharSkinRenderer;

        private const bool shouldDebugWarnings = false;

        private void Start()
        {
            NetworkObject networkObject = GetComponentInParent<NetworkObject>();

            Animator animator = GetComponentInParent<Animator>();
            Transform target = animator.transform;
            FindRootBone(ref target, target);

            var boneMap = new Dictionary<string, Transform>();
            GetAllSkinnedMeshRenderers(ref boneMap, target);
            SkinnedMeshRenderer[] renderList = GetComponentsInChildren<SkinnedMeshRenderer>();

            //nothing to map
            if (renderList.Length == 0)
                return;

            foreach (SkinnedMeshRenderer srenderer in renderList)
            {
                if (srenderer.GetComponent<MagicaCloth2.MagicaCloth>())
                {
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
                    srenderer.updateWhenOffscreen = networkObject.IsLocalPlayer;
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
                    srenderer.updateWhenOffscreen = networkObject.IsLocalPlayer;
                }
            }
        }

        private void FindRootBone(ref Transform target, Transform start)
        {
            if (start.gameObject.layer != LayerMask.NameToLayer("Character")) { return; }

            foreach (Transform child in start)
            {
                if (child.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
                {
                    target = skinnedMeshRenderer.rootBone;
                    return;
                }
                FindRootBone(ref target, child);
            }
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