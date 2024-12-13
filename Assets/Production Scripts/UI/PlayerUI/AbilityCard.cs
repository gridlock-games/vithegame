using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    [RequireComponent(typeof(CanvasGroup))]
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

        public ActionClip Ability { get; private set; }

        public void SetActive(bool isActive)
        {
            if (!canvas) { canvas = GetComponent<Canvas>(); }
            canvas.enabled = isActive;
        }

        public CanvasGroup CanvasGroup { get; private set; }
        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            borderImage = GetComponent<Image>();
            combatAgent = GetComponentInParent<CombatAgent>();
            CanvasGroup = GetComponent<CanvasGroup>();
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
                    borderImage.color = originalBorderImageColor;
                    staminaCostText.color = originalStaminaCostColor;
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
                borderImage.color = Color.red;
                staminaCostText.color = Color.red;
            }
            else
            {
                borderImage.color = originalBorderImageColor;
                staminaCostText.color = originalStaminaCostColor;
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