using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private Image defenseFillImage;
        [SerializeField] private Image rageFillImage;

        private Attributes attributes;

        private void Start()
        {
            attributes = GetComponentInParent<Attributes>();
        }

        private void Update()
        {
            healthFillImage.fillAmount = attributes.GetHP() / attributes.GetMaxHP();
            staminaFillImage.fillAmount = attributes.GetStamina() / attributes.GetMaxStamina();
            defenseFillImage.fillAmount = attributes.GetDefense() / attributes.GetMaxDefense();
            rageFillImage.fillAmount = attributes.GetRage() / attributes.GetMaxRage();
        }
    }
}