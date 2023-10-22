using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class PlayerCard : MonoBehaviour
    {
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private Image defenseFillImage;
        [SerializeField] private Image rageFillImage;

        private Attributes attributes;

        public void Initialize(Attributes attributes)
        {
            this.attributes = attributes;
        }

        private const float fillSpeed = 4;
        private void Update()
        {
            if (!attributes) { return; }

            //healthFillImage.fillAmount = attributes.GetHP() / attributes.GetMaxHP();
            //staminaFillImage.fillAmount = attributes.GetStamina() / attributes.GetMaxStamina();
            //defenseFillImage.fillAmount = attributes.GetDefense() / attributes.GetMaxDefense();
            //rageFillImage.fillAmount = attributes.GetRage() / attributes.GetMaxRage();

            healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, attributes.GetHP() / attributes.GetMaxHP(), Time.deltaTime * fillSpeed);
            staminaFillImage.fillAmount = Mathf.Lerp(staminaFillImage.fillAmount, attributes.GetStamina() / attributes.GetMaxStamina(), Time.deltaTime * fillSpeed);
            defenseFillImage.fillAmount = Mathf.Lerp(defenseFillImage.fillAmount, attributes.GetDefense() / attributes.GetMaxDefense(), Time.deltaTime * fillSpeed);
            rageFillImage.fillAmount = Mathf.Lerp(rageFillImage.fillAmount, attributes.GetRage() / attributes.GetMaxRage(), Time.deltaTime * fillSpeed);
        }
    }
}