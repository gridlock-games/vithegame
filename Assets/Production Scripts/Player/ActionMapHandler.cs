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
        private Camera playerCamera;
        private void Awake()
        {
            weaponHandler = GetComponent<WeaponHandler>();
            networkObject = GetComponent<NetworkObject>();
            playerCamera = GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            playerInput = GetComponent<PlayerInput>();
            if (playerInput.defaultActionMap == "Base")
            {
                playerUIInstance = Instantiate(playerUIPrefab, transform);
                Canvas canvas = playerUIInstance.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = playerCamera;
                canvas.planeDistance = playerCamera.nearClipPlane + 0.01f;
                if (networkObject.IsSpawned) { Cursor.lockState = CursorLockMode.Locked; }
            }
            else if (playerInput.defaultActionMap == "Spectator")
            {
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
    }
}