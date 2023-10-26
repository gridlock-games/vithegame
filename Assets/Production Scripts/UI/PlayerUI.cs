using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using UnityEngine.InputSystem;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private PlayerCard playerCard;
        [SerializeField] private Image ability1Image;
        [SerializeField] private Image ability2Image;
        [SerializeField] private Image ability3Image;
        [SerializeField] private Image ability4Image;

        private WeaponHandler weaponHandler;

        private void Start()
        {
            playerCard.Initialize(GetComponentInParent<Attributes>());

            weaponHandler = GetComponentInParent<WeaponHandler>();

            List<Sprite> abilityImages = weaponHandler.GetWeapon().GetAbilitySprites();

            ability1Image.sprite = abilityImages[0];
            ability2Image.sprite = abilityImages[1];
            ability3Image.sprite = abilityImages[2];
            ability4Image.sprite = abilityImages[3];

            foreach (InputBinding inputBinding in controlsAsset.bindings)
            {
                if (inputBinding.action == "Ability1")
                {
                    ability1Image.GetComponentInChildren<Text>().text = inputBinding.ToDisplayString();
                }
                else if (inputBinding.action == "Ability2")
                {
                    ability2Image.GetComponentInChildren<Text>().text = inputBinding.ToDisplayString();
                }
                else if (inputBinding.action == "Ability3")
                {
                    ability3Image.GetComponentInChildren<Text>().text = inputBinding.ToDisplayString();
                }
                else if (inputBinding.action == "Ability4")
                {
                    ability4Image.GetComponentInChildren<Text>().text = inputBinding.ToDisplayString();
                }
            }
        }
    }
}