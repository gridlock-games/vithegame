using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Core;

namespace Vi.Player
{
    public class ActionMapHandler : MonoBehaviour
    {
        [SerializeField] private GameObject playerUIPrefab;
        [SerializeField] private GameObject spectatorUIPrefab;

        private GameObject playerUIInstance;
        private GameObject spectatorUIInstance;
        private PlayerInput playerInput;

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

        //[SerializeField] private GameObject scoreboardPrefab;
        //void OnScoreboardToggle()
        //{
            
        //}

        //[SerializeField] private GameObject inventoryPrefab;
        //GameObject inventoryObject;
        //bool inventoryEnabled;
        //void OnInventoryToggle()
        //{
        //    //if (pauseEnabled) { return; }

        //    inventoryEnabled = !inventoryEnabled;
        //    if (inventoryEnabled)
        //    {
        //        Cursor.lockState = CursorLockMode.None;
        //        playerHUD.SetActive(false);
        //        inventoryObject = Instantiate(inventoryPrefab, transform);
        //        playerInput.SwitchCurrentActionMap("Inventory");
        //    }
        //    else
        //    {
        //        Cursor.lockState = CursorLockMode.Locked;
        //        playerHUD.SetActive(true);
        //        Destroy(inventoryObject);
        //        playerInput.SwitchCurrentActionMap("Base");
        //    }
        //}

        [SerializeField] private GameObject pausePrefab;
        GameObject pauseObject;
        void OnPause()
        {
            if (pauseObject)
            {
                Cursor.lockState = CursorLockMode.Locked;
                pauseObject.GetComponent<Menu>().DestroyAllMenus();
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
                pauseObject = Instantiate(pausePrefab, transform);
                playerInput.SwitchCurrentActionMap("Menu");
            }
        }
    }
}