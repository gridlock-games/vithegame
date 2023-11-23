using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System.Linq;

namespace Vi.Core
{
    public class NetworkSceneManager : NetworkBehaviour
    {
        [SerializeField] private ScenePayload[] scenePayloads;

        //public ScenePayload[] SceneDefinitions { get; private set; }

        public static NetworkSceneManager Singleton { get { return _singleton; } }
        private static NetworkSceneManager _singleton;

        private NetworkList<int> activeSceneGroupIndicies;

        public void LoadScene(string sceneGroupName)
        {
            if (!IsServer) { Debug.LogError("Should only call load scene on the server!"); return; }

            int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == sceneGroupName);

            switch (scenePayloads[sceneGroupIndex].sceneType)
            {
                case SceneType.UI:
                    LoadScenePayload(scenePayloads[sceneGroupIndex]);
                    break;
                case SceneType.Gameplay:
                    activeSceneGroupIndicies.Add(sceneGroupIndex);
                    break;
                case SceneType.Environment:
                    activeSceneGroupIndicies.Add(sceneGroupIndex);
                    break;
                default:
                    Debug.LogError("Scene type: " + scenePayloads[sceneGroupIndex].sceneType + " has not been implemented yet!");
                    break;
            }
        }

        public struct AsyncOperationUI
        {
            public string sceneName;
            public AsyncOperation asyncOperation;

            public AsyncOperationUI(string sceneName, AsyncOperation asyncOperation)
            {
                this.sceneName = sceneName;
                this.asyncOperation = asyncOperation;
            }
        }

        public List<AsyncOperationUI> loadingOperations { get; private set; } = new List<AsyncOperationUI>();
        private void LoadScenePayload(ScenePayload scenePayload)
        {
            if (scenePayload.sceneType == SceneType.Environment)
            {
                loadingOperations.Add(new AsyncOperationUI(scenePayload.sceneNames[0], SceneManager.LoadSceneAsync(scenePayload.sceneNames[0], LoadSceneMode.Additive)));
            }
            else
            {
                foreach (string sceneName in scenePayload.sceneNames)
                {
                    loadingOperations.Add(new AsyncOperationUI(sceneName, SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive)));
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            activeSceneGroupIndicies.OnListChanged += OnActiveSceneGroupIndiciesChange;

            for (int i = 0; i < activeSceneGroupIndicies.Count; i++)
            {
                OnActiveSceneGroupIndiciesChange(new NetworkListEvent<int>() { Index = i, PreviousValue = -1, Type = NetworkListEvent<int>.EventType.Add, Value = activeSceneGroupIndicies[i] });
            }
        }

        public override void OnNetworkDespawn()
        {
            activeSceneGroupIndicies.OnListChanged -= OnActiveSceneGroupIndiciesChange;
        }

        private void Awake()
        {
            _singleton = this;
            activeSceneGroupIndicies = new NetworkList<int>();
            //SceneDefinitions = scenePayloads.Select(x => x.ConvertToScenePayload()).ToArray();
        }

        private void OnActiveSceneGroupIndiciesChange(NetworkListEvent<int> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<int>.EventType.Add)
            {
                LoadScenePayload(scenePayloads[networkListEvent.Value]);
            }
        }

        public enum SceneType
        {
            UI,
            Gameplay,
            Environment
        }

        [System.Serializable]
        public struct ScenePayload
        {
            public string name;
            public SceneType sceneType;
            public string[] sceneNames;
        }
    }
}