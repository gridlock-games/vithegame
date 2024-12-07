using UnityEngine;
using Vi.Utility;
using UnityEngine.UI;

namespace Vi.UI
{
    [RequireComponent(typeof(Slider))]
    public class SetPlayerPrefFromSlider : MonoBehaviour
    {
        [SerializeField] private string playerPrefName;
        
        private void SetPlayerPref(float value)
        {
            FasterPlayerPrefs.Singleton.SetFloat(playerPrefName, value);
        }

        private Slider slider;
        private void Awake()
        {
            slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (FasterPlayerPrefs.Singleton.HasFloat(playerPrefName))
            {
                slider.SetValueWithoutNotify(FasterPlayerPrefs.Singleton.GetFloat(playerPrefName));
            }
            else
            {
                Debug.LogWarning("No player pref found " + playerPrefName);
            }
            slider.onValueChanged.AddListener(SetPlayerPref);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(SetPlayerPref);
        }
    }
}