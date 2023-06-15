using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace LightPat.Core
{
    public class NetworkManagerDebug : MonoBehaviour
    {
        private void Update()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                //Debug.Log(SceneManager.GetActiveScene().name);
            }
        }
    }
}