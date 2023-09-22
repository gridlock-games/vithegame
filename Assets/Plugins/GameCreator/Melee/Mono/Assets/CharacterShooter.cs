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
        [SerializeField] private AnimationClip aimDownSight;
        [SerializeField] private AvatarMask aimDownMask;
        [SerializeField] private Vector3 ADSModelRotation;

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

            shooterWeapon.Shoot(melee, attackClip, projectileSpeed);
        }

        private void Awake()
        {
            melee = GetComponent<CharacterMelee>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                CameraMotor motor = CameraMotor.MAIN_MOTOR;
                this.adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;
                this.adventureTargetOffset = this.adventureMotor.targetOffset;
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
            if (melee.IsBlocking) return;
            if (melee.IsStaggered) return;
            if (melee.IsCastingAbility) return;
            if (melee.Character.isCharacterDashing()) return;
            if (melee.Character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None) return;

            if (IsOwner)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    isAimedDown.Value = true;
                }
                if (Input.GetMouseButtonUp(1))
                {
                    isAimedDown.Value = false;
                }

                //if (Input.GetMouseButtonDown(1))
                //{
                //    isAimedDown = !isAimedDown;
                //}

                RaycastHit[] allHits = Physics.RaycastAll(UnityEngine.Camera.main.transform.position, UnityEngine.Camera.main.transform.forward, 100, Physics.AllLayers, QueryTriggerInteraction.Ignore);
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
                    aimPoint = UnityEngine.Camera.main.transform.position + UnityEngine.Camera.main.transform.forward * 5;

                this.aimPoint.Value = aimPoint;
            }

            PerformAimDownSight(isAimedDown.Value);
            handIK.AimRightHand(aimPoint.Value, shooterWeapon.GetAimOffset(), isAimedDown.Value);
        }

        private void PerformAimDownSight(bool isAimDown)
        {
            PlayADSAnim(melee, isAimDown);

            if (IsOwner)
            {
                UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
                CameraMotor motor = CameraMotor.MAIN_MOTOR;
                CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;

                adventureMotor.targetOffset = isAimDown ? new Vector3(0.15f, -0.15f, 1.50f) : this.adventureTargetOffset;
                mainCamera.fieldOfView = isAimDown ? 25.0f : 70.0f;
            }
            
        }

        private void PlayADSAnim(CharacterMelee melee, bool isAimedDown)
        {
            CharacterAnimator characterAnimator = melee.Character.GetCharacterAnimator();

            if (characterAnimator == null) { return; }
            if (aimDownSight == null) { return; }
            if (aimDownMask == null) { return; }

            if (isAimedDown)
            {
                characterAnimator.CrossFadeGesture(
                    aimDownSight, 0.25f, aimDownMask,
                    0.15f, 0.15f
                );
            }
            else
            {
                characterAnimator.StopGesture(0.0f);
            }

            if (isAimedDown)
            {
                limbReferences.transform.localRotation = Quaternion.Euler(ADSModelRotation);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(aimPoint.Value, 0.25f);
        }
    }
}