using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class SceneLoadingInfoUI : MonoBehaviour
    {
        [SerializeField] private List<Sprite> imageList;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image viLogoImage;
        [SerializeField] private Image gridlockLogoImage;
        [SerializeField] private Text mapNameText;

        private void Awake()
        {
            ChangeBackground();
        }

        public void ChangeBackground()
        {
            backgroundImage.sprite = imageList[Random.Range(0, imageList.Count)];
            viLogoImage.enabled = false;
            gridlockLogoImage.enabled = false;
            mapNameText.text = "";
            if (PlayerDataManager.DoesExist())
            {
                if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None)
                {
                    backgroundImage.sprite = NetSceneManager.Singleton.GetSceneGroupIcon(PlayerDataManager.Singleton.GetMapName());
                    viLogoImage.enabled = true;
                    gridlockLogoImage.enabled = true;
                    //mapNameText.text = "<b>" + PlayerDataManager.Singleton.GetMapName() + "</b>";
                }
            }
        }
    }
}