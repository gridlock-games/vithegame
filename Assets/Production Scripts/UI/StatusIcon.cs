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

        public ActionClip.Status Status { get; private set; }

        public void InitializeStatusIcon(ActionClip.Status status) { Status = status; }

        private void Update()
        {
            iconImage.sprite = statusImageReference.GetStatusIcon(Status);
        }
    }
}