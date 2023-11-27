using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class CharacterSelectElement : MonoBehaviour
    {
        [SerializeField] private Image characterIconImage;
        [SerializeField] private Button button;

        private CharacterSelectMenu characterSelectMenu;
        private int characterIndex;
        private int skinIndex;
        public void Initialize(CharacterSelectMenu characterSelectMenu, Sprite characterImage, int characterIndex, int skinIndex)
        {
            this.characterSelectMenu = characterSelectMenu;
            characterIconImage.sprite = characterImage;
            this.characterIndex = characterIndex;
            this.skinIndex = skinIndex;
        }

        public void ChangeCharacter()
        {
            PlayerDataManager.Singleton.GetLocalPlayer().Value.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
            characterSelectMenu.ResetSkinIndex();
        }

        private void Update()
        {
            button.interactable = PlayerDataManager.Singleton.GetPlayerData(PlayerDataManager.Singleton.GetLocalPlayer().Key).characterIndex != characterIndex;
        }
    }
}