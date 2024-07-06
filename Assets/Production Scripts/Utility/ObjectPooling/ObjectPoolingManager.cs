using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.VFX;
using UnityEngine.SceneManagement;

namespace Vi.Utility
{
    public class ObjectPoolingManager : MonoBehaviour
    {
        public const string cullingOverrideTag = "DoNotCull";
        public const string instantiationSceneName = "Base";
        private const HideFlags hideFlagsForSpawnedObjects = HideFlags.None;

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

        private static List<List<PooledObject>> objectPools = new List<List<PooledObject>>();

        public static PooledObject SpawnObject(PooledObject objectToSpawn)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = objectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

            if (spawnableObj == null)
            {
                // If there are no inactive objects, create a new one
                spawnableObj = Instantiate(objectToSpawn.gameObject).GetComponent<PooledObject>();
                if (spawnableObj.gameObject.scene.name != instantiationSceneName) { SceneManager.MoveGameObjectToScene(spawnableObj.gameObject, SceneManager.GetSceneByName(instantiationSceneName)); }
                spawnableObj.hideFlags = hideFlagsForSpawnedObjects;
            }
            else
            {
                // If there is an inactive object, reactivate it
                spawnableObj.transform.SetParent(null);
                spawnableObj.transform.position = Vector3.zero;
                spawnableObj.transform.rotation = Quaternion.identity;
                spawnableObj.transform.localScale = objectToSpawn.transform.localScale;
                objectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
                spawnableObj.gameObject.SetActive(true);
            }

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
                spawnableObj = Instantiate(objectToSpawn.gameObject, spawnPosition, spawnRotation).GetComponent<PooledObject>();
                if (spawnableObj.gameObject.scene.name != instantiationSceneName) { SceneManager.MoveGameObjectToScene(spawnableObj.gameObject, SceneManager.GetSceneByName(instantiationSceneName)); }
                spawnableObj.hideFlags = hideFlagsForSpawnedObjects;
            }
            else
            {
                // If there is an inactive object, reactivate it
                spawnableObj.transform.SetParent(null);
                spawnableObj.transform.position = spawnPosition;
                spawnableObj.transform.rotation = spawnRotation;
                spawnableObj.transform.localScale = objectToSpawn.transform.localScale;
                objectPools[objectToSpawn.GetPooledObjectIndex()].Remove(spawnableObj);
                spawnableObj.gameObject.SetActive(true);
            }

            return spawnableObj;
        }

        public static PooledObject SpawnObject(PooledObject objectToSpawn, Transform parentTransform)
        {
            if (objectToSpawn.GetPooledObjectIndex() == -1) { Debug.LogError(objectToSpawn + " isn't registered in the pooled object list!"); return null; }

            // Check if there are any inactive objects in the pool
            PooledObject spawnableObj = objectPools[objectToSpawn.GetPooledObjectIndex()].FirstOrDefault();

            if (spawnableObj == null)
            {
                // If there are no inactive objects, create a new one
                spawnableObj = Instantiate(objectToSpawn.gameObject, parentTransform).GetComponent<PooledObject>();
                if (parentTransform)
                {
                    spawnableObj.transform.localScale = new Vector3(objectToSpawn.transform.localScale.x / parentTransform.lossyScale.x,
                        objectToSpawn.transform.localScale.y / parentTransform.lossyScale.y,
                        objectToSpawn.transform.localScale.z / parentTransform.lossyScale.z);
                }
                spawnableObj.hideFlags = hideFlagsForSpawnedObjects;
            }
            else
            {
                // If there is an inactive object, reactivate it
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
                spawnableObj = Instantiate(objectToSpawn.gameObject, spawnPosition, spawnRotation, parentTransform).GetComponent<PooledObject>();
                if (parentTransform)
                {
                    spawnableObj.transform.localScale = new Vector3(objectToSpawn.transform.localScale.x / parentTransform.lossyScale.x,
                        objectToSpawn.transform.localScale.y / parentTransform.lossyScale.y,
                        objectToSpawn.transform.localScale.z / parentTransform.lossyScale.z);
                }
                spawnableObj.hideFlags = hideFlagsForSpawnedObjects;
            }
            else
            {
                // If there is an inactive object, reactivate it
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

            return spawnableObj;
        }

        public static void ReturnObjectToPool(PooledObject obj)
        {
            if (obj == null) { Debug.LogWarning("Trying to return a null gameobject to pool"); return; }

            if (obj.GetPooledObjectIndex() == -1) { Debug.LogError(obj + " isn't registered in the pooled object list!"); return; }

            obj.gameObject.SetActive(false);
            objectPools[obj.GetPooledObjectIndex()].Add(obj);
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
    }
}