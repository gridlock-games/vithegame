using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreator.Melee;
using TMPro;
using UnityEngine.UI;

namespace LightPat.Core
{
    public class PlayerCard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameTag;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider defenseSlider;
        [SerializeField] private Slider poiseSlider;

        private CharacterMelee melee;

        private bool instantiated;

        public void Instantiate(CharacterMelee melee, Team team)
        {
            if (ClientManager.Singleton)
            {
                nameTag.SetText(ClientManager.Singleton.GetClient(melee.OwnerClientId).clientName);
            }
            else
            {
                nameTag.SetText("No client manager");
            }

            if (team == Team.Red)
            {
                nameTag.color = Color.red;
            }
            else if (team == Team.Blue)
            {
                nameTag.color = Color.blue;
            }
            else
            {
                nameTag.color = Color.black;
            }

            this.melee = melee;
            instantiated = true;
        }

        private void Update()
        {
            if (!instantiated) { return; }

            healthSlider.value = melee.GetHP() / (float)melee.maxHealth;
            if (melee.currentShield) defenseSlider.value = melee.GetDefense() / melee.currentShield.maxDefense.GetValue(gameObject);
            poiseSlider.value = melee.GetPoise() / melee.maxPoise.GetValue(gameObject);
        }
    }
}