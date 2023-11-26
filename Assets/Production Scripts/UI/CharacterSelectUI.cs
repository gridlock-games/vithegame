using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core;

namespace Vi.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        public void StartClient()
        {
            NetworkManager.Singleton.StartClient();
        }

        public void GoToTrainingRoom()
        {
            NetworkManager.Singleton.StartHost();
            NetSceneManager.Singleton.LoadScene("Training Room");
        }
    }
}