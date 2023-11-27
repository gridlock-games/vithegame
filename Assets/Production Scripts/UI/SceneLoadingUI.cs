using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;

namespace Vi.UI
{
    public class SceneLoadingUI : MonoBehaviour
    {
        [SerializeField] private GameObject progressBarParent;
        [SerializeField] private Image progressBarImage;
        [SerializeField] private Text progressBarText;

        private void Update()
        {
            if (!NetSceneManager.Singleton) { progressBarParent.SetActive(false); return; }
            progressBarParent.SetActive(NetSceneManager.Singleton.LoadingOperations.Count > 0);

            if (NetSceneManager.Singleton.LoadingOperations.Count == 0)
            {
                progressBarText.text = "No scenes loading";
                progressBarImage.fillAmount = 1;
            }

            for (int i = 0; i < NetSceneManager.Singleton.LoadingOperations.Count; i++)
            {
                progressBarText.text = "Loading " + NetSceneManager.Singleton.LoadingOperations[i].sceneName + " | " + (NetSceneManager.Singleton.LoadingOperations.Count-i) + (NetSceneManager.Singleton.LoadingOperations.Count - i > 1 ? " Scenes" : " Scene") + " Left";
                progressBarImage.fillAmount = NetSceneManager.Singleton.LoadingOperations[i].asyncOperation.progress;
            }
        }
    }
}