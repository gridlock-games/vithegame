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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (lobbyManagerUI.activeInHierarchy)
                {
                    lobbyManagerUI.SetActive(false);
                }
                else if (localPlayerInRange)
                {
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
                    localPlayerInRange = false;
                }
            }
        }
    }
}