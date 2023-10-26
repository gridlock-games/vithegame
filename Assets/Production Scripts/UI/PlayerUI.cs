using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.InputSystem;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private PlayerCard playerCard;
        [Header("Ability Cards")]
        [SerializeField] private AbilityCard ability1;
        [SerializeField] private AbilityCard ability2;
        [SerializeField] private AbilityCard ability3;
        [SerializeField] private AbilityCard ability4;

        private WeaponHandler weaponHandler;

        private void Start()
        {
            playerCard.Initialize(GetComponentInParent<Attributes>());
            weaponHandler = GetComponentInParent<WeaponHandler>();
            List<ActionClip> abilities = weaponHandler.GetWeapon().GetAbilities();
            foreach (InputBinding inputBinding in controlsAsset.bindings)
            {
                if (inputBinding.action == "Ability1")
                {
                    ability1.UpdateCard(abilities[0], inputBinding.ToDisplayString());
                }
                else if (inputBinding.action == "Ability2")
                {
                    ability2.UpdateCard(abilities[1], inputBinding.ToDisplayString());
                }
                else if (inputBinding.action == "Ability3")
                {
                    ability3.UpdateCard(abilities[2], inputBinding.ToDisplayString());
                }
                else if (inputBinding.action == "Ability4")
                {
                    ability4.UpdateCard(abilities[3], inputBinding.ToDisplayString());
                }
            }
        }
    }
}