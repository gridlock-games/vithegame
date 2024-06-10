using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }
}