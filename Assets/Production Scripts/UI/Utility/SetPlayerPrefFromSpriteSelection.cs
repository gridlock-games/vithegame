using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    public class SetPlayerPrefFromSpriteSelection : MonoBehaviour
    {
        [SerializeField] private string playerPrefName;

        private void SetPlayerPref(int value)
        {
            FasterPlayerPrefs.Singleton.SetInt(playerPrefName, value);
        }

        [SerializeField] private Button leftArrow;
        [SerializeField] private Button rightArrow;
        [SerializeField] private Image displayImage;

        private int currentIndex;

        private void OnEnable()
        {
            if (FasterPlayerPrefs.Singleton.HasInt(playerPrefName))
            {
                currentIndex = FasterPlayerPrefs.Singleton.GetInt(playerPrefName);
                RefreshDisplay();
            }
            else
            {
                Debug.LogWarning("No player pref found " + playerPrefName);
            }

            leftArrow.onClick.AddListener(Decrement);
            rightArrow.onClick.AddListener(Increment);
        }

        private void OnDisable()
        {
            leftArrow.onClick.RemoveListener(Decrement);
            rightArrow.onClick.RemoveListener(Increment);
        }

        private void Increment()
        {
            currentIndex++;
            if (currentIndex >= FasterPlayerPrefs.Singleton.crosshairSprites.Length)
            {
                currentIndex = 0;
            }
            FasterPlayerPrefs.Singleton.SetInt(playerPrefName, currentIndex);
            RefreshDisplay();
        }

        private void Decrement()
        {
            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = FasterPlayerPrefs.Singleton.crosshairSprites.Length - 1;
            }
            FasterPlayerPrefs.Singleton.SetInt(playerPrefName, currentIndex);
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            displayImage.sprite = FasterPlayerPrefs.Singleton.crosshairSprites[currentIndex].Result;
        }
    }
}