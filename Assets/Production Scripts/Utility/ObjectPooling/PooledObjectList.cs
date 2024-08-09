using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditor;
using Unity.Netcode;

namespace Vi.Utility
{
    [CreateAssetMenu(fileName = "PooledObjectList", menuName = "Production/Pooled Object List")]
    public class PooledObjectList : ScriptableObject
    {
        [SerializeField] private List<PooledObject> pooledObjects = new List<PooledObject>();

        public List<PooledObject> GetPooledObjects() { return pooledObjects.ToList(); }

        private void Awake()
        {
            for (int i = 0; i < pooledObjects.Count; i++)
            {
                if (pooledObjects[i]) { pooledObjects[i].SetPooledObjectIndex(i); }
            }
        }

# if UNITY_EDITOR
        [ContextMenu("Find Unregistered Pooled Objects")]
        private void FindUnregisteredPooledObjects()
        {
            NetworkPrefabsList networkPrefabsList = (NetworkPrefabsList)Selection.activeObject;

            foreach (string prefabFilePath in Directory.GetFiles(Path.Join("Assets", "Production"), "*.prefab", SearchOption.AllDirectories))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (!prefab) { continue; }
                if (prefab.TryGetComponent(out PooledObject pooledObject))
                {
                    if (prefab.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (networkPrefabsList.Contains(prefab)) { Debug.Log(prefabFilePath); }
                    }
                    else
                    {
                        if (!pooledObjects.Contains(pooledObject)) { Debug.Log(prefabFilePath); }
                    }
                }
            }

            foreach (string prefabFilePath in Directory.GetFiles(Path.Join("Assets", "PackagedPrefabs"), "*.prefab", SearchOption.AllDirectories))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (!prefab) { continue; }
                if (prefab.TryGetComponent(out PooledObject pooledObject))
                {
                    if (prefab.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (networkPrefabsList.Contains(prefab)) { Debug.Log(prefabFilePath); }
                    }
                    else
                    {
                        if (!pooledObjects.Contains(pooledObject)) { Debug.Log(prefabFilePath); }
                    }
                }
            }
        }

        [ContextMenu("Add Unregistered Pooled Objects")]
        private void AddUnregisteredPooledObjects()
        {
            NetworkPrefabsList networkPrefabsList = (NetworkPrefabsList)Selection.activeObject;

            foreach (string prefabFilePath in Directory.GetFiles(Path.Join("Assets", "Production"), "*.prefab", SearchOption.AllDirectories))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (!prefab) { continue; }
                if (prefab.TryGetComponent(out PooledObject pooledObject))
                {
                    if (prefab.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (networkPrefabsList.Contains(prefab))
                        {
                            Debug.Log(prefabFilePath);
                            pooledObjects.Add(pooledObject);
                        }
                    }
                    else
                    {
                        if (!pooledObjects.Contains(pooledObject))
                        {
                            Debug.Log(prefabFilePath);
                            pooledObjects.Add(pooledObject);
                        }
                    }
                }
            }

            foreach (string prefabFilePath in Directory.GetFiles(Path.Join("Assets", "PackagedPrefabs"), "*.prefab", SearchOption.AllDirectories))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (!prefab) { continue; }
                if (prefab.TryGetComponent(out PooledObject pooledObject))
                {
                    if (prefab.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (networkPrefabsList.Contains(prefab))
                        {
                            Debug.Log(prefabFilePath);
                            pooledObjects.Add(pooledObject);
                        }
                    }
                    else
                    {
                        if (!pooledObjects.Contains(pooledObject))
                        {
                            Debug.Log(prefabFilePath);
                            pooledObjects.Add(pooledObject);
                        }
                    }
                }
            }
        }
#endif
    }
}