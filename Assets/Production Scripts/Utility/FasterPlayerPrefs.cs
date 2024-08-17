using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Vi.Utility
{
    public class FasterPlayerPrefs : MonoBehaviour
    {
        public static FasterPlayerPrefs Singleton { get { return _singleton; } }
        private static FasterPlayerPrefs _singleton;

        public const bool shouldDiscardMessageQueueOnNetworkShutdown = true;

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);

            if (!PlayerPrefs.HasKey(stringPrefKey)) { PlayerPrefs.SetString(stringPrefKey, JsonConvert.SerializeObject(stringPrefs)); }
            if (!PlayerPrefs.HasKey(floatPrefKey)) { PlayerPrefs.SetString(floatPrefKey, JsonConvert.SerializeObject(floatPrefs)); }
            if (!PlayerPrefs.HasKey(intPrefKey)) { PlayerPrefs.SetString(intPrefKey, JsonConvert.SerializeObject(intPrefs)); }
            if (!PlayerPrefs.HasKey(boolPrefKey)) { PlayerPrefs.SetString(boolPrefKey, JsonConvert.SerializeObject(boolPrefs)); }
            if (!PlayerPrefs.HasKey(colorPrefKey)) { PlayerPrefs.SetString(colorPrefKey, JsonConvert.SerializeObject(colorPrefs)); }

            stringPrefs = JsonConvert.DeserializeObject<Dictionary<string, string>>(PlayerPrefs.GetString(stringPrefKey));
            floatPrefs = JsonConvert.DeserializeObject<Dictionary<string, float>>(PlayerPrefs.GetString(floatPrefKey));
            intPrefs = JsonConvert.DeserializeObject<Dictionary<string, int>>(PlayerPrefs.GetString(intPrefKey));
            boolPrefs = JsonConvert.DeserializeObject<Dictionary<string, bool>>(PlayerPrefs.GetString(boolPrefKey));
            colorPrefs = JsonConvert.DeserializeObject<Dictionary<string, Color>>(PlayerPrefs.GetString(colorPrefKey));
        }

        private const string stringPrefKey = "StringPrefs";
        private const string floatPrefKey = "FloatPrefs";
        private const string intPrefKey = "IntPrefs";
        private const string boolPrefKey = "BoolPrefs";
        private const string colorPrefKey = "ColorPrefs";
        private Dictionary<string, string> stringPrefs = new Dictionary<string, string>();
        private Dictionary<string, float> floatPrefs = new Dictionary<string, float>();
        private Dictionary<string, int> intPrefs = new Dictionary<string, int>();
        private Dictionary<string, bool> boolPrefs = new Dictionary<string, bool>();
        private Dictionary<string, Color> colorPrefs = new Dictionary<string, Color>();

        public bool PlayerPrefsWasUpdatedThisFrame { get; private set; } = false;

        private Coroutine resetPlayerPrefsBoolCoroutine;
        private IEnumerator ResetPlayerPrefsWasUpdatedBool()
        {
            yield return null;
            PlayerPrefsWasUpdatedThisFrame = false;
        }

        public void DeleteAll()
        {
            stringPrefs.Clear();
            floatPrefs.Clear();
            intPrefs.Clear();
            boolPrefs.Clear();
            colorPrefs.Clear();

            PlayerPrefs.SetString(stringPrefKey, JsonConvert.SerializeObject(stringPrefs));
            PlayerPrefs.SetString(floatPrefKey, JsonConvert.SerializeObject(floatPrefs));
            PlayerPrefs.SetString(intPrefKey, JsonConvert.SerializeObject(intPrefs));
            PlayerPrefs.SetString(boolPrefKey, JsonConvert.SerializeObject(boolPrefs));
            PlayerPrefs.SetString(colorPrefKey, JsonConvert.SerializeObject(colorPrefs));

            PlayerPrefsWasUpdatedThisFrame = true;
            if (resetPlayerPrefsBoolCoroutine != null) { StopCoroutine(resetPlayerPrefsBoolCoroutine); }
            resetPlayerPrefsBoolCoroutine = StartCoroutine(ResetPlayerPrefsWasUpdatedBool());
        }

        public void DeleteKey(string key)
        {
            if (stringPrefs.ContainsKey(key)) { stringPrefs.Remove(key); }
            if (floatPrefs.ContainsKey(key)) { floatPrefs.Remove(key); }
            if (intPrefs.ContainsKey(key)) { intPrefs.Remove(key); }
            if (boolPrefs.ContainsKey(key)) { boolPrefs.Remove(key); }
            if (colorPrefs.ContainsKey(key)) { colorPrefs.Remove(key); }

            PlayerPrefs.SetString(stringPrefKey, JsonConvert.SerializeObject(stringPrefs));
            PlayerPrefs.SetString(floatPrefKey, JsonConvert.SerializeObject(floatPrefs));
            PlayerPrefs.SetString(intPrefKey, JsonConvert.SerializeObject(intPrefs));
            PlayerPrefs.SetString(boolPrefKey, JsonConvert.SerializeObject(boolPrefs));
            PlayerPrefs.SetString(colorPrefKey, JsonConvert.SerializeObject(colorPrefs));

            PlayerPrefsWasUpdatedThisFrame = true;
            if (resetPlayerPrefsBoolCoroutine != null) { StopCoroutine(resetPlayerPrefsBoolCoroutine); }
            resetPlayerPrefsBoolCoroutine = StartCoroutine(ResetPlayerPrefsWasUpdatedBool());
        }

        public float GetFloat(string key)
        {
            if (floatPrefs.ContainsKey(key))
                return floatPrefs[key];
            else
                return default;
        }

        public int GetInt(string key)
        {
            if (intPrefs.ContainsKey(key))
                return intPrefs[key];
            else
                return default;
        }

        public string GetString(string key)
        {
            if (stringPrefs.ContainsKey(key))
                return stringPrefs[key];
            else
                return default;
        }

        public bool GetBool(string key)
        {
            if (boolPrefs.ContainsKey(key))
                return boolPrefs[key];
            else
                return default;
        }

        public Color GetColor(string key)
        {
            if (colorPrefs.ContainsKey(key))
                return colorPrefs[key];
            else
                return default;
        }

        public bool HasKey(string key)
        {
            if (stringPrefs.ContainsKey(key)) { return true; }
            if (floatPrefs.ContainsKey(key)) { return true; }
            if (intPrefs.ContainsKey(key)) { return true; }
            if (boolPrefs.ContainsKey(key)) { return true; }
            if (colorPrefs.ContainsKey(key)) { return true; }
            return false;
        }

        public void SetFloat(string key, float value)
        {
            if (floatPrefs.ContainsKey(key))
                floatPrefs[key] = value;
            else
                floatPrefs.Add(key, value);
            PlayerPrefs.SetString(floatPrefKey, JsonConvert.SerializeObject(floatPrefs));

            PlayerPrefsWasUpdatedThisFrame = true;
            if (resetPlayerPrefsBoolCoroutine != null) { StopCoroutine(resetPlayerPrefsBoolCoroutine); }
            resetPlayerPrefsBoolCoroutine = StartCoroutine(ResetPlayerPrefsWasUpdatedBool());
        }

        public void SetInt(string key, int value)
        {
            if (intPrefs.ContainsKey(key))
                intPrefs[key] = value;
            else
                intPrefs.Add(key, value);
            PlayerPrefs.SetString(intPrefKey, JsonConvert.SerializeObject(intPrefs));

            PlayerPrefsWasUpdatedThisFrame = true;
            if (resetPlayerPrefsBoolCoroutine != null) { StopCoroutine(resetPlayerPrefsBoolCoroutine); }
            resetPlayerPrefsBoolCoroutine = StartCoroutine(ResetPlayerPrefsWasUpdatedBool());
        }

        public void SetString(string key, string value)
        {
            if (stringPrefs.ContainsKey(key))
                stringPrefs[key] = value;
            else
                stringPrefs.Add(key, value);
            PlayerPrefs.SetString(stringPrefKey, JsonConvert.SerializeObject(stringPrefs));

            PlayerPrefsWasUpdatedThisFrame = true;
            if (resetPlayerPrefsBoolCoroutine != null) { StopCoroutine(resetPlayerPrefsBoolCoroutine); }
            resetPlayerPrefsBoolCoroutine = StartCoroutine(ResetPlayerPrefsWasUpdatedBool());
        }

        public void SetBool(string key, bool value)
        {
            if (boolPrefs.ContainsKey(key))
                boolPrefs[key] = value;
            else
                boolPrefs.Add(key, value);
            PlayerPrefs.SetString(boolPrefKey, JsonConvert.SerializeObject(boolPrefs));

            PlayerPrefsWasUpdatedThisFrame = true;
            if (resetPlayerPrefsBoolCoroutine != null) { StopCoroutine(resetPlayerPrefsBoolCoroutine); }
            resetPlayerPrefsBoolCoroutine = StartCoroutine(ResetPlayerPrefsWasUpdatedBool());
        }

        public void SetColor(string key, Color value)
        {
            if (colorPrefs.ContainsKey(key))
                colorPrefs[key] = value;
            else
                colorPrefs.Add(key, value);
            PlayerPrefs.SetString(colorPrefKey, JsonConvert.SerializeObject(colorPrefs));

            PlayerPrefsWasUpdatedThisFrame = true;
            if (resetPlayerPrefsBoolCoroutine != null) { StopCoroutine(resetPlayerPrefsBoolCoroutine); }
            resetPlayerPrefsBoolCoroutine = StartCoroutine(ResetPlayerPrefsWasUpdatedBool());
        }
    }
}