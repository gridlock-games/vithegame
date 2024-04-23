using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Vi.ScriptableObjects
{
    [DisallowMultipleComponent]
    public class WearableEquipment : MonoBehaviour
    {
        public CharacterReference.EquipmentType equipmentType;
        public bool shouldDisableCharSkinRenderer;

        private void Start()
        {
            Transform target = GetComponentInParent<Animator>().transform;
            FindRootBone(ref target, target);

            var boneMap = new Dictionary<string, Transform>();
            GetAllSkinnedMeshRenderers(ref boneMap, target);
            List<SkinnedMeshRenderer> renderList = GetComponents<SkinnedMeshRenderer>().ToList();
            renderList.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>());

            //nothing to map
            if (renderList.Count == 0)
                return;

            foreach (var srenderer in renderList)
            {
                Transform[] newBones = new Transform[srenderer.bones.Length];

                for (int i = 0; i < srenderer.bones.Length; ++i)
                {
                    GameObject bone = srenderer.bones[i].gameObject;

                    if (!boneMap.TryGetValue(bone.name, out newBones[i]))
                    {
                        Debug.LogWarning(name + " Unable to map bone \"" + bone.name + "\" to target skeleton.");
                    }
                }
                srenderer.bones = newBones;
                srenderer.rootBone = FindBoundByName(srenderer.rootBone.name, boneMap);
                srenderer.updateWhenOffscreen = false;
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
                Debug.LogWarning(name + " Unable to map bone \"" + _name + "\" to target skeleton.");
            }
            return _rootBone;
        }
    }
}