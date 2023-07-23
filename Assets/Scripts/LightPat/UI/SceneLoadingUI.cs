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
        [SerializeField] private Slider progressSlider;

        private void Update()
        {
            //progressSlider.value = ClientManager.Singleton.SceneLoadingProgress;
            progressText.text = "Progress: " + Mathf.RoundToInt(ClientManager.Singleton.SceneLoadingProgress * 100).ToString() + "%";
        }
    }
}