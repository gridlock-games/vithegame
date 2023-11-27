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

        private readonly float size = 200;
        private readonly int height = 2;

        private void Awake()
        {
            CharacterReference.PlayerModelOption[] playerModelOptions = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions();

            Quaternion rotation = Quaternion.Euler(0, 0, -45);
            int characterIndex = 0;
            for (int x = 0; x < playerModelOptions.Length; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (characterIndex >= playerModelOptions.Length) { return; }

                    Vector3 pos = new Vector3(x * size, y * size, 0);
                    GameObject g = Instantiate(characterSelectElement.gameObject, characterSelectParent);
                    g.transform.localPosition = rotation * pos;
                    g.GetComponent<CharacterSelectElement>().Initialize(this, playerModelOptions[characterIndex].characterImage, characterIndex, 0);
                    characterIndex++;
                }
            }
        }

        public void ResetSkinIndex() { skinIndex = 0; }

        private int skinIndex;
        public void ChangeSkin()
        {
            KeyValuePair<int, Attributes> localKvp = PlayerDataManager.Singleton.GetLocalPlayer();
            PlayerDataManager.PlayerData localPlayerData = PlayerDataManager.Singleton.GetPlayerData(localKvp.Key);
            skinIndex += 1;
            if (skinIndex > PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[localPlayerData.characterIndex].skinOptions.Length - 1) { skinIndex = 0; }
            localKvp.Value.GetComponent<AnimationHandler>().SetCharacter(localPlayerData.characterIndex, skinIndex);
        }
    }
}