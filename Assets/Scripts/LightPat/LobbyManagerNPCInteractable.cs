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

        private bool localPlayerInRange;
        private Player.NetworkPlayer localPlayer;

        private bool joinLobbyCalled;
        public void JoinLobbyOnClick()
        {
            if (joinLobbyCalled) { return; }
            joinLobbyCalled = true;

            Debug.Log(Time.time + " Clicked " + targetIP);
        }

        private string targetIP;
        public void SetServerIP(string targetIP)
        {
            this.targetIP = targetIP;
        }

        private void Start()
        {
            foreach (string serverIP in serverIPList)
            {
                GameObject serverButton = Instantiate(serverButtonPrefab, serverButtonParent);
                serverButton.name = serverIP;
                serverButton.GetComponentInChildren<TextMeshProUGUI>().SetText(serverIP);
                serverButton.GetComponent<Button>().onClick.AddListener(() => SetServerIP(serverIP));
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
                }
                else if (localPlayerInRange)
                {
                    localPlayer.DisableActionsServerRpc(true);
                    localPlayer.cameraMotorInstance.allowOrbitInput = false;
                    lobbyManagerUI.SetActive(true);
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