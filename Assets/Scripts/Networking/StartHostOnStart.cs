using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class StartHostOnStart : MonoBehaviour
    {
        private void Start()
        {
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Starting Host");
            }
            else
            {
                Debug.Log("Start Host Failed");
            }
        }
    }
}