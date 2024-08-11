using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;
using System.Linq;

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

        private Attributes attributes;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        public void Initialize(Attributes attributes)
        {
            if ((attributes == this.attributes) & (attributes != null)) { return; }
            this.attributes = attributes;
            if (!canvas) { canvas = GetComponent<Canvas>(); }
            canvas.enabled = attributes != null;

            if (setNameTextCoroutine != null) { StopCoroutine(setNameTextCoroutine); }
            if (attributes) { StartCoroutine(SetNameText()); }
        }

        private Coroutine setNameTextCoroutine;

        private IEnumerator SetNameText()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId()));
            nameDisplay.text = PlayerDataManager.Singleton.GetTeamPrefix(attributes.CachedPlayerData.team) + attributes.CachedPlayerData.character.name.ToString();
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

            if (!ragingPreviewInstance) { ragingPreviewInstance = Instantiate(ragingPreviewPrefab, new Vector3(50, 100, 0), Quaternion.identity); }
            if (!rageReadyPreviewInstance) { rageReadyPreviewInstance = Instantiate(rageReadyPreviewPrefab, new Vector3(-50, 100, 0), Quaternion.identity); }
        }

        private void OnEnable()
        {
            if (!IsMainCard()) { DisableStaminaAndSpiritDisplay(); }
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
            if (!attributes) { canvas.enabled = false; return; }

            float HP = attributes.GetHP();
            if (HP < 0.1f & HP > 0) { HP = 0.1f; }

            float rage = attributes.GetRage();

            float maxHP = attributes.GetMaxHP();
            float maxRage = attributes.GetMaxRage();

            if (!Mathf.Approximately(lastHP, HP) | !Mathf.Approximately(lastMaxHP, maxHP))
            {
                healthText.text = "HP " + (HP < 10 & HP > 0 ? HP.ToString("F1") : HP.ToString("F0")) + " / " + maxHP.ToString("F0");
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

            if (!staminaAndSpiritAreDisabled)
            {
                float stamina = attributes.GetStamina();
                if (stamina < 0.1f & stamina > 0) { stamina = 0.1f; }
                float spirit = attributes.GetSpirit();
                if (spirit < 0.1f & spirit > 0) { spirit = 0.1f; }

                float maxStamina = attributes.GetMaxStamina();
                float maxSpirit = attributes.GetMaxSpirit();

                if (!Mathf.Approximately(lastStamina, stamina) | !Mathf.Approximately(lastMaxStamina, maxStamina))
                {
                    staminaText.text = "ST " + (stamina < 10 & stamina > 0 ? stamina.ToString("F1") : stamina.ToString("F0")) + " / " + maxStamina.ToString("F0");
                    staminaFillImage.fillAmount = stamina / maxStamina;
                }

                if (!Mathf.Approximately(lastSpirit, spirit) | !Mathf.Approximately(lastMaxSpirit, maxSpirit))
                {
                    spiritText.text = "SP " + (spirit < 10 & spirit > 0 ? spirit.ToString("F1") : spirit.ToString("F0")) + " / " + maxSpirit.ToString("F0");
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

            if (!playerUI)
            {
                if (attributes.StatusAgent.ActiveStatusesWasUpdatedThisFrame)
                {
                    List<ActionClip.Status> activeStatuses = attributes.StatusAgent.GetActiveStatuses();
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
                Color colorTarget = attributes.GetAilment() == ActionClip.Ailment.Death ? deathTintColor : aliveTintColor;
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
            if (attributes.IsRaging())
            {
                currentRageStatus = RageStatus.IsRaging;
            }
            else if (attributes.CanActivateRage())
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
                        rageStatusIndicator.color = new Color(1, 1, 1, 1);
                        break;
                    case RageStatus.CanActivateRage:
                        rageStatusIndicator.texture = rageReadyRT;
                        rageStatusIndicator.color = new Color(1, 1, 1, 1);
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