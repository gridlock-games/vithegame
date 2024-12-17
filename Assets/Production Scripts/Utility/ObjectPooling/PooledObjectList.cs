using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditor;
using Unity.Netcode;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

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

        private AsyncOperationHandle<PooledObject>[] pooledObjectHandles;

#if UNITY_EDITOR
        public List<PooledObjectReference> GetPooledObjectReferences() { return pooledObjectReferences; }
#endif

        public List<PooledObject> GetPooledObjects()
        {
            List<PooledObject> pooledObjects = new List<PooledObject>();
            foreach (AsyncOperationHandle<PooledObject> handle in pooledObjectHandles)
            {
                if (!handle.IsValid())
                {
                    pooledObjects.Add(null);
                }
                else if (handle.IsDone)
                {
                    pooledObjects.Add(handle.Result);
                }
                else
                {
                    pooledObjects.Add(null);
                }
            }
            return pooledObjects;
        }

        private void Awake()
        {
            pooledObjectHandles = new AsyncOperationHandle<PooledObject>[pooledObjectReferences.Count];
        }

        public IEnumerator LoadAssets()
        {
            int loadCalledCount = 0;
            for (int i = 0; i < pooledObjectReferences.Count; i++)
            {
                int var = i;
                pooledObjectReferences[i].LoadAssetAsync().Completed += (handle) => OnInitialObjectLoad(handle, var);
                loadCalledCount++;
                if (!Application.isEditor) { yield return new WaitUntil(() => loadCalledCount - LoadCompletedCount < 3); }
            }
        }

        public int LoadCompletedCount { get; private set; }
        private void OnInitialObjectLoad(AsyncOperationHandle<PooledObject> handle, int index)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                handle.Result.SetPooledObjectIndex(index);
                pooledObjectHandles[index] = handle;
                ObjectPoolingManager.EvaluateNetworkPrefabHandler(handle.Result);
                ObjectPoolingManager.PoolInitialObjects(handle.Result);
            }
            else
            {
                Debug.LogError("Loading pooled object failed! Index: " + index);
            }
            LoadCompletedCount++;
        }

# if UNITY_EDITOR
        public bool TryAddPooledObject(PooledObject pooledObject)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(pooledObject.gameObject));

            PooledObjectReference reference = new PooledObjectReference(guid);
            MakeReferenceAddressable(guid);
            if (!pooledObjectReferences.Exists(item => item.AssetGUID == guid))
            {
                Debug.Log("Adding pooled object to references list " + pooledObject);
                pooledObjectReferences.Add(reference);
                EditorUtility.SetDirty(this);
                return true;
            }
            return false;
        }

        private static bool IsAssetAddressable(string guid)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            return entry != null;
        }

        private void MakeReferenceAddressable(string guid)
        {
            //if (IsAssetAddressable(guid)) { return; }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            AddressableAssetGroup groupToOrganize = settings.FindGroup(item => item.Name == "Pooled Objects");

            if (!groupToOrganize)
            {
                groupToOrganize = settings.CreateGroup("Duplicate Asset Isolation", false, false, false, settings.DefaultGroup.Schemas.ToList(), settings.DefaultGroup.SchemaTypes.ToArray());
            }
            settings.CreateOrMoveEntry(guid, groupToOrganize);
        }

        [ContextMenu("Make All References In List Addressable")]
        private void MakeAllReferencesInListAddressable()
        {
            foreach (PooledObjectReference reference in pooledObjectReferences)
            {
                MakeReferenceAddressable(reference.AssetGUID);
            }
        }

        [ContextMenu("Find Duplicates")]
        private void FindDuplicates()
        {
            pooledObjectReferences.RemoveAll(item => item.editorAsset == null);

            List<string> query = pooledObjectReferences.GroupBy(x => x.AssetGUID)
              .Where(g => g.Count() > 1)
              .Select(y => y.Key)
              .ToList();

            foreach (string guid in query)
            {
                Debug.Log(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
#endif
    }
}