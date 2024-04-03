using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Vi.Core;

namespace Vi.UI
{
    public class ComboCounterUI : MonoBehaviour
    {
        [SerializeField] private Text comboCounterText;
        [SerializeField] private RectTransform comboNumberUI;

        private Attributes attributes;
        private Vector3 targetScale;
        private Vector2 originalAnchoredPosition;
        private void Start()
        {
            attributes = GetComponentInParent<Attributes>();
            targetScale = transform.localScale;
            transform.localScale = Vector3.zero;
            originalAnchoredPosition = comboNumberUI.anchoredPosition;
        }

        private const float shakeAmount = 300;

        private void Update()
        {
            comboCounterText.text = attributes.GetComboCounter().ToString();

            if (attributes.GetComboCounter() == 0)
            {
                transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, Time.deltaTime * 8);
            }
            else
            {
                comboNumberUI.anchoredPosition = originalAnchoredPosition + (Random.insideUnitCircle * (Time.deltaTime * shakeAmount));
                transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, Time.deltaTime * 8);
            }
        }
    }
}