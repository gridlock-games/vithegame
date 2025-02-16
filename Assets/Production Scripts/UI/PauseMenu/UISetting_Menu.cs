using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    public class UISetting_Menu : MonoBehaviour
    {
        [SerializeField] private UIModificationMenu UILayoutSetting;
        public void OpenUILayout()
        {
            UILayoutSetting.gameObject.SetActive(true);
        }

        public void CloseUILayout()
        {
            UILayoutSetting.gameObject.SetActive(false);
        }

        [SerializeField] private Slider UIOpacitySlider;
        private void Awake()
        {
            UIOpacitySlider.minValue = FasterPlayerPrefs.IsMobilePlatform ? 0.1f : 0;
        }
    }
}