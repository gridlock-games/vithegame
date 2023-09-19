using System;
using System.Collections;
using System.Collections.Generic;
using GameCreator.Camera;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.Netcode;
using GameCreator.Variables;
using GameCreator.Camera;
using GameCreator.Characters;
using GameCreator.Melee;


public class CharacterShooter : NetworkBehaviour
{
    [SerializeField] private int clipSize;
    [SerializeField] private int magCapacity;
    [SerializeField] private float reloadTime;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private AnimationClip aimDownSight;
    [SerializeField] public AvatarMask aimDownMask;


    private CameraMotorTypeAdventure adventureMotor = null;
    private bool isAimedDown = false;
    private Vector3 adventureTargetOffset;
    private CharacterMelee melee;

    private void Awake()
    {
        melee = GetComponentInParent<CharacterMelee>();
    }

    protected virtual void Start()
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
        // if (!Input.anyKeyDown) return;
        if (melee == null) return;
        if (melee.IsBlocking) return;
        if (melee.IsStaggered) return;
        if (melee.IsCastingAbility) return;
        if (melee.Character.isCharacterDashing()) return;
        if (melee.Character.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None) return;


        if (Input.GetMouseButtonDown(1))
        {
            this.isAimedDown = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            this.isAimedDown = false;
        }
        
        PerformAimDownSight(isAimedDown);
    }

    private void PerformAimDownSight(bool isAimDown)
    {
        Camera mainCamera = Camera.main;
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
    }
}