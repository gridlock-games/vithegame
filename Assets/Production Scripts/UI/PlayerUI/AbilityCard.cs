using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.GameModeManagers;
using System.Collections;

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
        [SerializeField] private Image viLogoUpgradeIcon;
        [SerializeField] private Image backgroundImageSquare;
        [SerializeField] private Image backgroundImageCircle;
        [SerializeField] private Image circleMask;
        [SerializeField] private Image stackBackground;
        [SerializeField] private Image stackCircleBackground;
        [SerializeField] private Text stackText;

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
                else if (graphic == viLogoUpgradeIcon)
                {
                    graphic.color = StringUtility.SetColorAlpha(graphic.color, 0);
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

        private void OnEnable()
        {
            if (combatAgent)
            {
                combatAgent.SessionProgressionHandler.OnAbilityUpgrade += OnAbilityUpgrade;
            }

            viLogoUpgradeIcon.color = originalAnimColor;
            upgradeIcon.color = StringUtility.SetColorAlpha(upgradeIcon.color, 0);
            upgradeIcon.rectTransform.position = upgradeIconInactivePosition.position;
        }

        private void OnDisable()
        {
            if (combatAgent)
            {
                combatAgent.SessionProgressionHandler.OnAbilityUpgrade -= OnAbilityUpgrade;
            }
        }

        public void SetPreviewOn()
        {
            if (previewAbility.GetClipType() != ActionClip.ClipType.Ability) { Debug.LogError("Preview ability is not of clip type ability! " + this); return; }
            Ability = previewAbility;
            abilityIcon.sprite = Ability.abilityImageIcon;
            inactiveAbilityIcon.sprite = Ability.abilityImageIcon;
            keybindText.text = previewAbility.name.Replace("Ability", "");
            OnKeybindTextChange();
            cooldownText.text = "";

            stackCircleBackground.enabled = false;
            stackBackground.enabled = false;
            stackText.enabled = false;
        }

        public void Initialize(ActionClip ability, string keybindText)
        {
            Ability = ability;
            abilityIcon.sprite = ability.abilityImageIcon;
            inactiveAbilityIcon.sprite = ability.abilityImageIcon;
            this.keybindText.text = keybindText;
            OnKeybindTextChange();

            abilityLevel = combatAgent.SessionProgressionHandler.GetAbilityLevel(combatAgent.WeaponHandler.GetWeapon(), Ability);

            float staminaCost = combatAgent.AnimationHandler.GetStaminaCostOfClip(Ability);
            staminaCostText.text = staminaCost.ToString("F0");
            lastStaminaCost = staminaCost;

            bool stackIsVisible = combatAgent.WeaponHandler.GetWeapon().GetMaxAbilityStacks(Ability) > 1;
            stackCircleBackground.enabled = stackIsVisible;
            stackBackground.enabled = stackIsVisible;
            stackText.enabled = stackIsVisible;

            viLogoUpgradeIcon.color = originalAnimColor;
            upgradeIcon.color = StringUtility.SetColorAlpha(upgradeIcon.color, 0);
            upgradeIcon.rectTransform.position = upgradeIconInactivePosition.position;
        }

        private void OnKeybindTextChange()
        {
            cooldownText.rectTransform.offsetMax = new Vector2(0, !keybindText.enabled | string.IsNullOrWhiteSpace(keybindText.text) ? 0 : 20);
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

        private int abilityLevel;
        private float lastStaminaCost = -1;
        private int lastNumAbilitiesOffCooldown = -1;
        private void Update()
        {
            if (Ability == null) { return; }

            float staminaCost = combatAgent.AnimationHandler.GetStaminaCostOfClip(Ability);
            if (!Mathf.Approximately(lastStaminaCost, staminaCost))
            {
                staminaCostText.text = staminaCost.ToString("F0");
            }
            lastStaminaCost = staminaCost;

            int numAbilitiesOffCooldown = combatAgent.WeaponHandler.GetWeapon().GetNumberOfAbilitiesOffCooldown(Ability);
            if (numAbilitiesOffCooldown != lastNumAbilitiesOffCooldown)
            {
                stackText.text = numAbilitiesOffCooldown.ToString();
                if (numAbilitiesOffCooldown > 0)
                {
                    stackText.text += "x";
                }
                stackText.color = StringUtility.SetColorAlpha(numAbilitiesOffCooldown > 0 ? Color.white : Color.red, lastOpacityEvaluated);
            }
            lastNumAbilitiesOffCooldown = numAbilitiesOffCooldown;

            bool canUpgrade = combatAgent.SessionProgressionHandler.CanUpgradeAbility(Ability, combatAgent.WeaponHandler.GetWeapon());
            upgradeIcon.rectTransform.position = Vector3.Lerp(upgradeIcon.transform.position,
                canUpgrade ? upgradeIconActivePosition.position : upgradeIconInactivePosition.position,
                Time.deltaTime * 4);

            upgradeIcon.raycastTarget = canUpgrade;

            upgradeIcon.color = StringUtility.SetColorAlpha(upgradeIcon.color, Mathf.Lerp(upgradeIcon.color.a, canUpgrade ? 1 : 0, Time.deltaTime * 4));

            if (GameModeManager.Singleton.LevelingEnabled)
            {
                if (abilityLevel == -1)
                {
                    borderImage.color = StringUtility.SetColorAlpha(originalBorderImageColor, lastOpacityEvaluated);
                    staminaCostText.color = StringUtility.SetColorAlpha(originalStaminaCostColor, lastOpacityEvaluated);
                    cooldownText.text = "";
                    abilityIcon.fillAmount = 1;
                    return;
                }
            }

            float cooldownTimeLeft = combatAgent.WeaponHandler.GetWeapon().GetAbilityCooldownTimeLeft(Ability);
            float bufferTimeLeft = combatAgent.WeaponHandler.GetWeapon().GetAbilityBufferTimeLeft(Ability);

            float timeLeft;
            float progressLeft;
            if (cooldownTimeLeft >= bufferTimeLeft)
            {
                progressLeft = 1 - combatAgent.WeaponHandler.GetWeapon().GetAbilityCooldownProgress(Ability);
                timeLeft = cooldownTimeLeft;
            }
            else
            {
                progressLeft = 1 - combatAgent.WeaponHandler.GetWeapon().GetAbilityBufferProgress(Ability);
                timeLeft = bufferTimeLeft;
            }

            cooldownText.text = timeLeft <= 0 ? "" : StringUtility.FormatDynamicFloatForUI(timeLeft, 1);
            abilityIcon.fillAmount = progressLeft;

            if (!combatAgent.AnimationHandler.AreActionClipRequirementsMet(Ability) | combatAgent.StatusAgent.IsSilenced())
            {
                Color borderColor = StringUtility.SetColorAlpha(Color.red, lastOpacityEvaluated);
                if (borderImage.color != borderColor)
                {
                    borderImage.color = borderColor;
                }

                Color staminaColor = StringUtility.SetColorAlpha(Color.red, lastOpacityEvaluated);
                if (staminaCostText.color != staminaColor)
                {
                    staminaCostText.color = staminaColor;
                }
            }
            else
            {
                Color borderColor = StringUtility.SetColorAlpha(originalBorderImageColor, lastOpacityEvaluated);
                if (borderImage.color != borderColor)
                {
                    borderImage.color = borderColor;
                }

                Color staminaColor = StringUtility.SetColorAlpha(originalStaminaCostColor, lastOpacityEvaluated);
                if (staminaCostText.color != staminaColor)
                {
                    staminaCostText.color = staminaColor;
                }
            }
        }

        private void OnAbilityUpgrade(ActionClip ability, int newAbilityLevel)
        {
            if (ability != Ability) { return; }

            abilityLevel = newAbilityLevel;

            if (upgradeAbilityAnimationCoroutine != null) { StopCoroutine(upgradeAbilityAnimationCoroutine); }
            upgradeAbilityAnimationCoroutine = StartCoroutine(UpgradeAbilityAnimation());

            AudioManager.Singleton.Play2DClip(combatAgent.gameObject, abilityUpgradeSoundEffects[Random.Range(0, abilityUpgradeSoundEffects.Length)], 0.5f);
        }

        private bool upgradeAbilityAnimationRunning;
        private Coroutine upgradeAbilityAnimationCoroutine;
        private static readonly Color originalAnimColor = new Color(1, 197 / 255f, 61 / 255f, 0);
        private static readonly Color visibleAnimColor = new Color(1, 197 / 255f, 61 / 255f, 1);
        private IEnumerator UpgradeAbilityAnimation()
        {
            upgradeAbilityAnimationRunning = true;

            viLogoUpgradeIcon.color = originalAnimColor;

            float lerpProgress = 0;
            while (true)
            {
                lerpProgress += Time.deltaTime * 1.5f;
                viLogoUpgradeIcon.color = Color.Lerp(originalAnimColor, visibleAnimColor, lerpProgress);
                yield return null;

                if (viLogoUpgradeIcon.color == visibleAnimColor) { break; }
            }

            lerpProgress = 0;
            while (true)
            {
                lerpProgress += Time.deltaTime * 1.5f;
                viLogoUpgradeIcon.color = Color.Lerp(visibleAnimColor, originalAnimColor, lerpProgress);
                yield return null;

                if (viLogoUpgradeIcon.color == originalAnimColor) { break; }
            }

            upgradeAbilityAnimationRunning = false;
        }

        [SerializeField] private AudioClip[] abilityUpgradeSoundEffects;
    }
}