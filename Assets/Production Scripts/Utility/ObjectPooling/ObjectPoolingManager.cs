using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.VFX;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace Vi.Utility
{
    public class ObjectPoolingManager : MonoBehaviour
    {
        public const string cullingOverrideTag = "DoNotCull";
        public const string instantiationSceneName = "Base";
        public const HideFlags hideFlagsForSpawnedObjects = HideFlags.HideInHierarchy;

        [SerializeField] private PooledObjectList pooledObjectList;

        private PooledObjectList pooledObjectListInstance;

        public static ObjectPoolingManager Singleton
        {
            get
            {
                if (!_singleton) { Debug.LogError("Object Pooling Manager is null"); }
                return _singleton;
            }
        }

        private static ObjectPoolingManager _singleton;
        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
            pooledObjectListInstance = Instantiate(pooledObjectList);

            for (int i = 0; i < pooledObjectListInstance.GetPooledObjects().Count; i++)
            {
                objectPools.Add(new List<PooledObject>());
            }
        }

        private void Start()
        {
            foreach (PooledObject pooledObject in pooledObjectListInstance.GetPooledObjects())
            {
                if (pooledObject.TryGetComponent(out NetworkObject networkObject))
                {
                    NetworkManager.Singleton.PrefabHandler.AddHandler(networkObject, new PooledPrefabInstanceHandler(pooledObject));
                }
            }
        }

        private class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
        {
            PooledObject m_Prefab;

            public PooledPrefabInstanceHandler(PooledObject prefab)
            {
                m_Prefab = prefab;
            }

            NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                return SpawnObject(m_Prefab.GetComponent<PooledObject>(), position, rotation).GetComponent<NetworkObject>();
            }

            void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
            {
                ReturnObjectToPool(networkObject.GetComponent<PooledObject>());
            }
        }

        private void OnEnable()
        {
            EventDelegateManager.sceneLoaded += PoolInitialObjects;
            EventDelegateManager.sceneUnloaded += RemoveNullObjects;
        }

        private void OnDisable()
        {
            EventDelegateManager.sceneLoaded -= PoolInitialObjects;
            EventDelegateManager.sceneUnloaded -= RemoveNullObjects;
        }

        private void PoolInitialObjects(Scene scene)
        {
            foreach (PooledObject pooledObject in pooledObjectList.GetPooledObjects())
            {
                for (int i = 0; i < pooledObject.GetNumberOfObjectsToPool(); i++)
                {
                    if (objectPools[pooledObject.GetPooledObjectIndex()].Count < pooledObject.GetNumberOfObjectsToPool()) { SpawnObjectForInitialPool(pooledObject); }
                }
            }
        }

        private void RemoveNullObjects()
        {
            for (int i = 0; i < objectPools.Count; i++)
            {
                objectPools[i].RemoveAll(item => !item);
            }
        }

        private static List<List<PooledObject>> objectPools = new List<List<PooledObject>>();

        private static void SpawnObjectForInitialPool(PooledObject objectToSpawn)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return; }

            objectToSpawn.SetIsPrewarmStatus(true);
            PooledObject spawnableObj = Instantiate(objectToSpawn.gameObject).GetComponent<PooledObject>();
            if (spawnableObj.gameObject.scene.name != instantiationSceneName) { SceneManager.MoveGameObjectToScene(spawnableObj.gameObject, SceneManager.GetSceneByName(instantiationSceneName)); }
            
            ReturnObjectToPool(spawnableObj);
        }

        public static PooledObject SpawnObject(PooledObject objectToSpawn)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = objectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

            if (spawnableObj == null)
            {
                // If there are no inactive objects, create a new one
                objectToSpawn.SetIsPrewarmStatus(false);
                spawnableObj = Instantiate(objectToSpawn.gameObject).GetComponent<PooledObject>();
                if (spawnableObj.gameObject.scene.name != instantiationSceneName) { SceneManager.MoveGameObjectToScene(spawnableObj.gameObject, SceneManager.GetSceneByName(instantiationSceneName)); }
            }
            else
            {
                // If there is an inactive object, reactivate it
                spawnableObj.SetIsPrewarmStatus(false);
                if (spawnableObj.TryGetComponent(out NetworkObject networkObject))
                    Singleton.StartCoroutine(SetParentAfterSpawn(networkObject, null));
                else
                    spawnableObj.transform.SetParent(null);
                spawnableObj.transform.position = Vector3.zero;
                spawnableObj.transform.rotation = Quaternion.identity;
                spawnableObj.transform.localScale = objectToSpawn.transform.localScale;
                objectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
                spawnableObj.gameObject.SetActive(true);
            }

            spawnableObj.InvokeOnSpawnFromPoolEvent();
            return spawnableObj;
        }

        public static PooledObject SpawnObject(PooledObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = objectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

            if (spawnableObj == null)
            {
                // If there are no inactive objects, create a new one
                objectToSpawn.SetIsPrewarmStatus(false);
                spawnableObj = Instantiate(objectToSpawn.gameObject, spawnPosition, spawnRotation).GetComponent<PooledObject>();
                if (spawnableObj.gameObject.scene.name != instantiationSceneName) { SceneManager.MoveGameObjectToScene(spawnableObj.gameObject, SceneManager.GetSceneByName(instantiationSceneName)); }
            }
            else
            {
                // If there is an inactive object, reactivate it
                spawnableObj.SetIsPrewarmStatus(false);
                if (spawnableObj.TryGetComponent(out NetworkObject networkObject))
                    Singleton.StartCoroutine(SetParentAfterSpawn(networkObject, null));
                else
                    spawnableObj.transform.SetParent(null);
                spawnableObj.transform.position = spawnPosition;
                spawnableObj.transform.rotation = spawnRotation;
                spawnableObj.transform.localScale = objectToSpawn.transform.localScale;
                objectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
                spawnableObj.gameObject.SetActive(true);
            }

            spawnableObj.InvokeOnSpawnFromPoolEvent();
            return spawnableObj;
        }

        private void Update()
        {
            foreach (var pool in objectPools)
            {
                foreach (PooledObject pooledObject in pool)
                {
                    if (pooledObject.gameObject.activeSelf)
                    {
                        Debug.LogError(pooledObject + " is already active! " + pooledObject.transform.parent);
                    }
                }
            }
        }

        public static PooledObject SpawnObject(PooledObject objectToSpawn, Transform parentTransform)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = objectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

            if (spawnableObj == null)
            {
                // If there are no inactive objects, create a new one
                objectToSpawn.SetIsPrewarmStatus(false);
                spawnableObj = Instantiate(objectToSpawn.gameObject, parentTransform).GetComponent<PooledObject>();
                if (parentTransform)
                {
                    spawnableObj.transform.localScale = new Vector3(objectToSpawn.transform.localScale.x / parentTransform.lossyScale.x,
                        objectToSpawn.transform.localScale.y / parentTransform.lossyScale.y,
                        objectToSpawn.transform.localScale.z / parentTransform.lossyScale.z);
                }
            }
            else
            {
                if (spawnableObj.gameObject.activeSelf) { Debug.LogError("Object is already active but you're trying to spawn it! " + spawnableObj + " " + spawnableObj.transform.parent); }
                // If there is an inactive object, reactivate it
                spawnableObj.SetIsPrewarmStatus(false);
                if (spawnableObj.TryGetComponent(out NetworkObject networkObject))
                    Singleton.StartCoroutine(SetParentAfterSpawn(networkObject, parentTransform));
                else
                    spawnableObj.transform.SetParent(parentTransform);
                spawnableObj.transform.localPosition = objectToSpawn.transform.localPosition;
                spawnableObj.transform.localRotation = objectToSpawn.transform.localRotation;
                if (parentTransform)
                {
                    spawnableObj.transform.localScale = new Vector3(objectToSpawn.transform.localScale.x / parentTransform.lossyScale.x,
                        objectToSpawn.transform.localScale.y / parentTransform.lossyScale.y,
                        objectToSpawn.transform.localScale.z / parentTransform.lossyScale.z);
                }
                objectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
                spawnableObj.gameObject.SetActive(true);
            }

            spawnableObj.InvokeOnSpawnFromPoolEvent();
            return spawnableObj;
        }

        public static PooledObject SpawnObject(PooledObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, Transform parentTransform)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = objectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

            if (spawnableObj == null)
            {
                // If there are no inactive objects, create a new one
                objectToSpawn.SetIsPrewarmStatus(false);
                spawnableObj = Instantiate(objectToSpawn.gameObject, spawnPosition, spawnRotation, parentTransform).GetComponent<PooledObject>();
                if (parentTransform)
                {
                    spawnableObj.transform.localScale = new Vector3(objectToSpawn.transform.localScale.x / parentTransform.lossyScale.x,
                        objectToSpawn.transform.localScale.y / parentTransform.lossyScale.y,
                        objectToSpawn.transform.localScale.z / parentTransform.lossyScale.z);
                }
            }
            else
            {
                // If there is an inactive object, reactivate it
                spawnableObj.SetIsPrewarmStatus(false);
                if (spawnableObj.TryGetComponent(out NetworkObject networkObject))
                    Singleton.StartCoroutine(SetParentAfterSpawn(networkObject, parentTransform));
                else
                    spawnableObj.transform.SetParent(parentTransform);
                spawnableObj.transform.position = spawnPosition;
                spawnableObj.transform.rotation = spawnRotation;
                if (parentTransform)
                {
                    spawnableObj.transform.localScale = new Vector3(objectToSpawn.transform.localScale.x / parentTransform.lossyScale.x,
                        objectToSpawn.transform.localScale.y / parentTransform.lossyScale.y,
                        objectToSpawn.transform.localScale.z / parentTransform.lossyScale.z);
                }
                objectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
                spawnableObj.gameObject.SetActive(true);
            }

            spawnableObj.InvokeOnSpawnFromPoolEvent();
            return spawnableObj;
        }

        private static IEnumerator SetParentAfterSpawn(NetworkObject networkObject, Transform parent)
        {
            if (!NetworkManager.Singleton.IsServer) { yield break; }
            if (networkObject.transform.parent == parent) { yield break; }
            yield return new WaitUntil(() => networkObject.IsSpawned);
            if (!networkObject.TrySetParent(parent)) { Debug.LogError("Error while setting parent for networ object " + networkObject + " to parent " + parent); }
        }

        public static void ReturnObjectToPool(PooledObject obj)
        {
            if (obj == null) { Debug.LogWarning("Trying to return a null gameobject to pool"); return; }

            if (obj.GetPooledObjectIndex() == -1) { Debug.LogError(obj + " isn't registered in the pooled object list!"); return; }

            foreach (PooledObject pooledObject in obj.GetComponentsInChildren<PooledObject>(true))
            {
                if (pooledObject == obj) { continue; }
                pooledObject.transform.SetParent(null, true);
            }

            obj.gameObject.SetActive(false);
            objectPools[obj.GetPooledObjectIndex()].Add(obj);
            obj.InvokeOnReturnToPoolEvent();
        }

        public static void ReturnObjectToPool(ref PooledObject obj)
        {
            if (obj == null) { Debug.LogWarning("Trying to return a null gameobject to pool"); return; }

            if (obj.GetPooledObjectIndex() == -1) { Debug.LogError(obj + " isn't registered in the pooled object list!"); return; }

            obj.gameObject.SetActive(false);
            objectPools[obj.GetPooledObjectIndex()].Add(obj);
            obj.InvokeOnReturnToPoolEvent();
            obj = null;
        }

        public static IEnumerator ReturnVFXToPoolWhenFinishedPlaying(PooledObject vfxInstance)
        {
            ParticleSystem particleSystem = vfxInstance.GetComponentInChildren<ParticleSystem>();
            if (particleSystem)
            {
                while (true)
                {
                    yield return null;
                    if (!vfxInstance) { yield break; }
                    if (!particleSystem.isPlaying) { break; }
                }
            }

            AudioSource audioSource = vfxInstance.GetComponentInChildren<AudioSource>();
            if (audioSource)
            {
                while (true)
                {
                    yield return null;
                    if (!vfxInstance) { yield break; }
                    if (!audioSource.isPlaying) { break; }
                }
            }

            VisualEffect visualEffect = vfxInstance.GetComponentInChildren<VisualEffect>();
            if (visualEffect)
            {
                while (true)
                {
                    yield return null;
                    if (!vfxInstance) { yield break; }
                    if (!visualEffect.HasAnySystemAwake()) { break; }
                }
            }

            ReturnObjectToPool(vfxInstance);
        }

        public static void OnPooledObjectDestroy(PooledObject pooledObject)
        {
            objectPools[pooledObject.GetPooledObjectIndex()].Remove(pooledObject);
        }
    }
}