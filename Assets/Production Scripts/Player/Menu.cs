using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Utility;
using Vi.Core;

namespace Vi.Player
{
    public class Menu : MonoBehaviour
    {
        protected GameObject childMenu;
        protected GameObject lastMenu;

        protected virtual void Awake()
        {
            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                Application.targetFrameRate = 30;
            }
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