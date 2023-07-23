using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using UnityEngine.UI;

namespace LightPat.UI
{
    public class SceneLoadingUI : MonoBehaviour
    {
        [SerializeField] private Text progressText;

        private void Update()
        {
            progressText.text = "Progress: " + Mathf.RoundToInt(ClientManager.Singleton.SceneLoadingProgress * 100).ToString() + "%";
        }
    }
}