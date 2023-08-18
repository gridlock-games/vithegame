using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using Unity.Collections;
using UnityEngine.Networking;

namespace LightPat.Core
{
    public class LobbyManagerNPCInteractable : NetworkBehaviour
    {
        [SerializeField] private GameObject serverButtonPrefab;
        [SerializeField] private Transform serverButtonParent;
        [SerializeField] private GameObject lobbyManagerUI;

        private List<Server> serverList = new List<Server>();

        private Button[] buttons;
        private bool localPlayerInRange;
        private Player.NetworkPlayer localPlayer;

        private struct Server : INetworkSerializable, System.IEquatable<Server>
        {
            public FixedString32Bytes _id;
            public int type;
            public int population;
            public int progress;
            public FixedString32Bytes ip;
            public FixedString32Bytes label;
            public FixedString32Bytes __v;
            public FixedString32Bytes port;

            public Server(string _id, int type, int population, int progress, string ip, string label, string __v, string port)
            {
                this._id = _id;
                this.type = type;
                this.population = population;
                this.progress = progress;
                this.ip = ip;
                this.label = label;
                this.__v = __v;
                this.port = port;
            }

            public bool Equals(Server other)
            {
                return _id == other._id & type == other.type & population == other.population & progress == other.progress & ip == other.ip & label == other.label & __v == other.__v;
            }

            public bool Equals(ClientManager.Server other)
            {
                return _id == other._id & type == other.type & population == other.population & progress == other.progress & ip == other.ip & label == other.label & __v == other.__v;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref _id);
                serializer.SerializeValue(ref type);
                serializer.SerializeValue(ref population);
                serializer.SerializeValue(ref progress);
                serializer.SerializeValue(ref ip);
                serializer.SerializeValue(ref label);
                serializer.SerializeValue(ref __v);
                serializer.SerializeValue(ref port);
            }
        }

        private void SyncUIWithList()
        {
            foreach (Transform child in serverButtonParent)
            {
                Destroy(child.gameObject);
            }

            buttons = new Button[serverList.Count];
            for (int i = 0; i < serverList.Count; i++)
            {
                Server server = serverList[i];
                GameObject serverElement = Instantiate(serverButtonPrefab, serverButtonParent);
                serverElement.name = server.label.ToString();

                serverElement.transform.Find("Button").GetComponentInChildren<TextMeshProUGUI>().SetText(server.label + " | " + server.ip.ToString());
                serverElement.transform.Find("Population").GetComponent<TextMeshProUGUI>().SetText("Player count: " + server.population.ToString());
                serverElement.transform.Find("Status").GetComponent<TextMeshProUGUI>().SetText("Status: " + (server.progress == 0 ? "Waiting for players" : "In Progress"));

                serverElement.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() => SetServerIP(server));
                buttons[i] = serverElement.transform.Find("Button").GetComponent<Button>();
            }

            targetIP = null;
            targetPort = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = true;
            }
        }

        private bool joinLobbyCalled;
        public void JoinLobbyOnClick()
        {
            if (targetIP == null) { Debug.Log("No target IP specified"); return; }
            if (targetPort == null) { Debug.Log("No target port specified"); return; }
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
            networkTransport.ConnectionData.Address = targetIP;
            networkTransport.ConnectionData.Port = (ushort)int.Parse(targetPort);
            Debug.Log("Starting client: " + networkTransport.ConnectionData.Address + " " + System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData));
            // Change the scene locally, then connect to the target IP
            NetworkManager.Singleton.StartClient();
        }

        private string targetIP;
        private string targetPort;
        private void SetServerIP(Server server)
        {
            int buttonIndex = serverList.IndexOf(server);
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

            targetIP = server.ip.ToString();
            targetPort = server.port.ToString();
            Debug.Log(targetIP + " " + targetPort);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (lobbyManagerUI.activeInHierarchy)
                {
                    lobbyManagerUI.SetActive(false);
                    localPlayer.DisableActionsServerRpc(false);
                    localPlayer.externalUIOpen = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
                else if (localPlayerInRange)
                {
                    localPlayer.DisableActionsServerRpc(true);
                    localPlayer.externalUIOpen = true;
                    lobbyManagerUI.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                }
            }

            if (!refreshServerListRunning) { StartCoroutine(RefreshServerList()); }
        }

        private bool refreshServerListRunning;
        private IEnumerator RefreshServerList()
        {
            refreshServerListRunning = true;

            // Get list of servers in the API
            UnityWebRequest getRequest = UnityWebRequest.Get(ClientManager.serverEndPointURL);

            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Get Request Error in LobbyManagerNPCInteractable.RefreshServerList() " + getRequest.error);
            }

            List<ClientManager.Server> APIServerList = new List<ClientManager.Server>();

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

                APIServerList.Add(JsonUtility.FromJson<ClientManager.Server>(finalJsonElement));
            }

            // Process servers here
            foreach (ClientManager.Server APIServer in APIServerList)
            {
                if (APIServer.type != 0) { continue; }

                Server server = new Server(APIServer._id, APIServer.type, APIServer.population, APIServer.progress, APIServer.ip, APIServer.label, APIServer.__v, APIServer.port);
                if (!serverList.Contains(server))
                {
                    Debug.Log("Adding " + server.label + " " + server.ip + " Population: " + server.population + " Progress: " + server.progress + " Port: " + server.port);
                    serverList.Add(server);
                    SyncUIWithList();
                }
            }

            // If we have the server in the network list but not in the API
            List<Server> serversToRemove = new List<Server>();
            foreach (Server loadedServer in this.serverList)
            {
                bool serverInAPI = false;

                foreach (ClientManager.Server APIServer in APIServerList)
                {
                    if (loadedServer.Equals(APIServer))
                    {
                        serverInAPI = true;
                        break;
                    }
                }

                if (!serverInAPI)
                {
                    serversToRemove.Add(loadedServer);
                }
            }

            // Remove servers that are not in the API but are in the ServerList
            foreach (Server server in serversToRemove)
            {
                Debug.Log("Removing " + server.label + " " + server.ip + " Population: " + server.population + " Progress: " + server.progress + " Port: " + server.port);
                serverList.Remove(server);
                SyncUIWithList();
            }

            refreshServerListRunning = false;
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