using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Vi.Isolated
{
    public class SpawnObjectsOnServerStart : MonoBehaviour
    {
        [SerializeField] private AssetReferenceGameObject[] referencesToSpawn;

        private AsyncOperationHandle<GameObject>[] loadedReferences;

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;

            loadedReferences = new AsyncOperationHandle<GameObject>[referencesToSpawn.Length];

            for (int i = 0; i < referencesToSpawn.Length; i++)
            {
                loadedReferences[i] = referencesToSpawn[i].LoadAssetAsync();
                loadedReferences[i].Completed += OnLoadCompleted;
            }
        }

        private void OnLoadCompleted(AsyncOperationHandle<GameObject> obj)
        {
            NetworkManager.Singleton.AddNetworkPrefab(obj.Result);
        }

        private void OnServerStarted()
        {
            foreach (AsyncOperationHandle<GameObject> handle in loadedReferences)
            {
                StartCoroutine(SpawnObject(handle));
            }
        }

        private IEnumerator SpawnObject(AsyncOperationHandle<GameObject> handle)
        {
            if (!handle.IsDone) { yield return handle; }

            GameObject g = Instantiate(handle.Result);
            g.GetComponent<NetworkObject>().Spawn();
        }
    }
}