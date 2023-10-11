namespace GameCreator.Melee
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using GameCreator.Core;
    using LightPat.Core;
    using LightPat.Player;
    using TMPro;

    [AddComponentMenu("UI/Game Creator/Character Melee UI", 0)]
    public class CharacterMeleeUI : MonoBehaviour
    {
        // PROPERTIES: ----------------------------------------------------------------------------

        public TargetCharacter character = new TargetCharacter(TargetCharacter.Target.Player);
        private CharacterMelee melee;
        private AbilityManager abilityManager;

        private Color lowPoiseColor = new Color(3, 0, 147);

        private Color normalPoiseColor = new Color(200, 200, 200);

        public Slider healthSlider;
        public Slider defenseSlider;
        public Slider poiseSlider;
        public Slider rageSlider;

        public Image weaponImageFill;

        [Header("Ability UI")]
        public Image abilityAImageFill;
        public Image abilityBImageFill;
        public Image abilityCImageFill;
        public Image abilityDImageFill;

        [Header("Ammo UI")]
        public TextMeshProUGUI ammoDisplayText;

        [System.Serializable]
        public struct StatusUI
        {
            public CharacterStatusManager.CHARACTER_STATUS status;
            public Sprite sprite;
        }

        [Header("Status UI")]
        public StatusUI[] statusUIAssignments;
        public Transform statusImageParent;
        public GameObject statusImagePrefab;

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
                foreach (StatusUI statusUI in statusUIAssignments)
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

        // INITIALIZERS: --------------------------------------------------------------------------

        public static StatusUI[] staticStatusUIAssignments { get; private set; }

        private TeamIndicator teamIndicator;
        private CharacterStatusManager statusManager;

        private void Awake()
        {
            staticStatusUIAssignments = new StatusUI[statusUIAssignments.Length];
            statusUIAssignments.CopyTo(staticStatusUIAssignments, 0);
        }

        private void Start()
        {
            melee = GetComponentInParent<CharacterMelee>();
            abilityManager = GetComponentInParent<AbilityManager>();
            teamIndicator = GetComponentInParent<TeamIndicator>();
            statusManager = GetComponentInParent<CharacterStatusManager>();
        }

        private void Update()
        {
            UpdateUI();
        }

        private void LateUpdate()
        {
            UpdateWeaponUI();
            UpdateTeammateHPUI();

            if (melee.TryGetComponent(out CharacterShooter characterShooter))
            {
                if (characterShooter.enableReload)
                {
                    ammoDisplayText.SetText(characterShooter.GetCurrentAmmo() + " / " + characterShooter.GetMagSize());
                }
                else
                {
                    ammoDisplayText.SetText("");
                }
            }
            else
            {
                ammoDisplayText.SetText("");
            }
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void UpdateUI()
        {
            healthSlider.value = melee.GetHP() / (float)melee.maxHealth;
            if (melee.currentShield) defenseSlider.value = melee.GetDefense() / melee.currentShield.maxDefense.GetValue(gameObject);
            poiseSlider.value = melee.GetPoise() / melee.maxPoise.GetValue(gameObject);
            rageSlider.value = melee.GetRage() / melee.maxRage.GetValue(gameObject);
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
                float cost = 0f;
                float costRequirement = 0f;

                if (ability.staminaCost > 0)
                {
                    costRequirement = melee.GetPoise();
                    cost = ability.staminaCost;
                }
                else if (ability.hpCost > 0)
                {
                    costRequirement = melee.GetHP();
                    cost = ability.hpCost;
                }
                else
                {
                    costRequirement = melee.GetRage();
                    cost = ability.rageCost;
                }

                switch (ability.skillKey)
                {
                    case KeyCode.Q:
                        abilityAImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityAImageFill.color = costRequirement < cost ? lowPoiseColor : normalPoiseColor;
                        break;
                    case KeyCode.E:
                        abilityBImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityBImageFill.color = costRequirement < cost ? lowPoiseColor : normalPoiseColor;
                        break;
                    case KeyCode.R:
                        abilityCImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityCImageFill.color = costRequirement < cost ? lowPoiseColor : normalPoiseColor;
                        break;
                    case KeyCode.T:
                        abilityDImageFill.sprite = abilityManager.IsAbilityOnCooldown(ability) == false ? ability.skillImageFill : null;
                        abilityDImageFill.color = costRequirement < cost ? lowPoiseColor : normalPoiseColor;
                        break;
                }
            }
        }

        [Header("Teammate Player Cards")]
        [SerializeField] private GameObject playerCardPrefab;
        [SerializeField] private Transform playerCardParent;
        [SerializeField] private float playerCardSpacing = 100;

        private string lastPlayersString;

        private void UpdateTeammateHPUI()
        {
            if (!melee.IsSpawned) { return; }
            if (!ClientManager.Singleton) { return; }

            string playersString = "";
            foreach (var kvp in ClientManager.Singleton.localNetworkPlayers)
            {
                if (!ClientManager.Singleton.GetClientDataDictionary().ContainsKey(kvp.Key)) { continue; }

                playersString += kvp.Key.ToString() + kvp.Value.ToString() + ClientManager.Singleton.GetClient(kvp.Key).team.ToString();
            }

            if (lastPlayersString != playersString)
            {
                foreach (Transform playerIcon in playerCardParent)
                {
                    Destroy(playerIcon.gameObject);
                }

                int counter = 0;
                Team localPlayerTeam = ClientManager.Singleton.GetClient(melee.OwnerClientId).team;
                foreach (KeyValuePair<ulong, GameObject> valuePair in ClientManager.Singleton.localNetworkPlayers)
                {
                    if (valuePair.Value.TryGetComponent(out CharacterMelee melee))
                    {
                        if (this.melee == melee) { continue; }

                        Team playerTeam = ClientManager.Singleton.GetClient(valuePair.Key).team;
                        if (!teamIndicator.teamsAreActive) { continue; }
                        if (playerTeam != localPlayerTeam) { continue; }

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