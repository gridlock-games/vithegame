using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.ProceduralAnimations
{
    public class AimTargetIKSolver : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0, 0, 10);

        private Transform mainCameraTransform;

        private void Start()
        {
            if (!Camera.main) { return; }
            mainCameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (!mainCameraTransform) { return; }

            bool bHit = Physics.Raycast(mainCameraTransform.position, mainCameraTransform.forward, out RaycastHit hit, 10, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            if (bHit)
            {
                if (hit.transform.root != transform.root)
                {
                    transform.position = hit.point;
                }
                else
                {
                    transform.position = mainCameraTransform.position + mainCameraTransform.rotation * offset;
                }
            }
            else
            {
                transform.position = mainCameraTransform.position + mainCameraTransform.rotation * offset;
            }
        }
    }
}