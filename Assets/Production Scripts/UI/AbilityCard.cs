using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.UI
{
    public class AbilityCard : MonoBehaviour
    {
        [SerializeField] private Image abilityIcon;
        [SerializeField] private Image inactiveAbilityIcon;
        [SerializeField] private Text cooldownText;
        [SerializeField] private Text keybindText;
        [SerializeField] private ActionClip previewAbility;

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

        public void UpdateCard(ActionClip ability, string keybindText)
        {
            Ability = ability;
            abilityIcon.sprite = ability.abilityImageIcon;
            inactiveAbilityIcon.sprite = ability.abilityImageIcon;
            this.keybindText.text = keybindText;
        }

        private Image borderImage;
        private Color originalBorderImageColor;
        private WeaponHandler weaponHandler;
        private AnimationHandler animationHandler;

        private void Start()
        {
            borderImage = GetComponent<Image>();
            originalBorderImageColor = borderImage.color;
            weaponHandler = GetComponentInParent<WeaponHandler>();
            animationHandler = weaponHandler.GetComponent<AnimationHandler>();

            keybindText.enabled = !(Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer);
        }

        private void Update()
        {
            if (Ability == null) { return; }

            float timeLeft = weaponHandler.GetWeapon().GetAbilityCooldownTimeLeft(Ability);
            cooldownText.text = timeLeft <= 0 ? "" : StringUtility.FormatDynamicFloatForUI(timeLeft, 1);
            abilityIcon.fillAmount = 1 - weaponHandler.GetWeapon().GetAbilityCooldownProgress(Ability);

            if (animationHandler.AreActionClipRequirementsMet(Ability))
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