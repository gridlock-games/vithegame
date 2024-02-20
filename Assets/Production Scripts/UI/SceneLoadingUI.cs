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
        [SerializeField] private GameObject parentOfAll;
        [Header("Scene Loading")]
        [SerializeField] private GameObject progressBarParent;
        [SerializeField] private Image progressBarImage;
        [SerializeField] private Text progressBarText;
        [Header("Player Object Spawning")]
        [SerializeField] private GameObject spawningPlayerObjectParent;
        [SerializeField] private Text spawningPlayerObjectText;

        private float lastTextChangeTime;
        private float lastDownloadChangeTime;
        private float lastBytesAmount;
        private void Update()
        {
            if (!NetSceneManager.Singleton) { parentOfAll.SetActive(false); return; }

            NetworkObject playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            spawningPlayerObjectParent.SetActive((!NetSceneManager.Singleton.IsSpawned & NetworkManager.Singleton.IsListening) | (NetSceneManager.Singleton.ShouldSpawnPlayer() & !playerObject));
            progressBarParent.SetActive(NetSceneManager.Singleton.LoadingOperations.Count > 0 | (NetSceneManager.Singleton.IsSpawned & NetSceneManager.Singleton.ShouldSpawnPlayer() & !playerObject));

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

            parentOfAll.SetActive(progressBarParent.activeSelf | spawningPlayerObjectParent.activeSelf);

            if (NetSceneManager.Singleton.LoadingOperations.Count == 0)
            {
                progressBarText.text = "Scene Loading Complete";
                progressBarImage.fillAmount = 1;
            }

            for (int i = 0; i < NetSceneManager.Singleton.LoadingOperations.Count; i++)
            {
                if (!NetSceneManager.Singleton.LoadingOperations[i].asyncOperation.IsValid()) { continue; }

                if (NetSceneManager.Singleton.LoadingOperations[i].asyncOperation.GetDownloadStatus().TotalBytes >= 0)
                {
                    progressBarText.text = (NetSceneManager.Singleton.LoadingOperations[i].loadingType == NetSceneManager.AsyncOperationUI.LoadingType.Loading ? "Loading " : "Unloading ") + NetSceneManager.Singleton.LoadingOperations[i].sceneName + " | " + (NetSceneManager.Singleton.LoadingOperations.Count - i) + (NetSceneManager.Singleton.LoadingOperations.Count - i > 1 ? " Scenes" : " Scene") + " Left";
                }
                else // If this scene has not been downloaded
                {
                    float downloadRate = 0;
                    float totalMB = NetSceneManager.Singleton.LoadingOperations[i].asyncOperation.GetDownloadStatus().TotalBytes * 0.000001f;

                    float downloadedMB = NetSceneManager.Singleton.LoadingOperations[i].asyncOperation.GetDownloadStatus().DownloadedBytes * 0.000001f;

                    if (Time.time - lastDownloadChangeTime >= 1)
                    {
                        downloadRate = downloadedMB - lastBytesAmount;
                        lastBytesAmount = downloadedMB;
                        lastDownloadChangeTime = Time.time;
                    }

                    progressBarText.text = downloadedMB.ToString("F2") + " MB / " + totalMB.ToString("F2") + " MB" + " (" + downloadRate.ToString("F2") + "Mbps)";
                }

                progressBarImage.fillAmount = NetSceneManager.Singleton.LoadingOperations[i].asyncOperation.IsDone ? 1 : NetSceneManager.Singleton.LoadingOperations[i].asyncOperation.PercentComplete;
            }
        }
    }
}