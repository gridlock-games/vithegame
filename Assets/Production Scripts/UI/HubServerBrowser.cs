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
        [SerializeField] private Button connectButton;
        [SerializeField] private Button closeServersMenuButton;
        [SerializeField] private Button refreshServersButton;

        List<ServerListElement> serverListElementList = new List<ServerListElement>();
        private void Update()
        {
            if (!WebRequestManager.Singleton.IsRefreshingServers)
            {
                foreach (WebRequestManager.Server server in WebRequestManager.Singleton.Servers)
                {
                    if (server.type == 0) { continue; } // Skip other hub servers

                    if (!serverListElementList.Find(item => item.Server._id == server._id))
                    {
                        ServerListElement serverListElementInstance = Instantiate(serverListElement.gameObject, serverListElementParent).GetComponent<ServerListElement>();
                        serverListElementInstance.Initialize(this, server);
                        serverListElementList.Add(serverListElementInstance);
                    }
                }
            }

            serverListElementList = serverListElementList.OrderBy(item => item.pingTime).ToList();
            for (int i = 0; i < serverListElementList.Count; i++)
            {
                serverListElementList[i].gameObject.SetActive(serverListElementList[i].pingTime >= 0);
                serverListElementList[i].transform.SetSiblingIndex(i);
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
        }

        public void ConnectToLobbyServer()
        {
            connectButton.interactable = false;
            closeServersMenuButton.interactable = false;
            refreshServersButton.interactable = false;
            StartCoroutine(ConnectToLobbyServerCoroutine());
        }

        private IEnumerator ConnectToLobbyServerCoroutine()
        {
            NetworkManager.Singleton.Shutdown();
            yield return new WaitUntil(() => NetworkManager.Singleton.ShutdownInProgress);
            var unityTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            //unityTransport.ConnectionData.Address = "";
            //unityTransport.ConnectionData.Port = 7777;
            //NetworkManager.Singleton.StartClient();
            Debug.Log("TODO Connect to lobby server");
        }

        public void CloseServerBrowser()
        {

        }
    }
}