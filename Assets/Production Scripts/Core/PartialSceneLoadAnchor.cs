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

        private Dictionary<Attributes, AsyncOperation> loadingOperations = new Dictionary<Attributes, AsyncOperation>();
        private void Update()
        {
            List<Attributes> attributesToCheck = new List<Attributes>();
            if (NetworkManager.Singleton.IsServer)
            {
                attributesToCheck = PlayerDataManager.Singleton.GetActivePlayers();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                attributesToCheck.Add(PlayerDataManager.Singleton.GetLocalPlayer().Value);
            }
            else
            {
                return;
            }

            foreach (Attributes attributes in attributesToCheck)
            {
                if (!attributes) { continue; }
                if (loadingOperations.ContainsKey(attributes))
                {
                    if (!CheckIfAsyncOperationIsCompleted(loadingOperations[attributes])) { continue; }
                }

                if (SceneManager.GetSceneByName(name).isLoaded)
                {
                    if (Vector3.Distance(attributes.transform.position, transform.position) > distanceThreshold)
                    {
                        if (loadingOperations.ContainsKey(attributes))
                        {
                            loadingOperations[attributes] = SceneManager.UnloadSceneAsync(name, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                        }
                        else
                        {
                            loadingOperations.Add(attributes, SceneManager.UnloadSceneAsync(name, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects));
                        }
                    }
                }
                else
                {
                    if (Vector3.Distance(attributes.transform.position, transform.position) < distanceThreshold)
                    {
                        if (loadingOperations.ContainsKey(attributes))
                        {
                            loadingOperations[attributes] = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
                        }
                        else
                        {
                            loadingOperations.Add(attributes, SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive));
                        }
                    }
                }
            }
        }

        private bool CheckIfAsyncOperationIsCompleted(AsyncOperation asyncOperation)
        {
            if (asyncOperation == null) { return true; }
            if (asyncOperation.isDone) { return true; }
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, distanceThreshold);
        }
    }
}