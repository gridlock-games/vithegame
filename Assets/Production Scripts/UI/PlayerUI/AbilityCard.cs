using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class AbilityCard : MonoBehaviour
    {
        [SerializeField] private Image abilityIcon;
        [SerializeField] private Image inactiveAbilityIcon;
        [SerializeField] private Text cooldownText;
        [SerializeField] private Text keybindText;
        [SerializeField] private Text staminaCostText;
        [SerializeField] private ActionClip previewAbility;
        [SerializeField] private Image upgradeIcon;
        [SerializeField] private RectTransform upgradeIconActivePosition;
        [SerializeField] private RectTransform upgradeIconInactivePosition;
        [SerializeField] private Animator upgradeAnimationAnimator;
        [SerializeField] private Image backgroundImageSquare;
        [SerializeField] private Image backgroundImageCircle;
        [SerializeField] private Image circleMask;

        public ActionClip Ability { get; private set; }

        public void SetActive(bool isActive)
        {
            if (!canvas) { canvas = GetComponent<Canvas>(); }
            if (canvas.enabled == isActive) { return; }
            canvas.enabled = isActive;
        }

        private float lastOpacityEvaluated = 1;
        private float lastOpacityEvaluatedSmoothened = 1;
        public void CrossFadeOpacity(float alpha)
        {
            if (Mathf.Approximately(lastOpacityEvaluatedSmoothened, alpha)) { return; }
            lastOpacityEvaluatedSmoothened = Mathf.MoveTowards(lastOpacityEvaluatedSmoothened, alpha, Time.deltaTime * PlayerUI.alphaTransitionSpeed);
            foreach (Graphic graphic in graphics)
            {
                if (graphic == upgradeIcon)
                {
                    if (Mathf.Approximately(lastOpacityEvaluatedSmoothened, alpha) | alpha < 1)
                    {
                        graphic.color = StringUtility.SetColorAlpha(graphic.color, alpha);
                    }
                }
                else
                {
                    graphic.color = StringUtility.SetColorAlpha(graphic.color, Mathf.MoveTowards(graphic.color.a, alpha, Time.deltaTime * 5));
                }
            }
            lastOpacityEvaluated = alpha;

            button.interactable = !Mathf.Approximately(alpha, 0);
        }

        private Button button;
        private Canvas canvas;
        private Graphic[] graphics;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            combatAgent = GetComponentInParent<CombatAgent>();

            graphics = GetComponentsInChildren<Graphic>();

            button = GetComponent<Button>();

            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                borderImage = backgroundImageCircle;
                backgroundImageSquare.gameObject.SetActive(false);
                backgroundImageCircle.gameObject.SetActive(true);
                circleMask.enabled = true;
            }
            else
            {
                borderImage = backgroundImageSquare;
                backgroundImageSquare.gameObject.SetActive(true);
                backgroundImageCircle.gameObject.SetActive(false);
                circleMask.enabled = false;
            }
        }

        public void SetPreviewOn()
        {
            if (previewAbility.GetClipType() != ActionClip.ClipType.Ability) { Debug.LogError("Preview ability is not of clip type ability! " + this); return; }
            Ability = previewAbility;
            abilityIcon.sprite = Ability.abilityImageIcon;
            inactiveAbilityIcon.sprite = Ability.abilityImageIcon;
            keybindText.text = previewAbility.name.Replace("Ability", "");
            cooldownText.text = "";
        }

        public void Initialize(ActionClip ability, string keybindText)
        {
            Ability = ability;
            abilityIcon.sprite = ability.abilityImageIcon;
            inactiveAbilityIcon.sprite = ability.abilityImageIcon;
            this.keybindText.text = keybindText;

            lastAbilityLevel = combatAgent.SessionProgressionHandler.GetAbilityLevel(combatAgent.WeaponHandler.GetWeapon(), Ability);
            
            float staminaCost = combatAgent.AnimationHandler.GetStaminaCostOfClip(Ability);
            staminaCostText.text = staminaCost.ToString("F0");
            lastStaminaCost = staminaCost;
        }

        private Image borderImage;
        private Color originalBorderImageColor;
        private Color originalStaminaCostColor;
        private CombatAgent combatAgent;

        private void Start()
        {
            originalBorderImageColor = borderImage.color;
            originalStaminaCostColor = staminaCostText.color;
            keybindText.enabled = !FasterPlayerPrefs.IsMobilePlatform;
        }

        private int lastAbilityLevel = -1;
        private float lastStaminaCost = -1;
        private void Update()
        {
            if (Ability == null) { return; }

            float staminaCost = combatAgent.AnimationHandler.GetStaminaCostOfClip(Ability);
            if (!Mathf.Approximately(lastStaminaCost, staminaCost))
            {
                staminaCostText.text = staminaCost.ToString("F0");
            }
            lastStaminaCost = staminaCost;

            bool canUpgrade = combatAgent.SessionProgressionHandler.CanUpgradeAbility(Ability, combatAgent.WeaponHandler.GetWeapon());
            upgradeIcon.rectTransform.position = Vector3.Lerp(upgradeIcon.transform.position,
                canUpgrade ? upgradeIconActivePosition.position : upgradeIconInactivePosition.position,
                Time.deltaTime * 4);

            upgradeIcon.raycastTarget = canUpgrade;

            if (GameModeManager.Singleton.LevelingEnabled)
            {
                if (combatAgent.SessionProgressionHandler.GetAbilityLevel(combatAgent.WeaponHandler.GetWeapon(), Ability) == -1)
                {
                    borderImage.color = StringUtility.SetColorAlpha(originalBorderImageColor, lastOpacityEvaluated);
                    staminaCostText.color = StringUtility.SetColorAlpha(originalStaminaCostColor, lastOpacityEvaluated);
                    cooldownText.text = "";
                    abilityIcon.fillAmount = 1;
                    return;
                }
            }
            
            float timeLeft = combatAgent.WeaponHandler.GetWeapon().GetAbilityCooldownTimeLeft(Ability);
            cooldownText.text = timeLeft <= 0 ? "" : StringUtility.FormatDynamicFloatForUI(timeLeft, 1);
            abilityIcon.fillAmount = 1 - combatAgent.WeaponHandler.GetWeapon().GetAbilityCooldownProgress(Ability);

            if (!combatAgent.AnimationHandler.AreActionClipRequirementsMet(Ability) | combatAgent.StatusAgent.IsSilenced())
            {
                borderImage.color = StringUtility.SetColorAlpha(Color.red, lastOpacityEvaluated);
                staminaCostText.color = StringUtility.SetColorAlpha(Color.red, lastOpacityEvaluated);
            }
            else
            {
                borderImage.color = StringUtility.SetColorAlpha(originalBorderImageColor, lastOpacityEvaluated);
                staminaCostText.color = StringUtility.SetColorAlpha(originalStaminaCostColor, lastOpacityEvaluated);
            }

            int abilityLevel = combatAgent.SessionProgressionHandler.GetAbilityLevel(combatAgent.WeaponHandler.GetWeapon(), Ability);
            if (abilityLevel != lastAbilityLevel)
            {
                upgradeAnimationAnimator.Play("AbilityCardUpgradeAnimation", 0, 0);
                AudioManager.Singleton.Play2DClip(combatAgent.gameObject, abilityUpgradeSoundEffects[Random.Range(0, abilityUpgradeSoundEffects.Length)], 0.5f);
            }
            lastAbilityLevel = abilityLevel;
        }

        [SerializeField] private AudioClip[] abilityUpgradeSoundEffects;
    }
}