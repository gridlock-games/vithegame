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
            //if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            //    NetworkManager.Singleton.StartServer();
            if (Application.isEditor)
                NetworkManager.Singleton.StartClient();
            else
                NetworkManager.Singleton.StartHost();
        }
    }
}