using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.Linq;
using Vi.Utility;
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

        public void LoadScene(params string[] sceneGroupNames)
        {
            foreach (string sceneGroupName in sceneGroupNames)
            {
                if (IsSceneGroupLoaded(sceneGroupName) | IsSceneGroupLoading(sceneGroupName)) { continue; }

                int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == sceneGroupName);

                if (sceneGroupIndex == -1) { Debug.LogError("Could not find scene group for: " + sceneGroupName); continue; }

                switch (scenePayloads[sceneGroupIndex].sceneType)
                {
                    case SceneType.LocalUI:
                        LoadScenePayload(scenePayloads[sceneGroupIndex]);
                        break;
                    case SceneType.SynchronizedUI:
                    case SceneType.Gameplay:
                        if (!IsServer) { Debug.LogError("Should only call load scene with scene type " + SceneType.Gameplay + " on the server!"); continue; }
                        activeSceneGroupIndicies.Add(sceneGroupIndex);
                        break;
                    case SceneType.Environment:
                        if (!IsServer) { Debug.LogError("Should only call load scene with scene type " + SceneType.Environment + " on the server!"); continue; }
                        activeSceneGroupIndicies.Add(sceneGroupIndex);
                        break;
                    default:
                        Debug.LogError("Scene type: " + scenePayloads[sceneGroupIndex].sceneType + " has not been implemented yet!");
                        break;
                }
            }
        }

        public Sprite GetSceneGroupIcon(string sceneGroupName)
        {
            int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == sceneGroupName);
            if (sceneGroupIndex == -1) { Debug.LogError("Could not find scene group for: " + sceneGroupName); return null; }

            if (scenePayloads[sceneGroupIndex].scenePreviewIconOptions == null)
            {
                return null;
            }
            else if (scenePayloads[sceneGroupIndex].scenePreviewIconOptions.Length == 0)
            {
                return null;
            }
            else
            {
                return scenePayloads[sceneGroupIndex].scenePreviewIconOptions[Random.Range(0, scenePayloads[sceneGroupIndex].scenePreviewIconOptions.Length)];
            }
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
                    UnloadAllScenePayloadsOfType(SceneType.LocalUI, SceneType.SynchronizedUI, SceneType.Gameplay, SceneType.Environment);
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

        private void SceneHandleLoaded(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            // If this scene is part of an environment scene payload
            ScenePayload associatedScenePayload = System.Array.Find(scenePayloads, scenePayload => System.Array.Exists(scenePayload.sceneReferences, sceneReference => sceneReference.SceneName == sceneHandle.Result.Scene.name));

            switch (associatedScenePayload.sceneType)
            {
                case SceneType.LocalUI:
                    DiscordManager.UpdateActivity(null, "At " + associatedScenePayload.name);
                    break;
                case SceneType.SynchronizedUI:
                    DiscordManager.UpdateActivity("In " + associatedScenePayload.name + " (" + PlayerDataManager.Singleton.GetPlayerDataListWithoutSpectators().Count + " Players)",
                        PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode()));
                    break;
                case SceneType.Gameplay:
                    if (PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None)
                    {
                        DiscordManager.UpdateActivity("In " + associatedScenePayload.name, null);
                    }
                    else
                    {
                        DiscordManager.UpdateActivity("In " + PlayerDataManager.GetGameModeString(PlayerDataManager.Singleton.GetGameMode()), null);
                    }
                    break;
                case SceneType.Environment:
                    if (SceneManager.GetActiveScene() != sceneHandle.Result.Scene)
                    {
                        SceneManager.SetActiveScene(sceneHandle.Result.Scene);
                    }
                    break;
                default:
                    Debug.LogError("Unsure how to handle scene type " + associatedScenePayload.sceneType);
                    break;
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
            else if (NetworkManager.Singleton.IsClient)
            {
                foreach (GameObject g in sceneHandle.Result.Scene.GetRootGameObjects())
                {
                    if (g.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (!networkObject.IsSpawned)
                        {
                            if (networkObject.TryGetComponent(out PooledObject pooledObject))
                            {
                                ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                            }
                            else
                            {
                                Destroy(g);
                            }
                        }
                    }
                }
            }

            ShouldSpawnPlayerCached = GetShouldSpawnPlayer();
            SetTargetFrameRate();

            EventDelegateManager.InvokeSceneLoadedEvent(sceneHandle.Result.Scene);
        }

        private void SceneHandleUnloaded(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            PersistentLocalObjects.Singleton.LoadingOperations.RemoveAll(item => item.asyncOperation.IsDone);
            ShouldSpawnPlayerCached = GetShouldSpawnPlayer();
            SetTargetFrameRate();
            EventDelegateManager.InvokeSceneUnloadedEvent();
        }

        private void UnloadAllScenePayloadsOfType(params SceneType[] sceneTypes)
        {
            foreach (SceneType sceneType in sceneTypes)
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
                                    activeSceneGroupIndicies.Remove(sceneGroupIndex);
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
                                    activeSceneGroupIndicies.Remove(sceneGroupIndex);
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
                                    activeSceneGroupIndicies.Remove(sceneGroupIndex);
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
        }

        public static void SetTargetFrameRate()
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            if (DoesExist())
            {
                if (Singleton.ShouldSpawnPlayerCached)
                {
                    Application.targetFrameRate = FasterPlayerPrefs.Singleton.GetInt("TargetFrameRate");
                }
                else
                {
                    Application.targetFrameRate = 30;
                }
            }
            else
            {
                Application.targetFrameRate = 30;
            }
#else
            Application.targetFrameRate = FasterPlayerPrefs.Singleton.GetInt("TargetFrameRate");
#endif
        }

        private void UnloadScenePayload(ScenePayload scenePayload)
        {
            foreach (SceneReference scene in scenePayload.sceneReferences)
            {
                AsyncOperationHandle<SceneInstance> loadedHandle = PersistentLocalObjects.Singleton.SceneHandles.Find(item => item.Result.Scene.name == scene.SceneName);
                if (!loadedHandle.IsValid()) { continue; }
                if (loadedHandle.IsDone)
                {
                    foreach (GameObject g in loadedHandle.Result.Scene.GetRootGameObjects())
                    {
                        foreach (PooledObject pooledObject in g.GetComponents<PooledObject>())
                        {
                            if (pooledObject.IsSpawned)
                            {
                                if (pooledObject.TryGetComponent(out NetworkObject networkObject))
                                {
                                    if (networkObject.transform.parent)
                                    {
                                        Debug.LogError(networkObject + " pooled network object that isn't a root object will be destroyed on scene unload! " + loadedHandle.Result.Scene.name);
                                    }
                                    else
                                    {
                                        SceneManager.MoveGameObjectToScene(networkObject.gameObject, SceneManager.GetSceneByName(ObjectPoolingManager.instantiationSceneName));
                                    }

                                    if (NetworkManager.Singleton.IsServer)
                                    {
                                        if (networkObject.IsSpawned)
                                        {
                                            networkObject.Despawn(true);
                                        }
                                        else
                                        {
                                            Debug.LogError(networkObject + " is despawned and will be destroyed on scene unload! Why wasn't it moved to the base scene when it was despawned?");
                                            if (pooledObject.IsSpawned)
                                            {
                                                ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                                            }
                                            else
                                            {
                                                Debug.LogError(pooledObject + " isn't spawned!");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (networkObject.IsSpawned)
                                        {
                                            Debug.LogError("Client unsure how to handle unload event for network object " + networkObject + " is spawned " + networkObject.IsSpawned);
                                        }
                                        else
                                        {
                                            ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                                        }
                                    }
                                }
                                else
                                {
                                    ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                                }
                            }
                            else
                            {
                                Debug.LogError(pooledObject + " in scene " + pooledObject.gameObject.scene.name + " will be destroyed on scene unload! ");
                            }
                        }
                    }
                }
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

        public static UnityAction OnNetSceneManagerDespawn;
        public override void OnNetworkDespawn()
        {
            activeSceneGroupIndicies.OnListChanged -= OnActiveSceneGroupIndiciesChange;

            UnloadAllScenePayloadsOfType(SceneType.SynchronizedUI, SceneType.Gameplay, SceneType.Environment);

            if (OnNetSceneManagerDespawn != null) { OnNetSceneManagerDespawn.Invoke(); }
        }

        private const string defaultActiveSceneName = "Base";

        private void Awake()
        {
            _singleton = this;
            activeSceneGroupIndicies = new NetworkList<int>();
        }

        public bool ShouldSpawnPlayerCached { get; private set; }

        public static bool GetShouldSpawnPlayer()
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

        public static bool IsBusyLoadingScenes()
        {
            return PersistentLocalObjects.Singleton.LoadingOperations.Count > 0;
        }

        public bool IsSceneGroupLoaded(string sceneGroupName)
        {
            int sceneGroupIndex = System.Array.FindIndex(scenePayloads, item => item.name == sceneGroupName);
            if (sceneGroupIndex == -1) { Debug.LogError("Scene group index is -1! " + sceneGroupName); return false; }
            foreach (SceneReference scene in scenePayloads[sceneGroupIndex].sceneReferences)
            {
                if (!SceneManager.GetSceneByName(scene.SceneName).isLoaded) { return false; }
            }
            return true;
        }

        private static bool IsSceneGroupLoading(string sceneGroupName)
        {
            foreach (AsyncOperationUI asyncOperationUI in PersistentLocalObjects.Singleton.LoadingOperations.FindAll(item => item.sceneName == sceneGroupName))
            {
                if (!asyncOperationUI.asyncOperation.IsDone) { return true; }
            }
            return false;
        }

        public static bool IsEnvironmentLoaded()
        {
            return PersistentLocalObjects.Singleton.CurrentlyLoadedScenePayloads.Count(item => item.sceneType == SceneType.Environment) > 0;
        }

        private void OnActiveSceneGroupIndiciesChange(NetworkListEvent<int> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<int>.EventType.Add)
            {
                LoadScenePayload(scenePayloads[networkListEvent.Value]);
                if (IsServer) { PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.UpdateServerProgress(ShouldSpawnPlayerCached ? 0 : 1)); }
            }
            else if (networkListEvent.Type == NetworkListEvent<int>.EventType.Remove | networkListEvent.Type == NetworkListEvent<int>.EventType.RemoveAt)
            {
                UnloadScenePayload(scenePayloads[networkListEvent.Value]);
                if (IsServer) { PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.UpdateServerProgress(ShouldSpawnPlayerCached ? 0 : 1)); }
            }

            if (IsClient)
            {
                if (!isClientSceneLoading)
                {
                    isClientSceneLoading = true;
                    StartCoroutine(WaitForClientSceneLoad());
                }
            }
        }

        private bool isClientSceneLoading;
        private IEnumerator WaitForClientSceneLoad()
        {
            yield return new WaitUntil(() => !IsBusyLoadingScenes());
            Debug.Log("Finished loading network scenes");
            TellServerClientFinishedLoadingRpc(NetworkManager.LocalClientId);
            isClientSceneLoading = false;
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        private void TellServerClientFinishedLoadingRpc(ulong clientId)
        {
            EventDelegateManager.InvokeClientFinishedLoadingScenesEvent(clientId);
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
            public Sprite[] scenePreviewIconOptions;
            public SceneReference[] sceneReferences;
        }
    }
}