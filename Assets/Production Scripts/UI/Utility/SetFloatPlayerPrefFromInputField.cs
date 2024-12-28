using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    [RequireComponent(typeof(InputField))]
    public class SetFloatPlayerPrefFromInputField : MonoBehaviour
    {
        [SerializeField] private string playerPrefName;

        private void SetPlayerPref(string value)
        {
            value = Regex.Replace(value, @"[^0-9|.]", "");
            inputField.SetTextWithoutNotify(value);

            if (float.TryParse(value, out float result))
            {
                FasterPlayerPrefs.Singleton.SetFloat(playerPrefName, result);
            }
            else if (value != ".")
            {
                Debug.LogWarning("Unable to parse float from string " + value);
            }
        }

        private InputField inputField;
        private void Awake()
        {
            inputField = GetComponent<InputField>();
        }

        private void OnEnable()
        {
            if (FasterPlayerPrefs.Singleton.HasFloat(playerPrefName))
            {
                inputField.text = FasterPlayerPrefs.Singleton.GetFloat(playerPrefName).ToString();
            }
            else
            {
                Debug.LogWarning("No player pref found " + playerPrefName);
            }
            inputField.onValueChanged.AddListener(SetPlayerPref);
        }

        private void OnDisable()
        {
            inputField.onValueChanged.RemoveListener(SetPlayerPref);
        }
    }
}