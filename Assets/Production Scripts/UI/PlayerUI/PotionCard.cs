using UnityEngine;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using Vi.Core;
using Vi.Utility;

namespace Vi.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PotionCard : MonoBehaviour
    {
        [SerializeField] private AnimationHandler.PotionType potionType;
        [SerializeField] private Image inactivePotionIcon;
        [SerializeField] private Text cooldownText;
        [SerializeField] private Text keybindText;
        [SerializeField] private Text potionsLeftText;

        private bool isPreview;
        public void SetPreviewOn()
        {
            isPreview = true;
            keybindText.text = "";
            cooldownText.text = "";

            inactivePotionIcon.fillAmount = 0;

            potionsLeftText.text = "10";
        }

        public CanvasGroup CanvasGroup { get; private set; }
        private Canvas canvas;
        private Image borderImage;
        private CombatAgent combatAgent;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            borderImage = GetComponent<Image>();
            combatAgent = GetComponentInParent<CombatAgent>();
            CanvasGroup = GetComponent<CanvasGroup>();

            GetComponent<Button>().onClick.AddListener(() => combatAgent.AnimationHandler.UsePotion(potionType));
        }

        public void Initialize(string keybindText)
        {
            this.keybindText.text = keybindText;
        }

        private void Update()
        {
            if (isPreview) { return; }

            float timeLeft = combatAgent.AnimationHandler.GetPotionCooldownTimeLeft(potionType);
            cooldownText.text = timeLeft <= 0 ? "" : StringUtility.FormatDynamicFloatForUI(timeLeft, 1);

            inactivePotionIcon.fillAmount = 1 - combatAgent.AnimationHandler.GetPotionProgress(potionType);

            potionsLeftText.text = combatAgent.AnimationHandler.GetPotionUsesLeft(potionType).ToString();
        }
    }
}