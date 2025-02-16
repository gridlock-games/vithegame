using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using Unity.Netcode;

namespace Vi.Utility
{
    [DisallowMultipleComponent]
    public class PooledObject : MonoBehaviour
    {
        [SerializeField] private int numberOfObjectsToPool = 5;

        public int GetNumberOfObjectsToPool() { return numberOfObjectsToPool; }

        [SerializeField] private int pooledObjectIndex = -1;
        public int GetPooledObjectIndex() { return pooledObjectIndex; }

        public void SetPooledObjectIndex(int index)
        {
            if (index != pooledObjectIndex)
            {
                Debug.Log("Setting pooled object index: " + index + " prev: " + pooledObjectIndex + " " + this);
                pooledObjectIndex = index;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
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

        public bool IsSpawned { get; private set; }

        private void Awake()
        {
            OnSpawnFromPool += OnSpawn;
            OnReturnToPool += OnReturn;
        }

        private void OnSpawn()
        {
            IsSpawned = true;
            ObjectPoolingManager.AddSpawnedObjectToActivePool(this);
            gameObject.hideFlags = HideFlags.None;
        }

        private void OnReturn()
        {
            ObjectPoolingManager.RemoveSpawnedObjectFromActivePool(this);
            IsSpawned = false;
            gameObject.hideFlags = HideFlags.HideInHierarchy;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) { return; }

            if (TryGetComponent(out NetworkObject networkObject))
            {
                if (networkObject.AutoObjectParentSync | networkObject.SceneMigrationSynchronization | networkObject.ActiveSceneSynchronization)
                {
                    networkObject.SceneMigrationSynchronization = false;
                    networkObject.ActiveSceneSynchronization = false;
                    networkObject.AutoObjectParentSync = false;
                    UnityEditor.EditorUtility.SetDirty(networkObject);
                }
            }

            //PooledObjectList pooledObjectList = UnityEditor.AssetDatabase.LoadAssetAtPath<PooledObjectList>(@"Assets\Production\PooledObjectList.asset");
            //var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            //if (prefabStage)
            //{
            //    if (!pooledObjectList.GetPooledObjectReferences().Exists(item => item.AssetGUID == UnityEditor.AssetDatabase.GUIDFromAssetPath(prefabStage.assetPath).ToString()))
            //    {
            //        SetPooledObjectIndex(-1);
            //    }
            //}
        }
#endif
    }
}