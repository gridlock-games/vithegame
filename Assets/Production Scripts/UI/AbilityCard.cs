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
        [SerializeField] private ActionClip previewAbility;
        [SerializeField] private Image upgradeIcon;
        [SerializeField] private RectTransform upgradeIconActivePosition;
        [SerializeField] private RectTransform upgradeIconInactivePosition;

        public ActionClip Ability { get; private set; }

        public void SetActive(bool isActive)
        {
            if (!canvas) { canvas = GetComponent<Canvas>(); }
            canvas.enabled = isActive;
        }

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
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
        }

        private Image borderImage;
        private Color originalBorderImageColor;
        private CombatAgent combatAgent;

        private void Start()
        {
            borderImage = GetComponent<Image>();
            originalBorderImageColor = borderImage.color;
            combatAgent = GetComponentInParent<CombatAgent>();

            keybindText.enabled = !(Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer);
        }

        private void Update()
        {
            if (Ability == null) { return; }

            upgradeIcon.rectTransform.position = Vector3.Lerp(upgradeIcon.transform.position,
                combatAgent.SessionProgressionHandler.SkillPoints > 0 ? upgradeIconActivePosition.position : upgradeIconInactivePosition.position,
                Time.deltaTime * 4);

            if (GameModeManager.Singleton.LevelingEnabled)
            {
                if (combatAgent.SessionProgressionHandler.GetAbilityLevel(combatAgent.WeaponHandler.GetWeapon(), Ability) == 0)
                {
                    borderImage.color = originalBorderImageColor;
                    cooldownText.text = "";
                    abilityIcon.fillAmount = 1;
                    return;
                }
            }
            
            float timeLeft = combatAgent.WeaponHandler.GetWeapon().GetAbilityCooldownTimeLeft(Ability);
            cooldownText.text = timeLeft <= 0 ? "" : StringUtility.FormatDynamicFloatForUI(timeLeft, 1);
            abilityIcon.fillAmount = 1 - combatAgent.WeaponHandler.GetWeapon().GetAbilityCooldownProgress(Ability);

            if (combatAgent.AnimationHandler.AreActionClipRequirementsMet(Ability))
            {
                borderImage.color = originalBorderImageColor;
            }
            else
            {
                borderImage.color = Color.red;
            }
        }
    }
}