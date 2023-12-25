using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class CharacterCard : MonoBehaviour
    {
        public Button editButton;
        [SerializeField] private Image characterIcon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;

        public void Initialize(WebRequestManager.Character character)
        {
            nameText.text = character.characterName;
            levelText.text = "Lv." + character.characterLevel.ToString();
        }
    }
}