using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.UI
{
    public class UIElementHighlight : MonoBehaviour
    {
        private const float speed = 40;
        private const float maxOffset = 20;

        private float positionOffset;

        private RectTransform rt;

        private void Awake()
        {
            rt = (RectTransform)transform;

            if (transform.parent is not RectTransform) { Debug.LogError("UI Element Highlight should be parented to a rect transform!"); }

            rt.anchoredPosition = new Vector2(0, ((RectTransform)transform.parent).sizeDelta.y / 2);
        }

        private float directionMultiplier = 1;
        private void Update()
        {
            if (Mathf.Abs(positionOffset) >= maxOffset) { directionMultiplier *= -1; }

            float amount = Time.deltaTime * speed * directionMultiplier;
            positionOffset = Mathf.Clamp(positionOffset + amount, -maxOffset, maxOffset);

            rt.anchoredPosition += new Vector2(0, amount);
        }
    }
}