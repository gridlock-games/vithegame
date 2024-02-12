using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Vi.Core
{
    public class GameInitialization : MonoBehaviour
    {
        [SerializeField] private AssetReference baseSceneReference;

        private void Start()
        {
            StartCoroutine(LoadScenes());
        }

        private IEnumerator LoadScenes()
        {
            yield return Addressables.LoadSceneAsync(baseSceneReference, LoadSceneMode.Additive);
            SceneManager.UnloadSceneAsync("Initialization", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        }
    }
}