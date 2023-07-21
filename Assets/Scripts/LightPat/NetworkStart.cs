using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;

public class NetworkStart : MonoBehaviour
{
    private void Start()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            NetworkManager.Singleton.StartServer();
        else if (Application.isEditor)
            NetworkManager.Singleton.StartServer();
        else
            NetworkManager.Singleton.StartClient();
    }

    private readonly float _hudRefreshRate = 1f;
    private float _timer;

    private void Update()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            // FPS Counter and Ping Display
            if (Time.unscaledTime > _timer)
            {
                int fps = (int)(1f / Time.unscaledDeltaTime);
                Debug.Log(fps + " FPS");
                _timer = Time.unscaledTime + _hudRefreshRate;
            }
        }

        if (NetworkManager.Singleton.IsServer)
        {
            if (!LightPat.Core.ClientManager.Singleton)
            {
                foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
                {
                    if (clientPair.Value.PlayerObject)
                        clientPair.Value.PlayerObject.GetComponent<LightPat.Player.NetworkPlayer>().roundTripTime.Value = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().GetCurrentRtt(clientPair.Key);
                }
            }
        }
    }
}
