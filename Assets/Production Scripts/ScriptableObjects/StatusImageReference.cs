using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

#if UNITY_EDITOR
        [ContextMenu("Check For Duplicates And Missing Statuses")]
        private void CheckForDuplicatesAndMissingStatuses()
        {
            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                int count = statusIcons.Count(item => item.status == status);
                if (count == 0)
                {
                    Debug.LogError("Missing status " + status);
                }
                else if (count > 1)
                {
                    Debug.LogError("Duplicate status - " + status + " count - " + count);
                }
            }
        }
#endif
    }
}