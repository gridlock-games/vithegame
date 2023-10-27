using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "StatusImageReference", menuName = "Production/Status Image Reference")]
    public class StatusImageReference : ScriptableObject
    {
        [SerializeField] private List<StatusIcon> statusIcons = new List<StatusIcon>();

        public Sprite GetStatusIcon(ActionClip.Status status) { return statusIcons.Find(item => item.status == status).icon; }

        [System.Serializable]
        private struct StatusIcon
        {
            public ActionClip.Status status;
            public Sprite icon;
        }
    }
}