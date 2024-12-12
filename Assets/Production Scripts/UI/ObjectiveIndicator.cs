using UnityEngine;
using System.Collections.Generic;
using Vi.Core;
using Vi.Core.CombatAgents;
using UnityEngine.UI;
using Vi.Core.MovementHandlers;

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

        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera)
            {
                if (mainCamera.gameObject.CompareTag("MainCamera"))
                {
                    return;
                }
            }
            mainCamera = Camera.main;
        }

        private void Start()
        {
            indicatorImage.enabled = false;
        }

        private void Update()
        {
            FindObjectiveHandler();
            FindMainCamera();

            if (localObjectiveHandler & mainCamera)
            {
                if (localObjectiveHandler.Objective)
                {
                    indicatorImage.enabled = true;
                    indicatorImage.transform.position = localObjectiveHandler.Objective.transform.position + localObjectiveHandler.MovementHandler.BodyHeightOffset;
                    indicatorImage.transform.position += Vector3.up * indicatorImage.rectTransform.sizeDelta.y;
                    indicatorImage.transform.position += Vector3.up * Mathf.PingPong(Time.time, 0.5f);

                    Vector3 rel = localObjectiveHandler.Objective.transform.position - indicatorImage.transform.position;

                    //Quaternion lookAtTargetRot = rel == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(rel);
                    //Debug.Log(lookAtTargetRot.eulerAngles);
                    //var t = lookAtCameraRot * Quaternion.Euler(lookAtTargetRot.eulerAngles.x, 0, 0);

                    Quaternion lookAtCameraRot = Quaternion.LookRotation(mainCamera.transform.position - indicatorImage.transform.position);

                    indicatorImage.transform.rotation = lookAtCameraRot;
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