using System;
using System.Collections;
using System.Collections.Generic;
using GameCreator.Camera;
using UnityEngine;
using Unity.Netcode;
using GameCreator.Characters;

namespace GameCreator.Melee
{
    public class CharacterShooter : NetworkBehaviour
    {
        [SerializeField] private int clipSize;
        [SerializeField] private int magCapacity;
        [SerializeField] private float reloadTime;
        [SerializeField] private float projectileSpeed = 10;
        [SerializeField] private float ADSRunSpeed = 3;
        [SerializeField] private Vector3 ADSModelRotation;
        [SerializeField] private UnityEngine.Camera ADSCamera;
        [SerializeField] private Transform ADSCamPivot;

        private NetworkVariable<bool> isAimedDown = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> aimPoint = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private CameraMotorTypeAdventure adventureMotor = null;
        private Vector3 adventureTargetOffset;
        private CharacterMelee melee;

        private ShooterComponent shooterWeapon;
        private CharacterHandIK handIK;
        private LimbReferences limbReferences;

        public void Shoot(MeleeClip attackClip)
        {
            if (!IsServer) { Debug.LogError("CharacterShooter.Shoot() should only be called on the server"); return; }

            //Debug.Log(Time.time + " shoot called");
            shooterWeapon.Shoot(melee, attackClip, projectileSpeed);
        }

        public bool IsAiming()
        {
            return isAimedDown.Value;
        }

        private Vector3 originalADSCamLocalPos;
        private Quaternion originalADSCamLocalRot;

        private void Awake()
        {
            melee = GetComponent<CharacterMelee>();
            originalADSCamLocalPos = ADSCamera.transform.localPosition;
            originalADSCamLocalRot = ADSCamera.transform.localRotation;

            ADSCamera.gameObject.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                CameraMotor motor = CameraMotor.MAIN_MOTOR;
                this.adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;
                this.adventureTargetOffset = this.adventureMotor.targetOffset;

                ADSCamera.gameObject.SetActive(true);
            }
            else
            {
                Destroy(ADSCamera.gameObject);
            }
        }

        void Update()
        {
            if (!handIK) handIK = GetComponentInChildren<CharacterHandIK>();
            if (!shooterWeapon) shooterWeapon = GetComponentInChildren<ShooterComponent>();
            if (!limbReferences) limbReferences = GetComponentInChildren<LimbReferences>();

            if (!handIK) return;
            if (!shooterWeapon) return;
            if (!limbReferences) return;

            if (melee == null) return;

            if (IsOwner)
            {
                if (melee.Character.isCharacterDashing() | melee.IsBlocking | melee.IsStaggered | melee.IsCastingAbility | melee.Character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None)
                {
                    isAimedDown.Value = false;
                }
                else
                {
                    //isAimedDown.Value = Input.GetMouseButton(1);

                    if (Input.GetMouseButtonDown(1))
                    {
                        isAimedDown.Value = !isAimedDown.Value;
                    }
                }

                RaycastHit[] allHits = Physics.RaycastAll(ADSCamera.transform.position, ADSCamera.transform.forward, 100, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                Vector3 aimPoint = Vector3.zero;
                bool bHit = false;
                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform == transform) { continue; }

                    aimPoint = hit.point;
                    bHit = true;
                    break;
                }

                if (!bHit)
                    aimPoint = ADSCamera.transform.position + ADSCamera.transform.forward * 5;

                this.aimPoint.Value = aimPoint;
            }

            PerformAimDownSight(isAimedDown.Value);
            Vector3 leftHandPos = shooterWeapon.GetLeftHandTarget() != null ? shooterWeapon.GetLeftHandTarget().position : new Vector3(0, 0, 0);
            Quaternion leftHandRot = shooterWeapon.GetLeftHandTarget() != null ? shooterWeapon.GetLeftHandTarget().rotation : new Quaternion(0, 0, 0, 0);

            handIK.AimRightHand(aimPoint.Value, shooterWeapon.GetAimOffset(), isAimedDown.Value, shooterWeapon.GetLeftHandTarget(), leftHandPos, leftHandRot);
        }

        [SerializeField] private float maxADSPitch = 90;
        float camAngle = 0;
        private void PerformAimDownSight(bool isAimDown)
        {
            if (IsServer)
            {
                if (isAimDown)
                    melee.SetRunSpeed(ADSRunSpeed);
                else
                    melee.ResetRunSpeed();
            }

            if (IsOwner)
            {
                UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
                CameraMotor motor = CameraMotor.MAIN_MOTOR;
                CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;

                if (isAimDown & !ADSCamera.enabled)
                {
                    ADSCamera.transform.localPosition = originalADSCamLocalPos;
                    ADSCamera.transform.localRotation = originalADSCamLocalRot;

                    Vector3 mainCamForward = UnityEngine.Camera.main.transform.forward;
                    float mainCamAngle = Vector3.Angle(mainCamForward, transform.forward);
                    if (mainCamForward.y > 0) { mainCamAngle *= -1; }
                    ADSCamera.transform.RotateAround(ADSCamPivot.position, transform.right, mainCamAngle);

                    camAngle = mainCamAngle;
                }
                else if (!isAimDown & ADSCamera.enabled)
                {
                    Vector3 mainCamForward = UnityEngine.Camera.main.transform.forward;
                    float mainCamAngle = Vector3.Angle(mainCamForward, transform.forward);
                    if (mainCamForward.y > 0) { mainCamAngle *= -1; }

                    Vector3 adsCamForward = ADSCamera.transform.forward;
                    float adsCamAngle = Vector3.Angle(adsCamForward, transform.forward);
                    if (adsCamForward.y > 0) { adsCamAngle *= -1; }
                    
                    adventureMotor.AddRotation(0, mainCamAngle - adsCamAngle);
                }
                else if (ADSCamera.enabled)
                {
                    float mouseY = -Input.GetAxisRaw("Mouse Y") * adventureMotor.sensitivity.GetValue(gameObject).y * Time.timeScale;
                    float mouseX = Input.GetAxisRaw("Mouse X") * adventureMotor.sensitivity.GetValue(gameObject).x * Time.timeScale;

                    if (camAngle + mouseY > maxADSPitch)
                    {
                        mouseY = maxADSPitch - camAngle;
                    }
                    else if (camAngle + mouseY < -maxADSPitch)
                    {
                        mouseY = -(Math.Abs(maxADSPitch) - Math.Abs(camAngle));
                    }

                    camAngle += mouseY;
                    ADSCamera.transform.RotateAround(ADSCamPivot.position, transform.right, mouseY);
                    adventureMotor.AddRotation(mouseX, 0);
                }

                ADSCamera.enabled = isAimDown;
                adventureMotor.allowOrbitInput = !isAimDown;

                adventureMotor.targetOffset = isAimDown ? new Vector3(0.15f, -0.15f, 1.50f) : this.adventureTargetOffset;
                mainCamera.fieldOfView = isAimDown ? 25.0f : 70.0f;
            }

            PlayADSAnim(melee, isAimDown, camAngle);
        }

        private NetworkVariable<float> aimAnglePercentage = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private void PlayADSAnim(CharacterMelee melee, bool isAimedDown, float verticalAimAngle)
        {
            CharacterAnimator characterAnimator = melee.Character.GetCharacterAnimator();

            if (characterAnimator == null) { return; }

            characterAnimator.animator.SetBool("IsAiming", isAimedDown);

            if (IsOwner) { aimAnglePercentage.Value = verticalAimAngle / maxADSPitch; }
            characterAnimator.animator.SetFloat("AimAngle", aimAnglePercentage.Value);

            if (isAimedDown)
            {
                limbReferences.transform.localRotation = Quaternion.Slerp(limbReferences.transform.localRotation, Quaternion.Euler(ADSModelRotation), Time.deltaTime * 10);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(aimPoint.Value, 0.25f);
        }
    }
}