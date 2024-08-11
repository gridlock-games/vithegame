using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Player;
using System.IO;
using jomarcentermjm.PlatformAPI;

namespace Vi.UI
{
    public class LobbyManagerNPC : NetworkInteractable
    {
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private HubServerBrowser UI;

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
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.Attributes.IsLocalPlayer) { localPlayerInRange = true; }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.Attributes.IsLocalPlayer) { localPlayerInRange = false; }
            }
        }

        private Vector3 originalScale;
        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;

#if UNITY_SERVER
        // The minimum number of EMPTY lobby instances we want to run at one time
        private const int emptyLobbyServersRequired = 2;
#else
        // The minimum number of EMPTY lobby instances we want to run at one time
        private const int emptyLobbyServersRequired = 1;
#endif

        private void Start()
        {
            originalScale = worldSpaceLabel.transform.localScale;
            worldSpaceLabel.transform.localScale = Vector3.zero;
            UI.gameObject.SetActive(false);
            networkTransport = NetworkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        }

        private const float scalingSpeed = 8;
        private const float rotationSpeed = 15;

        private Camera mainCamera;

        private void FindMainCamera()
        {
            if (mainCamera) { return; }
            mainCamera = Camera.main;
        }

        private void Update()
        {
            FindMainCamera();

            worldSpaceLabel.transform.localScale = Vector3.Lerp(worldSpaceLabel.transform.localScale, localPlayerInRange ? originalScale : Vector3.zero, Time.deltaTime * scalingSpeed);

            if (mainCamera)
            {
                worldSpaceLabel.transform.rotation = Quaternion.Slerp(worldSpaceLabel.transform.rotation, Quaternion.LookRotation(mainCamera.transform.position - worldSpaceLabel.transform.position), Time.deltaTime * rotationSpeed);
            }

            if (IsServer)
            {
                if (!creatingNewLobby & !WebRequestManager.Singleton.IsDeletingServer)
                {
                    WebRequestManager.Server[] emptyServers = System.Array.FindAll(WebRequestManager.Singleton.LobbyServers, item => item.ip == networkTransport.ConnectionData.Address & item.population == 0);

                    if (emptyServers.Length > emptyLobbyServersRequired)
                    {
                        Debug.Log("Deleting server with id " + emptyServers[0]._id.ToString());
                        WebRequestManager.Singleton.DeleteServer(emptyServers[0]._id.ToString());
                    }
                    else if (emptyServers.Length < emptyLobbyServersRequired)
                    {
                        StartCoroutine(CreateNewLobby());
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
                path = @"C:\Users\patse\OneDrive\Desktop\Windows Build\VitheGame.exe";

                #if UNITY_STANDALONE_OSX
                    path = @"/Users/odaleroxas/Documents/Builds/mac/headless/headless/VitheGame";
                #endif
            }
            else
            {
                path = Application.dataPath;
                path = path.Substring(0, path.LastIndexOf('/'));
                path = Path.Join(path, Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer ? "VitheGame.exe" : "VitheGame.x86_64");

                #if UNITY_STANDALONE_OSX
                    path = @"/Users/odaleroxas/Documents/Builds/mac/headless/headless/VitheGame";
                #endif
            }

            System.Diagnostics.Process.Start(path, "-launch-as-lobby-server");
            Debug.Log("Waiting for server count change: " + originalServerCount);
            yield return new WaitUntil(() => System.Array.FindAll(WebRequestManager.Singleton.LobbyServers, item => item.ip == networkTransport.ConnectionData.Address).Length != originalServerCount);
            Debug.Log("Prev server count: " + originalServerCount + " Current server count: " + System.Array.FindAll(WebRequestManager.Singleton.LobbyServers, item => item.ip == networkTransport.ConnectionData.Address).Length);

            creatingNewLobby = false;
        }

        public void HandlePlatformAPI()
        {
            //Rich presence
            if (PlatformRichPresence.instance != null)
            {
                //Change logic here that would handle scenario where the player is host.
                PlatformRichPresence.instance.UpdatePlatformStatus($"At Lobby");
            }
        }
    }
}