using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;
using Vi.Core;

namespace Vi.Networking
{
    public class NetworkStart : MonoBehaviour
    {
        [SerializeField] private bool startServerInsteadOfHost;
        [Header("Player Name|CharacterIndex|SkinIndex")]
        [SerializeField] private string payloadString;

        void Start()
        {
            StartCoroutine(StartNetworkAfter1Frame());
        }

        private IEnumerator StartNetworkAfter1Frame()
        {
            yield return null;

            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(payloadString);

            //if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            //    NetworkManager.Singleton.StartServer();
            if (Application.isEditor)
            {
                if (startServerInsteadOfHost)
                    NetworkManager.Singleton.StartServer();
                else
                    NetworkManager.Singleton.StartHost();
            }
            else
            {
                NetworkManager.Singleton.StartClient();
            }
        }
    }
}