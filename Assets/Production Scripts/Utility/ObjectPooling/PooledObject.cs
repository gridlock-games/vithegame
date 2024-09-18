using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace Vi.Utility
{
    public class PooledObject : MonoBehaviour
    {
        [SerializeField] private int numberOfObjectsToPool = 5;

        public int GetNumberOfObjectsToPool() { return numberOfObjectsToPool; }

        [SerializeField] private int pooledObjectIndex = -1;
        public int GetPooledObjectIndex() { return pooledObjectIndex; }

        public void SetPooledObjectIndex(int index)
        {
            #if UNITY_EDITOR
            if (index != pooledObjectIndex) { UnityEditor.EditorUtility.SetDirty(this); }
            #endif
            pooledObjectIndex = index;
        }

        [SerializeField] private bool isPrewarmObject;

        public void SetIsPrewarmStatus(bool isPrewarmObject)
        {
            this.isPrewarmObject = isPrewarmObject;
        }

        public bool IsPrewarmObject() { return isPrewarmObject; }

        public UnityAction OnSpawnFromPool;
        public UnityAction OnReturnToPool;

        public void InvokeOnSpawnFromPoolEvent()
        {
            if (OnSpawnFromPool != null) { OnSpawnFromPool.Invoke(); }
        }
        public void InvokeOnReturnToPoolEvent()
        {
            if (OnReturnToPool != null) { OnReturnToPool.Invoke(); }
        }

        private bool markedForDestruction;
        public void MarkForDestruction() { markedForDestruction = true; }

        private void OnDestroy()
        {
            if (!markedForDestruction) { ObjectPoolingManager.OnPooledObjectDestroy(this); }
            if (parentPooledObject)
            {
                parentPooledObject.childPooledObjects.Remove(this);
            }
        }

        public List<PooledObject> GetChildPooledObjects()
        {
            int nullCount = childPooledObjects.RemoveAll(item => !item);
            if (nullCount > 0) { Debug.LogWarning(nullCount + " null pooled child objects found in list " + this); }
            return childPooledObjects.ToList();
        }

        private List<PooledObject> childPooledObjects = new List<PooledObject>();
        private void OnBeforeTransformParentChanged()
        {
            if (parentPooledObject)
            {
                parentPooledObject.childPooledObjects.Remove(this);
            }
        }

        private PooledObject parentPooledObject;
        private void OnTransformParentChanged()
        {
            parentPooledObject = GetComponentInParent<PooledObject>();
            if (parentPooledObject)
            {
                parentPooledObject.childPooledObjects.Add(this);
            }
        }

        public bool IsSpawned { get { return isSpawned; } }
        private bool isSpawned;

        private void Awake()
        {
            OnSpawnFromPool += OnSpawn;
            OnReturnToPool += OnReturn;
        }

        private void OnSpawn()
        {
            isSpawned = true;
            ObjectPoolingManager.AddSpawnedObjectToActivePool(this);
            parentPooledObject = GetComponentInParent<PooledObject>();
        }

        private void OnReturn()
        {
            ObjectPoolingManager.RemoveSpawnedObjectFromActivePool(this);
            isSpawned = false;
            if (parentPooledObject)
            {
                parentPooledObject.childPooledObjects.Remove(this);
            }
            parentPooledObject = null;
        }

        private void OnEnable()
        {
            gameObject.hideFlags = HideFlags.None;
        }

        private void OnDisable()
        {
            gameObject.hideFlags = ObjectPoolingManager.hideFlagsForSpawnedObjects;
        }
    }
}