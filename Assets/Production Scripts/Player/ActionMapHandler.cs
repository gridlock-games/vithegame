using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Core.GameModeManagers;
using Unity.Netcode;
using Vi.Utility;

namespace Vi.Player
{
    public class ActionMapHandler : MonoBehaviour
    {
        [SerializeField] private GameObject playerUIPrefab;
        [SerializeField] private GameObject spectatorUIPrefab;

        private ExternalUI externalUI;

        private GameObject playerUIInstance;
        private GameObject spectatorUIInstance;
        private PlayerInput playerInput;
        private WeaponHandler weaponHandler;

        private void Update()
        {
            string actionMapName = "";
            if (playerInput.currentActionMap != null) { actionMapName = playerInput.currentActionMap.name; }

            if (!string.IsNullOrWhiteSpace(actionMapName))
            {
                if (!IsAnyUIOpen() & actionMapName != "Base")
                {
                    Debug.Log("no UI open but the action map name isn't base");
                }
            }
        }

        private bool IsAnyUIOpen()
        {
            if (externalUI != null) { return true; }
            if (scoreboardInstance != null) { return true; }
            if (pauseInstance != null) { return true; }
            if (inventoryInstance != null) { return true; }
            if (textChatIsOpen) { return true; }
            return false;
        }

        public void SetExternalUI(ExternalUI externalUI)
        {
            this.externalUI = externalUI;
            if (externalUI != null)
            {
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
                if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.Locked; }
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
                playerInput.SwitchCurrentActionMap("Base");
                playerUIInstance = Instantiate(playerUIPrefab, transform);
                if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.Locked; }
            }
            else if (playerInput.defaultActionMap == "Spectator")
            {
                playerInput.SwitchCurrentActionMap("Spectator");
                spectatorUIInstance = Instantiate(spectatorUIPrefab, transform);
                if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.Locked; }
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

            if (isOn)
            {
                if (scoreboardInstance) { return; }
                scoreboardInstance = ObjectPoolingManager.SpawnObject(scoreboardPrefab.GetComponent<PooledObject>());
            }
            else
            {
                if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.Locked; }
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
            if (inventoryInstance) { return; }

            if (pauseInstance)
            {
                if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.Locked; }
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

            if (inventoryInstance)
            {
                if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.Locked; }
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

            if (playerUIInstance)
            {
                playerUIInstance.SendMessage("OnTextChat");
            }
            else if (spectatorUIInstance)
            {
                spectatorUIInstance.SendMessage("OnTextChat");
            }
        }

        private bool textChatIsOpen;
        public void OnTextChatOpen()
        {
            textChatIsOpen = true;
            if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.None; }
            playerInput.SwitchCurrentActionMap("UI");
        }

        public void OnTextChatClose()
        {
            textChatIsOpen = false;
            if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.Locked; }
            playerInput.SwitchCurrentActionMap(playerInput.defaultActionMap);
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