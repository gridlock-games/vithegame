using UnityEngine;
using System.Collections.Generic;
using Vi.Core;
using Vi.Core.CombatAgents;
using UnityEngine.UI;
using Vi.Core.MovementHandlers;
using Vi.Utility;

namespace Vi.UI
{
    public class ObjectiveIndicator : MonoBehaviour
    {
        [SerializeField] private Image indicatorImage;

        private ObjectiveHandler localObjectiveHandler;

        private void FindObjectiveHandler()
        {
            if (localObjectiveHandler)
            {
                if (localObjectiveHandler.gameObject.activeInHierarchy) { return; }
            }

            if (PlayerDataManager.DoesExist())
            {
                KeyValuePair<int, Attributes> kvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
                if (kvp.Value)
                {
                    localObjectiveHandler = kvp.Value.MovementHandler.ObjectiveHandler;
                }
            }
        }

        private void Start()
        {
            indicatorImage.enabled = false;
        }

        private void Update()
        {
            FindObjectiveHandler();

            if (localObjectiveHandler & FindMainCamera.MainCamera)
            {
                if (localObjectiveHandler.Objective)
                {
                    Vector3 newPosition = localObjectiveHandler.Objective.GetUIPosition();
                    Quaternion newRot = Quaternion.LookRotation(FindMainCamera.MainCamera.transform.position - newPosition);

                    Vector3 viewportPos = FindMainCamera.MainCamera.WorldToViewportPoint(newPosition);

                    bool remakeRotation = false;
                    if (viewportPos.z < 0) // Check if objective is behind
                    {
                        viewportPos.x = 0.5f;
                        viewportPos.y = 0.05f;
                        viewportPos.z = 5;
                        remakeRotation = true;
                    }
                    else if (viewportPos.x > 1 | viewportPos.y > 1 | viewportPos.x < 0 | viewportPos.y < 0) // Check if the objective is off-screen
                    {
                        viewportPos.x = Mathf.Clamp(viewportPos.x, 0.05f, 0.95f);
                        viewportPos.y = Mathf.Clamp(viewportPos.y, 0.05f, 0.95f);
                        viewportPos.z = 5;
                        remakeRotation = true;
                    }

                    // Convert viewport position to world position for the UI
                    Vector3 worldPos = FindMainCamera.MainCamera.ViewportToWorldPoint(viewportPos);

                    if (remakeRotation)
                    {
                        newRot = Quaternion.LookRotation(FindMainCamera.MainCamera.transform.position - worldPos);

                        Vector2 viewportCenter = new Vector2(0.5f, 0.5f);
                        Vector2 direction = new Vector2(viewportPos.x, viewportPos.y) - viewportCenter;

                        float zAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

                        if (Mathf.Abs(direction.x) < Mathf.Abs(direction.y))
                        {
                            zAngle += 180;
                        }

                        newRot *= Quaternion.Euler(0, 0, zAngle);
                    }

                    // Set the position of the UI Indicator
                    indicatorImage.enabled = true;
                    indicatorImage.transform.position = worldPos;
                    indicatorImage.transform.position += indicatorImage.transform.up * Mathf.PingPong(Time.time, 0.5f);
                    indicatorImage.transform.rotation = newRot;
                }
                else
                {
                    indicatorImage.enabled = false;
                }
            }
            else
            {
                indicatorImage.enabled = false;
            }
        }
    }
}