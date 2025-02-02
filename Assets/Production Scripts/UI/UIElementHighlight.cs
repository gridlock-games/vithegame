using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.UI
{
    public class UIElementHighlight : MonoBehaviour
    {
        private float positionOffset;

        private RectTransform rt;

        private void Awake()
        {
            rt = (RectTransform)transform;

            if (transform.parent is not RectTransform) { Debug.LogError("UI Element Highlight should be parented to a rect transform!"); }

            RectTransform parentRT = (RectTransform)transform.parent;
            Vector3 pos = parentRT.TransformPoint(parentRT.rect.center);
            rt.position = pos;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.ClampMagnitude(rt.localScale, 1.732f);

            minPosition = rt.anchoredPosition;
            maxPosition = rt.anchoredPosition + new Vector2(0, 20);
        }

        private const float speed = 2;

        private Vector2 maxPosition;
        private Vector2 minPosition;
        private float directionMultiplier = 1;
        private void Update()
        {
            float normalizedTime = Mathf.PingPong(Time.time * speed, 1);
            rt.anchoredPosition = Vector2.Lerp(minPosition, maxPosition, normalizedTime);
        }
    }
}