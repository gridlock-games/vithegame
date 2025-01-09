using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Utility;
using Vi.Core;
using Vi.Core.CombatAgents;
using Unity.Netcode;

namespace Vi.Player
{
    public class Menu : MonoBehaviour
    {
        protected GameObject childMenu;
        protected GameObject lastMenu;

        private CameraController playerCameraController;
        private Camera spectatorCamera;

        protected virtual void Awake()
        {
            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                Application.targetFrameRate = 30;
            }

            KeyValuePair<int, Attributes> kvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
            if (kvp.Value)
            {
                if (kvp.Value.TryGetComponent(out PlayerMovementHandler playerMovementHandler))
                {
                    playerCameraController = playerMovementHandler.CameraController;
                    playerCameraController.SetActive(false);
                }
            }

            KeyValuePair<ulong, NetworkObject> spectatorKvp = PlayerDataManager.Singleton.GetLocalSpectatorObject();
            if (spectatorKvp.Value)
            {
                if (spectatorKvp.Value.TryGetComponent(out spectatorCamera))
                {
                    spectatorCamera.enabled = false;
                }
            }

            QualitySettings.resolutionScalingFixedDPIFactor = 1;
            QualitySettings.globalTextureMipmapLimit = 0;
        }

        public void QuitGame()
        {
            FasterPlayerPrefs.QuitGame();
        }

        public void SetLastMenu(GameObject lm)
        {
            lastMenu = lm;
        }

        public void GoBackToLastMenu()
        {
            Destroy(gameObject);
            if (lastMenu == null) { return; }
            lastMenu.SetActive(true);
        }

        public void DestroyAllMenus(string message = "")
        {
            NetSceneManager.SetTargetFrameRate();

            if (playerCameraController)
            {
                playerCameraController.SetActive(true);
            }

            if (spectatorCamera)
            {
                spectatorCamera.enabled = true;
            }

            AdaptivePerformanceManager.Singleton.RefreshThermalSettings();

            if (message != "")
            {
                try
                {
                    transform.parent.SendMessage(message);
                    return;
                }
                catch
                {

                }
            }

            if (childMenu)
            {
                childMenu.GetComponent<Menu>().DestroyAllMenus(message);
            }
            Destroy(gameObject);
        }
    }
}