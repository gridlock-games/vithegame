using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

namespace LightPat.Core
{
    public class LobbyManagerNPCInteractable : MonoBehaviour
    {
        [SerializeField] private string[] serverIPList;
        [SerializeField] private GameObject serverButtonPrefab;
        [SerializeField] private Transform serverButtonParent;
        [SerializeField] private GameObject lobbyManagerUI;

        private Button[] buttons;
        private bool localPlayerInRange;
        private Player.NetworkPlayer localPlayer;

        private bool joinLobbyCalled;
        public void JoinLobbyOnClick()
        {
            if (joinLobbyCalled) { return; }
            joinLobbyCalled = true;

            StartCoroutine(ConnectToLobby());
        }

        private IEnumerator ConnectToLobby()
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Shutdown started");
            Debug.Log(NetworkManager.Singleton.ShutdownInProgress);
            Debug.Log(System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);

            Debug.Log("Shutdown complete");
            Debug.Log(System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
            Debug.Log(targetIP);
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = targetIP;

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Started Client, looking for address: " + NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address);
            }
        }

        private string targetIP;
        public void SetServerIP(string targetIP)
        {
            int buttonIndex = System.Array.IndexOf(serverIPList, targetIP);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i == buttonIndex)
                {
                    buttons[i].interactable = false;
                }
                else
                {
                    buttons[i].interactable = true;
                }
            }

            this.targetIP = targetIP;
        }

        private void Start()
        {
            buttons = new Button[serverIPList.Length];
            for (int i = 0; i < serverIPList.Length; i++)
            {
                string serverIP = serverIPList[i];
                GameObject serverButton = Instantiate(serverButtonPrefab, serverButtonParent);
                serverButton.name = serverIP;
                serverButton.GetComponentInChildren<TextMeshProUGUI>().SetText(serverIP);
                serverButton.GetComponent<Button>().onClick.AddListener(() => SetServerIP(serverIP));
                buttons[i] = serverButton.GetComponent<Button>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (lobbyManagerUI.activeInHierarchy)
                {
                    lobbyManagerUI.SetActive(false);
                    localPlayer.DisableActionsServerRpc(false);
                    localPlayer.cameraMotorInstance.allowOrbitInput = true;
                    Cursor.lockState = CursorLockMode.Locked;
                }
                else if (localPlayerInRange)
                {
                    localPlayer.DisableActionsServerRpc(true);
                    localPlayer.cameraMotorInstance.allowOrbitInput = false;
                    lobbyManagerUI.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.TryGetComponent(out NetworkObject netObj))
            {
                if (netObj.IsLocalPlayer)
                {
                    localPlayer = netObj.GetComponent<Player.NetworkPlayer>();
                    localPlayerInRange = true;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.TryGetComponent(out NetworkObject netObj))
            {
                if (netObj.IsLocalPlayer)
                {
                    lobbyManagerUI.SetActive(false);
                    localPlayer = null;
                    localPlayerInRange = false;
                }
            }
        }
    }
}