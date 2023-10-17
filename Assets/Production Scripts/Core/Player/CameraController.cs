using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float orbitSpeed = 8;
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private float maxPitch = 40;

        private float targetRotationX;
        private float targetRotationY;

        private Vector3 _velocityPosition;

        private Vector3 targetOffset;
        private MovementHandler movementHandler;
        private void Awake()
        {
            targetOffset = transform.position - cameraPivot.position;
            movementHandler = GetComponentInParent<MovementHandler>();
        }

        private void Start()
        {
            transform.SetParent(null, true);
        }

        private void Update()
        {
            targetRotationX += movementHandler.GetLookInput().x;
            targetRotationY -= movementHandler.GetLookInput().y;

            targetRotationX %= 360f;
            targetRotationY %= 360f;

            targetRotationY = Mathf.Clamp(targetRotationY, -maxPitch / 2.0f, maxPitch / 2.0f);

            Quaternion targetRotation = Quaternion.Euler(targetRotationY, targetRotationX, 0);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                cameraPivot.TransformPoint(targetOffset),
                ref _velocityPosition,
                smoothTime
            );

            Vector3 eulers = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * orbitSpeed).eulerAngles;
            eulers.z = 0;
            transform.eulerAngles = eulers;
        }
    }
}

