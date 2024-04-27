using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;

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
        [Header("Camera collision settings")]
        [SerializeField] private float collisionPositionOffset;

        public GameObject CameraPositionClone { get; private set; }

        private float targetRotationY;
        private float targetRotationX;
        private Vector3 _velocityPosition;
        private PlayerMovementHandler movementHandler;
        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private GameObject cameraInterp;
        private Vector3 currentPositionOffset;

        public void SetRotation(float targetRotationX, float targetRotationY)
        {
            this.targetRotationX = targetRotationX;
            this.targetRotationY = targetRotationY - 180;

            cameraInterp.transform.position = cameraPivot.TransformPoint(Vector3.zero);
            cameraInterp.transform.rotation = Quaternion.Euler(targetRotationX, targetRotationY, 0);
        }

        public void AddRotation(float rotationX, float rotationY)
        {
            targetRotationX -= rotationX;
            targetRotationY += rotationY;
        }

        private void Start()
        {
            targetRotationX = 0;
            targetRotationY = transform.parent.eulerAngles.y - 180;

            movementHandler = GetComponentInParent<PlayerMovementHandler>();
            weaponHandler = movementHandler.GetComponent<WeaponHandler>();
            attributes = movementHandler.GetComponent<Attributes>();
            transform.SetParent(null, true);
            cameraInterp = new GameObject("Camera Interp");
            CameraPositionClone = new GameObject("Empty Camera Position Clone");
            currentPositionOffset = positionOffset;
        }

        private void OnDestroy()
        {
            Destroy(cameraInterp);
            Destroy(CameraPositionClone);
        }

        private const float killerRotationSpeed = 4;
        private const float killerRotationSlerpThreshold = 1;

        private void Update()
        {
            // Update camera interp transform
            Vector2 lookInput = movementHandler.GetLookInput();
            targetRotationX += lookInput.y;
            targetRotationY += lookInput.x;
            movementHandler.ResetLookInput();

            targetRotationX %= 360f;
            targetRotationY %= 360f;

            targetRotationX = Mathf.Clamp(targetRotationX, -maxPitch / 2.0f, maxPitch / 2.0f);

            bool shouldLookAtCameraInterp = true;
            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                Debug.Log(movementHandler.CameraFollowTarget);

                if (movementHandler.CameraFollowTarget)
                {
                    Vector3 targetPosition = movementHandler.CameraFollowTarget.transform.position + movementHandler.CameraFollowTarget.transform.rotation * new Vector3(0, 3, -3);
                    Quaternion targetRotation = Quaternion.LookRotation(movementHandler.CameraFollowTarget.transform.position - transform.position);

                    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 8);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8);
                    shouldLookAtCameraInterp = false;
                }
                else
                {
                    NetworkObject killer = attributes.GetKiller();
                    if (killer)
                    {
                        Quaternion killerRotation = Quaternion.LookRotation(killer.transform.position - transform.position, Vector3.up);
                        if (Quaternion.Angle(transform.rotation, killerRotation) > killerRotationSlerpThreshold) { killerRotation = Quaternion.Slerp(transform.rotation, killerRotation, Time.deltaTime * killerRotationSpeed); }
                        transform.rotation = killerRotation;
                        shouldLookAtCameraInterp = false;

                        currentPositionOffset = Vector3.MoveTowards(currentPositionOffset, weaponHandler.IsAiming() ? aimingPositionOffset : positionOffset, Time.deltaTime * aimingTransitionSpeed);
                        transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * currentPositionOffset;
                    }
                }
            }

            if (shouldLookAtCameraInterp)
            {
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
                transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * currentPositionOffset;

                transform.LookAt(cameraInterp.transform);

                // Do the same thing for the clone transform
                CameraPositionClone.transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * currentPositionOffset;
                CameraPositionClone.transform.LookAt(cameraInterp.transform);

                // Move camera if there is a wall in the way
                Debug.DrawRay(cameraInterp.transform.position, cameraInterp.transform.forward * currentPositionOffset.z, Color.blue, Time.deltaTime);
                if (Physics.Raycast(cameraInterp.transform.position, cameraInterp.transform.forward, out RaycastHit hit, currentPositionOffset.z, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
                {
                    transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * new Vector3(0, 0, hit.distance + collisionPositionOffset);
                }
            }
        }
    }
}