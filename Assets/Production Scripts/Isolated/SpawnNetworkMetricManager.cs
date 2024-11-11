using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Vi.Isolated
{
    public class SpawnNetworkMetricManager : MonoBehaviour
    {
        [SerializeField] private AssetReferenceGameObject networkMetricManagerPrefab;

        private AsyncOperationHandle<GameObject> loadedNetworkMetricManager;

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            loadedNetworkMetricManager = networkMetricManagerPrefab.LoadAssetAsync();
            loadedNetworkMetricManager.Completed += LoadedNetworkMetricManager_Completed;
        }

        private void LoadedNetworkMetricManager_Completed(AsyncOperationHandle<GameObject> obj)
        {
            NetworkManager.Singleton.AddNetworkPrefab(obj.Result);
        }

        private void OnServerStarted()
        {
            if (!loadedNetworkMetricManager.IsDone) { return; }
            GameObject g = Instantiate(loadedNetworkMetricManager.Result);
            g.GetComponent<NetworkObject>().Spawn();
        }
    }
}