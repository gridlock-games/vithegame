using UnityEngine;
using Vi.Utility;
using UnityEngine.UI;

namespace Vi.UI
{
    [RequireComponent(typeof(Toggle))]
    public class SetPlayerPrefFromToggle : MonoBehaviour
    {
        [SerializeField] private string playerPrefName;

        private void SetPlayerPref(bool value)
        {
            FasterPlayerPrefs.Singleton.SetBool(playerPrefName, value);
        }

        private Toggle toggle;
        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            if (FasterPlayerPrefs.Singleton.HasBool(playerPrefName))
            {
                toggle.SetIsOnWithoutNotify(FasterPlayerPrefs.Singleton.GetBool(playerPrefName));
            }
            else
            {
                Debug.LogWarning("No player pref found " + playerPrefName);
            }
            toggle.onValueChanged.AddListener(SetPlayerPref);
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(SetPlayerPref);
        }
    }
}