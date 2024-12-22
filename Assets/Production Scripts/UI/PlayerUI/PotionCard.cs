using UnityEngine;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using Vi.Core;
using Vi.Utility;

namespace Vi.UI
{
    public class PotionCard : MonoBehaviour
    {
        [SerializeField] private AnimationHandler.PotionType potionType;
        [SerializeField] private Image inactivePotionIcon;
        [SerializeField] private Text cooldownText;
        [SerializeField] private Text keybindText;
        [SerializeField] private Text potionsLeftText;
        [SerializeField] private Image backgroundImageSquare;
        [SerializeField] private Image backgroundImageCircle;
        [SerializeField] private Image circleMask;

        private bool isPreview;
        public void SetPreviewOn()
        {
            isPreview = true;
            keybindText.text = "";
            cooldownText.text = "";

            inactivePotionIcon.fillAmount = 0;

            potionsLeftText.text = "10";
        }

        public void SetActive(bool isActive)
        {
            if (!canvas) { canvas = GetComponent<Canvas>(); }
            if (canvas.enabled == isActive) { return; }
            canvas.enabled = isActive;
        }

        private float lastOpacityEvaluatedSmoothened = 1;
        public void CrossFadeOpacity(float alpha)
        {
            if (Mathf.Approximately(lastOpacityEvaluatedSmoothened, alpha)) { return; }
            lastOpacityEvaluatedSmoothened = Mathf.MoveTowards(lastOpacityEvaluatedSmoothened, alpha, Time.deltaTime * PlayerUI.alphaTransitionSpeed);
            foreach (Graphic graphic in graphics)
            {
                graphic.color = StringUtility.SetColorAlpha(graphic.color, Mathf.MoveTowards(graphic.color.a, alpha, Time.deltaTime * 5));
            }

            button.interactable = !Mathf.Approximately(alpha, 0);
        }

        private Canvas canvas;
        private CombatAgent combatAgent;

        private Graphic[] graphics;
        private Button button;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            combatAgent = GetComponentInParent<CombatAgent>();

            button = GetComponent<Button>();
            button.onClick.AddListener(() => combatAgent.AnimationHandler.UsePotion(potionType));

            graphics = GetComponentsInChildren<Graphic>();

            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                backgroundImageSquare.gameObject.SetActive(false);
                backgroundImageCircle.gameObject.SetActive(true);
                circleMask.enabled = true;
            }
            else
            {
                backgroundImageSquare.gameObject.SetActive(true);
                backgroundImageCircle.gameObject.SetActive(false);
                circleMask.enabled = false;
            }
        }

        private void Start()
        {
            keybindText.enabled = !FasterPlayerPrefs.IsMobilePlatform;
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