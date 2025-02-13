using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Vi.Core;
using Unity.Netcode;
using Vi.Utility;

namespace Vi.UI
{
    public class HubServerBrowser : MonoBehaviour
    {
        [SerializeField] private ServerListElement serverListElement;
        [SerializeField] private Transform serverListElementParent;
        [SerializeField] private Button closeServersMenuButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button refreshServersButton;
        [SerializeField] private Text errorText;

        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;
        private void Start()
        {
            networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            errorText.text = "";
        }

        private void OnEnable()
        {
            RefreshServerBrowser();
        }

        List<ServerListElement> serverListElementList = new List<ServerListElement>();
        List<ServerManager.Server> emptyLobbyServerList = new List<ServerManager.Server>();
        private float lastErrorTextDisplayTime = Mathf.NegativeInfinity;
        private const float errorTextDisplayDuration = 5;
        private void Update()
        {
            refreshServersButton.interactable = !WebRequestManager.Singleton.ServerManager.IsRefreshingServers;
            createLobbyButton.interactable = refreshServersButton.interactable;
            joinLobbyButton.interactable = refreshServersButton.interactable & serverListElementList.Exists(item => item.Server.ip == networkTransport.ConnectionData.Address & ushort.Parse(item.Server.port) == networkTransport.ConnectionData.Port);
            
            if (!WebRequestManager.Singleton.ServerManager.IsRefreshingServers)
            {
                foreach (ServerManager.Server server in WebRequestManager.Singleton.ServerManager.LobbyServers)
                {
                    if (server.ip != networkTransport.ConnectionData.Address) { continue; }

                    if (!serverListElementList.Find(item => item.Server._id == server._id))
                    {
                        if (server.population == 0)
                        {
                            if (!emptyLobbyServerList.Contains(server)) { emptyLobbyServerList.Add(server); }
                        }
                        else
                        {
                            ServerListElement serverListElementInstance = Instantiate(serverListElement.gameObject, serverListElementParent).GetComponent<ServerListElement>();
                            serverListElementInstance.Initialize(this, server);
                            serverListElementList.Add(serverListElementInstance);
                        }
                    }
                }
            }

            serverListElementList = serverListElementList.OrderBy(item => item.pingTime).ToList();
            for (int i = 0; i < serverListElementList.Count; i++)
            {
                serverListElementList[i].gameObject.SetActive(true);
                serverListElementList[i].transform.SetSiblingIndex(i);
            }

            if (Time.time - lastErrorTextDisplayTime > errorTextDisplayDuration)
            {
                errorText.text = "";
            }
        }

        public void RefreshServerBrowser()
        {
            WebRequestManager.Singleton.ServerManager.RefreshServers();
            foreach (ServerListElement serverListElement in serverListElementList)
            {
                Destroy(serverListElement.gameObject);
            }
            serverListElementList.Clear();
            emptyLobbyServerList.Clear();
        }

        public void ConnectToLobbyServer()
        {
            PersistentLocalObjects.Singleton.StartCoroutine(ConnectToLobbyServerCoroutine());
        }

        public void CreateNewLobbyRoom()
        {
            if (emptyLobbyServerList.Count > 0)
            {
                ServerManager.Server server = emptyLobbyServerList[0];
                networkTransport.SetConnectionData(server.ip, ushort.Parse(server.port), FasterPlayerPrefs.serverListenAddress);
                PersistentLocalObjects.Singleton.StartCoroutine(ConnectToLobbyServerCoroutine());
            }
            else
            {
                errorText.text = "Please Refresh and Try Again.";
                lastErrorTextDisplayTime = Time.time;
            }
        }

        private IEnumerator ConnectToLobbyServerCoroutine()
        {
            NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown);
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            yield return new WaitUntil(() => !NetSceneManager.IsBusyLoadingScenes());
            NetworkManager.Singleton.StartClient();
        }
    }
}