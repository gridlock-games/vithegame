using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Player;
using System.IO;
using UnityEngine.Video;
using Vi.Utility;

namespace Vi.UI
{
    public class LobbyManagerNPC : NetworkInteractable, ExternalUI
    {
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private HubServerBrowser UI;
        [SerializeField] private GameObject gameModePreviewUI;
        [SerializeField] private VideoPlayer videoPlayer;

        private GameObject invoker;

        public override void Interact(GameObject invoker)
        {
            this.invoker = invoker;
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(this);
            gameModePreviewUI.gameObject.SetActive(true);
            videoPlayer.Play();
        }

        public void ShowServerBrowser()
        {
            gameModePreviewUI.gameObject.SetActive(false);
            videoPlayer.Pause();
            UI.gameObject.SetActive(true);
        }

        public void OnPause()
        {
            CloseServerBrowser();
        }

        public void CloseServerBrowser()
        {
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(null);
            invoker = null;
            UI.gameObject.SetActive(false);
            gameModePreviewUI.gameObject.SetActive(false);
            videoPlayer.Stop();
        }

        private bool localPlayerInRange;

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent.IsLocalPlayer)
                {
                    localPlayerInRange = true;
                    networkCollider.MovementHandler.SetInteractableInRange(this, true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent.IsLocalPlayer)
                {
                    localPlayerInRange = false;
                    networkCollider.MovementHandler.SetInteractableInRange(this, false);
                }
            }
        }

        private Vector3 originalScale;
        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;

        // The minimum number of EMPTY lobby instances we want to run at one time
#if UNITY_EDITOR
        private const int emptyLobbyServersRequired = 0;
#elif UNITY_SERVER
        private const int emptyLobbyServersRequired = 2;
#else
        private const int emptyLobbyServersRequired = 1;
#endif

        private void Start()
        {
            originalScale = worldSpaceLabel.transform.localScale;
            worldSpaceLabel.transform.localScale = Vector3.zero;
            UI.gameObject.SetActive(false);
            gameModePreviewUI.gameObject.SetActive(false);
            networkTransport = NetworkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

            currentLobbyCount = LobbyServers.Length;

            videoPlayer.Prepare();
        }

        private ServerManager.Server[] LobbyServers { get { return System.Array.FindAll(WebRequestManager.Singleton.ServerManager.LobbyServers, item => item.ip == networkTransport.ConnectionData.Address); } }

        private const float scalingSpeed = 8;
        private const float rotationSpeed = 15;

        private int currentLobbyCount;
        private float lastLobbyCreationTime = Mathf.NegativeInfinity;

        private List<string> currentlyDeletingServers = new List<string>();

        private void Update()
        {
            worldSpaceLabel.transform.localScale = Vector3.Lerp(worldSpaceLabel.transform.localScale, localPlayerInRange ? originalScale : Vector3.zero, Time.deltaTime * scalingSpeed);

            if (FindMainCamera.MainCamera)
            {
                worldSpaceLabel.transform.rotation = Quaternion.Slerp(worldSpaceLabel.transform.rotation, Quaternion.LookRotation(FindMainCamera.MainCamera.transform.position - worldSpaceLabel.transform.position), Time.deltaTime * rotationSpeed);
            }

            if (IsServer)
            {
                currentlyDeletingServers.RemoveAll(item => !System.Array.Exists(LobbyServers, server => server._id == item));

                ServerManager.Server[] emptyServers = System.Array.FindAll(LobbyServers, item => item.population == 0 & !currentlyDeletingServers.Contains(item._id));
                ServerManager.Server[] notEmptyServers = System.Array.FindAll(LobbyServers, item => item.population != 0 & !currentlyDeletingServers.Contains(item._id));

                if (emptyServers.Length > emptyLobbyServersRequired)
                {
                    Debug.Log("Deleting server with id " + emptyServers[0]._id + " prev count: " + currentLobbyCount);
                    WebRequestManager.Singleton.ServerManager.DeleteServer(emptyServers[0]._id);
                    currentlyDeletingServers.Add(emptyServers[0]._id);
                    currentLobbyCount--;
                }
                else if (currentLobbyCount - notEmptyServers.Length < emptyLobbyServersRequired & Time.time - lastLobbyCreationTime > 5)
                {
                    Debug.Log("Creating new lobby - prev count: " + currentLobbyCount);
                    CreateNewLobby();
                    currentLobbyCount++;
                    lastLobbyCreationTime = Time.time;
                }
            }
        }

        private void CreateNewLobby()
        {
            int originalServerCount = System.Array.FindAll(WebRequestManager.Singleton.ServerManager.LobbyServers, item => item.ip == networkTransport.ConnectionData.Address).Length;

            string path = "";
            if (Application.isEditor)
            {
                path = @"C:\Users\patse\OneDrive\Desktop\Windows Build\Vi The Game.exe";

#if UNITY_STANDALONE_OSX
                    path = @"/Users/odaleroxas/Documents/Builds/mac/headless/Vi The Game";
#endif
            }
            else
            {
                path = Application.dataPath;
                path = path.Substring(0, path.LastIndexOf('/'));
                path = Path.Join(path, Application.platform == RuntimePlatform.WindowsPlayer | Application.platform == RuntimePlatform.WindowsServer ? "Vi The Game.exe" : "Vi The Game.x86_64");

#if UNITY_STANDALONE_OSX
                    path = @"/Users/odaleroxas/Documents/Builds/mac/headless/Vi The Game";
#endif
            }

            System.Diagnostics.Process.Start(path, "-launch-as-lobby-server");
        }
    }
}