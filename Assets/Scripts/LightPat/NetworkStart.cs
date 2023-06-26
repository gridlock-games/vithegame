using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkStart : MonoBehaviour
{
    private void Start()
    {
        if (Application.isEditor)
            NetworkManager.Singleton.StartServer();
        else
            NetworkManager.Singleton.StartClient();
    }

    private void Update()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (!LightPat.Core.ClientManager.Singleton)
            {
                foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
                {
                    if (clientPair.Value.PlayerObject)
                        clientPair.Value.PlayerObject.GetComponent<LightPat.Player.NetworkPlayer>().roundTripTime.Value = NetworkManager.Singleton.GetComponent<NetworkTransport>().GetCurrentRtt(clientPair.Key);
                }
            }
        }
    }
}
