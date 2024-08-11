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
    public class EditorUtilityMethods : UnityEditor.Editor
    {
        [MenuItem("Tools/Find Objects Not In Network Prefabs List")]
        static void FindObjectsNotInNetworkPrefabsList()
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

            string packagedPrefabsFolder = @"Assets\PackagedPrefabs";
            foreach (string prefabFilePath in Directory.GetFiles(packagedPrefabsFolder, "*.prefab", SearchOption.AllDirectories))
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

        [MenuItem("Tools/Set Texture Overrides for Android Platform")]
        static void SetTextureOverridesForAndroidPlatform()
        {
            string[] textures = AssetDatabase.FindAssets("t:Texture");
            for (int i = 0; i < textures.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Overriding Textures For Android",
                    i.ToString() + " out of " + textures.Length.ToString() + " textures completed",
                    i / textures.Length);

                string assetPath = AssetDatabase.GUIDToAssetPath(textures[i]);
                if (assetPath.Length == 0) { Debug.LogError(textures[i] + " not found"); continue; }

                try
                {
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                    TextureImporterPlatformSettings existingSettings = importer.GetPlatformTextureSettings("Android");
                    if (!existingSettings.overridden)
                    {
                        TextureImporterPlatformSettings androidSettings = new TextureImporterPlatformSettings();
                        androidSettings.name = "Android";
                        androidSettings.overridden = true;
                        androidSettings.maxTextureSize = 256;
                        importer.SetPlatformTextureSettings(androidSettings);
                        importer.SaveAndReimport();
                    }
                }
                catch // This happens on shit like font textures
                {

                }
            }
            EditorUtility.ClearProgressBar();
        }

        //[MenuItem("Tools/Set Objects In Network Prefab List To Not Spawn With Observers")]
        //static void SetNotSpawnWithObservers()
        //{
        //    NetworkPrefabsList networkPrefabsList = (NetworkPrefabsList)Selection.activeObject;
        //    if (!networkPrefabsList) { Debug.LogError("Please select a network prefabs list before running this!"); return; }

        //    foreach (NetworkPrefab networkPrefab in networkPrefabsList.PrefabList)
        //    {
        //        if (networkPrefab.Prefab.TryGetComponent(out NetworkObject networkObject))
        //        {
        //            if (!networkPrefab.Prefab.GetComponent<ActionVFX>() & !networkPrefab.Prefab.GetComponent<Projectile>()) { continue; }
        //            networkObject.SpawnWithObservers = true;
        //            EditorUtility.SetDirty(networkObject);
        //            Debug.Log(networkObject);
        //        }
        //    }
        //}

        //[MenuItem("Tools/Convert Projectile Layers To Colliders")]
        //static void SelectGameObjectsInLayer()
        //{
        //    foreach (GameObject g in FindGameObjectsInLayer(LayerMask.NameToLayer("Projectile")))
        //    {
        //        Debug.Log(g);
        //        g.layer = LayerMask.NameToLayer("ProjectileCollider");
        //        EditorUtility.SetDirty(g);
        //    }
        //}

        //private static GameObject[] FindGameObjectsInLayer(int layer)
        //{
        //    var goArray = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        //    var goList = new List<GameObject>();
        //    for (int i = 0; i < goArray.Length; i++)
        //    {
        //        if (goArray[i].layer == layer)
        //        {
        //            goList.Add(goArray[i]);
        //        }
        //    }
        //    if (goList.Count == 0)
        //    {
        //        return null;
        //    }
        //    return goList.ToArray();
        //}

        //[MenuItem("Tools/Find Item By GUID")]
        //static void FindItemByGUIDMethod()
        //{
        //    string guid = "56f1fae43c882434d94c645713a29ec6";
        //    string p = AssetDatabase.GUIDToAssetPath(guid);
        //    if (p.Length == 0) p = "not found";
        //    Debug.Log(p);
        //}
    }
}