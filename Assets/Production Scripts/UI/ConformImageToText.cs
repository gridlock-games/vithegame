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
        private const int padding = 10;

        private Image image;
        private void Start()
        {
            image = GetComponent<Image>();
        }

        private void Update()
        {
            image.enabled = textToConformTo.enabled;
            // Scale size of name background by size of text
            image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textToConformTo.preferredWidth * textToConformTo.transform.localScale.x + padding);
            image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textToConformTo.preferredHeight * textToConformTo.transform.localScale.y + padding);
        }
    }
}