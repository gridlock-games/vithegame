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

        [SerializeField] private GameObject cameraMotor;
        [SerializeField] private GameObject playerCamera;
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private GameObject playerHUD;
        [SerializeField] private GameObject[] crosshairs;

        [HideInInspector] public Camera cameraCollisionDuplicate;
        [HideInInspector] public Camera ADSCamera;
        [HideInInspector] public bool externalUIOpen;

        private CameraMotorTypeAdventure cameraMotorInstance;

        private NetworkVariable<bool> spawnedOnOwnerInstance = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<Vector3> camPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Quaternion> camRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public Vector3 GetCamPosition() { return camPosition.Value; }

        public Quaternion GetCamRotation() { return camRotation.Value; }

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
                Destroy(cameraMotor);
                Destroy(playerCamera);
                Destroy(playerHUD);
            }
            StartCoroutine(WaitForClientConnection());
        }

        private void Start()
        {
            if (!IsSpawned)
            {
                playerHUD.SetActive(false);
            }

            StartCoroutine(ChangeSkin());
        }

        private IEnumerator WaitForClientConnection()
        {
	    if (!ClientManager.Singleton) { yield break; }
            yield return new WaitUntil(() => ClientManager.Singleton.GetClientDataDictionary().ContainsKey(OwnerClientId));

            if (!IsLocalPlayer)
            {
                worldSpaceLabel.SetActive(true);
            }

            // Add this player object to the local player list so that we can access player instances on any client
            ClientManager.Singleton.localNetworkPlayers.Add(OwnerClientId, gameObject);
        }

        public void ChangeSkinWithoutSpawn(ulong clientId)
        {
            GetComponent<CharacterAnimator>().ChangeModel(ClientManager.Singleton.GetPlayerModelOptions()[ClientManager.Singleton.GetClient(clientId).playerPrefabOptionIndex].skinOptions[ClientManager.Singleton.GetClient(clientId).skinIndex]);
        }

        private IEnumerator ChangeSkin()
        {
	    if (!ClientManager.Singleton) { yield break; }
            yield return new WaitUntil(() => ClientManager.Singleton.GetClientDataDictionary().ContainsKey(OwnerClientId));
            // Change player skin
            GetComponent<CharacterAnimator>().ChangeModel(ClientManager.Singleton.GetPlayerModelOptions()[ClientManager.Singleton.GetClient(OwnerClientId).playerPrefabOptionIndex].skinOptions[ClientManager.Singleton.GetClient(OwnerClientId).skinIndex]);
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

            bool camFound = false;
            if (TryGetComponent(out CharacterShooter shooter))
            {
                if (shooter.GetADSCamera().enabled)
                {
                    camPosition.Value = shooter.GetADSCamera().transform.position;
                    camRotation.Value = shooter.GetADSCamera().transform.rotation;
                    camFound = true;
                }
            }

            if (!camFound)
            {
                if (cameraCollisionDuplicate)
                {
                    if (cameraCollisionDuplicate.enabled)
                    {
                        camPosition.Value = cameraCollisionDuplicate.transform.position;
                        camRotation.Value = cameraCollisionDuplicate.transform.rotation;
                        camFound = true;
                    }
                }
            }
            
            if (!camFound)
            {
                camPosition.Value = playerCamera.transform.position;
                camRotation.Value = playerCamera.transform.rotation;
                camFound = true;
            }

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

            if (TryGetComponent(out CharacterShooter characterShooter))
            {
                cameraMotorInstance.allowOrbitInputOverride = !pauseInstance & !externalUIOpen & !characterShooter.IsAiming();
            }
            else
            {
                cameraMotorInstance.allowOrbitInputOverride = !pauseInstance & !externalUIOpen;
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