using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.CombatAgents;

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

        private const int minHitsToDisplayUI = 1;

        private void Update()
        {
            if (attributes.GetComboCounter() > minHitsToDisplayUI) { comboCounterText.text = attributes.GetComboCounter().ToString(); }

            if (attributes.GetComboCounter() <= minHitsToDisplayUI)
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