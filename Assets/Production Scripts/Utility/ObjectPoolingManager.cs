using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Vi.Utility
{
    public class ObjectPoolingManager : MonoBehaviour
    {
        public static List<PooledObjectInfo> ObjectPools = new List<PooledObjectInfo>();

        private const HideFlags hideFlagsForSpawnedObjects = HideFlags.None;

        public static GameObject SpawnObject(GameObject objectToSpawn)
        {
            PooledObjectInfo pool = ObjectPools.Find(item => item.LookUpString == objectToSpawn.name);

            // If this pool doesn't exist, create it
            if (pool == null)
            {
                pool = new PooledObjectInfo() { LookUpString = objectToSpawn.name };
                ObjectPools.Add(pool);
            }

            // Check if there are any inactive objects in the pool
            GameObject spawnableObj = pool.InactiveObjects.FirstOrDefault();

            if (spawnableObj == null)
            {
                // If there are no inactive objects, create a new one
                spawnableObj = Instantiate(objectToSpawn);
                spawnableObj.hideFlags = hideFlagsForSpawnedObjects;
            }
            else
            {
                // If there is an inactive object, reactivate it
                spawnableObj.transform.position = Vector3.zero;
                spawnableObj.transform.rotation = Quaternion.identity;
                pool.InactiveObjects.Remove(spawnableObj);
                spawnableObj.SetActive(true);
            }

            return spawnableObj;
        }

        public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            PooledObjectInfo pool = ObjectPools.Find(item => item.LookUpString == objectToSpawn.name);

            // If this pool doesn't exist, create it
            if (pool == null)
            {
                pool = new PooledObjectInfo() { LookUpString = objectToSpawn.name };
                ObjectPools.Add(pool);
            }

            // Check if there are any inactive objects in the pool
            GameObject spawnableObj = pool.InactiveObjects.FirstOrDefault();

            if (spawnableObj == null)
            {
                // If there are no inactive objects, create a new one
                spawnableObj = Instantiate(objectToSpawn, spawnPosition, spawnRotation);
                spawnableObj.hideFlags = hideFlagsForSpawnedObjects;
            }
            else
            {
                // If there is an inactive object, reactivate it
                spawnableObj.transform.position = spawnPosition;
                spawnableObj.transform.rotation = spawnRotation;
                pool.InactiveObjects.Remove(spawnableObj);
                spawnableObj.SetActive(true);
            }

            return spawnableObj;
        }

        public static GameObject SpawnObject(GameObject objectToSpawn, Transform parentTransform)
        {
            PooledObjectInfo pool = ObjectPools.Find(item => item.LookUpString == objectToSpawn.name);

            // If this pool doesn't exist, create it
            if (pool == null)
            {
                pool = new PooledObjectInfo() { LookUpString = objectToSpawn.name };
                ObjectPools.Add(pool);
            }

            // Check if there are any inactive objects in the pool
            GameObject spawnableObj = pool.InactiveObjects.FirstOrDefault();

            if (spawnableObj == null)
            {
                // If there are no inactive objects, create a new one
                spawnableObj = Instantiate(objectToSpawn, parentTransform);
                spawnableObj.hideFlags = hideFlagsForSpawnedObjects;
            }
            else
            {
                // If there is an inactive object, reactivate it
                spawnableObj.transform.SetParent(parentTransform);
                pool.InactiveObjects.Remove(spawnableObj);
                spawnableObj.SetActive(true);
            }

            return spawnableObj;
        }

        public static void ReturnObjectToPool(GameObject obj)
        {
            PooledObjectInfo pool = ObjectPools.Find(item => item.LookUpString == obj.name.Replace("(Clone)", ""));

            if (pool == null)
            {
                Debug.LogWarning("Trying to release an object that hasn't been pooled: " + obj.name);
            }
            else
            {
                obj.SetActive(false);
                pool.InactiveObjects.Add(obj);
            }
        }
    }

    public class PooledObjectInfo
    {
        public string LookUpString;
        public List<GameObject> InactiveObjects = new List<GameObject>();
    }
}