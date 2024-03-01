using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Vi.Core
{
    public class PersistentLocalObjects : MonoBehaviour
    {
        public static PersistentLocalObjects Singleton { get { return _singleton; } }
        private static PersistentLocalObjects _singleton;

        private void Awake()
        {
            _singleton = this;
        }

        public List<NetSceneManager.ScenePayload> CurrentlyLoadedScenePayloads { get; private set; } = new List<NetSceneManager.ScenePayload>();
        public List<NetSceneManager.AsyncOperationUI> LoadingOperations { get; private set; } = new List<NetSceneManager.AsyncOperationUI>();
        public List<AsyncOperationHandle<SceneInstance>> SceneHandles { get; private set; } = new List<AsyncOperationHandle<SceneInstance>>();
    }
}