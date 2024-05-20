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

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);

            if (!PlayerPrefs.HasKey(stringPrefKey)) { PlayerPrefs.SetString(stringPrefKey, JsonConvert.SerializeObject(stringPrefs)); }
            if (!PlayerPrefs.HasKey(floatPrefKey)) { PlayerPrefs.SetString(floatPrefKey, JsonConvert.SerializeObject(floatPrefs)); }
            if (!PlayerPrefs.HasKey(intPrefKey)) { PlayerPrefs.SetString(intPrefKey, JsonConvert.SerializeObject(intPrefs)); }

            stringPrefs = JsonConvert.DeserializeObject<Dictionary<string, string>>(PlayerPrefs.GetString(stringPrefKey));
            floatPrefs = JsonConvert.DeserializeObject<Dictionary<string, float>>(PlayerPrefs.GetString(floatPrefKey));
            intPrefs = JsonConvert.DeserializeObject<Dictionary<string, int>>(PlayerPrefs.GetString(intPrefKey));
        }

        private const string stringPrefKey = "StringPrefs";
        private const string floatPrefKey = "FloatPrefs";
        private const string intPrefKey = "IntPrefs";
        private Dictionary<string, string> stringPrefs = new Dictionary<string, string>();
        private Dictionary<string, float> floatPrefs = new Dictionary<string, float>();
        private Dictionary<string, int> intPrefs = new Dictionary<string, int>();

        public void DeleteAll()
        {
            stringPrefs.Clear();
            floatPrefs.Clear();
            intPrefs.Clear();

            PlayerPrefs.SetString(stringPrefKey, JsonConvert.SerializeObject(stringPrefs));
            PlayerPrefs.SetString(floatPrefKey, JsonConvert.SerializeObject(floatPrefs));
            PlayerPrefs.SetString(intPrefKey, JsonConvert.SerializeObject(intPrefs));
        }

        public void DeleteKey(string key)
        {
            if (stringPrefs.ContainsKey(key)) { stringPrefs.Remove(key); }
            if (floatPrefs.ContainsKey(key)) { floatPrefs.Remove(key); }
            if (intPrefs.ContainsKey(key)) { intPrefs.Remove(key); }

            PlayerPrefs.SetString(stringPrefKey, JsonConvert.SerializeObject(stringPrefs));
            PlayerPrefs.SetString(floatPrefKey, JsonConvert.SerializeObject(floatPrefs));
            PlayerPrefs.SetString(intPrefKey, JsonConvert.SerializeObject(intPrefs));
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

        public bool HasKey(string key)
        {
            if (stringPrefs.ContainsKey(key)) { return true; }
            if (floatPrefs.ContainsKey(key)) { return true; }
            if (intPrefs.ContainsKey(key)) { return true; }
            return false;
        }

        public void SetFloat(string key, float value)
        {
            if (floatPrefs.ContainsKey(key))
                floatPrefs[key] = value;
            else
                floatPrefs.Add(key, value);
            PlayerPrefs.SetString(floatPrefKey, JsonConvert.SerializeObject(floatPrefs));
        }

        public void SetInt(string key, int value)
        {
            if (intPrefs.ContainsKey(key))
                intPrefs[key] = value;
            else
                intPrefs.Add(key, value);
            PlayerPrefs.SetString(intPrefKey, JsonConvert.SerializeObject(intPrefs));
        }

        public void SetString(string key, string value)
        {
            if (stringPrefs.ContainsKey(key))
                stringPrefs[key] = value;
            else
                stringPrefs.Add(key, value);
            PlayerPrefs.SetString(stringPrefKey, JsonConvert.SerializeObject(stringPrefs));
        }
    }
}