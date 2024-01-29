using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Core.GameModeManagers;
using Unity.Netcode;

namespace Vi.Player
{
    public class ActionMapHandler : MonoBehaviour
    {
        [SerializeField] private GameObject playerUIPrefab;
        [SerializeField] private GameObject spectatorUIPrefab;

        public MonoBehaviour ExternalUI { get; private set; }

        private GameObject playerUIInstance;
        private GameObject spectatorUIInstance;
        private PlayerInput playerInput;

        public void SetExternalUI(MonoBehaviour externalUI)
        {
            ExternalUI = externalUI;
            if (externalUI)
            {
                Cursor.lockState = CursorLockMode.None;
                if (playerUIInstance)
                    playerUIInstance.SetActive(false);
                playerInput.SwitchCurrentActionMap("Menu");
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                if (playerUIInstance)
                    playerUIInstance.SetActive(true);
                playerInput.SwitchCurrentActionMap(playerInput.defaultActionMap);
            }
        }

        private LoadoutManager loadoutManager;
        private void Awake()
        {
            loadoutManager = GetComponent<LoadoutManager>();
        }

        private void OnEnable()
        {
            playerInput = GetComponent<PlayerInput>();
            if (playerInput.defaultActionMap == "Base")
            {
                playerUIInstance = Instantiate(playerUIPrefab, transform);
                Cursor.lockState = CursorLockMode.Locked;
            }
            else if (playerInput.defaultActionMap == "Spectator")
            {
                spectatorUIInstance = Instantiate(spectatorUIPrefab, transform);
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Debug.LogError("Not sure how to handle action map: " + playerInput.defaultActionMap);
            }
        }

        private void OnDestroy()
        {
            if (TryGetComponent(out NetworkObject netObj))
            {
                if (netObj.IsLocalPlayer) { Cursor.lockState = CursorLockMode.None; }
            }
        }

        [SerializeField] private GameObject scoreboardPrefab;
        GameObject scoreboardInstance;
        void OnScoreboard(InputValue value)
        {
            if (ExternalUI) { return; }
            if (!GameModeManager.Singleton) { return; }
            if (minimapInstance) { return; }
            if (pauseInstance) { return; }
            if (inventoryInstance) { return; }

            if (value.isPressed)
            {
                scoreboardInstance = Instantiate(scoreboardPrefab);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Destroy(scoreboardInstance);
            }

            if (playerUIInstance)
                playerUIInstance.SetActive(!value.isPressed);
            if (spectatorUIInstance)
                spectatorUIInstance.SetActive(!value.isPressed);
        }

        void OnHeavyAttack()
        {
            if (scoreboardInstance) { Cursor.lockState = CursorLockMode.None; }
        }

        [SerializeField] private GameObject pausePrefab;
        GameObject pauseInstance;
        public void OnPause()
        {
            if (ExternalUI)
            {
                ExternalUI.SendMessage("OnPause");
                return;
            }
            if (minimapInstance) { return; }
            if (scoreboardInstance) { return; }
            if (inventoryInstance) { return; }

            if (pauseInstance)
            {
                Cursor.lockState = CursorLockMode.Locked;
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
                playerInput.SwitchCurrentActionMap("Menu");
            }
        }

        [SerializeField] private GameObject inventoryPrefab;
        GameObject inventoryInstance;
        void OnInventory()
        {
            if (!loadoutManager.CanSwapWeapons()) { return; }
            if (ExternalUI) { return; }
            if (scoreboardInstance) { return; }
            if (pauseInstance) { return; }
            if (minimapInstance) { return; }

            if (inventoryInstance)
            {
                Cursor.lockState = CursorLockMode.Locked;
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
                playerInput.SwitchCurrentActionMap("Menu");
            }
        }

        [SerializeField] private GameObject minimapPrefab;
        GameObject minimapInstance;
        void OnMinimap(InputValue value)
        {
            if (ExternalUI) { return; }
            if (scoreboardInstance) { return; }
            if (pauseInstance) { return; }
            if (inventoryInstance) { return; }

            if (value.isPressed)
            {
                minimapInstance = Instantiate(minimapPrefab);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Destroy(minimapInstance);
            }
        }
    }
}