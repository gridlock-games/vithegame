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

namespace Vi.UI
{
    public class PlayerCard : MonoBehaviour
    {
        [SerializeField] private Text nameDisplay;

        [Header("Status UI")]
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private StatusIcon statusImagePrefab;

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

        [Header("Spirit UI")]
        [SerializeField] private Text spiritText;
        [SerializeField] private Image spiritFillImage;
        [SerializeField] private Image interimSpiritFillImage;
        [SerializeField] private Image bottomSpiritBorder;
        [SerializeField] private Image spiritBackground;

        [Header("Rage UI")]
        [SerializeField] private RenderTexture ragingRT;
        [SerializeField] private GameObject ragingPreviewPrefab;
        [SerializeField] private RenderTexture rageReadyRT;
        [SerializeField] private GameObject rageReadyPreviewPrefab;
        [SerializeField] private RawImage rageStatusIndicator;
        [SerializeField] private Image rageFillImage;
        [SerializeField] private Image interimRageFillImage;

        [Header("Experience UI")]
        [SerializeField] private Image experienceProgressImage;
        [SerializeField] private Text levelText;

        private CombatAgent combatAgent;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        public void Initialize(CombatAgent combatAgent, bool useTeamColor = false)
        {
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

                List<ActionClip.Status> activeStatuses = combatAgent.StatusAgent.GetActiveStatuses();
                foreach (StatusIcon statusIcon in statusIcons)
                {
                    if (activeStatuses.Contains(statusIcon.Status))
                    {
                        statusIcon.SetActive(true);
                        statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 2);
                    }
                    else
                    {
                        statusIcon.SetActive(false);
                    }
                }

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
        private bool staminaAndSpiritAreDisabled;
        private void DisableStaminaAndSpiritDisplay()
        {
            staminaFillImage.gameObject.SetActive(false);
            interimStaminaFillImage.gameObject.SetActive(false);
            staminaText.gameObject.SetActive(false);
            bottomStaminaBorder.gameObject.SetActive(false);
            staminaBackground.gameObject.SetActive(false);

            spiritFillImage.gameObject.SetActive(false);
            interimSpiritFillImage.gameObject.SetActive(false);
            spiritText.gameObject.SetActive(false);
            bottomSpiritBorder.gameObject.SetActive(false);
            spiritBackground.gameObject.SetActive(false);

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

            staminaAndSpiritAreDisabled = true;
        }

        private static GameObject ragingPreviewInstance;
        private static GameObject rageReadyPreviewInstance;

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();

            experienceProgressImage.fillAmount = 0;

            if (!ragingPreviewInstance) { ragingPreviewInstance = Instantiate(ragingPreviewPrefab, new Vector3(50, 100, 0), Quaternion.identity); }
            if (!rageReadyPreviewInstance) { rageReadyPreviewInstance = Instantiate(rageReadyPreviewPrefab, new Vector3(-50, 100, 0), Quaternion.identity); }
        }

        private void OnEnable()
        {
            if (!IsMainCard())
            {
                DisableStaminaAndSpiritDisplay();

                if (combatAgent)
                {
                    List<ActionClip.Status> activeStatuses = combatAgent.StatusAgent.GetActiveStatuses();
                    foreach (StatusIcon statusIcon in statusIcons)
                    {
                        if (activeStatuses.Contains(statusIcon.Status))
                        {
                            statusIcon.SetActive(true);
                            statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 2);
                        }
                        else
                        {
                            statusIcon.SetActive(false);
                        }
                    }
                }
            }

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

        [SerializeField] private Graphic[] graphicsToTint = new Graphic[0];

        private PlayerUI playerUI;
        private List<Material> tintMaterialInstances = new List<Material>();
        private void Start()
        {
            playerUI = GetComponentInParent<PlayerUI>();

            List<Graphic> graphicsToTint = this.graphicsToTint.ToList();
            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                StatusIcon statusIcon = Instantiate(statusImagePrefab.gameObject, statusImageParent).GetComponent<StatusIcon>();
                statusIcon.InitializeStatusIcon(status);
                statusIcons.Add(statusIcon);
                graphicsToTint.AddRange(statusIcon.GetComponentsInChildren<Graphic>());
            }

            healthFillImage.fillAmount = 0;
            staminaFillImage.fillAmount = 0;
            spiritFillImage.fillAmount = 0;
            rageFillImage.fillAmount = 0;

            interimHealthFillImage.fillAmount = 0;
            interimStaminaFillImage.fillAmount = 0;
            interimSpiritFillImage.fillAmount = 0;
            interimRageFillImage.fillAmount = 0;

            foreach (Graphic graphic in graphicsToTint)
            {
                graphic.material = new Material(graphic.material);
                tintMaterialInstances.Add(graphic.material);
            }
        }

        private static readonly Color aliveTintColor = new Color(1, 1, 1, 1);
        private static readonly Color deathTintColor = new Color(100 / 255f, 100 / 255f, 100 / 255f, 1);

        public const float fillSpeed = 4;

        private Color lastColorTarget = aliveTintColor;

        private float lastHP = -1;
        private float lastStamina = -1;
        private float lastSpirit = -1;
        private float lastRage = -1;

        private float lastMaxHP = -1;
        private float lastMaxStamina = -1;
        private float lastMaxSpirit = -1;
        private float lastMaxRage = -1;

        private void Update()
        {
            if (!combatAgent) { canvas.enabled = false; return; }

            float HP = combatAgent.GetHP();
            if (HP < 0.1f & HP > 0) { HP = 0.1f; }

            float rage = combatAgent.GetRage();

            float maxHP = combatAgent.GetMaxHP();
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
            
            if (!staminaAndSpiritAreDisabled)
            {
                float stamina = combatAgent.GetStamina();
                float spirit = combatAgent.GetSpirit();

                float maxStamina = combatAgent.GetMaxStamina();
                float maxSpirit = combatAgent.GetMaxSpirit();

                if (!Mathf.Approximately(lastStamina, stamina) | !Mathf.Approximately(lastMaxStamina, maxStamina))
                {
                    staminaText.text = "ST " + StringUtility.FormatDynamicFloatForUI(stamina) + " / " + maxStamina.ToString("F0");
                    staminaFillImage.fillAmount = stamina / maxStamina;
                }

                if (!Mathf.Approximately(lastSpirit, spirit) | !Mathf.Approximately(lastMaxSpirit, maxSpirit))
                {
                    spiritText.text = "SP " + StringUtility.FormatDynamicFloatForUI(spirit) + " / " + maxSpirit.ToString("F0");
                    spiritFillImage.fillAmount = spirit / maxSpirit;
                }

                // Interim images - these update every frame
                interimStaminaFillImage.fillAmount = Mathf.Lerp(interimStaminaFillImage.fillAmount, stamina / maxStamina, Time.deltaTime * fillSpeed);
                interimSpiritFillImage.fillAmount = Mathf.Lerp(interimSpiritFillImage.fillAmount, spirit / maxSpirit, Time.deltaTime * fillSpeed);

                lastStamina = stamina;
                lastSpirit = spirit;

                lastMaxStamina = maxStamina;
                lastMaxSpirit = maxSpirit;
            }

            // Interim images - these update every frame
            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, HP / maxHP, Time.deltaTime * fillSpeed);
            interimRageFillImage.fillAmount = Mathf.Lerp(interimRageFillImage.fillAmount, rage / maxRage, Time.deltaTime * fillSpeed);

            if (!playerUI | staminaAndSpiritAreDisabled)
            {
                if (combatAgent.StatusAgent.ActiveStatusesWasUpdatedThisFrame)
                {
                    List<ActionClip.Status> activeStatuses = combatAgent.StatusAgent.GetActiveStatuses();
                    foreach (StatusIcon statusIcon in statusIcons)
                    {
                        if (activeStatuses.Contains(statusIcon.Status))
                        {
                            statusIcon.SetActive(true);
                            statusIcon.transform.SetSiblingIndex(statusImageParent.childCount / 2);
                        }
                        else
                        {
                            statusIcon.SetActive(false);
                        }
                    }
                }
            }

            if (!IsMainCard())
            {
                Color colorTarget = combatAgent.GetAilment() == ActionClip.Ailment.Death ? deathTintColor : aliveTintColor;
                if (colorTarget != lastColorTarget)
                {
                    foreach (Material material in tintMaterialInstances)
                    {
                        material.color = colorTarget;
                    }
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

            if (currentRageStatus != lastRageStatus)
            {
                switch (currentRageStatus)
                {
                    case RageStatus.IsRaging:
                        rageStatusIndicator.texture = ragingRT;
                        rageStatusIndicator.color = new Color(1, 1, 1, levelText.enabled ? 0.4f : 1);
                        break;
                    case RageStatus.CanActivateRage:
                        rageStatusIndicator.texture = rageReadyRT;
                        rageStatusIndicator.color = new Color(1, 1, 1, levelText.enabled ? 0.4f : 1);
                        break;
                    case RageStatus.CannotActivateRage:
                        rageStatusIndicator.texture = null;
                        rageStatusIndicator.color = new Color(1, 1, 1, 0);
                        break;
                    default:
                        Debug.LogError("Unsure how to handle rage status " + currentRageStatus);
                        break;
                }
            }
            lastRageStatus = currentRageStatus;
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