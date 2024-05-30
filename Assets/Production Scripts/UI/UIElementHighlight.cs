using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.UI
{
    public class UIElementHighlight : MonoBehaviour
    {
        private const float speed = 24;
        private const float maxOffset = 20;

        private float positionOffset;

        private RectTransform rt;

        private void Awake()
        {
            rt = (RectTransform)transform;
        }

        private float directionMultiplier = 1;
        private void Update()
        {
            if (Mathf.Abs(positionOffset) > maxOffset) { directionMultiplier *= -1; }

            float amount = Time.deltaTime * speed * directionMultiplier;
            positionOffset += amount;

            rt.anchoredPosition += new Vector2(0, amount);
        }
    }
}