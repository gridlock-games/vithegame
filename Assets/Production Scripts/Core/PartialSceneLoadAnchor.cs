using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Vi.Core
{
    public class PartialSceneLoadAnchor : MonoBehaviour
    {
        [SerializeField] private float distanceThreshold = 30;

        //private void Awake()
        //{
        //    if (NetworkManager.Singleton.IsServer) { SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive); }
        //}

        private AsyncOperation loadingOperation;
        private void Update()
        {
            //if (NetworkManager.Singleton.IsServer) { return; }
            if (!NetworkManager.Singleton.LocalClient.PlayerObject) { return; }
            if (loadingOperation != null) { if (!loadingOperation.isDone) { return; } }

            if (SceneManager.GetSceneByName(name).isLoaded)
            {
                if (Vector3.Distance(NetworkManager.Singleton.LocalClient.PlayerObject.transform.position, transform.position) > distanceThreshold)
                {
                    loadingOperation = SceneManager.UnloadSceneAsync(name, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                }
            }
            else
            {
                if (Vector3.Distance(NetworkManager.Singleton.LocalClient.PlayerObject.transform.position, transform.position) < distanceThreshold)
                {
                    loadingOperation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, distanceThreshold);
        }
    }
}