using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class StatusIcon : MonoBehaviour
    {
        [SerializeField] private StatusImageReference statusImageReference;
        [SerializeField] private Image iconImage;

        private Image image;

        public ActionClip.Status Status { get; private set; }

        public void InitializeStatusIcon(ActionClip.Status status)
        {
            Status = status;
            iconImage.sprite = statusImageReference.GetStatusIcon(Status);
        }

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            image = GetComponent<Image>();
            SetActive(false);
        }

        public void SetActive(bool isActive)
        {
            canvas.enabled = isActive;
            image.enabled = isActive;
        }
    }
}