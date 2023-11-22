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
        [SerializeField] private InspectorScenePayload[] scenePayloads;

        //public ScenePayload[] SceneDefinitions { get; private set; }

        public static NetworkSceneManager Singleton { get { return _singleton; } }
        private static NetworkSceneManager _singleton;

        private NetworkList<int> activeSceneGroupIndicies;

        public void LoadScene()
        {
            if (!IsServer) { Debug.LogError("Should only call load scene on the server!"); return; }

            activeSceneGroupIndicies.Add(0);
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
                if (scenePayloads[networkListEvent.Index].sceneType == SceneType.Environment)
                {
                    if (IsServer)
                    {
                        SceneManager.LoadSceneAsync(scenePayloads[networkListEvent.Index].sceneNames[0], LoadSceneMode.Additive);
                        //foreach (UnityEditor.SceneAsset scene in scenePayloads[networkListEvent.Index].scenes)
                        //{
                        //    SceneManager.LoadSceneAsync(scene.name, LoadSceneMode.Additive);
                        //}
                    }
                    else
                    {
                        SceneManager.LoadSceneAsync(scenePayloads[networkListEvent.Index].sceneNames[0], LoadSceneMode.Additive);
                    }
                }
                else
                {
                    foreach (string sceneName in scenePayloads[networkListEvent.Index].sceneNames)
                    {
                        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    }
                }
            }
        }

        public enum SceneType
        {
            UI,
            Gameplay,
            Environment
        }

        [System.Serializable]
        public struct InspectorScenePayload
        {
            public SceneType sceneType;
            public string[] sceneNames;

            //public ScenePayload ConvertToScenePayload()
            //{
            //    return new ScenePayload(sceneType, scenes.Select(x => (FixedString512Bytes)x.name));
            //}
        }

        //public struct ScenePayload : INetworkSerializable, System.IEquatable<ScenePayload>
        //{
        //    public SceneType sceneType;
        //    public NativeList<FixedString512Bytes> scenes;

        //    public ScenePayload(SceneType sceneType, IEnumerable<FixedString512Bytes> scenes)
        //    {
        //        this.sceneType = sceneType;
        //        this.scenes = new NativeList<FixedString512Bytes>(Allocator.Persistent);
        //        foreach (FixedString512Bytes scene in scenes)
        //        {
        //            this.scenes.Add(scene);
        //        }
        //    }

        //    public bool Equals(ScenePayload other)
        //    {
        //        return sceneType == other.sceneType & scenes.SequenceEqual(other.scenes);
        //    }

        //    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        //    {
        //        serializer.SerializeValue(ref sceneType);
        //        serializer.SerializeValue(ref scenes);
        //    }
        //}
    }
}