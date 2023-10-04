using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;
using LightPat.Core;

public class NetworkStart : MonoBehaviour
{
    [SerializeField] private GameMode gameMode;
    [Header("Formatted: Name|CharacterIndex|SkinIndex")]
    [SerializeField] private string connectionDataString;

    private void Start()
    {
        StartCoroutine(StartNetwork());
    }

    private IEnumerator StartNetwork()
    {
        yield return null;
        yield return new WaitForSeconds(1);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(connectionDataString);

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            NetworkManager.Singleton.StartServer();
        else if (Application.isEditor)
            NetworkManager.Singleton.StartHost();
        else
            NetworkManager.Singleton.StartClient();
    }

    private readonly float _hudRefreshRate = 1f;
    private float _timer;

    private void Update()
    {
        if (NetworkManager.Singleton.IsServer & ClientManager.Singleton.gameMode.Value != gameMode)
        {
            ClientManager.Singleton.gameMode.Value = gameMode;
        }

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
            if (!ClientManager.Singleton)
            {
                foreach (KeyValuePair<ulong, NetworkClient> clientPair in NetworkManager.Singleton.ConnectedClients)
                {
                    if (clientPair.Value.PlayerObject)
                    {
                        if (clientPair.Value.PlayerObject.TryGetComponent(out LightPat.Player.NetworkPlayer networkPlayer))
                        {
                            networkPlayer.roundTripTime.Value = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().GetCurrentRtt(clientPair.Key);
                        }
                    }
                }
            }
        }
    }
}
