using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

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
        }

        public List<PooledObject> ChildPooledObjects { get; private set; } = new List<PooledObject>();
        private void OnBeforeTransformParentChanged()
        {
            PooledObject parentPooledObject = GetComponentInParent<PooledObject>();
            if (parentPooledObject)
            {
                parentPooledObject.ChildPooledObjects.Remove(this);
            }
        }

        private void OnTransformParentChanged()
        {
            PooledObject parentPooledObject = GetComponentInParent<PooledObject>();
            if (parentPooledObject)
            {
                parentPooledObject.ChildPooledObjects.Add(this);
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
        }

        private void OnReturn()
        {
            ObjectPoolingManager.RemoveSpawnedObjectFromActivePool(this);
            isSpawned = false;
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