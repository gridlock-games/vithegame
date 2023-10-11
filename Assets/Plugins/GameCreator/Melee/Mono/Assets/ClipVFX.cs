using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Melee;
using GameCreator.Core;
using Unity.Netcode;

public class ClipVFX : MonoBehaviour
{
    public enum ATTACHMENT_TYPE
    {
        AttachSelf,
        AttachTarget,
        DetachedSelf,
        Projectile,
        StickToGround
    }

    public TargetGameObject abilityVFX = new TargetGameObject();
    public Vector3 vfxPositionOffset = new Vector3(0, 0, 0);
    public Vector3 vfxRotationOffset = new Vector3(0, 0, 0);
    public MeleeClip.AttachVFXPhase attachVFXOnPhase = MeleeClip.AttachVFXPhase.OnExecute;
    public ATTACHMENT_TYPE attachmentType = ATTACHMENT_TYPE.DetachedSelf;
    [Header("For Attachment Type: StickToGround")]
    public Vector3 raycastOffset = new Vector3(0, 2, 0);
    public Vector3 crossProductDirection = new Vector3(1, 0, 0);
    public Vector3 lookRotationUpDirection = new Vector3(0, 1, 0);
}