namespace GameCreator.Melee
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using GameCreator.Core;
    using LightPat.Core;

    [AddComponentMenu("UI/Game Creator/Character Melee UI", 0)]
    public class CharacterMeleeUI : MonoBehaviour
    {
        // PROPERTIES: ----------------------------------------------------------------------------

        public TargetCharacter character = new TargetCharacter(TargetCharacter.Target.Player);
        private CharacterMelee melee;
        private AbilityManager abilityManager;

        private Color lowPoiseColor = new Color(3,0,147);

        private Color normalPoiseColor = new Color(200,200,200);

        public Slider healthSlider;
        public Slider defenseSlider;
        public Slider poiseSlider;
        public Image weaponImageFill;

        public Image abilityAImageFill;

        public Image abilityBImageFill;

        public Image abilityCImageFill;

        // INITIALIZERS: --------------------------------------------------------------------------

        private void Start()
        {
            melee = GetComponentInParent<CharacterMelee>();
            abilityManager = GetComponentInParent<AbilityManager>();
        }

        private void Update()
        {
            this.UpdateUI();
        }

        private void LateUpdate()
        {
            this.UpdateWeaponUI();
            this.UpdateTeammateHPUI();
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void UpdateUI()
        {
            healthSlider.value = melee.GetHP() / (float)melee.maxHealth;
            if (melee.currentShield) defenseSlider.value = melee.GetDefense() / melee.currentShield.maxDefense.GetValue(gameObject);
            poiseSlider.value = melee.GetPoise() / melee.maxPoise.GetValue(gameObject);
        }

        /*
        * Update image of currently equipped Weapon
        */
        private void UpdateWeaponUI()
        {
            if (!melee) { return; }
            if (!melee.currentWeapon) { return; }
            if (!melee.currentWeapon.weaponImage) { return; }
            weaponImageFill.sprite = melee.currentWeapon.weaponImage;

            foreach (Ability ability in abilityManager.GetAbilityInstanceList())
            {
                switch (ability.skillKey)
                {
                    case KeyCode.Q:
                        abilityAImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityAImageFill.color = melee.GetPoise() < ability.staminaCost ? lowPoiseColor : normalPoiseColor;
                        break;
                    case KeyCode.E:
                        abilityBImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityBImageFill.color = melee.GetPoise() < ability.staminaCost ? lowPoiseColor : normalPoiseColor;
                        break;
                    case KeyCode.R:
                        abilityCImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityCImageFill.color = melee.GetPoise() < ability.staminaCost ? lowPoiseColor : normalPoiseColor;
                        break;
                }
            }
        }

        [SerializeField] private GameObject playerCardPrefab;
        [SerializeField] private Transform playerCardParent;
        [SerializeField] private float playerCardSpacing = 100;

        private string lastPlayersString;

        private void UpdateTeammateHPUI()
        {
            string playersString = "";
            foreach (var kvp in ClientManager.Singleton.localNetworkPlayers)
            {
                playersString += kvp.Key.ToString() + kvp.Value.ToString();
            }

            if (lastPlayersString != playersString)
            {
                foreach (Transform playerIcon in playerCardParent)
                {
                    Destroy(playerIcon.gameObject);
                }

                int counter = 0;
                foreach (KeyValuePair<ulong, GameObject> valuePair in ClientManager.Singleton.localNetworkPlayers)
                {
                    if (valuePair.Value.TryGetComponent(out CharacterMelee melee))
                    {
                        if (this.melee == melee) { continue; }

                        Team playerTeam = ClientManager.Singleton.GetClient(valuePair.Key).team;
                        if (playerTeam != Team.Red & playerTeam != Team.Blue) { continue; }
                        if (playerTeam != ClientManager.Singleton.GetClient(melee.OwnerClientId).team) { continue; }

                        GameObject playerCard = Instantiate(playerCardPrefab, playerCardParent);
                        playerCard.GetComponent<PlayerCard>().Instantiate(melee, playerTeam, true);
                        playerCard.transform.localPosition = new Vector3(playerCard.transform.localPosition.x, counter * playerCardSpacing, playerCard.transform.localPosition.z);
                        counter++;
                    }
                }
            }

            lastPlayersString = playersString;
        }
    }
}