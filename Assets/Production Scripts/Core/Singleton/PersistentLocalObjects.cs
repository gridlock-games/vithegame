using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

namespace Vi.Core
{
    public class PersistentLocalObjects : MonoBehaviour
    {
        public static PersistentLocalObjects Singleton { get { return _singleton; } }
        private static PersistentLocalObjects _singleton;

        private void Awake()
        {
            _singleton = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // If on performant
            if (QualitySettings.GetQualityLevel() == 0)
            {
                StartCoroutine(CacheOpaqueTexture((UniversalRenderPipelineAsset)QualitySettings.renderPipeline));
            }
        }

        private IEnumerator CacheOpaqueTexture(UniversalRenderPipelineAsset pipeline)
        {
            pipeline.supportsCameraOpaqueTexture = true;
            for (int i = 0; i < 9; i++) { yield return null; }
            pipeline.supportsCameraOpaqueTexture = false;
        }

        public List<NetSceneManager.ScenePayload> CurrentlyLoadedScenePayloads { get; private set; } = new List<NetSceneManager.ScenePayload>();
        public List<NetSceneManager.AsyncOperationUI> LoadingOperations { get; private set; } = new List<NetSceneManager.AsyncOperationUI>();
        public List<AsyncOperationHandle<SceneInstance>> SceneHandles { get; private set; } = new List<AsyncOperationHandle<SceneInstance>>();

        //private void Update()
        //{
        //    if (NetworkManager.Singleton)
        //    {
        //        if (NetworkManager.Singleton.SpawnManager != null)
        //        {
        //            Debug.Log(string.Join(" - ", NetworkManager.Singleton.SpawnManager.SpawnedObjects));
        //        }
        //    }
        //}
    }
}