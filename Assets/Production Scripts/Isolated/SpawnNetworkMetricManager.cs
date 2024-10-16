using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Isolated
{
    public class SpawnNetworkMetricManager : MonoBehaviour
    {
        [SerializeField] private NetworkObject networkMetricManagerPrefab;

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }

        private void OnServerStarted()
        {
            GameObject g = Instantiate(networkMetricManagerPrefab.gameObject);
            g.GetComponent<NetworkObject>().Spawn();
        }
    }
}