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
                despawnedObjectPools.Add(new List<PooledObject>());
                spawnedObjectPools.Add(new List<PooledObject>());
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
        }

        private void OnDisable()
        {
            EventDelegateManager.sceneLoaded -= PoolInitialObjects;
        }

        private void PoolInitialObjects(Scene scene)
        {
            foreach (PooledObject pooledObject in pooledObjectList.GetPooledObjects())
            {
                while (despawnedObjectPools[pooledObject.GetPooledObjectIndex()].Count + spawnedObjectPools[pooledObject.GetPooledObjectIndex()].Count < pooledObject.GetNumberOfObjectsToPool())
                {
                    SpawnObjectForInitialPool(pooledObject);
                }

                while (despawnedObjectPools[pooledObject.GetPooledObjectIndex()].Count + spawnedObjectPools[pooledObject.GetPooledObjectIndex()].Count > pooledObject.GetNumberOfObjectsToPool())
                {
                    PooledObject objToDestroy = despawnedObjectPools[pooledObject.GetPooledObjectIndex()].FirstOrDefault();
                    if (objToDestroy)
                    {
                        despawnedObjectPools[pooledObject.GetPooledObjectIndex()].Remove(objToDestroy);
                        objToDestroy.MarkForDestruction();
                        Destroy(objToDestroy.gameObject);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public static void AddSpawnedObjectToActivePool(PooledObject pooledObject)
        {
            if (pooledObject == null) { Debug.LogError("Trying to add a null object to active pool!"); return; }
            if (pooledObject.GetPooledObjectIndex() == -1) { Debug.LogError(pooledObject + " isn't registered in the pooled object list!"); return; }
            if (spawnedObjectPools[pooledObject.GetPooledObjectIndex()].Contains(pooledObject)) { Debug.LogError(pooledObject + " Trying to add an object to active pool that is already present!"); return; }
            if (!pooledObject.IsSpawned) { Debug.LogError(pooledObject + " Trying to add a despawned object to active pool!"); return; }

            spawnedObjectPools[pooledObject.GetPooledObjectIndex()].Add(pooledObject);
        }

        public static void RemoveSpawnedObjectFromActivePool(PooledObject pooledObject)
        {
            if (pooledObject == null) { Debug.LogError("Trying to remove a null object from active pool!"); return; }
            if (pooledObject.GetPooledObjectIndex() == -1) { Debug.LogError(pooledObject + " isn't registered in the pooled object list!"); return; }
            if (!pooledObject.IsSpawned) { Debug.LogError(pooledObject + " Trying to remove a despawned object from active pool!"); return; }

            if (!spawnedObjectPools[pooledObject.GetPooledObjectIndex()].Remove(pooledObject))
            {
                Debug.LogError(pooledObject + " Trying to remove object from active pool that wasn't present!");
            }
        }

        private static List<List<PooledObject>> spawnedObjectPools = new List<List<PooledObject>>();
        private static List<List<PooledObject>> despawnedObjectPools = new List<List<PooledObject>>();

        private static void SpawnObjectForInitialPool(PooledObject objectToSpawn)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return; }

            objectToSpawn.SetIsPrewarmStatus(true);
            PooledObject spawnableObj = Instantiate(objectToSpawn.gameObject).GetComponent<PooledObject>();
            if (spawnableObj.gameObject.scene.name != instantiationSceneName) { SceneManager.MoveGameObjectToScene(spawnableObj.gameObject, SceneManager.GetSceneByName(instantiationSceneName)); }
            spawnableObj.InvokeOnSpawnFromPoolEvent();

            ReturnObjectToPool(spawnableObj);
        }

        public static PooledObject SpawnObject(PooledObject objectToSpawn)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = despawnedObjectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

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
                despawnedObjectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
                spawnableObj.gameObject.SetActive(true);
            }

            spawnableObj.InvokeOnSpawnFromPoolEvent();
            return spawnableObj;
        }

        public static PooledObject SpawnObject(PooledObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = despawnedObjectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

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
                despawnedObjectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
                spawnableObj.gameObject.SetActive(true);
            }

            spawnableObj.InvokeOnSpawnFromPoolEvent();
            return spawnableObj;
        }

        public static PooledObject SpawnObject(PooledObject objectToSpawn, Transform parentTransform)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = despawnedObjectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

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
                despawnedObjectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
                spawnableObj.gameObject.SetActive(true);
            }

            spawnableObj.InvokeOnSpawnFromPoolEvent();
            return spawnableObj;
        }

        public static PooledObject SpawnObject(PooledObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, Transform parentTransform)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = despawnedObjectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

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
                despawnedObjectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
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
            if (despawnedObjectPools[obj.GetPooledObjectIndex()].Contains(obj)) { Debug.LogError(obj + " Trying to return an object to pool that is already in the pool! Was it returned by the scene manager?"); return; }
            if (!obj.IsSpawned) { Debug.LogError(obj + " isn't spawned but you're trying to return it to a pool! Did you create it with Instantiate?"); return; }

            if (obj.transform.parent)
            {
                if (obj.TryGetComponent(out NetworkObject networkObject))
                {
                    networkObject.AutoObjectParentSync = false;
                    // We can't use TryRemoveParent here because that requires auto object parent sync to be enabled
                    networkObject.transform.SetParent(null, true);
                    networkObject.AutoObjectParentSync = true;
                }
                else
                {
                    obj.transform.SetParent(null, true);
                }
            }
            
            obj.gameObject.SetActive(false);
            if (obj.gameObject.scene.name != instantiationSceneName) { SceneManager.MoveGameObjectToScene(obj.gameObject, SceneManager.GetSceneByName(instantiationSceneName)); }
            despawnedObjectPools[obj.GetPooledObjectIndex()].Add(obj);
            obj.InvokeOnReturnToPoolEvent();
        }

        public static void ReturnObjectToPool(ref PooledObject obj)
        {
            ReturnObjectToPool(obj);
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
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) { return; }
#endif
            Debug.LogError(pooledObject + " was destroyed unexpectedly! Please use pooledobject.markfordestruction() before destroying");
            despawnedObjectPools[pooledObject.GetPooledObjectIndex()].Remove(pooledObject);
            spawnedObjectPools[pooledObject.GetPooledObjectIndex()].Remove(pooledObject);
        }
    }
}