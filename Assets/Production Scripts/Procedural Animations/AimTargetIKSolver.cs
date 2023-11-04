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
            transform.position = mainCameraTransform.position + mainCameraTransform.rotation * offset;
        }
    }
}