using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Netcode;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Core.MeshSlicing;
using System.Linq;
using Vi.Core.CombatAgents;

namespace Vi.Editor
{
    public class EditorUtilityMethods : UnityEditor.Editor
    {
        [MenuItem("Tools/Production/Generate Dropped Weapon Variants")]
        static void GenerateDroppedWeaponVariants()
        {
            CharacterReference characterReference = (CharacterReference)Selection.activeObject;

            foreach (var weaponOption in characterReference.GetWeaponOptions())
            {
                foreach (var weaponModelData in weaponOption.weapon.GetWeaponModelData())
                {
                    foreach (var data in weaponModelData.data)
                    {
                        if (data.weaponPrefab.TryGetComponent(out RuntimeWeapon runtimeWeapon))
                        {
                            runtimeWeapon.CreateDropWeaponPrefabVariant();
                        }
                        else
                        {
                            Debug.LogError(data.weaponPrefab + " doesn't have a runtime weapon");
                        }
                    }
                }
            }

            string mobFolder = @"Assets/Production/Prefabs/Mobs";
            foreach (string mobFilePath in Directory.GetFiles(mobFolder, "*.prefab", SearchOption.AllDirectories))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mobFilePath);
                if (prefab)
                {
                    if (prefab.TryGetComponent(out Mob mob))
                    {
                        if (mob.GetWeaponOption() == null) { continue; }
                        if (mob.GetWeaponOption().weapon == null) { continue; }
                        foreach (var weaponModelData in mob.GetWeaponOption().weapon.GetWeaponModelData())
                        {
                            foreach (var data in weaponModelData.data)
                            {
                                if (data.weaponPrefab.TryGetComponent(out RuntimeWeapon runtimeWeapon))
                                {
                                    runtimeWeapon.CreateDropWeaponPrefabVariant();
                                }
                                else
                                {
                                    Debug.LogError(data.weaponPrefab + " doesn't have a runtime weapon");
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("Unable to load prefab at path " + mobFilePath);
                }
            }
        }

        [MenuItem("Tools/Production/Remove Components From Weapon Previews")]
        static void RemoveComponentsFromWeaponPreviews()
        {
            CharacterReference characterReference = (CharacterReference)Selection.activeObject;
            foreach (var weaponOption in characterReference.GetWeaponOptions())
            {
                foreach (Component component in weaponOption.weaponPreviewPrefab.GetComponentsInChildren<Component>())
                {
                    if (component is not Transform
                        & component is not Camera
                        & component is not UnityEngine.Rendering.Universal.UniversalAdditionalCameraData
                        & component is not Renderer)
                    {
                        DestroyImmediate(component, true);
                    }
                }
                EditorUtility.SetDirty(weaponOption.weaponPreviewPrefab);
            }
        }

        [MenuItem("Tools/Production/Generate Exploded Meshes")]
        static void GenerateExplodedMeshes()
        {
            Mesh mesh = (Mesh)Selection.activeObject;
            if (!mesh) { Debug.LogError("Please select a mesh to explode"); return; }

            GameObject root = new GameObject("Root");

            SkinnedMeshRenderer skinnedMeshRenderer = root.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.sharedMesh = mesh;
            root.AddComponent<Sliceable>();

            List<GameObject> objectsToSlice = Slicer.RandomSlice(root).ToList();
            DestroyImmediate(root);

            bool canceled = false;
            int sliceCount = 5;
            for (int i = 0; i < sliceCount; i++)
            {
                GameObject[] copy = objectsToSlice.ToArray();
                objectsToSlice.Clear();
                int j = 0;
                foreach (GameObject slice in copy)
                {
                    if (EditorUtility.DisplayCancelableProgressBar((i + 1).ToString() + " slices out of " + sliceCount, j.ToString() + " meshes out of " + copy.Length, j / (float)copy.Length))
                    {
                        canceled = true;
                        break;
                    }

                    objectsToSlice.AddRange(Slicer.RandomSlice(slice));
                    DestroyImmediate(slice);
                    j++;
                }

                if (canceled) { break; }

                foreach (GameObject slice in objectsToSlice)
                {
                    slice.name = mesh.name + "_Slice";
                }
            }

            EditorUtility.ClearProgressBar();

            GameObject parent = new GameObject("Delete Me");
            foreach (GameObject slice in objectsToSlice)
            {
                slice.transform.SetParent(parent.transform);
            }

            if (canceled) { return; }

            string meshPath = AssetDatabase.GetAssetPath(mesh);
            meshPath = meshPath.Replace(".fbx", "_Explosion");
            string resultFolderGUID = AssetDatabase.CreateFolder(meshPath.Substring(0, meshPath.LastIndexOf('/')), mesh.name + "_Explosion");

            int counter = 0;
            foreach (GameObject slice in objectsToSlice)
            {
                counter++;
                AssetDatabase.CreateAsset(slice.GetComponent<MeshFilter>().sharedMesh, Path.Join(AssetDatabase.GUIDToAssetPath(resultFolderGUID), slice.name + "_" + counter.ToString() + ".asset"));
            }
        }

        [MenuItem("Tools/Production/Find Missing Network Prefabs")]
        static void FindMissingNetworkPrefabs()
        {
            List<NetworkPrefabsList> networkPrefabsLists = new List<NetworkPrefabsList>();
            foreach (Object obj in Selection.objects)
            {
                networkPrefabsLists.Add((NetworkPrefabsList)obj);
            }

            string baseFolder = @"Assets\Production\Prefabs";
            foreach (string prefabFilePath in Directory.GetFiles(baseFolder, "*.prefab", SearchOption.AllDirectories))
            {
                if (prefabFilePath.Contains("VFX")) { continue; }
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (prefab)
                {
                    if (prefab.TryGetComponent(out NetworkObject networkObject))
                    {
                        bool contains = false;
                        foreach (NetworkPrefabsList networkPrefabsList in networkPrefabsLists)
                        {
                            if (networkPrefabsList.Contains(networkObject.gameObject))
                            {
                                contains = true;
                                break;
                            }
                        }

                        if (!contains) { Debug.Log(prefabFilePath); }
                    }
                }
            }
        }

        [MenuItem("Tools/Production/Find VFX Not In Network Prefabs List")]
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

        [MenuItem("Tools/Production/Set Video Clip Import Overrides")]
        static void SetVideoClipImportOverrides()
        {
            string[] videoClips = AssetDatabase.FindAssets("t:VideoClip");
            for (int i = 0; i < videoClips.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Overriding video clips For Android",
                    i.ToString() + " out of " + videoClips.Length.ToString() + " video clips completed",
                    i / videoClips.Length);

                string assetPath = AssetDatabase.GUIDToAssetPath(videoClips[i]);
                if (assetPath.Contains("com.unity.")) { continue; }
                if (assetPath.Length == 0) { Debug.LogError(videoClips[i] + " not found"); continue; }

                VideoClipImporter importer = (VideoClipImporter)AssetImporter.GetAtPath(assetPath);
                importer.importAudio = false;

                VideoImporterTargetSettings defaultSettings = importer.defaultTargetSettings;
                defaultSettings.enableTranscoding = true;

                importer.defaultTargetSettings = defaultSettings;
                VideoImporterTargetSettings androidSettings = defaultSettings;
                androidSettings.spatialQuality = VideoSpatialQuality.MediumSpatialQuality;

                importer.SetTargetSettings("Android", androidSettings);

                importer.SaveAndReimport();
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Tools/Production/Set Texture Import Overrides")]
        static void SetTextureImportOverrides()
        {
            string[] textures = AssetDatabase.FindAssets("t:Texture");
            for (int i = 0; i < textures.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Overriding Textures For Android",
                    i.ToString() + " out of " + textures.Length.ToString() + " textures completed",
                    i / textures.Length)) { break; }

                string assetPath = AssetDatabase.GUIDToAssetPath(textures[i]);
                if (assetPath.Contains("com.unity.")) { continue; }
                if (assetPath.Length == 0) { Debug.LogError(textures[i] + " not found"); continue; }

                if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
                {
                    bool shouldReimport = false;

                    //TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
                    //if (!defaultSettings.crunchedCompression)
                    //{
                    //    defaultSettings.crunchedCompression = true;
                    //    importer.SetPlatformTextureSettings(defaultSettings);
                    //    shouldReimport = true;
                    //}

                    if (!importer.GetPlatformTextureSettings("Android").overridden)
                    {
                        TextureImporterPlatformSettings androidSettings = new TextureImporterPlatformSettings();
                        androidSettings.name = "Android";
                        androidSettings.overridden = true;
                        androidSettings.maxTextureSize = 256;
                        importer.SetPlatformTextureSettings(androidSettings);
                        shouldReimport = true;
                    }

                    if (shouldReimport) { importer.SaveAndReimport(); }
                }
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Tools/Clear Progress Bar")]
        static void ClearProgressBar()
        {
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