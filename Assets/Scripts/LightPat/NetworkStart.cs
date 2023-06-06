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
}
