using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Vi.Core
{
    public class GameInitialization : MonoBehaviour
    {
        [SerializeField] private SceneReference baseSceneReference;

        private void Start()
        {
            StartCoroutine(LoadScenes());
        }

        private IEnumerator LoadScenes()
        {
            Debug.Log(baseSceneReference);

            AsyncOperationHandle downloadSize = Addressables.DownloadDependenciesAsync(baseSceneReference);
            yield return null;
            //yield return new WaitUntil(() => downloadSize.IsDone);
            //Debug.Log(downloadSize.Result);

            //yield return Addressables.LoadSceneAsync(baseSceneReference, LoadSceneMode.Additive);
            //SceneManager.UnloadSceneAsync("Initialization", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        }
    }
}