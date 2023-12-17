using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Vi.Core
{
    public class NetSceneManager : NetworkBehaviour
    {
        [SerializeField] private ScenePayload[] scenePayloads;

        public static NetSceneManager Singleton { get { return _singleton; } }
        private static NetSceneManager _singleton;

        private NetworkList<int> activeSceneGroupIndicies;

        public void LoadScene(string sceneGroupName)
        {
            int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == sceneGroupName);

            switch (scenePayloads[sceneGroupIndex].sceneType)
            {
                case SceneType.LocalUI:
                    LoadScenePayload(scenePayloads[sceneGroupIndex]);
                    break;
                case SceneType.SynchronizedUI:
                case SceneType.Gameplay:
                    if (!IsServer) { Debug.LogError("Should only call load scene with scene type " + SceneType.Gameplay + " on the server!"); return; }
                    activeSceneGroupIndicies.Add(sceneGroupIndex);
                    break;
                case SceneType.Environment:
                    if (!IsServer) { Debug.LogError("Should only call load scene with scene type " + SceneType.Environment + " on the server!"); return; }
                    activeSceneGroupIndicies.Add(sceneGroupIndex);
                    break;
                default:
                    Debug.LogError("Scene type: " + scenePayloads[sceneGroupIndex].sceneType + " has not been implemented yet!");
                    break;
            }
        }

        public struct AsyncOperationUI : System.IEquatable<AsyncOperationUI>
        {
            public string sceneName;
            public AsyncOperation asyncOperation;
            public LoadingType loadingType;

            public AsyncOperationUI(string sceneName, AsyncOperation asyncOperation, LoadingType loadingType)
            {
                this.sceneName = sceneName;
                this.asyncOperation = asyncOperation;
                this.loadingType = loadingType;
            }

            public enum LoadingType
            {
                Loading,
                Unloading
            }

            public bool Equals(AsyncOperationUI other)
            {
                return sceneName == other.sceneName & other.loadingType == loadingType;
            }
        }

        public List<AsyncOperationUI> LoadingOperations { get; private set; } = new List<AsyncOperationUI>();
        private void LoadScenePayload(ScenePayload scenePayload)
        {
            switch (scenePayload.sceneType)
            {
                case SceneType.LocalUI:
                    // Unload UI scenes
                    UnloadAllScenePayloadsOfType(SceneType.LocalUI);
                    foreach (string sceneName in scenePayload.sceneNames)
                    {
                        LoadingOperations.Add(new AsyncOperationUI(sceneName, SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive), AsyncOperationUI.LoadingType.Loading));
                    }
                    break;
                case SceneType.SynchronizedUI:
                case SceneType.Gameplay:
                    // Unload UI scenes
                    UnloadAllScenePayloadsOfType(SceneType.LocalUI);
                    UnloadAllScenePayloadsOfType(SceneType.SynchronizedUI);
                    UnloadAllScenePayloadsOfType(SceneType.Gameplay);
                    UnloadAllScenePayloadsOfType(SceneType.Environment);
                    foreach (string sceneName in scenePayload.sceneNames)
                    {
                        LoadingOperations.Add(new AsyncOperationUI(sceneName, SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive), AsyncOperationUI.LoadingType.Loading));
                    }
                    break;
                case SceneType.Environment:
                    LoadingOperations.Add(new AsyncOperationUI(scenePayload.sceneNames[0], SceneManager.LoadSceneAsync(scenePayload.sceneNames[0], LoadSceneMode.Additive), AsyncOperationUI.LoadingType.Loading));
                    break;
                default:
                    Debug.LogError("SceneType: " + scenePayload.sceneType + "has not been implemented yet!");
                    break;
            }
            //Debug.Log("Loading " + scenePayload.name);
            currentlyLoadedScenePayloads.Add(scenePayload);
        }

        private void UnloadAllScenePayloadsOfType(SceneType sceneType)
        {
            //Debug.Log("Unloading all " + sceneType + " Count: " + currentlyLoadedScenePayloads.FindAll(item => item.sceneType == sceneType).Count);
            foreach (ScenePayload scenePayload in currentlyLoadedScenePayloads.FindAll(item => item.sceneType == sceneType))
            {
                foreach (string sceneName in scenePayload.sceneNames)
                {
                    LoadingOperations.Add(new AsyncOperationUI(sceneName, SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects), AsyncOperationUI.LoadingType.Unloading));
                }
            }
            currentlyLoadedScenePayloads.RemoveAll(item => item.sceneType == sceneType);
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

            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnload;

            activeSceneGroupIndicies = new NetworkList<int>();
        }

        private void Update()
        {
            LoadingOperations.RemoveAll(item => item.asyncOperation.isDone);
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            foreach (ScenePayload scenePayload in currentlyLoadedScenePayloads)
            {
                foreach (string sceneName in scenePayload.sceneNames)
                {
                    LoadingOperations.Add(new AsyncOperationUI(sceneName, SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects), AsyncOperationUI.LoadingType.Unloading));
                }
            }
        }

        public bool ShouldSpawnPlayer()
        {
            return currentlyLoadedScenePayloads.FindAll(item => item.sceneType == SceneType.Gameplay).Count > 0;
        }

        private List<ScenePayload> currentlyLoadedScenePayloads = new List<ScenePayload>();
        private void OnSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
        {
            //Debug.Log("Loaded " + scene.name);
            if (IsServer)
            {
                foreach (GameObject g in scene.GetRootGameObjects())
                {
                    if (g.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (!networkObject.IsSpawned) { networkObject.Spawn(true); }
                    }
                }
            }
            else
            {
                foreach (GameObject g in scene.GetRootGameObjects())
                {
                    if (g.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (!networkObject.IsSpawned) { Destroy(g); }
                    }
                }
            }
        }

        private void OnSceneUnload(Scene scene)
        {
            //Debug.Log("Unloaded " + scene.name);
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
            LocalUI,
            SynchronizedUI,
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