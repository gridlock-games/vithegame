using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using UnityEngine.UI;

namespace Vi.UI
{
    public class CharacterSelectElement : MonoBehaviour
    {
        [SerializeField] private Image characterIconImage;

        public void Initialize(CharacterReference.PlayerModelOption playerModelOption)
        {
            characterIconImage.sprite = playerModelOption.characterImage;
        }

        public void ChangeCharacter()
        {

        }
    }
}