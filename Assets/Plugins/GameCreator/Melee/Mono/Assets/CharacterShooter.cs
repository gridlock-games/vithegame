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
        [Header("Projectile Setttings")]
        [SerializeField] private Vector3 projectileForce = new Vector3(0, 0, 10);
        [Header("ADS Settings")]
        [SerializeField] private CharacterState ADSState;
        [SerializeField] private UnityEngine.Camera ADSCamera;
        [SerializeField] private Transform ADSCamPivot;
        [SerializeField] private bool aimLeftHand = true;
        [SerializeField] private bool aimDuringAttackAnticipation = true;
        [SerializeField] private float ADSRunSpeed = 3;
        [SerializeField] private float maxADSPitch = 90;
        [Header("Reload Settings")]
        public bool enableReload;
        [SerializeField] private int magSize;

        private NetworkVariable<int> currentAmmo = new NetworkVariable<int>(1);

        private NetworkVariable<bool> isAimedDown = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Vector3> aimPoint = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private CameraMotorTypeAdventure adventureMotor = null;
        private Vector3 adventureTargetOffset;
        private CharacterMelee melee;

        private ShooterComponent shooterWeapon;
        private CharacterHandIK handIK;
        private LimbReferences limbReferences;

        private int shootCount;
        private float lastShootTime;

        public void Shoot(MeleeClip attackClip)
        {
            if (!IsServer) { Debug.LogError("CharacterShooter.Shoot() should only be called on the server"); return; }

            if (!CanShoot(attackClip)) { return; }

            if (!handIK.IsRightHandAiming())
                Debug.LogError(Time.time + " " + handIK.IsRightHandAiming());

            shooterWeapon.Shoot(melee, attackClip, projectileForce);

            lastShootTime = Time.time;
            shootCount++;
            currentAmmo.Value -= 1;
        }

        public void ResetShootCount() { shootCount = 0; }

        public UnityEngine.Camera GetADSCamera() { return ADSCamera; }

        public bool IsAiming() { return isAimedDown.Value; }

        public bool IsReloading() { return reloading.Value; }

        public int GetCurrentAmmo() { return currentAmmo.Value; }

        public int GetMagSize() { return magSize; }

        private bool CanShoot(MeleeClip attackClip)
        {
            if (reloading.Value) { return false; }
            if (enableReload & currentAmmo.Value <= 0) { return false; }
            if (shootCount >= attackClip.hitCount) { return false; }
            if (Time.time - lastShootTime < attackClip.multiHitRegDelay) { return false; }

            return true;
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
            isAimedDown.OnValueChanged += OnAimChange;
            reloading.OnValueChanged += OnReloadingChange;
            currentAmmo.OnValueChanged += OnAmmoChange;

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

            if (IsServer)
                currentAmmo.Value = magSize;
        }

        public override void OnNetworkDespawn()
        {
            isAimedDown.OnValueChanged -= OnAimChange;
            reloading.OnValueChanged -= OnReloadingChange;
            currentAmmo.OnValueChanged -= OnAmmoChange;
        }

        private void OnAimChange(bool prev, bool current)
        {
            if (reloading.Value)
            {
                if (changeStateAfterReloadCoroutine != null)
                {
                    StopCoroutine(changeStateAfterReloadCoroutine);
                }
                changeStateAfterReloadCoroutine = StartCoroutine(ChangeStateAfterReload(current));
            }
            else
            {
                if (current)
                {
                    melee.ChangeState(
                        ADSState,
                        melee.currentWeapon.characterMask,
                        MeleeWeapon.LAYER_STANCE,
                        melee.GetComponent<CharacterAnimator>()
                    );
                }
                else
                {
                    melee.ChangeState(
                        melee.currentWeapon.characterState,
                        melee.currentWeapon.characterMask,
                        MeleeWeapon.LAYER_STANCE,
                        melee.GetComponent<CharacterAnimator>()
                    );
                }
            }
        }

        private Coroutine changeStateAfterReloadCoroutine;
        private IEnumerator ChangeStateAfterReload(bool current)
        {
            yield return new WaitUntil(() => !reloading.Value);
            if (aimStateOnReload == current) { yield break; }
            if (current)
            {
                melee.ChangeState(
                    ADSState,
                    melee.currentWeapon.characterMask,
                    MeleeWeapon.LAYER_STANCE,
                    melee.GetComponent<CharacterAnimator>()
                );
            }
            else
            {
                melee.ChangeState(
                    melee.currentWeapon.characterState,
                    melee.currentWeapon.characterMask,
                    MeleeWeapon.LAYER_STANCE,
                    melee.GetComponent<CharacterAnimator>()
                );
            }
        }

        private bool aimStateOnReload;
        private void OnReloadingChange(bool prev, bool current)
        {
            melee.Character.GetCharacterAnimator().animator.SetBool("Reload", current);
            if (current)
            {
                aimStateOnReload = isAimedDown.Value;
                waitForAmmoChange = true;
            }
            else if (IsServer)
            {
                currentAmmo.Value = magSize;
            }
        }

        private void OnAmmoChange(int prev, int current)
        {
            waitForAmmoChange = false;
        }

        public bool triggerReload;
        private NetworkVariable<bool> reloading = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private bool reloadReached;
        private bool waitForAmmoChange;

        void Update()
        {
            if (!handIK) handIK = GetComponentInChildren<CharacterHandIK>();
            if (!shooterWeapon) shooterWeapon = GetComponentInChildren<ShooterComponent>();
            if (!limbReferences) limbReferences = GetComponentInChildren<LimbReferences>();

            if (!handIK) return;
            if (!shooterWeapon) return;
            if (!limbReferences) return;

            if (melee == null) return;

            Animator animator = melee.Character.GetCharacterAnimator().animator;
            if (IsOwner)
            {
                if (melee.Character.isCharacterDashing() | melee.IsBlocking.Value | melee.IsStaggered | melee.Character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None)
                {
                    isAimedDown.Value = false;
                }
                else if (melee.IsCastingAbility.Value)
                {
                    if (!melee.abilityManager.GetActivatedAbility().allowADS)
                    {
                        isAimedDown.Value = false;
                    }
                    else
                    {
                        isAimedDown.Value = Input.GetMouseButton(1);
                    }
                }
                else
                {
                    isAimedDown.Value = Input.GetMouseButton(1);

                    //if (Input.GetMouseButtonDown(1))
                    //{
                    //    isAimedDown.Value = !isAimedDown.Value;
                    //}

                    if (enableReload & !melee.IsAttacking)
                    {
                        if (((Input.GetKeyDown(KeyCode.Z) | triggerReload) & currentAmmo.Value < magSize) | (currentAmmo.Value <= 0 & !waitForAmmoChange))
                        {
                            reloading.Value = true;
                        }
                    }
                }

                Vector3 aimPoint = Vector3.zero;
                if (isAimedDown.Value)
                {
                    RaycastHit[] allHits = Physics.RaycastAll(ADSCamera.transform.position, ADSCamera.transform.forward, 100, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                    Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
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
                }
                else
                {
                    aimPoint = UnityEngine.Camera.main.transform.position + UnityEngine.Camera.main.transform.forward * 10;
                }

                this.aimPoint.Value = aimPoint;
            }

            PerformAimDownSight(isAimedDown.Value);

            int phase = melee.GetCurrentPhase();
            bool shouldAim = isAimedDown.Value;
            bool shouldAimLeftHand = aimLeftHand;

            if (melee.IsCastingAbility.Value)
            {
                switch (phase)
                {
                    case -1:
                        shouldAim = false;
                        shouldAimLeftHand = false;
                        break;
                    case 0:
                        shouldAim = melee.GetActivatedAbility().aimDuringAttackAnticipation;
                        shouldAimLeftHand = shouldAim & melee.GetActivatedAbility().aimLeftHand;
                        break;
                    case 1:
                        shouldAim = melee.GetActivatedAbility().aimDuringAttack;
                        shouldAimLeftHand = shouldAim & melee.GetActivatedAbility().aimLeftHand;
                        break;
                    case 2:
                        shouldAim = melee.GetActivatedAbility().aimDuringAttackRecovery;
                        shouldAimLeftHand = shouldAim & melee.GetActivatedAbility().aimLeftHand;
                        break;
                }
            }
            else
            {
                shouldAim = isAimedDown.Value;

                if (!aimDuringAttackAnticipation)
                {
                    if (phase == 0)
                    {
                        shouldAim = false;
                    }
                }

                if (phase == 1)
                {
                    shouldAim = true;
                }
            }

            shouldAimLeftHand = aimLeftHand & shouldAimLeftHand;

            if (IsOwner)
            {
                if (reloading.Value)
                {
                    if (reloadReached)
                    {
                        if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Reload")).IsName("Reload"))
                        {
                            reloading.Value = false;
                            reloadReached = false;
                        }
                    }
                    else
                    {
                        if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Reload")).IsName("Reload"))
                        {
                            reloadReached = true;
                        }
                    }
                }
            }

            handIK.AimRightHand(aimPoint.Value,
                   Quaternion.Euler(limbReferences.rightHandAimIKOffset),
                   shouldAim & !reloading.Value,
                   shouldAimLeftHand & !reloading.Value,
                   this);

            triggerReload = false;
        }

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

                    if (adventureMotor.invertLookFearStatus)
                    {
                        mouseY *= -1;
                        mouseX *= -1;
                    }

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
            characterAnimator.animator.SetFloat("AimAngle", 0); // aimAnglePercentage.Value
        }
    }
}