using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    [RequireComponent(typeof(InputField))]
    public class SetIntegerPlayerPrefFromInputField : MonoBehaviour
    {
        [SerializeField] private string playerPrefName;

        private void SetPlayerPref(string value)
        {
            value = Regex.Replace(value, @"[^0-9]", "");
            inputField.SetTextWithoutNotify(value);

            if (int.TryParse(value, out int result))
            {
                FasterPlayerPrefs.Singleton.SetInt(playerPrefName, result);
            }
            else
            {
                Debug.LogWarning("Unable to parse integer from string " + value);
            }
        }

        private InputField inputField;
        private void Awake()
        {
            inputField = GetComponent<InputField>();
        }

        private void OnEnable()
        {
            if (FasterPlayerPrefs.Singleton.HasInt(playerPrefName))
            {
                inputField.SetTextWithoutNotify(FasterPlayerPrefs.Singleton.GetInt(playerPrefName).ToString());
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