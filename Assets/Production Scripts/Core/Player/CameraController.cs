using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Transform Settings")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float orbitSpeed = 8;
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private float maxPitch = 40;
        [SerializeField] private Vector3 positionOffset = new Vector3(0, 0, 3);

        private float targetRotationX;
        private float targetRotationY;
        private Vector3 _velocityPosition;
        private PlayerMovementHandler movementHandler;
        private GameObject cameraInterp;
        private void Start()
        {
            movementHandler = GetComponentInParent<PlayerMovementHandler>();
            transform.SetParent(null, true);
            cameraInterp = new GameObject("Camera Interp");
        }

        private void Update()
        {
            // Update camera interp transform
            targetRotationX += movementHandler.GetLookInput().x;
            targetRotationY += movementHandler.GetLookInput().y;

            targetRotationX %= 360f;
            targetRotationY %= 360f;

            targetRotationY = Mathf.Clamp(targetRotationY, -maxPitch / 2.0f, maxPitch / 2.0f);

            Quaternion targetRotation = Quaternion.Euler(targetRotationY, targetRotationX, 0);

            cameraInterp.transform.position = Vector3.SmoothDamp(
                cameraInterp.transform.position,
                cameraPivot.TransformPoint(Vector3.zero),
                ref _velocityPosition,
                smoothTime
            );

            Vector3 eulers = Quaternion.Slerp(cameraInterp.transform.rotation, targetRotation, Time.deltaTime * orbitSpeed).eulerAngles;
            eulers.z = 0;
            cameraInterp.transform.eulerAngles = eulers;

            // Update camera transform itself
            transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * positionOffset;
            transform.LookAt(cameraInterp.transform);
        }
    }
}

