using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Vi.Core;
using Unity.Netcode;

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

        List<ServerListElement> serverListElementList = new List<ServerListElement>();
        List<WebRequestManager.Server> emptyLobbyServerList = new List<WebRequestManager.Server>();
        private float lastErrorTextDisplayTime = Mathf.NegativeInfinity;
        private const float errorTextDisplayDuration = 5;
        private void Update()
        {
            joinLobbyButton.interactable = serverListElementList.Exists(item => item.Server.ip == networkTransport.ConnectionData.Address & ushort.Parse(item.Server.port) == networkTransport.ConnectionData.Port);
            
            if (!WebRequestManager.Singleton.IsRefreshingServers)
            {
                foreach (WebRequestManager.Server server in WebRequestManager.Singleton.LobbyServers)
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
                serverListElementList[i].gameObject.SetActive(serverListElementList[i].pingTime >= 0);
                serverListElementList[i].transform.SetSiblingIndex(i);
            }

            if (Time.time - lastErrorTextDisplayTime > errorTextDisplayDuration)
            {
                errorText.text = "";
            }
        }

        public void RefreshServerBrowser()
        {
            WebRequestManager.Singleton.RefreshServers();
            foreach (ServerListElement serverListElement in serverListElementList)
            {
                Destroy(serverListElement.gameObject);
            }
            serverListElementList.Clear();
            emptyLobbyServerList.Clear();
        }

        public void ConnectToLobbyServer()
        {
            NetSceneManager.Singleton.StartCoroutine(ConnectToLobbyServerCoroutine());
        }

        public void CreateNewLobbyRoom()
        {
            if (emptyLobbyServerList.Count > 0)
            {
                WebRequestManager.Server server = emptyLobbyServerList[0];
                networkTransport.ConnectionData.Address = server.ip;
                networkTransport.ConnectionData.Port = ushort.Parse(server.port);
                NetSceneManager.Singleton.StartCoroutine(ConnectToLobbyServerCoroutine());
            }
            else
            {
                errorText.text = "Please Refresh and Try Again.";
                lastErrorTextDisplayTime = Time.time;
            }
        }

        private IEnumerator ConnectToLobbyServerCoroutine()
        {
            NetworkManager.Singleton.Shutdown(true);
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            NetworkManager.Singleton.StartClient();
        }
    }
}