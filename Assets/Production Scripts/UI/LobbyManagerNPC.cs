using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Player;
using System.IO;

namespace Vi.UI
{
    public class LobbyManagerNPC : NetworkInteractable
    {
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private HubServerBrowser UI;

        // The minimum number of lobby instances we want to run at one time
        private const int minimumLobbyServersRequired = 1;
        // The minimum number of EMPTY lobby instances we want to run at one time
        private const int emptyLobbyServersRequired = 1;

        private GameObject invoker;
        public override void Interact(GameObject invoker)
        {
            this.invoker = invoker;
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(this);
            UI.gameObject.SetActive(true);
        }

        private void OnPause()
        {
            CloseServerBrowser();
        }

        public void CloseServerBrowser()
        {
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(null);
            invoker = null;
            UI.gameObject.SetActive(false);
        }

        private bool localPlayerInRange;
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root.TryGetComponent(out Attributes attributes))
            {
                if (attributes.IsLocalPlayer) { localPlayerInRange = true; }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.root.TryGetComponent(out Attributes attributes))
            {
                if (attributes.IsLocalPlayer) { localPlayerInRange = false; }
            }
        }

        private Vector3 originalScale;
        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;
        private void Start()
        {
            originalScale = worldSpaceLabel.transform.localScale;
            worldSpaceLabel.transform.localScale = Vector3.zero;
            UI.gameObject.SetActive(false);
            networkTransport = NetworkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        }

        private const float scalingSpeed = 8;
        private const float rotationSpeed = 15;
        private void Update()
        {
            worldSpaceLabel.transform.localScale = Vector3.Lerp(worldSpaceLabel.transform.localScale, localPlayerInRange ? originalScale : Vector3.zero, Time.deltaTime * scalingSpeed);

            if (Camera.main)
            {
                worldSpaceLabel.transform.rotation = Quaternion.Slerp(worldSpaceLabel.transform.rotation, Quaternion.LookRotation(Camera.main.transform.position - worldSpaceLabel.transform.position), Time.deltaTime * rotationSpeed);
            }

            if (IsServer)
            {
                
                List<WebRequestManager.Server> emptyServerList = new List<WebRequestManager.Server>();
                WebRequestManager.Server[] lobbyServers = System.Array.FindAll(WebRequestManager.Singleton.LobbyServers, item => item.ip == networkTransport.ConnectionData.Address);
                foreach (WebRequestManager.Server server in lobbyServers)
                {
                    if (server.ip != networkTransport.ConnectionData.Address) { continue; }

                    if (server.population == 0)
                        emptyServerList.Add(server);
                }

                if (emptyServerList.Count < emptyLobbyServersRequired | lobbyServers.Length < minimumLobbyServersRequired)
                {
                    if (!creatingNewLobby)
                    {
                        StartCoroutine(CreateNewLobby());
                    }
                }
                else if (emptyServerList.Count > emptyLobbyServersRequired & lobbyServers.Length > minimumLobbyServersRequired)
                {
                    if (emptyServerList.Count > 0 & !WebRequestManager.Singleton.IsDeletingServer)
                    {
                        WebRequestManager.Singleton.DeleteServer(emptyServerList[0]._id.ToString());
                    }
                }
            }
        }

        private bool creatingNewLobby;
        private IEnumerator CreateNewLobby()
        {
            creatingNewLobby = true;

            int originalServerCount = System.Array.FindAll(WebRequestManager.Singleton.LobbyServers, item => item.ip == networkTransport.ConnectionData.Address).Length;

            string path = "";
            if (Application.isEditor)
            {
                path = @"C:\Users\patse\OneDrive\Desktop\Build\VitheGame.exe";
            }
            else
            {
                path = Application.dataPath;
                path = path.Substring(0, path.LastIndexOf('/'));
                path = Path.Join(path, Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer ? "VitheGame.exe" : "VitheGame.x86_64");
            }

            System.Diagnostics.Process.Start(path, "-launch-as-lobby-server");
            Debug.Log("Waiting for server count change: " + originalServerCount);
            yield return new WaitUntil(() => System.Array.FindAll(WebRequestManager.Singleton.LobbyServers, item => item.ip == networkTransport.ConnectionData.Address).Length != originalServerCount);
            Debug.Log("Prev server count: " + originalServerCount + " Current server count: " + WebRequestManager.Singleton.LobbyServers.Length);

            creatingNewLobby = false;
        }
    }
}