using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;

namespace Vi.UI
{
    public class SceneLoadingUI : MonoBehaviour
    {
        [Header("Scene Loading")]
        [SerializeField] private GameObject progressBarParent;
        [SerializeField] private Image progressBarImage;
        [SerializeField] private Text progressBarText;
        [Header("Player Object Spawning")]
        [SerializeField] private GameObject spawningPlayerObjectParent;
        [SerializeField] private Text spawningPlayerObjectText;

        private float lastTextChangeTime;
        private void Update()
        {
            if (!NetSceneManager.Singleton) { progressBarParent.SetActive(false); return; }

            if (NetworkManager.Singleton.IsClient | !NetworkManager.Singleton.IsListening)
            {
                spawningPlayerObjectParent.SetActive(NetSceneManager.Singleton.ShouldSpawnPlayer() & !PlayerDataManager.Singleton.GetLocalPlayerObject().Value);
                progressBarParent.SetActive(NetSceneManager.Singleton.LoadingOperations.Count > 0 | spawningPlayerObjectParent.activeSelf);

                if (spawningPlayerObjectParent.activeSelf)
                {
                    string topText = NetSceneManager.Singleton.IsSpawned ? "Spawning Player Object" : "Connecting To Server";
                    if (!spawningPlayerObjectText.text.Contains(topText)) { spawningPlayerObjectText.text = topText; }
                    
                    if (Time.time - lastTextChangeTime > 0.5f)
                    {
                        lastTextChangeTime = Time.time;
                        switch (spawningPlayerObjectText.text.Split(".").Length)
                        {
                            case 1:
                                spawningPlayerObjectText.text = spawningPlayerObjectText.text.Replace(".", "") + ".";
                                break;
                            case 2:
                                spawningPlayerObjectText.text = spawningPlayerObjectText.text.Replace(".", "") + "..";
                                break;
                            case 3:
                                spawningPlayerObjectText.text = spawningPlayerObjectText.text.Replace(".", "") + "...";
                                break;
                            case 4:
                                spawningPlayerObjectText.text = spawningPlayerObjectText.text.Replace(".", "");
                                break;
                        }
                    }
                }
            }
            else
            {
                spawningPlayerObjectParent.SetActive(false);
                progressBarParent.SetActive(NetSceneManager.Singleton.LoadingOperations.Count > 0);
            }
            
            if (NetSceneManager.Singleton.LoadingOperations.Count == 0)
            {
                progressBarText.text = "Scene Loading Complete";
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