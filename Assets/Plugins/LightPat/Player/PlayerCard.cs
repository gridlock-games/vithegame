using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreator.Melee;
using TMPro;
using UnityEngine.UI;
using System.Linq;

namespace LightPat.Core
{
    public class PlayerCard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameTag;
        [SerializeField] private Text hotKeyNumText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthImage;
        [SerializeField] private Slider defenseSlider;
        [SerializeField] private Slider poiseSlider;

        private CharacterMelee melee;

        private bool instantiated;

        public void Instantiate(CharacterMelee melee, Team team, bool isTeammateUI)
        {
            if (isTeammateUI)
            {
                defenseSlider.gameObject.SetActive(false);
                poiseSlider.gameObject.SetActive(false);
                healthImage.color = Color.cyan;
            }

            if (ClientManager.Singleton)
            {
                GameObject[] localNetworkPlayers = ClientManager.Singleton.localNetworkPlayers.Values.ToArray();

                string hotkeyNum = "";
                for (int i = 0; i < localNetworkPlayers.Length; i++)
                {
                    if (localNetworkPlayers[i] == melee.gameObject)
                    {
                        hotkeyNum = (i+1).ToString() + ": ";
                        break;
                    }
                }

                nameTag.SetText(ClientManager.Singleton.GetClient(melee.OwnerClientId).clientName);
                hotKeyNumText.text = hotkeyNum;
            }
            else
            {
                nameTag.SetText("No client manager");
            }

            if (!isTeammateUI)
            {
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