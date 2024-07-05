using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.Events;

namespace Vi.Core
{
    public class NetSceneManager : NetworkBehaviour
    {
        [SerializeField] private ScenePayload[] scenePayloads;

        public static bool DoesExist() { return _singleton; }

        public static NetSceneManager Singleton
        {
            get
            {
                if (!_singleton) { Debug.LogError("Net Scene Manager is null"); }
                return _singleton;
            }
        }
        private static NetSceneManager _singleton;

        private NetworkList<int> activeSceneGroupIndicies;

        public void LoadScene(string sceneGroupName)
        {
            if (IsSceneGroupLoaded(sceneGroupName) | IsSceneGroupLoading(sceneGroupName)) { return; }

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

        public void CheckStatus()
        {
            Debug.Log(IsServer + " " + NetworkManager.IsServer + " " + NetworkManager.Singleton.IsServer);
        }

        public Sprite GetSceneGroupIcon(string sceneGroupName)
        {
            int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == sceneGroupName);
            if (sceneGroupIndex == -1) { Debug.LogError("Could not find scene group for: " + sceneGroupName); return null; }
            return scenePayloads[sceneGroupIndex].scenePreviewIcon;
        }

        public struct AsyncOperationUI : System.IEquatable<AsyncOperationUI>
        {
            public string sceneName;
            public AsyncOperationHandle<SceneInstance> asyncOperation;
            public LoadingType loadingType;

            public AsyncOperationUI(ScenePayload scenePayload, AsyncOperationHandle<SceneInstance> asyncOperation, LoadingType loadingType)
            {
                sceneName = scenePayload.name;
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
                        handle.Completed += SceneHandleLoaded;
                        PersistentLocalObjects.Singleton.LoadingOperations.Add(new AsyncOperationUI(scenePayload, handle, AsyncOperationUI.LoadingType.Loading));
                        PersistentLocalObjects.Singleton.SceneHandles.Add(handle);
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
                        handle.Completed += SceneHandleLoaded;
                        PersistentLocalObjects.Singleton.LoadingOperations.Add(new AsyncOperationUI(scenePayload, handle, AsyncOperationUI.LoadingType.Loading));
                        PersistentLocalObjects.Singleton.SceneHandles.Add(handle);
                    }
                    break;
                case SceneType.Environment:
                    AsyncOperationHandle<SceneInstance> handle2 = Addressables.LoadSceneAsync(scenePayload.sceneReferences[0], LoadSceneMode.Additive);
                    handle2.Completed += SceneHandleLoaded;
                    PersistentLocalObjects.Singleton.LoadingOperations.Add(new AsyncOperationUI(scenePayload, handle2, AsyncOperationUI.LoadingType.Loading));
                    PersistentLocalObjects.Singleton.SceneHandles.Add(handle2);
                    break;
                default:
                    Debug.LogError("SceneType: " + scenePayload.sceneType + "has not been implemented yet!");
                    break;
            }
            //Debug.Log("Loading " + scenePayload.name);
            PersistentLocalObjects.Singleton.CurrentlyLoadedScenePayloads.Add(scenePayload);
        }

        public static event UnityAction<Scene> sceneLoaded;
        public static event UnityAction sceneUnloaded;

        private void SceneHandleLoaded(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            // If this scene is part of an environment scene payload
            if (System.Array.Exists(scenePayloads, scenePayload => System.Array.Exists(scenePayload.sceneReferences, sceneReference => sceneReference.SceneName == sceneHandle.Result.Scene.name) & scenePayload.sceneType == SceneType.Environment))
            {
                if (SceneManager.GetActiveScene() != sceneHandle.Result.Scene)
                {
                    SceneManager.SetActiveScene(sceneHandle.Result.Scene);
                }
            }

            PersistentLocalObjects.Singleton.LoadingOperations.RemoveAll(item => item.asyncOperation.IsDone);

            // Need to check singleton because this object may be despawned and not know
            if (NetworkManager.Singleton.IsServer)
            {
                foreach (GameObject g in sceneHandle.Result.Scene.GetRootGameObjects())
                {
                    if (g.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (!networkObject.IsSpawned) { networkObject.Spawn(true); }
                    }
                }
            }

            if (NetworkManager.Singleton.IsClient)
            {
                foreach (GameObject g in sceneHandle.Result.Scene.GetRootGameObjects())
                {
                    if (g.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (!networkObject.IsSpawned) { Destroy(g); }
                    }
                }
            }

            sceneLoaded.Invoke(sceneHandle.Result.Scene);
        }

        private void SceneHandleUnloaded(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            PersistentLocalObjects.Singleton.LoadingOperations.RemoveAll(item => item.asyncOperation.IsDone);
            sceneUnloaded.Invoke();
        }

        private void UnloadAllScenePayloadsOfType(SceneType sceneType)
        {
            switch (sceneType)
            {
                case SceneType.LocalUI:
                    foreach (ScenePayload scenePayload in PersistentLocalObjects.Singleton.CurrentlyLoadedScenePayloads.FindAll(item => item.sceneType == sceneType))
                    {
                        UnloadScenePayload(scenePayload);
                    }
                    break;
                case SceneType.SynchronizedUI:
                    foreach (ScenePayload scenePayload in PersistentLocalObjects.Singleton.CurrentlyLoadedScenePayloads.FindAll(item => item.sceneType == sceneType))
                    {
                        if (IsSpawned)
                        {
                            if (IsServer)
                            {
                                int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == scenePayload.name);
                                if (activeSceneGroupIndicies.Contains(sceneGroupIndex)) { activeSceneGroupIndicies.Remove(sceneGroupIndex); }
                            }
                        }
                        else
                        {
                            UnloadScenePayload(scenePayload);
                        }
                    }
                    break;
                case SceneType.Gameplay:
                    foreach (ScenePayload scenePayload in PersistentLocalObjects.Singleton.CurrentlyLoadedScenePayloads.FindAll(item => item.sceneType == sceneType))
                    {
                        if (IsSpawned)
                        {
                            if (IsServer)
                            {
                                int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == scenePayload.name);
                                if (activeSceneGroupIndicies.Contains(sceneGroupIndex)) { activeSceneGroupIndicies.Remove(sceneGroupIndex); }
                            }
                        }
                        else
                        {
                            UnloadScenePayload(scenePayload);
                        }
                    }
                    break;
                case SceneType.Environment:
                    foreach (ScenePayload scenePayload in PersistentLocalObjects.Singleton.CurrentlyLoadedScenePayloads.FindAll(item => item.sceneType == sceneType))
                    {
                        if (IsSpawned)
                        {
                            if (IsServer)
                            {
                                int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == scenePayload.name);
                                if (activeSceneGroupIndicies.Contains(sceneGroupIndex)) { activeSceneGroupIndicies.Remove(sceneGroupIndex); }
                            }
                        }
                        else
                        {
                            UnloadScenePayload(scenePayload);
                        }
                    }
                    break;
                default:
                    Debug.LogError("SceneType: " + sceneType + "has not been implemented yet!");
                    break;
            }
        }

        private void UnloadScenePayload(ScenePayload scenePayload)
        {
            foreach (SceneReference scene in scenePayload.sceneReferences)
            {
                AsyncOperationHandle<SceneInstance> loadedHandle = PersistentLocalObjects.Singleton.SceneHandles.Find(item => item.Result.Scene.name == scene.SceneName);
                if (!loadedHandle.IsValid()) { continue; }
                AsyncOperationHandle<SceneInstance> unloadHandle = Addressables.UnloadSceneAsync(loadedHandle, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                unloadHandle.Completed += SceneHandleUnloaded;
                PersistentLocalObjects.Singleton.LoadingOperations.Add(new AsyncOperationUI(scenePayload, unloadHandle, AsyncOperationUI.LoadingType.Unloading));
                PersistentLocalObjects.Singleton.SceneHandles.Remove(loadedHandle);
            }
            PersistentLocalObjects.Singleton.CurrentlyLoadedScenePayloads.Remove(scenePayload);
        }

        public override void OnNetworkSpawn()
        {
            activeSceneGroupIndicies.OnListChanged += OnActiveSceneGroupIndiciesChange;

            if (IsServer)
            {
                activeSceneGroupIndicies.Clear();
            }
            else
            {
                for (int i = 0; i < activeSceneGroupIndicies.Count; i++)
                {
                    OnActiveSceneGroupIndiciesChange(new NetworkListEvent<int>() { Index = i, PreviousValue = -1, Type = NetworkListEvent<int>.EventType.Add, Value = activeSceneGroupIndicies[i] });
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            activeSceneGroupIndicies.OnListChanged -= OnActiveSceneGroupIndiciesChange;

            UnloadAllScenePayloadsOfType(SceneType.SynchronizedUI);
            UnloadAllScenePayloadsOfType(SceneType.Gameplay);
            UnloadAllScenePayloadsOfType(SceneType.Environment);
        }

        private void Awake()
        {
            _singleton = this;
            activeSceneGroupIndicies = new NetworkList<int>();
            Debug.Log("NET SCENE MANAGER AWAKE");
        }

        public bool ShouldSpawnPlayer()
        {
            bool gameplaySceneIsLoaded = false;
            foreach (ScenePayload scenePayload in PersistentLocalObjects.Singleton.CurrentlyLoadedScenePayloads.FindAll(item => item.sceneType == SceneType.Gameplay | item.sceneType == SceneType.Environment))
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
            return PersistentLocalObjects.Singleton.LoadingOperations.Count > 0;
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

        private bool IsSceneGroupLoading(string sceneGroupName)
        {
            foreach (AsyncOperationUI asyncOperationUI in PersistentLocalObjects.Singleton.LoadingOperations.FindAll(item => item.sceneName == sceneGroupName))
            {
                if (!asyncOperationUI.asyncOperation.IsDone) { return true; }
            }
            return false;
        }

        public bool IsEnvironmentLoaded()
        {
            return PersistentLocalObjects.Singleton.CurrentlyLoadedScenePayloads.FindAll(item => item.sceneType == SceneType.Environment).Count > 0;
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
                UnloadScenePayload(scenePayloads[networkListEvent.Value]);
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
            public Sprite scenePreviewIcon;
            public SceneReference[] sceneReferences;
        }
    }
}