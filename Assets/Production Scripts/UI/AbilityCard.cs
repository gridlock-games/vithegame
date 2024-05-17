using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class AbilityCard : MonoBehaviour
    {
        [SerializeField] private Image abilityIcon;
        [SerializeField] private Image inactiveAbilityIcon;
        [SerializeField] private Image cooldownIcon;
        [SerializeField] private Text keybindText;
        [SerializeField] private ActionClip previewAbility;

        private ActionClip ability;

        public void SetPreviewOn()
        {
            if (previewAbility.GetClipType() != ActionClip.ClipType.Ability) { Debug.LogError("Preview ability is not of clip type ability! " + this); return; }
            ability = previewAbility;
            abilityIcon.sprite = ability.abilityImageIcon;
            inactiveAbilityIcon.sprite = ability.abilityImageIcon;
            keybindText.text = previewAbility.name.Replace("Ability", "");
            cooldownIcon.fillAmount = 0;
        }

        public void UpdateCard(ActionClip ability, string keybindText)
        {
            this.ability = ability;
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
            if (ability == null) { return; }
            cooldownIcon.fillAmount = 1 - weaponHandler.GetWeapon().GetAbilityCooldownProgress(ability);
            abilityIcon.fillAmount = 1 - weaponHandler.GetWeapon().GetAbilityCooldownProgress(ability);

            if (animationHandler.AreActionClipRequirementsMet(ability))
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