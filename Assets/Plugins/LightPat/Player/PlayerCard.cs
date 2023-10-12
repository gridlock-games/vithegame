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
        [SerializeField] private Image characterIconImage;
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private GameObject statusImagePrefab;

        private CharacterMelee melee;
        private CharacterStatusManager statusManager;

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
                        hotkeyNum = (i+1).ToString();
                        break;
                    }
                }

                nameTag.SetText(ClientManager.Singleton.GetClient(melee.OwnerClientId).clientName);
                hotKeyNumText.text = hotkeyNum;

                Sprite playerIcon = ClientManager.Singleton.GetPlayerModelOptions()[ClientManager.Singleton.GetClient(melee.OwnerClientId).playerPrefabOptionIndex].characterImage;
                characterIconImage.sprite = playerIcon;
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
            statusManager = melee.GetComponent<CharacterStatusManager>();
            instantiated = true;
        }

        public void UpdateStatusUI()
        {
            foreach (Transform playerIcon in statusImageParent)
            {
                Destroy(playerIcon.gameObject);
            }

            bool found = false;
            CharacterStatusManager.CHARACTER_STATUS missingStatus = CharacterStatusManager.CHARACTER_STATUS.damageMultiplier;
            foreach (var status in statusManager.GetCharacterStatusList())
            {
                GameObject g = Instantiate(statusImagePrefab, statusImageParent);
                foreach (CharacterMeleeUI.StatusUI statusUI in CharacterMeleeUI.staticStatusUIAssignments)
                {
                    if (statusUI.status == status)
                    {
                        g.GetComponent<Image>().sprite = statusUI.sprite;
                        found = true;
                        break;
                    }
                }
                missingStatus = status;
            }

            if (!found & statusManager.GetCharacterStatusList().Count > 0)
            {
                Debug.LogError("You need to assign a character status image for " + missingStatus);
            }
        }

        private void Update()
        {
            if (!instantiated) { return; }

            healthSlider.value = melee.GetHP() / melee.maxHealth;
            if (melee.currentShield) defenseSlider.value = melee.GetDefense() / melee.currentShield.maxDefense.GetValue(gameObject);
            poiseSlider.value = melee.GetPoise() / melee.maxPoise.GetValue(gameObject);

            UpdateStatusUI();
        }
    }
}