using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;

namespace Vi.Player
{
    public class ActionMapHandler : MonoBehaviour
    {
        [SerializeField] private Menu playerHUD;

        PlayerInput playerInput;

        private void OnEnable()
        {
            playerInput = GetComponent<PlayerInput>();
            if (playerInput.currentActionMap.name == "Base")
                Cursor.lockState = CursorLockMode.Locked;
        }

        [SerializeField] private GameObject scoreboardPrefab;
        void OnScoreboardToggle()
        {

        }

        [SerializeField] private GameObject inventoryPrefab;
        GameObject inventoryObject;
        bool inventoryEnabled;
        void OnInventoryToggle()
        {
            if (pauseEnabled) { return; }

            inventoryEnabled = !inventoryEnabled;
            if (inventoryEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                playerHUD.gameObject.SetActive(false);
                inventoryObject = Instantiate(inventoryPrefab, transform);
                playerInput.SwitchCurrentActionMap("Inventory");
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                playerHUD.gameObject.SetActive(true);
                Destroy(inventoryObject);
                playerInput.SwitchCurrentActionMap("Base");
            }
        }

        [SerializeField] private GameObject pausePrefab;
        GameObject pauseObject;
        bool pauseEnabled;
        void OnPause()
        {
            if (inventoryEnabled) { return; }

            pauseEnabled = !pauseEnabled;
            if (pauseEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                playerHUD.gameObject.SetActive(false);
                pauseObject = Instantiate(pausePrefab, transform);
                playerInput.SwitchCurrentActionMap("Menu");
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                playerHUD.gameObject.SetActive(true);
                pauseObject.GetComponent<Menu>().DestroyAllMenus();
                Destroy(pauseObject);
                playerInput.SwitchCurrentActionMap("Base");
            }
        }
    }
}