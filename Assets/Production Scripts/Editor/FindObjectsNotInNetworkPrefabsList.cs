using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Netcode;
using Vi.Core;
using Vi.ScriptableObjects;

namespace Vi.Editor
{
    public class FindObjectsNotInNetworkPrefabsList : UnityEditor.Editor
    {
        [MenuItem("Tools/Find Objects Not In Network Prefabs List")]
        static void SelectGameObjectsInLayer()
        {
            NetworkPrefabsList networkPrefabsList = (NetworkPrefabsList)Selection.activeObject;
            if (!networkPrefabsList) { Debug.LogError("Please select a network prefabs list before running this!"); return; }

            List<ActionClip> actionClips = new List<ActionClip>();

            string actionClipFolder = @"Assets/Production/Actions";
            foreach (string actionClipFilePath in Directory.GetFiles(actionClipFolder, "*.asset", SearchOption.AllDirectories))
            {
                ActionClip actionClip = AssetDatabase.LoadAssetAtPath<ActionClip>(actionClipFilePath);
                if (actionClip) { actionClips.Add(actionClip); }
            }

            string VFXFolder = @"Assets\Production\Prefabs\VFX";
            foreach (string prefabFilePath in Directory.GetFiles(VFXFolder, "*.prefab", SearchOption.AllDirectories))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (prefab)
                {
                    if (prefab.TryGetComponent(out ActionVFX actionVFX))
                    {
                        bool vfxReferencedInActionClip = false;
                        foreach (ActionClip actionClip in actionClips)
                        {
                            if (actionClip.actionVFXList.Contains(actionVFX))
                            {
                                vfxReferencedInActionClip = true;
                                break;
                            }
                        }

                        if (vfxReferencedInActionClip)
                        {
                            if (!networkPrefabsList.Contains(prefab))
                            {
                                // Add object here
                                Debug.Log(prefab.name + " referenced in action clip, but not in the selected network prefabs list");
                                Debug.Log(prefabFilePath);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("Could not load prefab at path: " + prefabFilePath);
                }
            }
        }

        [MenuItem("Tools/Set Objects In Network Prefab List To Not Spawn With Observers")]
        static void SetNotSpawnWithObservers()
        {
            NetworkPrefabsList networkPrefabsList = (NetworkPrefabsList)Selection.activeObject;
            if (!networkPrefabsList) { Debug.LogError("Please select a network prefabs list before running this!"); return; }

            foreach (NetworkPrefab networkPrefab in networkPrefabsList.PrefabList)
            {
                if (networkPrefab.Prefab.TryGetComponent(out NetworkObject networkObject))
                {
                    if (!networkPrefab.Prefab.GetComponent<ActionVFX>() & !networkPrefab.Prefab.GetComponent<Projectile>()) { continue; }
                    networkObject.SpawnWithObservers = true;
                    EditorUtility.SetDirty(networkObject);
                    Debug.Log(networkObject);
                }
            }
        }
    }
}