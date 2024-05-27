using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class PlayerCard : MonoBehaviour
    {
        [SerializeField] private Text nameDisplay;

        [Header("True Value Images")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private Image defenseFillImage;
        [SerializeField] private Image rageFillImage;

        [Header("Interm Images")]
        [SerializeField] private Image interimHealthFillImage;
        [SerializeField] private Image interimStaminaFillImage;
        [SerializeField] private Image interimDefenseFillImage;
        [SerializeField] private Image interimRageFillImage;

        [Header("Status UI")]
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private StatusIcon statusImagePrefab;

        private Attributes attributes;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        public void Initialize(Attributes attributes)
        {
            this.attributes = attributes;
            canvas.enabled = attributes != null;
        }

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private PlayerUI playerUI;
        private void Start()
        {
            playerUI = GetComponentInParent<PlayerUI>();

            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                StatusIcon statusIcon = Instantiate(statusImagePrefab.gameObject, statusImageParent).GetComponent<StatusIcon>();
                statusIcon.InitializeStatusIcon(status);
                statusIcons.Add(statusIcon);
            }

            healthFillImage.fillAmount = 0;
            staminaFillImage.fillAmount = 0;
            defenseFillImage.fillAmount = 0;
            rageFillImage.fillAmount = 0;

            interimHealthFillImage.fillAmount = 0;
            interimStaminaFillImage.fillAmount = 0;
            interimDefenseFillImage.fillAmount = 0;
            interimRageFillImage.fillAmount = 0;
        }

        public const float fillSpeed = 4;
        private void Update()
        {
            if (!attributes) { canvas.enabled = false; return; }
            if (!PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId())) { return; }

            if (nameDisplay.isActiveAndEnabled) { nameDisplay.text = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).character.name.ToString(); }

            healthFillImage.fillAmount = attributes.GetHP() / attributes.GetMaxHP();
            staminaFillImage.fillAmount = attributes.GetStamina() / attributes.GetMaxStamina();
            defenseFillImage.fillAmount = attributes.GetSpirit() / attributes.GetMaxSpirit();
            rageFillImage.fillAmount = attributes.GetRage() / attributes.GetMaxRage();

            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, attributes.GetHP() / attributes.GetMaxHP(), Time.deltaTime * fillSpeed);
            interimStaminaFillImage.fillAmount = Mathf.Lerp(interimStaminaFillImage.fillAmount, attributes.GetStamina() / attributes.GetMaxStamina(), Time.deltaTime * fillSpeed);
            interimDefenseFillImage.fillAmount = Mathf.Lerp(interimDefenseFillImage.fillAmount, attributes.GetSpirit() / attributes.GetMaxSpirit(), Time.deltaTime * fillSpeed);
            interimRageFillImage.fillAmount = Mathf.Lerp(interimRageFillImage.fillAmount, attributes.GetRage() / attributes.GetMaxRage(), Time.deltaTime * fillSpeed);

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
        }
    }
}