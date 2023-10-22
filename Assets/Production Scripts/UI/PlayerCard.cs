using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class PlayerCard : MonoBehaviour
    {
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

        private Attributes attributes;

        public void Initialize(Attributes attributes)
        {
            this.attributes = attributes;
        }

        private const float fillSpeed = 4;
        private void Update()
        {
            if (!attributes) { return; }

            healthFillImage.fillAmount = attributes.GetHP() / attributes.GetMaxHP();
            staminaFillImage.fillAmount = attributes.GetStamina() / attributes.GetMaxStamina();
            defenseFillImage.fillAmount = attributes.GetDefense() / attributes.GetMaxDefense();
            rageFillImage.fillAmount = attributes.GetRage() / attributes.GetMaxRage();

            interimHealthFillImage.fillAmount = Mathf.Lerp(interimHealthFillImage.fillAmount, attributes.GetHP() / attributes.GetMaxHP(), Time.deltaTime * fillSpeed);
            interimStaminaFillImage.fillAmount = Mathf.Lerp(interimStaminaFillImage.fillAmount, attributes.GetStamina() / attributes.GetMaxStamina(), Time.deltaTime * fillSpeed);
            interimDefenseFillImage.fillAmount = Mathf.Lerp(interimDefenseFillImage.fillAmount, attributes.GetDefense() / attributes.GetMaxDefense(), Time.deltaTime * fillSpeed);
            interimRageFillImage.fillAmount = Mathf.Lerp(interimRageFillImage.fillAmount, attributes.GetRage() / attributes.GetMaxRage(), Time.deltaTime * fillSpeed);
        }
    }
}