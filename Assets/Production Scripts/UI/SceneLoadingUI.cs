using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;
using Vi.Isolated;
using Vi.Utility;
using System.Linq;

namespace Vi.UI
{
    public class SceneLoadingUI : MonoBehaviour
    {
        [Header("Scene Loading")]
        [SerializeField] private GameObject progressBarParent;
        [SerializeField] private Image progressBarImage;
        [SerializeField] private Text progressBarText;
        [SerializeField] private Text scenesLeftText;
        [Header("Player Object Spawning")]
        [SerializeField] private GameObject spawningPlayerObjectParent;
        [SerializeField] private Text spawningPlayerObjectText;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private SceneLoadingInfoUI backgroundImageSelector;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            backgroundImageSelector = GetComponent<SceneLoadingInfoUI>();
        }

        private void OnEnable()
        {
            NetSceneManager.OnNetSceneManagerDespawn += OnNetSceneManagerDespawn;
        }

        private void OnDisable()
        {
            NetSceneManager.OnNetSceneManagerDespawn -= OnNetSceneManagerDespawn;
        }

        private void OnNetSceneManagerDespawn()
        {
            UpdateUI(false);
            Canvas.ForceUpdateCanvases();
        }

        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera)
            {
                if (mainCamera.gameObject.CompareTag("MainCamera"))
                {
                    return;
                }
            }
            mainCamera = Camera.main;
        }

        NetworkCallbackManager networkCallbackManager;
        private void Start()
        {
            networkCallbackManager = FindFirstObjectByType<NetworkCallbackManager>();
            if (!networkCallbackManager)
            {
                Debug.LogError("Can't find network callback manager");
            }
        }

        private const float alphaLerpSpeed = 8;

        private float lastTextChangeTime;
        private float lastDownloadChangeTime;
        private float lastBytesAmount;

        private void Update()
        {
            UpdateUI(true);
        }

        public static bool IsDisplaying { get; private set; }
        private void UpdateUI(bool fade)
        {
            FindMainCamera();

            string topText = "";
            if (NetworkManager.Singleton)
            {
                if (NetworkManager.Singleton.ShutdownInProgress)
                {
                    spawningPlayerObjectParent.SetActive(true);
                    topText = "Network Shutdown In Progress";
                }
                else if (NetworkManager.Singleton.IsConnectedClient & NetSceneManager.DoesExist())
                {
                    spawningPlayerObjectParent.SetActive(NetSceneManager.Singleton.ShouldSpawnPlayerCached & !mainCamera);

                    if (PlayerDataManager.DoesExist())
                    {
                        topText = PlayerDataManager.Singleton.IsWaitingForSpawnPoint() ? "Waiting For Good Spawn Point" : "Spawning Player Object";
                    }
                    else
                    {
                        topText = "Spawning Player Object";
                    }
                }
                else if (NetworkManager.Singleton.IsListening & NetworkManager.Singleton.IsClient)
                {
                    spawningPlayerObjectParent.SetActive(true);
                    topText = "Connecting To Server";
                }
                else
                {
                    spawningPlayerObjectParent.SetActive(false);
                }
            }
            else
            {
                spawningPlayerObjectParent.SetActive(false);
            }

            bool mainMenuLoading = false;
            if (!AudioManager.AudioConfigurationApplied)
            {
                mainMenuLoading = true;
            }
            else if (!networkCallbackManager.NetworkManagerLoadingOperation.IsDone)
            {
                mainMenuLoading = true;
            }
            else if (!networkCallbackManager.LoadedNetSceneManagerPrefab.IsDone)
            {
                mainMenuLoading = true;
            }
            else if (networkCallbackManager.NetworkPrefabsLoading.Count(item => !item.IsDone | !item.IsValid()) > 0)
            {
                mainMenuLoading = true;
            }
            
            progressBarParent.SetActive(mainMenuLoading | NetSceneManager.IsBusyLoadingScenes() | spawningPlayerObjectParent.activeSelf);

            if (spawningPlayerObjectParent.activeSelf)
            {
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

            float alphaTarget = progressBarParent.activeSelf | spawningPlayerObjectParent.activeSelf ? 1 : 0;
            canvasGroup.alpha = fade ? Mathf.Lerp(canvasGroup.alpha, alphaTarget, Time.deltaTime * alphaLerpSpeed) : alphaTarget;

            scenesLeftText.text = "";
            if (mainMenuLoading)
            {
                if (!AudioManager.AudioConfigurationApplied)
                {
                    progressBarText.text = "Applying Audio Configuration";
                    progressBarImage.fillAmount = 0;
                }
                if (!networkCallbackManager.NetworkManagerLoadingOperation.IsDone)
                {
                    progressBarText.text = "Loading Network Manager " + (networkCallbackManager.NetworkManagerLoadingOperation.PercentComplete * 100).ToString("F0") + "%";
                    progressBarImage.fillAmount = networkCallbackManager.NetworkManagerLoadingOperation.PercentComplete;
                }
                else if (!networkCallbackManager.LoadedNetSceneManagerPrefab.IsDone)
                {
                    progressBarText.text = "Loading Scene Manager " + (networkCallbackManager.LoadedNetSceneManagerPrefab.PercentComplete * 100).ToString("F0") + "%";
                    progressBarImage.fillAmount = networkCallbackManager.LoadedNetSceneManagerPrefab.PercentComplete;
                }
                else
                {
                    float percentComplete = networkCallbackManager.NetworkPrefabsLoading.Count(item => item.IsDone & item.IsValid()) / (float)networkCallbackManager.NetworkPrefabsLoading.Length;
                    progressBarText.text = "Prepare Yourself, Lifeless " + (percentComplete * 100).ToString("F0") + "%";
                    progressBarImage.fillAmount = percentComplete;
                }
            }
            else if (PersistentLocalObjects.Singleton.LoadingOperations.Count == 0)
            {
                bool evaluated = false;
                if (NetworkManager.Singleton)
                {
                    if (NetworkManager.Singleton.ShutdownInProgress)
                    {
                        progressBarText.text = "";
                        progressBarImage.fillAmount = 0;
                    }
                }

                if (!evaluated)
                {
                    progressBarText.text = "Scene Loading Complete";
                    progressBarImage.fillAmount = 1;
                }
            }

            for (int i = 0; i < PersistentLocalObjects.Singleton.LoadingOperations.Count; i++)
            {
                if (!PersistentLocalObjects.Singleton.LoadingOperations[i].asyncOperation.IsValid()) { continue; }

                if (PersistentLocalObjects.Singleton.LoadingOperations[i].asyncOperation.GetDownloadStatus().TotalBytes >= 0)
                {
                    progressBarText.text = (PersistentLocalObjects.Singleton.LoadingOperations[i].loadingType == NetSceneManager.AsyncOperationUI.LoadingType.Loading ? "Loading " : "Unloading ") + PersistentLocalObjects.Singleton.LoadingOperations[i].sceneName;
                    scenesLeftText.text = (PersistentLocalObjects.Singleton.LoadingOperations.Count - i) + (PersistentLocalObjects.Singleton.LoadingOperations.Count - i > 1 ? " Scenes" : " Scene") + " Left";
                }
                else // If this scene has not been downloaded
                {
                    float downloadRate = 0;
                    float totalMB = PersistentLocalObjects.Singleton.LoadingOperations[i].asyncOperation.GetDownloadStatus().TotalBytes * 0.000001f;

                    float downloadedMB = PersistentLocalObjects.Singleton.LoadingOperations[i].asyncOperation.GetDownloadStatus().DownloadedBytes * 0.000001f;

                    if (Time.time - lastDownloadChangeTime >= 1)
                    {
                        downloadRate = downloadedMB - lastBytesAmount;
                        lastBytesAmount = downloadedMB;
                        lastDownloadChangeTime = Time.time;
                    }

                    progressBarText.text = downloadedMB.ToString("F2") + " MB / " + totalMB.ToString("F2") + " MB" + " (" + downloadRate.ToString("F2") + "Mbps)";
                }

                progressBarImage.fillAmount = PersistentLocalObjects.Singleton.LoadingOperations[i].asyncOperation.IsDone ? 1 : PersistentLocalObjects.Singleton.LoadingOperations[i].asyncOperation.PercentComplete;
            }

            OnEndUpdateUI();
        }

        private void OnEndUpdateUI()
        {
            canvas.enabled = canvasGroup.alpha > 0.05f;
            if (canvas.enabled & !IsDisplaying) { backgroundImageSelector.ChangeBackground(); }
            IsDisplaying = canvas.enabled;
        }
    }
}