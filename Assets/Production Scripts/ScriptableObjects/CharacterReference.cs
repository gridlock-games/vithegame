using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "CharacterReference", menuName = "Production/Character Reference")]
    public class CharacterReference : ScriptableObject
    {
        [SerializeField] private PlayerModelOption[] playerModelOptions;
        [SerializeField] private WeaponOption[] weaponOptions;

        [System.Serializable]
        public class PlayerModelOption
        {
            public string name;
            public Weapon weapon;
            public string role;
            public string characterDescription = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
            public Sprite characterImage;
            public GameObject playerPrefab;
            public GameObject botPrefab;
            public GameObject[] skinOptions;
        }

        [System.Serializable]
        public class WeaponOption
        {
            public RuntimeAnimatorController animationController;
            public Weapon weapon;
        }

        public PlayerModelOption[] GetPlayerModelOptions() { return playerModelOptions; }

        public WeaponOption[] GetWeaponOptions() { return weaponOptions; }
    }
}