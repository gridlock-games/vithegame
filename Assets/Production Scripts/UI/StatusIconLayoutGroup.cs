using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Core.CombatAgents;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class StatusIconLayoutGroup : MonoBehaviour
    {
        private StatusAgent statusAgent;
        public void Initialize(StatusAgent statusAgent)
        {
            this.statusAgent = statusAgent;
            UpdateStatusesUI();

            if (!statusAgent)
            {
                foreach (StatusIcon statusIcon in statusIcons)
                {
                    statusIcon.SetActive(true);
                }
            }
        }

        [SerializeField] private StatusIcon statusImagePrefab;
        private List<StatusIcon> statusIcons = new List<StatusIcon>();

        private void Awake()
        {
            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                StatusIcon statusIcon = Instantiate(statusImagePrefab.gameObject, transform).GetComponent<StatusIcon>();
                statusIcon.InitializeStatusIcon(status);
                statusIcons.Add(statusIcon);
                statusIcon.gameObject.layer = gameObject.layer;
            }
        }

        private void OnEnable()
        {
            UpdateStatusesUI();
        }

        private void OnDisable()
        {
            foreach (StatusIcon statusIcon in statusIcons)
            {
                statusIcon.SetActive(false);
            }
        }

        private void Update()
        {
            if (!statusAgent) { return; }

            if (statusAgent.ActiveStatusesWasUpdatedThisFrame)
            {
                UpdateStatusesUI();
            }
        }

        private void UpdateStatusesUI()
        {
            if (!CanUpdateUI()) { return; }

            List<ActionClip.Status> activeStatuses = statusAgent.GetActiveStatuses();
            foreach (StatusIcon statusIcon in statusIcons)
            {
                if (activeStatuses.Contains(statusIcon.Status))
                {
                    statusIcon.SetActive(true);
                    statusIcon.transform.SetAsFirstSibling();
                }
                else
                {
                    statusIcon.SetActive(false);
                }
            }
        }

        private bool CanUpdateUI()
        {
            if (!statusAgent) { return false; }
            if (!statusAgent.IsSpawned) { return false; }

            return true;
        }
    }
}

