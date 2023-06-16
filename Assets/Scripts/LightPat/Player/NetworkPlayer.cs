using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using LightPat.Core;
using GameCreator.Characters;
using GameCreator.Melee;
using UnityEngine.SceneManagement;

namespace LightPat.Player
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [HideInInspector] public NetworkVariable<ulong> roundTripTime = new NetworkVariable<ulong>();

        [SerializeField] private GameObject cameraMotor;
        [SerializeField] private GameObject playerCamera;
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private GameObject playerHUD;
        [SerializeField] private GameObject[] crosshairs;

        public override void OnNetworkSpawn()
        {
            GetComponent<PlayerCharacter>().enabled = true;
            if (IsLocalPlayer) // This checks if we are this instance's player object
            {
                // Activate the camera motor object and set the parent to the root
                cameraMotor.SetActive(true);
                cameraMotor.transform.SetParent(null, true);
                // Need to add the hook camera component to the player camera. The camera controller component relies on the hook camera component, so you also add that
                playerCamera.SetActive(true);
                playerCamera.transform.SetParent(null, true);
                playerCamera.AddComponent<GameCreator.Core.Hooks.HookCamera>();
                var camControl = playerCamera.AddComponent<GameCreator.Camera.CameraController>();
                camControl.currentCameraMotor = cameraMotor.GetComponent<GameCreator.Camera.CameraMotor>();
                playerCamera.GetComponent<Camera>().enabled = true;
                playerCamera.GetComponent<AudioListener>().enabled = true;
                // Add the hook player component
                gameObject.AddComponent<GameCreator.Core.Hooks.HookPlayer>();
                playerHUD.SetActive(true);
                // Deactivate crosshair in the player hub
                foreach (GameObject crosshair in crosshairs)
                {
                    crosshair.SetActive(SceneManager.GetActiveScene().name != "Hub");
                }
                Cursor.lockState = CursorLockMode.Locked;
            }
            else // If we are not this instance's player object
            {
                worldSpaceLabel.SetActive(true);
                worldSpaceLabel.GetComponent<UI.WorldSpaceLabel>().disableHealthbar = SceneManager.GetActiveScene().name == "Hub";
                Destroy(cameraMotor);
                Destroy(playerCamera);
                Destroy(playerHUD);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                Destroy(cameraMotor);
                Destroy(playerCamera);
                Destroy(playerHUD);
                Cursor.lockState = CursorLockMode.None;
            }
        }

        [SerializeField] private GameObject pauseMenuPrefab;
        [SerializeField] private GameObject scoreboardPrefab;
        private GameObject pauseInstance;
        private GameObject scoreboardInstance;

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
                    DisableActionsServerRpc(true);
                    Cursor.lockState = CursorLockMode.None;
                    pauseInstance = Instantiate(pauseMenuPrefab);
                }
                else
                {
                    DisableActionsServerRpc(false);
                    Cursor.lockState = CursorLockMode.Locked;
                    pauseInstance.GetComponent<Menu>().DestroyAllMenus();
                    Destroy(pauseInstance);
                }
            }

            if (!ClientManager.Singleton) { return; }

            // Scoreboard
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (!scoreboardInstance)
                    scoreboardInstance = Instantiate(scoreboardPrefab);
            }
            
            if (Input.GetKeyUp(KeyCode.Tab))
            {
                if (scoreboardInstance)
                    Destroy(scoreboardInstance);
            }
        }

        [ServerRpc] public void DisableActionsServerRpc(bool disableActions) { GetComponent<Character>().disableActions.Value = disableActions; }

        // Messages from Character Melee
        void OnDamageDealt(int damage)
        {
            if (!ClientManager.Singleton) { return; }
            ClientManager.Singleton.AddDamage(OwnerClientId, damage);
        }

        void OnKill(CharacterMelee victim)
        {
            if (!ClientManager.Singleton) { return; }
            ClientManager.Singleton.AddKills(OwnerClientId, 1);
        }

        void OnDeath(CharacterMelee killer)
        {
            if (!ClientManager.Singleton) { return; }
            ClientManager.Singleton.AddDeaths(OwnerClientId, 1);
        }
    }
}