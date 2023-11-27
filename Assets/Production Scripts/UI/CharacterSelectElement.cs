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

        private int characterIndex;
        private int skinIndex;
        public void Initialize(Sprite characterImage, int characterIndex, int skinIndex)
        {
            characterIconImage.sprite = characterImage;
            this.characterIndex = characterIndex;
            this.skinIndex = skinIndex;
        }

        public void ChangeCharacter()
        {
            PlayerDataManager.Singleton.GetLocalPlayer().Value.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
        }
    }
}