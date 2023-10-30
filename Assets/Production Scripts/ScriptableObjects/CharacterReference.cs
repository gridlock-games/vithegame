using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "CharacterReference", menuName = "Production/Character Reference")]
    public class CharacterReference : ScriptableObject
    {
        [SerializeField] private PlayerModelOption[] playerModelOptions;

        [System.Serializable]
        public class PlayerModelOption
        {
            public string name;
            public Weapon weapon;
            public string role;
            public string characterDescription = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
            public Sprite characterImage;
            public GameObject playerPrefab;
            public GameObject[] skinOptions;
        }

        public PlayerModelOption[] GetPlayerModelOptions() { return playerModelOptions; }
    }
}