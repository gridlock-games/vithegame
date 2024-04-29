using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class ConformImageToText : MonoBehaviour
    {
        [SerializeField] private Text textToConformTo;
        [SerializeField] private int horizontalPadding = 10;
        [SerializeField] private int verticalPadding = 10;

        private Image image;
        private void Start()
        {
            image = GetComponent<Image>();
            EvaluateText();
        }

        private void Update()
        {
            EvaluateText();
        }

        private float lastPreferredWidth;
        private float lastPreferredHeight;
        private void EvaluateText()
        {
            image.enabled = textToConformTo.enabled & textToConformTo.text != "";
            if (image.enabled)
            {
                float preferredWidth = textToConformTo.preferredWidth * textToConformTo.transform.localScale.x;
                float preferredHeight = textToConformTo.preferredHeight * textToConformTo.transform.localScale.y;

                if (Mathf.Approximately(preferredWidth, lastPreferredWidth) & Mathf.Approximately(preferredHeight, lastPreferredHeight)) { return; }

                // Scale size of background by size of text
                image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth + horizontalPadding);
                image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight + verticalPadding);

                lastPreferredWidth = textToConformTo.preferredWidth;
                lastPreferredHeight = textToConformTo.preferredHeight;
            }
        }
    }
}