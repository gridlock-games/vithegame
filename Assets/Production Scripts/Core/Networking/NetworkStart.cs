using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;

namespace Vi.Networking
{
    public class NetworkStart : MonoBehaviour
    {
        void Start()
        {
            GetComponent<NetworkManager>().StartHost();

            //if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            //    NetworkManager.Singleton.StartServer();
            if (Application.isEditor)
                NetworkManager.Singleton.StartHost();
            else
                NetworkManager.Singleton.StartClient();
        }
    }
}