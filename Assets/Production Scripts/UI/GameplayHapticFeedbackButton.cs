using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    [RequireComponent(typeof(Button))]
    public class GameplayHapticFeedbackButton : MonoBehaviour, IPointerDownHandler
    {
        private Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            RefreshStatus();
        }

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }
        }

        private bool vibrationsEnabled;
        private void RefreshStatus()
        {
            vibrationsEnabled = FasterPlayerPrefs.Singleton.GetBool("GameplayVibrationsEnabled");
        }

        private void PlayHapticFeedback()
        {
            if (vibrationsEnabled)
            {
# if UNITY_ANDROID || UNITY_IOS
                CandyCoded.HapticFeedback.HapticFeedback.LightFeedback();
#endif
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;
            PlayHapticFeedback();
        }
    }
}