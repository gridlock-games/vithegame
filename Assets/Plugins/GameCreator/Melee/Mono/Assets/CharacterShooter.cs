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

        private CameraMotorTypeAdventure adventureMotor = null;
        private bool isAimedDown = false;
        private Vector3 adventureTargetOffset;
        private CharacterMelee melee;
        private ShooterComponent shooterWeapon;
        private CharacterHandIK handIK;
        private LimbReferences limbReferences;

        public void Shoot(MeleeClip attackClip)
        {
            if (!IsServer) { Debug.LogError("CharacterShooter.Shoot() should only be called on the server"); return; }

            shooterWeapon.Shoot(melee, attackClip);
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
            if (!IsOwner) return;
            if (melee == null) return;
            if (melee.IsBlocking) return;
            if (melee.IsStaggered) return;
            if (melee.IsCastingAbility) return;
            if (melee.Character.isCharacterDashing()) return;
            if (melee.Character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None) return;

            if (!handIK) handIK = GetComponentInChildren<CharacterHandIK>();
            if (!shooterWeapon) shooterWeapon = GetComponentInChildren<ShooterComponent>();
            if (!limbReferences) limbReferences = GetComponentInChildren<LimbReferences>();

            if (!handIK) return;
            if (!shooterWeapon) return;
            if (!limbReferences) return;

            if (Input.GetMouseButtonDown(1))
            {
                isAimedDown = true;
            }
            if (Input.GetMouseButtonUp(1))
            {
                isAimedDown = false;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                Shoot(null);
            }

            //if (Input.GetMouseButtonDown(1))
            //{
            //    isAimedDown = !isAimedDown;
            //}

            PerformAimDownSight(isAimedDown);
        }

        private void PerformAimDownSight(bool isAimDown)
        {
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            CameraMotor motor = CameraMotor.MAIN_MOTOR;
            CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;

            this.PlayADSAnim(melee, isAimDown);

            adventureMotor.targetOffset = isAimDown ? new Vector3(0.15f, -0.15f, 1.50f) : this.adventureTargetOffset;
            mainCamera.fieldOfView = isAimDown ? 25.0f : 70.0f;
        }

        public void PlayADSAnim(CharacterMelee melee, bool isAimedDown)
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

            this.aimPoint = aimPoint;
            Quaternion aimOffset = shooterWeapon.GetAimOffset();
            handIK.AimRightHand(aimPoint, aimOffset, isAimedDown);
        }

        Vector3 aimPoint;
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(aimPoint, 0.5f);
        }
    }
}