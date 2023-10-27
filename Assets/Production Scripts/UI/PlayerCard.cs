using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.ScriptableObjects;

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

        [Header("Status UI")]
        [SerializeField] private StatusImageReference statusImageReference;
        [SerializeField] private Transform statusImageParent;
        [SerializeField] private GameObject statusImagePrefab;

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

            foreach (Transform child in statusImageParent)
            {
                Destroy(child.gameObject);
            }

            foreach (ActionClip.Status status in attributes.GetActiveStatuses())
            {
                GameObject statusImage = Instantiate(statusImagePrefab, statusImageParent);
                statusImage.GetComponent<Image>().sprite = statusImageReference.GetStatusIcon(status);
            }
        }
    }
}