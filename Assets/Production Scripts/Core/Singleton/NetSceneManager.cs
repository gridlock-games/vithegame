using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

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
            
            if (sceneGroupIndex == -1) { Debug.LogError("Could not find scene group for: " + sceneGroupName); return; }

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
            public AsyncOperationHandle<SceneInstance> asyncOperation;
            public LoadingType loadingType;

            public AsyncOperationUI(string sceneName, AsyncOperationHandle<SceneInstance> asyncOperation, LoadingType loadingType)
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
        private List<AsyncOperationHandle<SceneInstance>> sceneHandles = new List<AsyncOperationHandle<SceneInstance>>();
        private void LoadScenePayload(ScenePayload scenePayload)
        {
            switch (scenePayload.sceneType)
            {
                case SceneType.LocalUI:
                    // Unload UI scenes
                    UnloadAllScenePayloadsOfType(SceneType.LocalUI);
                    foreach (SceneReference scene in scenePayload.sceneReferences)
                    {
                        AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(scene, LoadSceneMode.Additive);
                        LoadingOperations.Add(new AsyncOperationUI(scene.SceneName, handle, AsyncOperationUI.LoadingType.Loading));
                        sceneHandles.Add(handle);
                    }
                    break;
                case SceneType.SynchronizedUI:
                case SceneType.Gameplay:
                    // Unload UI scenes
                    UnloadAllScenePayloadsOfType(SceneType.LocalUI);
                    UnloadAllScenePayloadsOfType(SceneType.SynchronizedUI);
                    UnloadAllScenePayloadsOfType(SceneType.Gameplay);
                    UnloadAllScenePayloadsOfType(SceneType.Environment);
                    foreach (SceneReference scene in scenePayload.sceneReferences)
                    {
                        AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(scene, LoadSceneMode.Additive);
                        LoadingOperations.Add(new AsyncOperationUI(scene.SceneName, handle, AsyncOperationUI.LoadingType.Loading));
                        sceneHandles.Add(handle);
                    }
                    break;
                case SceneType.Environment:
                    AsyncOperationHandle<SceneInstance> handle2 = Addressables.LoadSceneAsync(scenePayload.sceneReferences[0], LoadSceneMode.Additive);
                    LoadingOperations.Add(new AsyncOperationUI(scenePayload.sceneReferences[0].SceneName, handle2, AsyncOperationUI.LoadingType.Loading));
                    sceneHandles.Add(handle2);
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
                foreach (SceneReference scene in scenePayload.sceneReferences)
                {
                    AsyncOperationHandle<SceneInstance> handle = sceneHandles.Find(item => item.Result.Scene.name == scene.SceneName);
                    if (!handle.IsValid()) { continue; }
                    LoadingOperations.Add(new AsyncOperationUI(scene.SceneName, Addressables.UnloadSceneAsync(handle, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects), AsyncOperationUI.LoadingType.Unloading));
                    sceneHandles.Remove(handle);
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

            UnloadAllScenePayloadsOfType(SceneType.SynchronizedUI);
            UnloadAllScenePayloadsOfType(SceneType.Gameplay);
            UnloadAllScenePayloadsOfType(SceneType.Environment);

            activeSceneGroupIndicies.Clear();
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
            LoadingOperations.RemoveAll(item => item.asyncOperation.IsDone);
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            foreach (ScenePayload scenePayload in currentlyLoadedScenePayloads)
            {
                foreach (SceneReference scene in scenePayload.sceneReferences)
                {
                    AsyncOperationHandle<SceneInstance> handle = sceneHandles.Find(item => item.Result.Scene.name == scene.SceneName);
                    LoadingOperations.Add(new AsyncOperationUI(scene.SceneName, Addressables.UnloadSceneAsync(handle, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects), AsyncOperationUI.LoadingType.Unloading));
                    sceneHandles.Remove(handle);
                }
            }
        }

        public bool ShouldSpawnPlayer()
        {
            bool gameplaySceneIsLoaded = false;
            foreach (ScenePayload scenePayload in currentlyLoadedScenePayloads.FindAll(item => item.sceneType == SceneType.Gameplay | item.sceneType == SceneType.Environment))
            {
                foreach (SceneReference scene in scenePayload.sceneReferences)
                {
                    if (!SceneManager.GetSceneByName(scene.SceneName).isLoaded) { return false; }
                }
                if (scenePayload.sceneType == SceneType.Gameplay) { gameplaySceneIsLoaded = true; }
            }
            return gameplaySceneIsLoaded;
        }

        public bool IsBusyLoadingScenes()
        {
            return LoadingOperations.Count > 0;
        }

        public bool IsSceneGroupLoaded(string sceneGroupName)
        {
            int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == sceneGroupName);
            foreach (SceneReference scene in scenePayloads[sceneGroupIndex].sceneReferences)
            {
                if (!SceneManager.GetSceneByName(scene.SceneName).isLoaded) { return false; }
            }
            return true;
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
                if (IsServer) { StartCoroutine(WebRequestManager.Singleton.UpdateServerProgress(ShouldSpawnPlayer() ? 0 : 1)); }
            }
            else if (networkListEvent.Type == NetworkListEvent<int>.EventType.Remove | networkListEvent.Type == NetworkListEvent<int>.EventType.RemoveAt)
            {
                if (IsServer) { StartCoroutine(WebRequestManager.Singleton.UpdateServerProgress(ShouldSpawnPlayer() ? 0 : 1)); }
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
            public SceneReference[] sceneReferences;
        }
    }
}