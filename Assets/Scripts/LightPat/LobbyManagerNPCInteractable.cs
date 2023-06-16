using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class LobbyManagerNPCInteractable : MonoBehaviour
    {
        [SerializeField] private GameObject lobbyManagerUI;

        private bool localPlayerInRange;
        private Player.NetworkPlayer localPlayer;

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