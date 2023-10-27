using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class StatusIcon : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text timerText;

        private float duration;
        private float delay;

        public void UpdateStatusIcon(Sprite icon, float duration, float delay)
        {
            iconImage.sprite = icon;
            this.duration = duration;
            this.delay = delay;
        }

        private void Update()
        {
            // Process duration and delay here
            timerText.text = duration.ToString();
        }
    }
}