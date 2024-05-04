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
        [SerializeField] private bool shouldConformHorizontal = true;
        [SerializeField] private int horizontalPadding = 10;
        [SerializeField] private bool shouldConformVertical = true;
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

        private string lastEvaluatedTextValue = null;
        private void EvaluateText()
        {
            image.enabled = textToConformTo.enabled & textToConformTo.text != "";
            if (image.enabled)
            {
                if (textToConformTo.text == lastEvaluatedTextValue) { return; }

                lastEvaluatedTextValue = textToConformTo.text;

                // Scale size of background by size of text
                if (shouldConformHorizontal) { image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textToConformTo.preferredWidth * textToConformTo.transform.localScale.x + horizontalPadding); }
                if (shouldConformVertical) { image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textToConformTo.preferredHeight * textToConformTo.transform.localScale.y + verticalPadding); }
            }
        }
    }
}