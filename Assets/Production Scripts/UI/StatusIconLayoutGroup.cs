using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.CombatAgents;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    [RequireComponent(typeof(Canvas))]
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

        private GridLayoutGroup gridLayoutGroup;
        private HorizontalLayoutGroup horizontalLayoutGroup;
        private void Awake()
        {
            gridLayoutGroup = GetComponent<GridLayoutGroup>();

            horizontalLayoutGroup = GetComponent<HorizontalLayoutGroup>();

            if (!gridLayoutGroup & !horizontalLayoutGroup)
            {
                Debug.LogWarning("Status icon layout group has no grid layout group and no horizontal layout group");
            }

            if (horizontalLayoutGroup)
            {

            }

            foreach (ActionClip.Status status in System.Enum.GetValues(typeof(ActionClip.Status)))
            {
                StatusIcon statusIcon = Instantiate(statusImagePrefab.gameObject, transform).GetComponent<StatusIcon>();
                statusIcon.InitializeStatusIcon(status);
                statusIcons.Add(statusIcon);
                statusIcon.SetActive(false);
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

            int centerSiblingIndex = statusIcons.Count / (gridLayoutGroup ? 6 : 2);
            int siblingOffset = 0;
            int siblingOffsetMultiplier = 1;

            foreach (StatusIcon statusIcon in statusIcons)
            {
                if (activeStatuses.Contains(statusIcon.Status))
                {
                    statusIcon.SetActive(true);

                    int newSiblingIndex = centerSiblingIndex + (siblingOffset * siblingOffsetMultiplier);
                    if (newSiblingIndex >= transform.childCount)
                    {
                        newSiblingIndex = 0;
                    }
                    else if (newSiblingIndex < 0)
                    {
                        newSiblingIndex = 0;
                    }

                    statusIcon.transform.SetSiblingIndex(newSiblingIndex);
                    if (siblingOffsetMultiplier == 1) { siblingOffset++; }
                    siblingOffsetMultiplier *= -1;
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

#if UNITY_EDITOR
        [ContextMenu("Debug Max Status Count")]
        private void DebugMaxStatusCount()
        {
            Debug.Log(System.Enum.GetValues(typeof(ActionClip.Status)).Length);
        }
#endif
    }
}

