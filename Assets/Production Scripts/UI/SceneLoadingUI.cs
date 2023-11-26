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

            for (int i = 0; i < NetSceneManager.Singleton.LoadingOperations.Count; i++)
            {
                AsyncOperation asyncOperation = NetSceneManager.Singleton.LoadingOperations[i].asyncOperation;
                if (asyncOperation == null) { continue; }
                if (asyncOperation.isDone) { continue; }

                progressBarText.text = "Loading: " + NetSceneManager.Singleton.LoadingOperations[i].sceneName + " " + (NetSceneManager.Singleton.LoadingOperations.Count-i) + " scenes left";
                progressBarImage.fillAmount = asyncOperation.progress;
            }
        }
    }
}