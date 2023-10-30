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

        private GameObject playerUIInstance;
        private PlayerInput playerInput;

        private void OnEnable()
        {
            playerUIInstance = Instantiate(playerUIPrefab, transform);
            playerInput = GetComponent<PlayerInput>();
            if (playerInput.currentActionMap.name == "Base")
                Cursor.lockState = CursorLockMode.Locked;
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
                playerUIInstance.SetActive(true);
                playerInput.SwitchCurrentActionMap("Base");
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                playerUIInstance.SetActive(false);
                pauseObject = Instantiate(pausePrefab, transform);
                playerInput.SwitchCurrentActionMap("Menu");
            }
        }
    }
}