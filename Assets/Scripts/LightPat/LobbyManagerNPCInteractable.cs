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
            Debug.Log("Shutting down NetworkManager");
            NetworkManager.Singleton.Shutdown();
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            Debug.Log("Shutdown complete");
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = targetIP;
            Debug.Log("Switching to lobby scene: " + NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address + " " + System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
            // Change the scene locally, then connect to the target IP
            ClientManager.Singleton.ChangeLocalSceneThenStartClient("Lobby");
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