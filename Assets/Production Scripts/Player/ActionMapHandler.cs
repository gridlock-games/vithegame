using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Core.GameModeManagers;
using Unity.Netcode;
using Vi.Utility;
using Vi.Player.TextChat;

namespace Vi.Player
{
    public class ActionMapHandler : MonoBehaviour
    {
        [SerializeField] private GameObject playerUIPrefab;
        [SerializeField] private GameObject spectatorUIPrefab;

        private ExternalUI externalUI;

        private GameObject playerUIInstance;
        private GameObject spectatorUIInstance;
        private TextChat.TextChat textChatInstance;
        private PlayerInput playerInput;
        private WeaponHandler weaponHandler;

        private const CursorLockMode PlayerActiveLockMode = CursorLockMode.Locked;

        public void SetExternalUI(ExternalUI externalUI)
        {
            this.externalUI = externalUI;
            if (externalUI != null)
            {
                if (textChatIsOpen)
                {
                    OnTextChat();
                    return;
                }

                if (playerCameraController) { playerCameraController.SetOrbitalCameraState(false); }

                Cursor.lockState = CursorLockMode.None;
                if (playerUIInstance)
                    playerUIInstance.SetActive(false);
                playerInput.SwitchCurrentActionMap("UI");

                if (scoreboardInstance) { ObjectPoolingManager.ReturnObjectToPool(ref scoreboardInstance); }
                if (pauseInstance) { pauseInstance.GetComponent<Menu>().DestroyAllMenus(); }
                if (inventoryInstance) { inventoryInstance.GetComponent<Menu>().DestroyAllMenus(); }

                if (weaponHandler) { weaponHandler.ClearPreviewActionVFXInstances(); }
            }
            else
            {
                if (networkObject.IsSpawned)
                {
                    Cursor.lockState = PlayerActiveLockMode;
                }
                if (playerUIInstance)
                    playerUIInstance.SetActive(true);
                playerInput.SwitchCurrentActionMap(playerInput.defaultActionMap);
            }
        }

        private NetworkObject networkObject;
        private CameraController playerCameraController;
        private CombatAgent combatAgent;
        private void Awake()
        {
            weaponHandler = GetComponent<WeaponHandler>();
            networkObject = GetComponent<NetworkObject>();
            playerCameraController = GetComponentInChildren<CameraController>();
            combatAgent = GetComponent<CombatAgent>();
        }

        private void OnEnable()
        {
            playerInput = GetComponent<PlayerInput>();
            if (playerInput.defaultActionMap == "Base")
            {
                playerUIInstance = Instantiate(playerUIPrefab, transform);
                textChatInstance = playerUIInstance.GetComponentInChildren<TextChat.TextChat>(true);
                if (networkObject.IsSpawned)
                {
                    Cursor.lockState = PlayerActiveLockMode;
                }
            }
            else if (playerInput.defaultActionMap == "Spectator")
            {
                spectatorUIInstance = Instantiate(spectatorUIPrefab, transform);
                textChatInstance = spectatorUIInstance.GetComponentInChildren<TextChat.TextChat>(true);
                if (networkObject.IsSpawned)
                {
                    Cursor.lockState = PlayerActiveLockMode;
                }
            }
            else
            {
                Debug.LogError("Not sure how to handle action map: " + playerInput.defaultActionMap);
            }
        }

        [SerializeField] private GameObject scoreboardPrefab;
        PooledObject scoreboardInstance;
        void OnScoreboard(InputValue value)
        {
            ToggleScoreboard(value.isPressed);
        }

        public void OpenScoreboard()
        {
            ToggleScoreboard(true);
        }

        public void CloseScoreboard()
        {
            ToggleScoreboard(false);
        }

        private void OnDisable()
        {
            externalUI = null;
            if (pauseInstance) { pauseInstance.GetComponent<Menu>().DestroyAllMenus(); }
            if (inventoryInstance) { inventoryInstance.GetComponent<Menu>().DestroyAllMenus(); }
            if (scoreboardInstance) { ObjectPoolingManager.ReturnObjectToPool(ref scoreboardInstance); }
            if (playerUIInstance) { Destroy(playerUIInstance); }
            if (spectatorUIInstance) { Destroy(spectatorUIInstance); }
        }

        private void ToggleScoreboard(bool isOn)
        {
            if (externalUI != null) { return; }
            if (!GameModeManager.Singleton) { return; }
            if (pauseInstance) { return; }
            if (inventoryInstance) { return; }
            if (textChatIsOpen) { return; }
            if (playerCameraController) { playerCameraController.SetOrbitalCameraState(false); }

            if (isOn)
            {
                if (scoreboardInstance) { return; }
                scoreboardInstance = ObjectPoolingManager.SpawnObject(scoreboardPrefab.GetComponent<PooledObject>());
            }
            else
            {
                if (networkObject.IsSpawned)
                {
                    Cursor.lockState = PlayerActiveLockMode;
                }
                if (scoreboardInstance) { ObjectPoolingManager.ReturnObjectToPool(ref scoreboardInstance); }
            }

            if (playerUIInstance)
                playerUIInstance.SetActive(!isOn);
            if (spectatorUIInstance)
                spectatorUIInstance.SetActive(!isOn);
        }

        private void OnAim(InputValue value)
        {
            if (value.isPressed)
            {
                if (scoreboardInstance) { Cursor.lockState = CursorLockMode.None; }
            }
        }

        [SerializeField] private GameObject pausePrefab;
        GameObject pauseInstance;
        public void OnPause()
        {
            if (externalUI != null)
            {
                externalUI.OnPause();
                return;
            }
            if (textChatIsOpen)
            {
                OnTextChat();
                return;
            }
            if (scoreboardInstance) { return; }
            if (inventoryInstance)
            {
                OnInventory();
                return;
            }
            if (playerCameraController) { playerCameraController.SetOrbitalCameraState(false); }

            if (pauseInstance)
            {
                if (networkObject.IsSpawned)
                {
                    Cursor.lockState = PlayerActiveLockMode;
                }
                pauseInstance.GetComponent<Menu>().DestroyAllMenus();
                if (playerUIInstance)
                    playerUIInstance.SetActive(true);
                if (spectatorUIInstance)
                    spectatorUIInstance.SetActive(true);
                playerInput.SwitchCurrentActionMap(playerInput.defaultActionMap);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                if (playerUIInstance)
                    playerUIInstance.SetActive(false);
                if (spectatorUIInstance)
                    spectatorUIInstance.SetActive(false);
                pauseInstance = Instantiate(pausePrefab, transform);
                playerInput.SwitchCurrentActionMap("UI");
            }
        }

        [SerializeField] private GameObject inventoryPrefab;
        GameObject inventoryInstance;
        public void OnInventory()
        {
            if (externalUI != null) { return; }
            if (scoreboardInstance) { return; }
            if (pauseInstance) { return; }
            if (textChatIsOpen) { return; }
            if (playerCameraController) { playerCameraController.SetOrbitalCameraState(false); }

            if (inventoryInstance)
            {
                if (networkObject.IsSpawned)
                {
                    Cursor.lockState = PlayerActiveLockMode;
                }
                inventoryInstance.GetComponent<Menu>().DestroyAllMenus();
                if (playerUIInstance)
                    playerUIInstance.SetActive(true);
                if (spectatorUIInstance)
                    spectatorUIInstance.SetActive(true);
                playerInput.SwitchCurrentActionMap(playerInput.defaultActionMap);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                if (playerUIInstance)
                    playerUIInstance.SetActive(false);
                if (spectatorUIInstance)
                    spectatorUIInstance.SetActive(false);
                inventoryInstance = Instantiate(inventoryPrefab, transform);
                playerInput.SwitchCurrentActionMap("UI");
            }
        }

        public void OnTextChat()
        {
            if (externalUI != null) { return; }
            if (scoreboardInstance) { return; }
            if (pauseInstance) { return; }
            if (inventoryInstance) { return; }
            if (playerCameraController) { playerCameraController.SetOrbitalCameraState(false); }

            if (textChatInstance)
            {
                textChatInstance.OnTextChat();
            }
        }

        private bool textChatIsOpen;
        public void OnTextChatOpen()
        {
            textChatIsOpen = true;
            if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.None; }
            playerInput.SwitchCurrentActionMap("UI");
        }

        public void OnTextChatClose(bool evaluateCursorLockMode)
        {
            textChatIsOpen = false;
            if (evaluateCursorLockMode)
            {
                if (networkObject.IsSpawned) { Cursor.lockState = PlayerActiveLockMode; }
                playerInput.SwitchCurrentActionMap(playerInput.defaultActionMap);
            }
        }

        public static bool CanUseOrbitalCamera()
        {
            return PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None;
        }

        public void SetOrbitalCamState(bool isPressed)
        {
            if (!playerCameraController) { return; }
            if (externalUI != null) { return; }
            if (scoreboardInstance) { return; }
            if (pauseInstance) { return; }
            if (inventoryInstance) { return; }
            if (textChatIsOpen) { return; }
            if (!CanUseOrbitalCamera()) { return; }

            if (combatAgent)
            {
                if (combatAgent.GetAilment() == ScriptableObjects.ActionClip.Ailment.Death) { return; }
            }
            else
            {
                return;
            }

            string orbitalCamMode = FasterPlayerPrefs.Singleton.GetString("OrbitalCameraMode");
            if (orbitalCamMode == "HOLD")
            {
                playerCameraController.SetOrbitalCameraState(isPressed);
            }
            else if (orbitalCamMode == "TOGGLE")
            {
                if (isPressed) { playerCameraController.ToggleOrbitalCameraState(); }
            }
            else
            {
                Debug.LogError("Unsure how to handle orbital camera mode " + orbitalCamMode);
            }
        }

        private void OnOrbitalCam(InputValue value)
        {
            SetOrbitalCamState(value.isPressed);
        }
    }
}