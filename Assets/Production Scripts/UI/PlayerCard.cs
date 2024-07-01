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
        [SerializeField] private Image rageFillImage;
        [SerializeField] private Image interimRageFillImage;

        private Attributes attributes;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        public void Initialize(Attributes attributes)
        {
            this.attributes = attributes;
            if (!canvas) { canvas = GetComponent<Canvas>(); }
            canvas.enabled = attributes != null;
        }

        private bool IsMainCard()
        {
            return !nameDisplay.gameObject.activeSelf;
        }

        [SerializeField] private bool isRightSideCard;
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
        }

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
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
        private void Update()
        {
            if (!attributes) { canvas.enabled = false; return; }
            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }

            if (nameDisplay.isActiveAndEnabled)
            {
                PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId());
                nameDisplay.text = PlayerDataManager.Singleton.GetTeamPrefix(playerData.team) + playerData.character.name.ToString();
            }

            float HP = attributes.GetHP();
            float stamina = attributes.GetStamina();
            float spirit = attributes.GetSpirit();
            float rage = attributes.GetRage();

            float maxHP = attributes.GetMaxHP();
            float maxStamina = attributes.GetMaxStamina();
            float maxSpirit = attributes.GetMaxSpirit();
            float maxRage = attributes.GetMaxRage();

            healthText.text = "HP " + (HP < 10 & !Mathf.Approximately(0, HP) ? HP.ToString("F1") : HP.ToString("F0")) + " / " + maxHP.ToString("F0");
            staminaText.text = "ST " + (stamina < 10 & !Mathf.Approximately(0, stamina) ? stamina.ToString("F1") : stamina.ToString("F0")) + " / " + maxStamina.ToString("F0");
            spiritText.text = "SP " + (spirit < 10 & !Mathf.Approximately(0, spirit) ? spirit.ToString("F1") : spirit.ToString("F0")) + " / " + maxSpirit.ToString("F0");

            healthFillImage.fillAmount = HP / maxHP;
            staminaFillImage.fillAmount = stamina / maxStamina;
            spiritFillImage.fillAmount = spirit / maxSpirit;
            rageFillImage.fillAmount = rage / maxRage;

            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, HP / maxHP, Time.deltaTime * fillSpeed);
            interimStaminaFillImage.fillAmount = Mathf.Lerp(interimStaminaFillImage.fillAmount, stamina / maxStamina, Time.deltaTime * fillSpeed);
            interimSpiritFillImage.fillAmount = Mathf.Lerp(interimSpiritFillImage.fillAmount, spirit / maxSpirit, Time.deltaTime * fillSpeed);
            interimRageFillImage.fillAmount = Mathf.Lerp(interimRageFillImage.fillAmount, rage / maxRage, Time.deltaTime * fillSpeed);

            if (!playerUI)
            {
                if (attributes.ActiveStatusesWasUpdatedThisFrame)
                {
                    List<ActionClip.Status> activeStatuses = attributes.GetActiveStatuses();
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
        }
    }
}