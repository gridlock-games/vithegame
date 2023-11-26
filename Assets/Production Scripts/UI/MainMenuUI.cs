using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;

namespace Vi.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public void StartServer()
        {
            NetworkManager.Singleton.StartServer();
        }

        public void GoToCharacterSelect()
        {
            NetSceneManager.Singleton.LoadScene("Character Select");
        }
    }
}

