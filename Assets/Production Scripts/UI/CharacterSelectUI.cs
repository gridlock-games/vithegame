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
    }
}