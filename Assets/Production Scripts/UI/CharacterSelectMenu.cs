using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;

namespace Vi.UI
{
    public class CharacterSelectMenu : Menu
    {
        [SerializeField] private Transform characterSelectParent;
        [SerializeField] private CharacterSelectElement characterSelectElement;
        [SerializeField] private CharacterReference characterReference;

        private void Awake()
        {
            PlayerDataManager.PlayerData localPlayerData = PlayerDataManager.Singleton.GetPlayerData(PlayerDataManager.Singleton.GetLocalPlayer().Key);
            CharacterReference.PlayerModelOption[] playerModelOptions = characterReference.GetPlayerModelOptions();
            for (int i = 0; i < playerModelOptions.Length; i++)
            {
                GameObject g = Instantiate(characterSelectElement.gameObject, characterSelectParent);
                g.GetComponent<CharacterSelectElement>().Initialize(playerModelOptions[i].characterImage, i, 0);
                g.transform.localPosition = new Vector3(i * 200, 0, 0);

                g.GetComponent<Button>().interactable = localPlayerData.characterIndex != i;
            }
        }
    }
}