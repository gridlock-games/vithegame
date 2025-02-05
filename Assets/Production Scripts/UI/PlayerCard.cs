using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;
using System.Linq;
using Vi.Core.CombatAgents;
using Vi.Utility;
using Vi.Core.GameModeManagers;
using UnityEngine.Video;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Vi.UI
{
    public class PlayerCard : MonoBehaviour
    {
        [SerializeField] private Text nameDisplay;

        [Header("Status UI")]
        [SerializeField] private StatusIconLayoutGroup statusIconLayoutGroup;

        [Header("Health UI")]
        [SerializeField] private Text healthText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image interimHealthFillImage;
        [SerializeField] private Image topHealthBorder;
        [SerializeField] private Image healthBackground;

        [Header("Stamina UI")]
        [SerializeField] private Text staminaText;
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private Image interimStaminaFillImage;
        [SerializeField] private Image bottomStaminaBorder;
        [SerializeField] private Image staminaBackground;

        [Header("Armor UI")]
        [SerializeField] private Text armorText;
        [SerializeField] private Image armorFillImage;
        [SerializeField] private Image interimArmorFillImage;
        [SerializeField] private Image bottomArmorBorder;
        [SerializeField] private Image armorBackground;

        [Header("Rage UI")]
        [SerializeField] private ImageSequencePlayer rageImageSequencePlayer;
        [SerializeField] private AssetReference rageReadyImageSequence;
        [SerializeField] private AssetReference ragingImageSequence;
        [SerializeField] private Image rageStatusIndicator;
        [SerializeField] private Image rageFillImage;
        [SerializeField] private Image interimRageFillImage;

        [Header("Experience UI")]
        [SerializeField] private Image experienceProgressImage;
        [SerializeField] private Text levelText;

        private CombatAgent combatAgent;

        public void Initialize(CombatAgent combatAgent, bool useTeamColor = false)
        {
            if (!IsMainCard())
            {
                if (!combatAgent)
                {
                    statusIconLayoutGroup.Initialize(null);
                }
            }
            
            if ((combatAgent == this.combatAgent) & (combatAgent != null)) { return; }
            this.combatAgent = combatAgent;
            if (!canvas) { canvas = GetComponent<Canvas>(); }
            canvas.enabled = combatAgent != null;

            if (setNameTextCoroutine != null) { StopCoroutine(setNameTextCoroutine); }
            if (combatAgent)
            {
                if (useTeamColor)
                {
                    healthFillImage.color = PlayerDataManager.Singleton.GetRelativeHealthBarColor(combatAgent.GetTeam());
                }

                if (!IsMainCard())
                {
                    statusIconLayoutGroup.Initialize(combatAgent.StatusAgent);
                }

                RefreshLevelingSystem();
                StartCoroutine(SetNameText());
            }
        }

        private Coroutine setNameTextCoroutine;

        private IEnumerator SetNameText()
        {
            if (combatAgent is Attributes attr)
            {
                yield return new WaitUntil(() => PlayerDataManager.Singleton.ContainsId(attr.GetPlayerDataId()));
            }
            nameDisplay.text = PlayerDataManager.Singleton.GetTeamPrefix(combatAgent.GetTeam()) + combatAgent.GetName();
        }

        private bool IsMainCard()
        {
            return !nameDisplay.gameObject.activeSelf;
        }

        [SerializeField] private bool isRightSideCard;
        private bool staminaAndArmorAreDisabled;
        private void DisableStaminaAndArmorDisplay()
        {
            staminaFillImage.gameObject.SetActive(false);
            interimStaminaFillImage.gameObject.SetActive(false);
            staminaText.gameObject.SetActive(false);
            bottomStaminaBorder.gameObject.SetActive(false);
            staminaBackground.gameObject.SetActive(false);

            armorFillImage.gameObject.SetActive(false);
            interimArmorFillImage.gameObject.SetActive(false);
            armorText.gameObject.SetActive(false);
            bottomArmorBorder.gameObject.SetActive(false);
            armorBackground.gameObject.SetActive(false);

            if (isRightSideCard)
            {
                ((RectTransform)healthText.transform).anchoredPosition = ((RectTransform)staminaText.transform).anchoredPosition + new Vector2(60, 0);
                ((RectTransform)topHealthBorder.transform).anchoredPosition = ((RectTransform)bottomStaminaBorder.transform).anchoredPosition;
                ((RectTransform)healthBackground.transform).anchoredPosition = ((RectTransform)staminaBackground.transform).anchoredPosition;
                ((RectTransform)healthFillImage.transform).anchoredPosition = ((RectTransform)staminaFillImage.transform).anchoredPosition;
                ((RectTransform)interimHealthFillImage.transform).anchoredPosition = ((RectTransform)interimStaminaFillImage.transform).anchoredPosition;
            }
            else
            {
                ((RectTransform)healthText.transform).anchoredPosition = ((RectTransform)staminaText.transform).anchoredPosition;
                ((RectTransform)topHealthBorder.transform).anchoredPosition = ((RectTransform)bottomStaminaBorder.transform).anchoredPosition;
                ((RectTransform)healthBackground.transform).anchoredPosition = ((RectTransform)staminaBackground.transform).anchoredPosition;
                ((RectTransform)healthFillImage.transform).anchoredPosition = ((RectTransform)staminaFillImage.transform).anchoredPosition;
                ((RectTransform)interimHealthFillImage.transform).anchoredPosition = ((RectTransform)interimStaminaFillImage.transform).anchoredPosition;
            }

            staminaAndArmorAreDisabled = true;
        }

        private static AsyncOperationHandle<ImageSequence> rageReadyImageSequenceHandle;
        private static AsyncOperationHandle<ImageSequence> ragingImageSequenceHandle;

        private static bool rageReadyImageSequenceLoaded;
        private static bool ragingImageSequenceLoaded;

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            experienceProgressImage.fillAmount = 0;

            if (!rageReadyImageSequenceHandle.IsValid())
            {
                rageReadyImageSequenceHandle = rageReadyImageSequence.LoadAssetAsync<ImageSequence>();
                rageReadyImageSequenceHandle.Completed += (imageSequence) =>
                {
                    rageReadyImageSequenceLoaded = true;
                };
            }
            
            if (!ragingImageSequenceHandle.IsValid())
            {
                ragingImageSequenceHandle = ragingImageSequence.LoadAssetAsync<ImageSequence>();
                ragingImageSequenceHandle.Completed += (imageSequence) =>
                {
                    ragingImageSequenceLoaded = true;
                };
            }
        }

        private void OnEnable()
        {
            if (!IsMainCard())
            {
                DisableStaminaAndArmorDisplay();
            }

            RefreshLevelingSystem();
        }

        private void RefreshLevelingSystem()
        {
            if (GameModeManager.Singleton)
            {
                if (GameModeManager.Singleton.LevelingEnabled)
                {
                    if (combatAgent)
                    {
                        levelText.enabled = true;
                        experienceProgressImage.fillAmount = combatAgent.SessionProgressionHandler.ExperienceAsPercentTowardsNextLevel;
                        levelText.text = combatAgent.SessionProgressionHandler.DisplayLevel;
                    }
                    else
                    {
                        levelText.enabled = false;
                        experienceProgressImage.fillAmount = 0;
                    }
                }
                else
                {
                    levelText.enabled = false;
                    experienceProgressImage.fillAmount = 0;
                }
            }
        }

        private PlayerUI playerUI;
        private Material materialInstance;
        private void Start()
        {
            playerUI = GetComponentInParent<PlayerUI>();

            healthFillImage.fillAmount = 0;
            staminaFillImage.fillAmount = 0;
            armorFillImage.fillAmount = 0;
            rageFillImage.fillAmount = 0;

            interimHealthFillImage.fillAmount = 0;
            interimStaminaFillImage.fillAmount = 0;
            interimArmorFillImage.fillAmount = 0;
            interimRageFillImage.fillAmount = 0;

            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            {
                if (!materialInstance) { materialInstance = new Material(graphic.material); }

                graphic.material = materialInstance;
            }

            rageImageSequencePlayer.ChangeImageSequence(null);
            rageStatusIndicator.color = new Color(1, 1, 1, 0);
        }

        private static readonly Color aliveTintColor = new Color(1, 1, 1, 1);
        private static readonly Color deathTintColor = new Color(100 / 255f, 100 / 255f, 100 / 255f, 1);

        public const float fillSpeed = 4;

        private Color lastColorTarget = aliveTintColor;

        private float lastHP = -1;
        private float lastStamina = -1;
        private float lastArmor = -1;
        private float lastRage = -1;

        private float lastMaxHP = -1;
        private float lastMaxStamina = -1;
        private float lastMaxArmor = -1;
        private float lastMaxRage = -1;

        private void Update()
        {
            if (!combatAgent) { canvas.enabled = false; return; }

            float HP = combatAgent.GetHP();
            if (staminaAndArmorAreDisabled) { HP += combatAgent.GetArmor(); }
            if (HP < 0.1f & HP > 0) { HP = 0.1f; }

            float rage = combatAgent.GetRage();

            float maxHP = combatAgent.GetMaxHP();
            if (staminaAndArmorAreDisabled) { maxHP += combatAgent.GetMaxArmor(); }

            float maxRage = combatAgent.GetMaxRage();

            if (!Mathf.Approximately(lastHP, HP) | !Mathf.Approximately(lastMaxHP, maxHP))
            {
                healthText.text = "HP " + StringUtility.FormatDynamicFloatForUI(HP) + " / " + maxHP.ToString("F0");
                healthFillImage.fillAmount = HP / maxHP;
            }

            if (!Mathf.Approximately(lastRage, rage) | !Mathf.Approximately(lastMaxRage, maxRage))
            {
                rageFillImage.fillAmount = rage / maxRage;
            }

            lastHP = HP;
            lastRage = rage;

            lastMaxHP = maxHP;
            lastMaxRage = maxRage;

            if (levelText.enabled)
            {
                levelText.text = combatAgent.SessionProgressionHandler.DisplayLevel;
                experienceProgressImage.fillAmount = Mathf.Lerp(experienceProgressImage.fillAmount, combatAgent.SessionProgressionHandler.ExperienceAsPercentTowardsNextLevel, Time.deltaTime * fillSpeed);
            }
            
            if (!staminaAndArmorAreDisabled)
            {
                float stamina = combatAgent.GetStamina();
                float armor = combatAgent.GetArmor();

                float maxStamina = combatAgent.GetMaxStamina();
                float maxArmor = combatAgent.GetMaxArmor();

                if (!Mathf.Approximately(lastStamina, stamina) | !Mathf.Approximately(lastMaxStamina, maxStamina))
                {
                    staminaText.text = "ST " + StringUtility.FormatDynamicFloatForUI(stamina) + " / " + maxStamina.ToString("F0");
                    staminaFillImage.fillAmount = stamina / maxStamina;
                }

                if (!Mathf.Approximately(lastArmor, armor) | !Mathf.Approximately(lastMaxArmor, maxArmor))
                {
                    armorText.text = "AR " + StringUtility.FormatDynamicFloatForUI(armor) + " / " + maxArmor.ToString("F0");
                    armorFillImage.fillAmount = armor / maxArmor;
                }

                // Interim images - these update every frame
                interimStaminaFillImage.fillAmount = Mathf.Lerp(interimStaminaFillImage.fillAmount, stamina / maxStamina, Time.deltaTime * fillSpeed);
                interimArmorFillImage.fillAmount = Mathf.Lerp(interimArmorFillImage.fillAmount, armor / maxArmor, Time.deltaTime * fillSpeed);

                lastStamina = stamina;
                lastArmor = armor;

                lastMaxStamina = maxStamina;
                lastMaxArmor = maxArmor;
            }

            // Interim images - these update every frame
            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, HP / maxHP, Time.deltaTime * fillSpeed);
            interimRageFillImage.fillAmount = Mathf.Lerp(interimRageFillImage.fillAmount, rage / maxRage, Time.deltaTime * fillSpeed);

            if (!IsMainCard())
            {
                Color colorTarget = combatAgent.GetAilment() == ActionClip.Ailment.Death ? deathTintColor : aliveTintColor;
                if (colorTarget != lastColorTarget)
                {
                    materialInstance.color = colorTarget;
                }
                lastColorTarget = colorTarget;
            }

            RageStatus currentRageStatus;
            if (combatAgent.IsRaging)
            {
                currentRageStatus = RageStatus.IsRaging;
            }
            else if (combatAgent.CanActivateRage())
            {
                currentRageStatus = RageStatus.CanActivateRage;
            }
            else // Cannot activate rage and we are not raging
            {
                currentRageStatus = RageStatus.CannotActivateRage;
            }

            if (currentRageStatus != lastRageStatus & rageReadyImageSequenceLoaded & ragingImageSequenceLoaded)
            {
                switch (currentRageStatus)
                {
                    case RageStatus.IsRaging:
                        rageImageSequencePlayer.ChangeImageSequence(ragingImageSequenceHandle.Result);
                        rageStatusIndicator.color = new Color(1, 1, 1, levelText.enabled ? 0.4f : 1);
                        break;
                    case RageStatus.CanActivateRage:
                        rageImageSequencePlayer.ChangeImageSequence(rageReadyImageSequenceHandle.Result);
                        rageStatusIndicator.color = new Color(1, 1, 1, levelText.enabled ? 0.4f : 1);
                        break;
                    case RageStatus.CannotActivateRage:
                        rageImageSequencePlayer.ChangeImageSequence(null);
                        rageStatusIndicator.color = new Color(1, 1, 1, 0);
                        break;
                    default:
                        Debug.LogError("Unsure how to handle rage status " + currentRageStatus);
                        break;
                }
            }

            if (rageReadyImageSequenceLoaded & ragingImageSequenceLoaded)
            {
                lastRageStatus = currentRageStatus;
            }
        }

        private RageStatus lastRageStatus = RageStatus.None;

        private enum RageStatus
        {
            None,
            IsRaging,
            CanActivateRage,
            CannotActivateRage
        }
    }
}