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
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private GameObject playerHUD;

        [HideInInspector] public NetworkVariable<ulong> roundTripTime = new NetworkVariable<ulong>();

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
                playerHUD.SetActive(true);
                Destroy(worldSpaceLabel);

                gameObject.transform.position = new Vector3(-5,0,-5);

                Cursor.lockState = CursorLockMode.Locked;
            }
            else // If we are not this instance's player object
            {
                worldSpaceLabel.SetActive(true);
                Destroy(cameraMotor);
                Destroy(playerCamera);
                Destroy(playerHUD);
                // If we are not the local player, display the name tag
            }
        }

        [SerializeField] private GameObject pauseMenuPrefab;
        [SerializeField] private GameObject triggersParent;
        private GameObject pauseInstance;

        [SerializeField] private TextMeshProUGUI pingDisplay;
        [SerializeField] private TextMeshProUGUI fpsCounterDisplay;
        private float _hudRefreshRate = 1f;
        private float _timer;

        private void Update()
        {
            if (!IsSpawned) { return; }

            if (!IsOwner) { return; }

            pingDisplay.SetText("Ping: " + roundTripTime.Value + " ms");

            // FPS Counter
            if (Time.unscaledTime > _timer)
            {
                int fps = (int)(1f / Time.unscaledDeltaTime);
                fpsCounterDisplay.SetText("FPS: " + fps);
                _timer = Time.unscaledTime + _hudRefreshRate;
            }

            // Pause menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!pauseInstance)
                {
                    triggersParent.SetActive(false);
                    Cursor.lockState = CursorLockMode.None;
                    pauseInstance = Instantiate(pauseMenuPrefab);
                }
                else
                {
                    triggersParent.SetActive(true);
                    Cursor.lockState = CursorLockMode.Locked;
                    pauseInstance.GetComponent<Menu>().DestroyAllMenus();
                    Destroy(pauseInstance);
                }
            }
        }
    }
}