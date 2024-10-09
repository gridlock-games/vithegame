using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditor;
using Unity.Netcode;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace Vi.Utility
{
    [CreateAssetMenu(fileName = "PooledObjectList", menuName = "Production/Pooled Object List")]
    public class PooledObjectList : ScriptableObject
    {
        public int TotalReferenceCount { get { return pooledObjectReferences.Count; } }

        [SerializeField] private List<PooledObjectReference> pooledObjectReferences = new List<PooledObjectReference>();

        private PooledObject[] pooledObjects;

        public List<PooledObject> GetPooledObjects() { return pooledObjects.ToList(); }

        private void Awake()
        {
            pooledObjects = new PooledObject[pooledObjectReferences.Count];
            for (int i = 0; i < pooledObjectReferences.Count; i++)
            {
                int var = i;
                pooledObjectReferences[i].LoadAssetAsync().Completed += (handle) => OnInitialObjectLoad(handle, var);
            }
        }

        public int LoadCompletedCount { get; private set; }
        private void OnInitialObjectLoad(AsyncOperationHandle<PooledObject> handle, int index)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                handle.Result.SetPooledObjectIndex(index);
                pooledObjects[index] = handle.Result;
            }
            else
            {
                Debug.LogError("Loading pooled object failed! Index: " + index);
            }
            LoadCompletedCount++;
        }

# if UNITY_EDITOR
        private static string networkPrefabListFolderPath = @"Assets\Production\NetworkPrefabLists";
        static List<NetworkPrefabsList> GetNetworkPrefabsLists()
        {
            List<NetworkPrefabsList> networkPrefabsLists = new List<NetworkPrefabsList>();
            foreach (string listPath in Directory.GetFiles(networkPrefabListFolderPath, "*.asset", SearchOption.AllDirectories))
            {
                var prefabsList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(listPath);
                if (prefabsList) { networkPrefabsLists.Add(prefabsList); }
            }
            return networkPrefabsLists;
        }

        [ContextMenu("Find Unregistered Pooled Objects")]
        private void FindUnregisteredPooledObjects()
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(Path.Join("Assets", "Production"), "*.prefab", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(Path.Join("Assets", "PackagedPrefabs"), "*.prefab", SearchOption.AllDirectories));
            foreach (string prefabFilePath in files)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (!prefab) { continue; }
                if (prefab.TryGetComponent(out PooledObject pooledObject))
                {
                    if (prefab.TryGetComponent(out NetworkObject networkObject))
                    {
                        bool contains = false;
                        foreach (NetworkPrefabsList networkPrefabsList in GetNetworkPrefabsLists())
                        {
                            if (networkPrefabsList.Contains(networkObject.gameObject))
                            {
                                contains = true;
                                break;
                            }
                        }

                        if (contains)
                        {
                            PooledObjectReference reference = new PooledObjectReference(AssetDatabase.AssetPathToGUID(prefabFilePath));
                            if (!pooledObjectReferences.Contains(reference)) { Debug.Log(prefabFilePath); }
                        }
                    }
                    else
                    {
                        PooledObjectReference reference = new PooledObjectReference(AssetDatabase.AssetPathToGUID(prefabFilePath));
                        if (!pooledObjectReferences.Contains(reference)) { Debug.Log(prefabFilePath); }
                    }
                }
            }
        }

        [ContextMenu("Add Unregistered Pooled Objects")]
        public void AddUnregisteredPooledObjects()
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(Path.Join("Assets", "Production"), "*.prefab", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(Path.Join("Assets", "PackagedPrefabs"), "*.prefab", SearchOption.AllDirectories));
            foreach (string prefabFilePath in files)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (!prefab) { continue; }
                if (prefab.TryGetComponent(out PooledObject pooledObject))
                {
                    if (prefab.TryGetComponent(out NetworkObject networkObject))
                    {
                        bool contains = false;
                        foreach (NetworkPrefabsList networkPrefabsList in GetNetworkPrefabsLists())
                        {
                            if (networkPrefabsList.Contains(networkObject.gameObject))
                            {
                                contains = true;
                                break;
                            }
                        }

                        if (contains)
                        {
                            PooledObjectReference reference = new PooledObjectReference(AssetDatabase.AssetPathToGUID(prefabFilePath));
                            MakeReferenceAddressable(reference);
                            if (!pooledObjectReferences.Contains(reference))
                            {
                                Debug.Log(prefabFilePath);
                                pooledObjectReferences.Add(reference);
                            }
                        }
                    }
                    else
                    {
                        PooledObjectReference reference = new PooledObjectReference(AssetDatabase.AssetPathToGUID(prefabFilePath));
                        MakeReferenceAddressable(reference);
                        if (!pooledObjectReferences.Contains(reference))
                        {
                            Debug.Log(prefabFilePath);
                            pooledObjectReferences.Add(reference);
                        }
                    }
                }
            }
        }

        private static bool IsAssetAddressable(Object obj)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));
            return entry != null;
        }

        private void MakeReferenceAddressable(PooledObjectReference reference)
        {
            if (IsAssetAddressable(reference.Asset)) { return; }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            AddressableAssetGroup referenceGroup = settings.FindGroup(item => item.Name == "Assets Production Prefabs Mobs Ogres ");
            AddressableAssetGroup groupToOrganize = settings.FindGroup(item => item.Name == "Duplicate Asset Isolation");

            if (!groupToOrganize)
            {
                groupToOrganize = settings.CreateGroup("Duplicate Asset Isolation", false, false, false, referenceGroup.Schemas.ToList(), referenceGroup.SchemaTypes.ToArray());
            }
            settings.CreateOrMoveEntry(reference.AssetGUID, groupToOrganize);
        }

        [ContextMenu("Find Duplicates")]
        private void FindDuplicates()
        {
            pooledObjectReferences.RemoveAll(item => item == null);

            List<PooledObjectReference> query = pooledObjectReferences.GroupBy(x => x)
              .Where(g => g.Count() > 1)
              .Select(y => y.Key)
              .ToList();

            foreach (PooledObjectReference pooledObject in query)
            {
                Debug.Log(pooledObject);
            }
        }
#endif
    }
}