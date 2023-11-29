using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float orbitSpeed = 8;
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private float maxPitch = 40;
        [SerializeField] private Vector3 positionOffset = new Vector3(0, 0, 3);
        [Header("Aiming Settings")]
        [SerializeField] private float aimingTransitionSpeed = 8;
        [SerializeField] private Vector3 aimingPositionOffset = new Vector3(0, 0, 1);

        private float targetRotationY;
        private float targetRotationX;
        private Vector3 _velocityPosition;
        private PlayerMovementHandler movementHandler;
        private WeaponHandler weaponHandler;
        private GameObject cameraInterp;
        private Vector3 currentPositionOffset;

        public void SetRotation(float targetRotationX, float targetRotationY)
        {
            this.targetRotationX = targetRotationX;
            this.targetRotationY = targetRotationY - 180;

            cameraInterp.transform.position = cameraPivot.TransformPoint(Vector3.zero);
            cameraInterp.transform.rotation = Quaternion.Euler(targetRotationX, targetRotationY, 0);
        }

        private void Start()
        {
            targetRotationX = 0;
            targetRotationY = transform.parent.eulerAngles.y - 180;

            movementHandler = GetComponentInParent<PlayerMovementHandler>();
            weaponHandler = movementHandler.GetComponent<WeaponHandler>();
            transform.SetParent(null, true);
            cameraInterp = new GameObject("Camera Interp");
            currentPositionOffset = positionOffset;
        }

        private void OnDestroy()
        {
            Destroy(cameraInterp);
        }

        private void Update()
        {
            // Update camera interp transform
            targetRotationX += movementHandler.GetLookInput().y;
            targetRotationY += movementHandler.GetLookInput().x;

            targetRotationX %= 360f;
            targetRotationY %= 360f;

            targetRotationX = Mathf.Clamp(targetRotationX, -maxPitch / 2.0f, maxPitch / 2.0f);

            Quaternion targetRotation = Quaternion.Euler(targetRotationX, targetRotationY, 0);

            if (weaponHandler.IsAiming())
            {
                cameraInterp.transform.position = cameraPivot.TransformPoint(Vector3.zero);
                cameraInterp.transform.rotation = targetRotation;
            }
            else
            {
                cameraInterp.transform.position = Vector3.SmoothDamp(
                   cameraInterp.transform.position,
                   cameraPivot.TransformPoint(Vector3.zero),
                   ref _velocityPosition,
                   smoothTime
               );

                Vector3 eulers = Quaternion.Slerp(cameraInterp.transform.rotation, targetRotation, Time.deltaTime * orbitSpeed).eulerAngles;
                eulers.z = 0;
                cameraInterp.transform.eulerAngles = eulers;
            }

            currentPositionOffset = Vector3.MoveTowards(currentPositionOffset, weaponHandler.IsAiming() ? aimingPositionOffset : positionOffset, Time.deltaTime * aimingTransitionSpeed);

            // Update camera transform itself
            transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * currentPositionOffset;
            transform.LookAt(cameraInterp.transform);
        }
    }
}

