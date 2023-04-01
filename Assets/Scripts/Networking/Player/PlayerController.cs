using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Player
{
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private GameObject cameraMotor;
        [SerializeField] private GameObject playerCamera;

        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                cameraMotor.SetActive(true);
                cameraMotor.transform.SetParent(null, true);
                playerCamera.transform.SetParent(null, true);
                playerCamera.GetComponent<Camera>().enabled = true;
                playerCamera.GetComponent<AudioListener>().enabled = true;
                gameObject.AddComponent<GameCreator.Core.Hooks.HookPlayer>();
            }
            else
            {
                Destroy(cameraMotor);
                Destroy(playerCamera);
            }
        }
    }
}