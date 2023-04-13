using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using LightPat.Core;

namespace LightPat.Player
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField] private GameObject cameraMotor;
        [SerializeField] private GameObject playerCamera;
        [SerializeField] private TextMeshPro nameTag;

        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer) // This checks if we are this instance's player object
            {
                // Activate the camera motor object and set the parent to the root
                cameraMotor.SetActive(true);
                cameraMotor.transform.SetParent(null, true);
                // Need to add the hook camera component to the player camera. The camera controller component relies on the hook camera component, so you also add that
                playerCamera.transform.SetParent(null, true);
                playerCamera.AddComponent<GameCreator.Core.Hooks.HookCamera>();
                var camControl = playerCamera.AddComponent<GameCreator.Camera.CameraController>();
                camControl.currentCameraMotor = cameraMotor.GetComponent<GameCreator.Camera.CameraMotor>();
                playerCamera.GetComponent<Camera>().enabled = true;
                playerCamera.GetComponent<AudioListener>().enabled = true;
                // Add the hook player component
                gameObject.AddComponent<GameCreator.Core.Hooks.HookPlayer>();
                // If we are the local player, destroy the name tag
                Destroy(nameTag.gameObject);
            }
            else // If we are not this instance's player object
            {
                Destroy(cameraMotor);
                Destroy(playerCamera);
                // If we are not the local player, display the name tag
                nameTag.SetText(ClientManager.Singleton.GetClient(OwnerClientId).clientName);
            }
        }

        [SerializeField] private TextMeshProUGUI fpsCounterDisplay;
        private float _hudRefreshRate = 1f;
        private float _timer;

        private void Update()
        {
            if (!IsOwner) { return; }

            if (Time.unscaledTime > _timer)
            {
                int fps = (int)(1f / Time.unscaledDeltaTime);
                fpsCounterDisplay.SetText("FPS: " + fps);
                _timer = Time.unscaledTime + _hudRefreshRate;
            }
        }
    }
}