using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System.IO;
using Unity.Collections;
using System.Net;

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
                //string filename = "IP Config.txt";
                //string path = Path.Join(Application.dataPath, filename);
                //// This text is added only once to the file
                //if (!File.Exists(path))
                //{
                //    // Create a file to write to.
                //    using (StreamWriter sw = File.CreateText(path))
                //    {
                //        sw.WriteLine("# Please write a name for a server followed by an IP on each line of this text document like so: (Desktop | 192.168.50.150)");
                //        sw.WriteLine("# Lines that start with hashtag (#) will be ignored");
                //        sw.WriteLine("# DO NOT LEAVE WHITESPACE");
                //    }
                //}

                //using (StreamReader sr = File.OpenText(path))
                //{
                //    string s = "";
                //    List<FixedString32Bytes> IPListCache = new List<FixedString32Bytes>();
                //    while ((s = sr.ReadLine()) != null)
                //    {
                //        if (s[0] == '#') continue;
                //        IPListCache.Add(s);
                //    }

                //    // Sync IP List if the file has changed
                //    if (IPListCache.Count != IPList.Count)
                //    {
                //        IPList.Clear();
                //        foreach (FixedString32Bytes fixedString in IPListCache)
                //        {
                //            IPList.Add(fixedString);
                //        }
                //    }
                //    else // If the lengths are not the same, look for a mismatched value
                //    {
                //        bool mismatchedValue = false;
                //        for (int i = 0; i < IPListCache.Count; i++)
                //        {
                //            if (IPList[i] != IPListCache[i]) { mismatchedValue = true; break; }
                //        }

                //        if (mismatchedValue)
                //        {
                //            IPList.Clear();
                //            foreach (FixedString32Bytes fixedString in IPListCache)
                //            {
                //                IPList.Add(fixedString);
                //            }
                //        }
                //    }
                //}

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://us-central1-vithegame.cloudfunctions.net/api/servers/duels");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                List<Server> serverList = new List<Server>();

                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    string json = sr.ReadToEnd();
                    foreach (string jsonSplit in json.Split("},"))
                    {
                        string finalJsonElement;
                        if (jsonSplit[0] == '[')
                        {
                            finalJsonElement = jsonSplit.Remove(0, 1) + "}";
                        }
                        else if (jsonSplit[^1] == ']')
                        {
                            finalJsonElement = jsonSplit.Remove(jsonSplit.Length - 1, 1);
                        }
                        else
                        {
                            finalJsonElement = jsonSplit + "}";
                        }

                        serverList.Add(JsonUtility.FromJson<Server>(finalJsonElement));
                    }
                }

                foreach (Server server in serverList)
                {
                    if (!serversProcessed.Contains(server))
                    {
                        serversProcessed.Add(server);
                        StartCoroutine(WaitForPingToComplete(server));
                    }
                }
            }
        }

        List<Server> serversProcessed = new List<Server>();

        private IEnumerator WaitForPingToComplete(Server server)
        {
            Ping ping = new Ping(server.ip);
            yield return new WaitUntil(() => ping.isDone);

            string serverString = server.label + "|" + server.ip;
            if (!IPList.Contains(serverString))
                IPList.Add(serverString);
        }

        private struct Server
        {
            public string _id;
            public int type;
            public int population;
            public int progress;
            public string ip;
            public string label;
            public string __v;
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