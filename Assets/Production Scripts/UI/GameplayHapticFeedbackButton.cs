using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    [RequireComponent(typeof(Button))]
    public class GameplayHapticFeedbackButton : MonoBehaviour
    {
# if UNITY_ANDROID || UNITY_IOS
        private Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            RefreshStatus();
            button.onClick.AddListener(PlayHapticFeedback);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(PlayHapticFeedback);
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
                CandyCoded.HapticFeedback.HapticFeedback.LightFeedback();
            }
        }
#endif
    }
}