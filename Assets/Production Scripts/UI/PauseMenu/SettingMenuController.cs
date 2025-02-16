using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Player;
using UnityEngine.UI;

namespace Vi.UI
{
    public class SettingMenuController : Menu
    {

        [SerializeField] private List<GameObject> uiList = new List<GameObject>();
        [SerializeField] private int defaultUI;
        [SerializeField] private Text errorText;
        private int currentSelection;

        [SerializeField] private UIModificationMenu UIModificationMenu;

        protected override void Awake()
        {
            base.Awake();
            //Clean up current set
            foreach (var item in uiList)
            {
                item.SetActive(false);
            }

            errorText.text = "";

            currentSelection = defaultUI;
            uiList[defaultUI].SetActive(true);
        }

        public void changeObject(int selectionObject)
        {
            if (selectionObject != currentSelection)
            {
                errorText.text = "";

                //Turn off current selection
                uiList[currentSelection].SetActive(false);

                //Turn on Selected Selection
                uiList[selectionObject].SetActive(true);

                //Set new ID
                currentSelection = selectionObject;
            }
        }

        public void CloseUI()
        {
            Destroy(this.gameObject);
        }

        public void OpenUIModificationMenu()
        {
            GameObject _settings = Instantiate(UIModificationMenu.gameObject);
            _settings.GetComponent<Menu>().SetLastMenu(gameObject);
            childMenu = _settings;
            gameObject.SetActive(false);
        }
    }
}