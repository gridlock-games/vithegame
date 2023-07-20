using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System.IO;
using Unity.Collections;
using System.Net;
using static LightPat.Core.ClientManager;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

namespace LightPat.Core
{
    public class LobbyManagerNPCInteractable : NetworkBehaviour
    {
        [SerializeField] private GameObject serverButtonPrefab;
        [SerializeField] private Transform serverButtonParent;
        [SerializeField] private GameObject lobbyManagerUI;

        private NetworkList<FixedString32Bytes> IPList;

        private Button[] buttons;
        private bool localPlayerInRange;
        private Player.NetworkPlayer localPlayer;

        public override void OnNetworkSpawn()
        {
            IPList.OnListChanged += OnIPListChange;
            SyncUIWithList();
        }

        public override void OnNetworkDespawn()
        {
            IPList.OnListChanged -= OnIPListChange;
        }

        private void OnIPListChange(NetworkListEvent<FixedString32Bytes> changeEvent) { SyncUIWithList(); }

        private void SyncUIWithList()
        {
            foreach (Transform child in serverButtonParent)
            {
                Destroy(child.gameObject);
            }

            buttons = new Button[IPList.Count];
            for (int i = 0; i < IPList.Count; i++)
            {
                string serverIP = IPList[i].ToString();
                GameObject serverButton = Instantiate(serverButtonPrefab, serverButtonParent);
                serverButton.name = serverIP;
                serverButton.GetComponentInChildren<TextMeshProUGUI>().SetText(serverIP);
                serverButton.GetComponent<Button>().onClick.AddListener(() => SetServerIP(serverIP));
                buttons[i] = serverButton.GetComponent<Button>();
            }

            targetIP = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = true;
            }
        }

        private bool joinLobbyCalled;
        public void JoinLobbyOnClick()
        {
            if (targetIP == null) { Debug.Log("No target IP specified"); return; }
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
            var networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

            ClientManager.Singleton.playerHubIP = networkTransport.ConnectionData.Address;
            networkTransport.ConnectionData.Address = targetIP;
            Debug.Log("Switching to lobby scene: " + networkTransport.ConnectionData.Address + " " + System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
            // Change the scene locally, then connect to the target IP
            ClientManager.Singleton.ChangeLocalSceneThenStartClient("Lobby");
        }

        private string targetIP;
        private void SetServerIP(string targetIP)
        {
            int buttonIndex = IPList.IndexOf(targetIP);
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

            this.targetIP = targetIP.Split('|')[1].Trim();
        }

        private void Awake()
        {
            IPList = new NetworkList<FixedString32Bytes>();
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
                    SyncUIWithList();
                }
                else if (localPlayerInRange)
                {
                    localPlayer.DisableActionsServerRpc(true);
                    localPlayer.cameraMotorInstance.allowOrbitInput = false;
                    lobbyManagerUI.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                }
            }

            if (IsServer)
            {
                if (!refreshServerListRunning) { StartCoroutine(RefreshServerList()); }
            }
        }

        private bool refreshServerListRunning;
        private IEnumerator RefreshServerList()
        {
            refreshServerListRunning = true;

            // Get list of servers in the API
            string endpointURL = "https://us-central1-vithegame.cloudfunctions.net/api/servers/duels";

            UnityWebRequest getRequest = UnityWebRequest.Get(endpointURL);

            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(getRequest.error);
            }

            List<Server> serverList = new List<Server>();

            string json = getRequest.downloadHandler.text;

            foreach (string jsonSplit in json.Split("},"))
            {
                string finalJsonElement = jsonSplit;
                if (finalJsonElement[0] == '[')
                {
                    finalJsonElement = finalJsonElement.Remove(0, 1);
                }

                if (finalJsonElement[^1] == ']')
                {
                    finalJsonElement = finalJsonElement.Remove(finalJsonElement.Length - 1, 1);
                }

                if (finalJsonElement[^1] != '}')
                {
                    finalJsonElement += "}";
                }

                serverList.Add(JsonUtility.FromJson<Server>(finalJsonElement));
            }

            // Process servers here
            foreach (Server server in serverList)
            {
                if (server.type != 0) { continue; }

                string serverString = server.label + "|" + server.ip;
                if (!serversProcessed.Contains(server) & !IPList.Contains(serverString))
                {
                    serversProcessed.Add(server);
                    StartCoroutine(WaitForPingToComplete(server));
                }
            }

            // If we have the server in the IPList but not in the API
            List<FixedString32Bytes> stringsToRemove = new List<FixedString32Bytes>();
            foreach (FixedString32Bytes serverString in IPList)
            {
                bool serverStringInAPI = false;

                foreach (Server server in serverList)
                {
                    string APIServerString = server.label + "|" + server.ip;

                    if (APIServerString == serverString)
                    {
                        serverStringInAPI = true;
                        break;
                    }
                }

                if (!serverStringInAPI)
                {
                    stringsToRemove.Add(serverString);
                }
            }

            // Remove strings that are not in the API but are in the IPList
            foreach (FixedString32Bytes serverString in stringsToRemove)
            {
                IPList.Remove(serverString);
            }

            refreshServerListRunning = false;
        }

        private List<Server> serversProcessed = new List<Server>();

        private IEnumerator WaitForPingToComplete(Server server)
        {
            Ping ping = new Ping(server.ip);
            yield return new WaitUntil(() => ping.isDone);

            string serverString = server.label + "|" + server.ip;
            if (!IPList.Contains(serverString))
            {
                IPList.Add(serverString);
                serversProcessed.Remove(server);
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