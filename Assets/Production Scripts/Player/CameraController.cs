using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.CombatAgents;
using Vi.Core.MovementHandlers;

namespace Vi.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private float maxPitch = 90;
        [SerializeField] private Vector3 positionOffset = new Vector3(0, 0, 3);
        [Header("Aiming Settings")]
        [SerializeField] private float aimingTransitionSpeed = 8;
        [SerializeField] private Vector3 aimingPositionOffset = new Vector3(0, 0, 1);
        [Header("Camera collision settings")]
        [SerializeField] private float collisionPositionOffset = -0.3f;

        public GameObject CameraPositionClone { get; private set; }

        [SerializeField] private Camera thisCam;
        [SerializeField] private Camera orbitalCam;

        private float targetRotationY;
        private float targetRotationX;
        private Vector3 _velocityPosition;
        private PlayerMovementHandler movementHandler;
        private WeaponHandler weaponHandler;
        private Attributes attributes;
        private GameObject cameraInterp;
        private Vector3 currentPositionOffset;
        private UniversalAdditionalCameraData cameraData;

        public void SetRotation(float targetRotationX, float targetRotationY)
        {
            this.targetRotationX = targetRotationX;
            this.targetRotationY = targetRotationY - 180;

            cameraInterp.transform.position = GetCameraTargetPosition();
            cameraInterp.transform.rotation = Quaternion.Euler(targetRotationX, targetRotationY, 0);
        }

        public void AddRotation(float rotationX, float rotationY)
        {
            targetRotationX -= rotationX;
            targetRotationY += rotationY;
        }

        public void ToggleOrbitalCameraState()
        {
            if (orbitalCam.enabled)
            {
                thisCam.enabled = true;
                orbitalCam.enabled = false;
            }
            else
            {
                thisCam.enabled = false;
                orbitalCam.enabled = true;
            }
        }

        public void SetOrbitalCameraState(bool isActive)
        {
            thisCam.enabled = !isActive;
            orbitalCam.enabled = isActive;
        }

        public void SetActive(bool isActive)
        {
            thisCam.enabled = isActive;
            orbitalCam.enabled = false;
        }

        private Animator animator;
        private void Awake()
        {
            animator = GetComponent<Animator>();
            movementHandler = GetComponentInParent<PlayerMovementHandler>();
            weaponHandler = movementHandler.GetComponent<WeaponHandler>();
            attributes = movementHandler.GetComponent<Attributes>();
        }

        private void OnEnable()
        {
            targetRotationX = 0;
            targetRotationY = transform.parent.eulerAngles.y - 180;

            transform.SetParent(null, true);

            if (cameraInterp)
            {
                cameraInterp.SetActive(true);
            }
            else
            {
                cameraInterp = new GameObject("Camera Interp");
            }
            
            if (CameraPositionClone)
            {
                CameraPositionClone.SetActive(true);
            }
            else
            {
                CameraPositionClone = new GameObject("Empty Camera Position Clone");
            }
            
            currentPositionOffset = positionOffset;
            RefreshStatus();

            cameraInterp.transform.position = GetCameraTargetPosition();

            CameraPositionClone.transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * currentPositionOffset;
            CameraPositionClone.transform.LookAt(cameraInterp.transform);

            transform.position = CameraPositionClone.transform.position;
            transform.rotation = CameraPositionClone.transform.rotation;

            UpdateCamera();
        }

        private void OnDisable()
        {
            if (cameraInterp) { cameraInterp.SetActive(false); }
            if (CameraPositionClone) { CameraPositionClone.SetActive(false); }
            thisCam.enabled = false;
            orbitalCam.enabled = false;
        }

        private void OnDestroy()
        {
            Destroy(cameraInterp);
            Destroy(CameraPositionClone);
        }

        private const float killerRotationSpeed = 4;
        private const float killerRotationSlerpThreshold = 1;

        private void RefreshStatus()
        {
            thisCam.farClipPlane = FasterPlayerPrefs.Singleton.GetInt("RenderDistance");
            thisCam.fieldOfView = FasterPlayerPrefs.Singleton.GetFloat("FieldOfView");
            if (thisCam.TryGetComponent(out UniversalAdditionalCameraData data))
            {
                data.renderPostProcessing = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");
            }

            orbitalCam.farClipPlane = FasterPlayerPrefs.Singleton.GetInt("RenderDistance");
            orbitalCam.fieldOfView = FasterPlayerPrefs.Singleton.GetFloat("FieldOfView");
            if (orbitalCam.TryGetComponent(out data))
            {
                data.renderPostProcessing = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");
            }
        }

        private Vector3 GetCameraTargetPosition()
        {
            //cameraPivot.TransformPoint(Vector3.zero);
            return cameraPivot.position;
        }

        private static readonly Vector3 followTargetOffset = new Vector3(0, 3, -3);
        private static readonly Vector3 followTargetLookAtPositionOffset = new Vector3(0, 0.5f, 0);

        public const float orbitSpeed = 15;

        private float followCamAngleOffset;
        public void UpdateCamera()
        {
            if (attributes.GetAilment() == ActionClip.Ailment.Death) { SetOrbitalCameraState(false); }

            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            IsAnimating = animator.IsInTransition(0) ? !animator.GetNextAnimatorStateInfo(0).IsName("Empty") : !animator.GetCurrentAnimatorStateInfo(0).IsName("Empty");

            // Update camera interp transform
            if (movementHandler.TargetToLockOn)
            {
                Quaternion targetRot = Quaternion.LookRotation(movementHandler.TargetToLockOn.position + PlayerMovementHandler.targetSystemOffset - transform.position, Vector3.up);
                targetRotationX = -targetRot.eulerAngles.x;
                targetRotationY = targetRot.eulerAngles.y - 180;
            }
            else
            {
                Vector2 lookInput = IsAnimating ? Vector2.zero : movementHandler.GetLookInput();
                targetRotationX += lookInput.y;
                targetRotationY += lookInput.x;
                if (orbitalCam.enabled) { targetRotationX = 0; }
            }

            targetRotationX %= 360f;
            targetRotationY %= 360f;

            targetRotationX = Mathf.Clamp(targetRotationX, -maxPitch / 2.0f, maxPitch / 2.0f);

            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                if (movementHandler.CameraFollowTarget)
                {
                    Vector3 targetPosition = movementHandler.CameraFollowTarget.transform.position + movementHandler.CameraFollowTarget.transform.rotation * Quaternion.Euler(0, followCamAngleOffset, 0) * followTargetOffset;
                    Quaternion targetRotation = Quaternion.LookRotation(movementHandler.CameraFollowTarget.transform.position + followTargetLookAtPositionOffset - transform.position);

                    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 8);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8);

                    followCamAngleOffset += movementHandler.GetLookInput().x;
                }
                else
                {
                    NetworkObject killer = attributes.GetKiller();
                    if (killer)
                    {
                        Quaternion killerRotation = Quaternion.LookRotation(killer.transform.position - transform.position, Vector3.up);
                        if (Quaternion.Angle(transform.rotation, killerRotation) > killerRotationSlerpThreshold) { killerRotation = Quaternion.Slerp(transform.rotation, killerRotation, Time.deltaTime * killerRotationSpeed); }
                        transform.rotation = killerRotation;

                        currentPositionOffset = Vector3.MoveTowards(currentPositionOffset, weaponHandler.IsAiming() ? aimingPositionOffset : positionOffset, Time.deltaTime * aimingTransitionSpeed);
                        transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * currentPositionOffset;
                    }
                }
            }
            else
            {
                Quaternion targetCameraInterpRotation = Quaternion.Euler(targetRotationX, targetRotationY, 0);
                if (weaponHandler.IsAiming() | IsAnimating)
                {
                    cameraInterp.transform.position = GetCameraTargetPosition();
                    cameraInterp.transform.rotation = targetCameraInterpRotation;
                }
                else
                {
                    cameraInterp.transform.position = Vector3.SmoothDamp(
                       cameraInterp.transform.position,
                       GetCameraTargetPosition(),
                       ref _velocityPosition,
                       smoothTime
                    );

                    Vector3 eulers = Quaternion.Slerp(cameraInterp.transform.rotation, targetCameraInterpRotation, Time.deltaTime * orbitSpeed).eulerAngles;
                    eulers.z = 0;
                    cameraInterp.transform.eulerAngles = eulers;
                }

                currentPositionOffset = Vector3.MoveTowards(currentPositionOffset, weaponHandler.IsAiming() ? aimingPositionOffset : positionOffset, Time.deltaTime * aimingTransitionSpeed);

                CameraPositionClone.transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * currentPositionOffset;
                CameraPositionClone.transform.LookAt(cameraInterp.transform);

                if (IsAnimating)
                {
                    transform.position = CameraPositionClone.transform.position + animationPositionOffset;
                    transform.rotation = CameraPositionClone.transform.rotation * animationRotationOffset;
                }
                else
                {
                    transform.position = CameraPositionClone.transform.position;
                    transform.rotation = CameraPositionClone.transform.rotation;

                    // Move camera if there is a wall in the way
                    if (Application.isEditor) { Debug.DrawRay(cameraInterp.transform.position, cameraInterp.transform.forward * currentPositionOffset.z, Color.blue, Time.deltaTime); }
                    if (Physics.Raycast(cameraInterp.transform.position, cameraInterp.transform.forward, out RaycastHit hit, currentPositionOffset.z, LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
                    {
                        transform.position = cameraInterp.transform.position + cameraInterp.transform.rotation * new Vector3(0, 0, hit.distance + collisionPositionOffset);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            movementHandler.ResetLookInput();
        }

        public bool IsAnimating { get; private set; }

        public void PlayAnimation(string stateName)
        {
            animator.Play(stateName);
        }

        public Vector3 GetCamDirection()
        {
            return IsAnimating ? CameraPositionClone.transform.forward : transform.forward;
        }

        Vector3 animationPositionOffset;
        Quaternion animationRotationOffset;
        private void OnAnimatorMove()
        {
            if (IsAnimating)
            {
                animationPositionOffset += animator.deltaPosition;
                animationRotationOffset *= animator.deltaRotation;
            }
            else
            {
                animationPositionOffset = Vector3.zero;
                animationRotationOffset = Quaternion.identity;
            }
        }
    }
}