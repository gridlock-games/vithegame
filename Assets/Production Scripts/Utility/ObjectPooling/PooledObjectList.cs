using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditor;

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
            foreach (string prefabFilePath in Directory.GetFiles(Path.Join("Assets", "Production"), "*.prefab", SearchOption.AllDirectories))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (!prefab) { continue; }
                if (prefab.TryGetComponent(out PooledObject pooledObject))
                {
                    if (!pooledObjects.Contains(pooledObject)) { Debug.Log(prefabFilePath); }
                }
            }

            foreach (string prefabFilePath in Directory.GetFiles(Path.Join("Assets", "PackagedPrefabs"), "*.prefab", SearchOption.AllDirectories))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (!prefab) { continue; }
                if (prefab.TryGetComponent(out PooledObject pooledObject))
                {
                    if (!pooledObjects.Contains(pooledObject)) { Debug.Log(prefabFilePath); }
                }
            }
        }
#endif
    }
}