using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using LightPat.Core;
using GameCreator.Characters;
using GameCreator.Melee;
using UnityEngine.SceneManagement;
using GameCreator.Camera;

namespace LightPat.Player
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [HideInInspector] public NetworkVariable<ulong> roundTripTime = new NetworkVariable<ulong>();

        [SerializeField] private GameObject modelInstance;
        [SerializeField] private GameObject cameraMotor;
        [SerializeField] private GameObject playerCamera;
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private GameObject playerHUD;
        [SerializeField] private GameObject[] crosshairs;

        [HideInInspector] public bool externalUIOpen;

        private CameraMotorTypeAdventure cameraMotorInstance;

        private NetworkVariable<bool> spawnedOnOwnerInstance = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public bool IsSpawnedOnOwnerInstance() { return spawnedOnOwnerInstance.Value; }

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
                var camControl = playerCamera.AddComponent<CameraController>();
                camControl.currentCameraMotor = cameraMotor.GetComponent<CameraMotor>();
                cameraMotorInstance = (CameraMotorTypeAdventure)camControl.currentCameraMotor.cameraMotorType;
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

                spawnedOnOwnerInstance.Value = true;
                Destroy(worldSpaceLabel);
            }
            else // If we are not this instance's player object
            {
                worldSpaceLabel.SetActive(true);
                Destroy(cameraMotor);
                Destroy(playerCamera);
                Destroy(playerHUD);
            }

            // Add this player object to the local player list so that we can access player instances on any client
            ClientManager.Singleton.localNetworkPlayers.Add(OwnerClientId, gameObject);

            // Change player skin
            int playerPrefabOptionIndex = ClientManager.Singleton.GetClient(OwnerClientId).playerPrefabOptionIndex;
            int skinIndex = ClientManager.Singleton.GetClient(OwnerClientId).skinIndex;
            GetComponent<CharacterAnimator>().ChangeModel(ClientManager.Singleton.GetPlayerModelOptions()[playerPrefabOptionIndex].skinOptions[skinIndex]);
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
        private readonly float _hudRefreshRate = 1f;
        private float _timer;

        private void Update()
        {
            if (!IsSpawned) { return; }

            if (!IsOwner) { return; }

            // FPS Counter and Ping Display
            if (Time.unscaledTime > _timer)
            {
                int fps = (int)(1f / Time.unscaledDeltaTime);
                fpsCounterDisplay.SetText("FPS: " + fps);
                pingDisplay.SetText("Ping: " + roundTripTime.Value + " ms");
                _timer = Time.unscaledTime + _hudRefreshRate;
            }

            // Pause menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!pauseInstance)
                {
                    if (!externalUIOpen & cameraMotorInstance.allowOrbitInput)
                    {
                        DisableActionsServerRpc(true);
                        Cursor.lockState = CursorLockMode.None;
                        pauseInstance = Instantiate(pauseMenuPrefab);
                    }
                }
                else
                {
                    DisableActionsServerRpc(false);
                    Cursor.lockState = CursorLockMode.Locked;
                    pauseInstance.GetComponent<Menu>().DestroyAllMenus();
                    Destroy(pauseInstance);
                }
            }

            cameraMotorInstance.allowOrbitInputOverride = !pauseInstance & !externalUIOpen;

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