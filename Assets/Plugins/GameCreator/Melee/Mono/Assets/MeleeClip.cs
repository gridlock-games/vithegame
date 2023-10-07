namespace GameCreator.Melee
{
    using System.Collections;
    using System.Collections.Generic;
    using GameCreator.Core;
    using UnityEngine;
    using GameCreator.Camera;
    using Unity.Netcode;
    using UnityEngine.VFX;

    [CreateAssetMenu(
        fileName = "Melee Clip",
        menuName = "Game Creator/Melee/Melee Clip"
    )]
    public class MeleeClip : ScriptableObject
    {
        public enum Interrupt
        {
            Interruptible,
            Uninterruptible,
        }

        public enum Vulnerable
        {
            Vulnerable,
            Invincible,
        }

        public enum Posture
        {
            Steady,
            Stagger,
        }

        public enum AttackType
        {
            None,
            Stun,
            Knockdown,
            Knockedup,
            Heavy,
            Stagger,
            Followup,
            Pull,
            Grab
        }

        // STATIC & CONSTS: -----------------------------------------------------------------------

        private const int HITPAUSE_TIME_LAYER = 80;

        private static readonly Keyframe[] DEFAULT_KEY_MOVEMENT = {
            new Keyframe(0f, 0f),
            new Keyframe(1f, 0f)
        };

        // PROPERTIES: ----------------------------------------------------------------------------

        public AnimationClip animationClip;
        public AnimationClip attackDodgeClip;
        public AvatarMask avatarMask;
        public float transitionIn = 0.25f;
        public float transitionOut = 0.25f;

        // movement:
        public AnimationCurve movementForward = new AnimationCurve(DEFAULT_KEY_MOVEMENT);
        public AnimationCurve movementSides = new AnimationCurve(DEFAULT_KEY_MOVEMENT);
        public AnimationCurve movementVertical = new AnimationCurve(DEFAULT_KEY_MOVEMENT);

        // Lunge Attack Purposes:
        public AnimationCurve movementForward_OnLunge = new AnimationCurve(DEFAULT_KEY_MOVEMENT);
        public AnimationCurve movementSides_OnLunge = new AnimationCurve(DEFAULT_KEY_MOVEMENT);

        [Range(0f, 1f)] public float gravityInfluence = 1f;
        public float movementMultiplier = 1.0f;

        // Dodge Attack Purposes:
        public AnimationCurve movementForward_OnAttack = new AnimationCurve(DEFAULT_KEY_MOVEMENT);
        public AnimationCurve movementSides_OnAttack = new AnimationCurve(DEFAULT_KEY_MOVEMENT);
        public AnimationCurve movementVertical_OnAttack = new AnimationCurve(DEFAULT_KEY_MOVEMENT);

        [Range(0f, 1f)] public float gravityInfluence_OnAttack = 1f;
        public float movementMultiplier_OnAttack = 1.0f;


        // audio:
        public AudioClip soundEffect;

        // hit pause:
        public bool hitPause = false;
        [Range(0f, 1f)]
        public float hitPauseAmount = 0.05f;
        public float hitPauseDuration = 0.05f;

        // dodge:
        public bool isDodge = false;

        // attack:
        public bool isAttack = true;
        public bool isHeavy = false;
        public AttackType attackType = AttackType.None;
        public float grabDistance = 1;
        public float grabDuration = 0.5f;

        public bool isBlockable = true;
        public float pushForce = 50f;

        public float poiseDamage = 2f;
        public float defenseDamage = 1f;
        public int baseDamage = 10;

        public float bladeSizeMultiplier = 1.0f;


        // properties:
        public Interrupt interruptible = Interrupt.Interruptible;
        public Vulnerable vulnerability = Vulnerable.Vulnerable;
        public Posture posture = Posture.Steady;
        public bool isOrbitLocked = false;
        public bool applyRootMotion = true;
        public bool isLunge = false;
        private float disableOrbitDuration = 0.0f;
        public bool isModifyFocus = false;
        public Vector3 boxCastHalfExtents = new Vector3(2.0f, 1.0f, 2.0f);


        // abilities

        public int hitCount = 1;
        public float multiHitRegDelay = 1.0f;
        public bool isSequence = false;
        public List<MeleeClip> sequencedClips = new List<MeleeClip>();


        public enum AttachVFXPhase
        {
            OnExecute,
            OnActivate,
            OnHit,
            OnRecovery
        }

        public List<ClipVFX> vfxList = new List<ClipVFX>();


        // VFX:
        public TargetGameObject abilityVFX = new TargetGameObject();
        public Vector3 vfxPositionOffset = new Vector3(0, 0, 0);
        public Vector3 vfxRotationOffset = new Vector3(0, 0, 0);
        public AttachVFXPhase attachVFXOnPhase = AttachVFXPhase.OnExecute;


        // animation:
        public float animSpeed = 1.0f;
        public AnimationCurve attackPhase = new AnimationCurve(
            new Keyframe(0.00f, 0f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.50f, 2f),
            new Keyframe(1.00f, 2f)
        );

        public float Length { get => this.animationClip == null ? 0f : this.animationClip.length; }

        public TargetGameObject attackSpawnVFX = new TargetGameObject();

        private WaitForSecondsRealtime hitPauseCoroutine;

        public IActionsList actionsOnExecute;
        public IActionsList actionOnActivate;
        public IActionsList actionsOnHit;

        public List<MeleeWeapon.WeaponBone> affectedBones = new List<MeleeWeapon.WeaponBone> { MeleeWeapon.WeaponBone.RightHand };

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public void PlayVFXAttachment(CharacterMelee character)
        {
            if (this.vfxList.Count < 0) return;
            if (character == null) return;

            foreach (ClipVFX vfx in this.vfxList)
            {
                if (vfx.gameObject == null) return;

                GameObject abilityVFXPrefab = vfx.abilityVFX.GetGameObject(character.gameObject);

                Vector3 vfxPositionOffset = vfx.attachmentType == ClipVFX.ATTACHMENT_TYPE.AttachSelf ?  new Vector3(0, 0, 0) : vfx.vfxPositionOffset;
                Vector3 vfxRotationOffset = vfx.attachmentType == ClipVFX.ATTACHMENT_TYPE.AttachSelf ?  new Vector3(0, 0, 0) : vfx.vfxRotationOffset;

                GameObject abilityVFXInstance = Instantiate(abilityVFXPrefab,
                    character.transform.position + character.transform.rotation * vfxPositionOffset,
                    character.transform.rotation * Quaternion.Euler(vfxRotationOffset),
                    vfx.attachmentType == ClipVFX.ATTACHMENT_TYPE.AttachSelf ? character.transform : null
                    );

                if (abilityVFXInstance.TryGetComponent(out ParticleSystemProjectile dmg))
                {
                    dmg.Initialize(character, this, Vector3.zero);
                }

                CoroutinesManager.Instance.StartCoroutine(DestroyAfterEffectsFinish(abilityVFXInstance));
            }
        }

        private IEnumerator DestroyAfterEffectsFinish(GameObject obj)
        {
            ParticleSystem particleSystem = obj.GetComponentInChildren<ParticleSystem>();
            if (particleSystem) { yield return new WaitUntil(() => !particleSystem.isPlaying); }

            AudioSource audioSource = obj.GetComponentInChildren<AudioSource>();
            if (audioSource) { yield return new WaitUntil(() => !audioSource.isPlaying); }

            VisualEffect visualEffect = obj.GetComponentInChildren<VisualEffect>();
            if (visualEffect) { yield return new WaitUntil(() => !visualEffect.HasAnySystemAwake()); }

            Destroy(obj);
        }

        public void PlayNetworked(CharacterMelee melee, float? animSpeed = null, float? transitionIn = null, float? transitionOut = null)
        {
            if (!melee.IsSpawned) { Debug.LogError("Spawn the character before trying to play melee clips"); return; }

            if (melee.NetworkManager.IsServer)
            {
                melee.PropogateMeleeClipChange(this);
            }
            else
            {
                //Debug.LogError("PlayNetworked() is only supposed to be called on the server");
                return;
            }

            // If we are not a client (the host), play the clip
            if (!melee.NetworkManager.IsClient)
                PlayLocally(melee);
        }

        public void PlayLocally(CharacterMelee melee, float? animSpeed = null, float? transitionIn = null, float? transitionOut = null)
        {
            if (this.interruptible == Interrupt.Uninterruptible) melee.SetUninterruptable(this.Length);
            if (this.vulnerability == Vulnerable.Invincible) melee.SetInvincibility(isDodge ? this.Length * 0.35f : this.Length);

            if (this.isAttack)
            {
                if (CameraMotor.MAIN_MOTOR)
                {
                    CameraMotor motor = CameraMotor.MAIN_MOTOR;

                    this.disableOrbitDuration = (this.animationClip.length * 0.35f);

                    if (motor != null && motor.cameraMotorType.GetType() == typeof(CameraMotorTypeAdventure))
                    {
                        CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;

                        CoroutinesManager.Instance.StartCoroutine(this.EnableOrbitRoutine());
                    }
                }
            }

            melee.SetPosture(this.posture, this.Length);
            melee.PlayAudio(this.soundEffect);

            float selectedAnimSpeed = animSpeed == null ? this.animSpeed : (float)animSpeed;
            float selectedTransitionIn = transitionIn == null ? this.transitionIn : (float)transitionIn;
            float selectedTransitionOut = transitionOut == null ? this.transitionOut : (float)transitionOut;

            float duration = Mathf.Max(0, this.animationClip.length - selectedTransitionOut);

            if (isAttack)
            {
                if (applyRootMotion)
                {
                    AnimationCurve newMovementForwardCurve = melee.isLunging ? melee.movementForward : this.movementForward;
                    AnimationCurve newMovementSidesCurve = melee.isLunging ? new AnimationCurve(DEFAULT_KEY_MOVEMENT) : this.movementSides;
                    melee.Character.RootMovement(
                        this.movementMultiplier,
                        duration / selectedAnimSpeed,
                        this.gravityInfluence,
                        newMovementForwardCurve,
                        newMovementSidesCurve,
                        this.movementVertical
                    );
                }

                melee.Character.GetCharacterAnimator().StopGesture(0.1f);
                melee.Character.GetCharacterAnimator().CrossFadeGesture(
                    this.animationClip, selectedAnimSpeed, this.avatarMask,
                    selectedTransitionIn, selectedTransitionOut
                );
            }
            else if (isDodge)
            {
                melee.Character.RootMovement(
                    !melee.IsAttacking ? movementMultiplier : movementMultiplier_OnAttack,
                    duration,
                    1.0f,
                    !melee.IsAttacking ? movementForward : movementForward_OnAttack,
                    !melee.IsAttacking ? movementSides : movementSides_OnAttack,
                    !melee.IsAttacking ? movementVertical : movementVertical_OnAttack
                );

                melee.Character.GetCharacterAnimator().StopGesture(0.1f);
                melee.Character.GetCharacterAnimator().CrossFadeGesture(
                    !melee.IsAttacking ? animationClip : attackDodgeClip,
                    selectedAnimSpeed, this.avatarMask,
                    selectedTransitionIn, selectedTransitionOut
                );

                melee.StartCoroutine(DashValueRoutine(melee, false, animationClip.length ));
            }
            else
            {
                melee.Character.RootMovement(
                    this.movementMultiplier,
                    duration / this.animSpeed,
                    this.gravityInfluence,
                    this.movementForward,
                    this.movementSides,
                    this.movementVertical
                );

                melee.Character.GetCharacterAnimator().StopGesture(0.1f);
                melee.Character.GetCharacterAnimator().CrossFadeGesture(
                    this.animationClip, selectedAnimSpeed, this.avatarMask,
                    selectedTransitionIn, selectedTransitionOut
                );
            }

            this.ExecuteActionsOnStart(melee.transform.position, melee.gameObject);
        }

        public void Stop(CharacterMelee melee)
        {
            melee.Character.RootMovement(
                0,
                0,
                this.gravityInfluence,
                this.movementForward,
                this.movementSides,
                this.movementVertical
            );
        }

        public void ExecuteHitPause()
        {
            if (!this.hitPause) return;
            
            TimeManager.Instance.SetTimeScale(this.hitPauseAmount, HITPAUSE_TIME_LAYER);
            CoroutinesManager.Instance.StartCoroutine(this.ExecuteHitPause(
                this.hitPauseDuration
            ));
        }

        public void ExecuteActionsOnStart(Vector3 position, GameObject target)
        {
            if (this.actionsOnExecute)
            {
                GameObject actionsInstance = Instantiate<GameObject>(
                    this.actionsOnExecute.gameObject,
                    position,
                    Quaternion.identity
                );

                actionsInstance.hideFlags = HideFlags.HideInHierarchy;
                Actions actions = actionsInstance.GetComponent<Actions>();

                if (!actions) return;
                actions.Execute(target, null);
            }
        }

        public void ExecuteActionsOnHit(Vector3 position, GameObject target)
        {
            if (this.actionsOnHit)
            {
                GameObject actionsInstance = Instantiate<GameObject>(
                    this.actionsOnHit.gameObject,
                    position,
                    Quaternion.identity
                );

                actionsInstance.hideFlags = HideFlags.HideInHierarchy;
                Actions actions = actionsInstance.GetComponent<Actions>();

                if (!actions) return;
                actions.Execute(target, null);
            }
        }

        public void ExecuteActionsOnActivate(Vector3 position, GameObject target)
        {
            if (this.actionOnActivate)
            {
                GameObject actionsInstance = Instantiate<GameObject>(
                    this.actionOnActivate.gameObject,
                    position,
                    Quaternion.identity
                );

                actionsInstance.hideFlags = HideFlags.HideInHierarchy;
                Actions actions = actionsInstance.GetComponent<Actions>();

                if (!actions) return;
                actions.Execute(target, null);
            }
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private IEnumerator EnableOrbitRoutine()
        {
            CameraMotor motor = CameraMotor.MAIN_MOTOR;
            if (motor != null && motor.cameraMotorType.GetType() == typeof(CameraMotorTypeAdventure))
            {
                float initTime = Time.time;
                CameraMotorTypeAdventure adventureMotor = (CameraMotorTypeAdventure)motor.cameraMotorType;

                while (initTime + this.disableOrbitDuration >= Time.time)
                {
                    adventureMotor.orbitSpeed = 200.00f;

                    yield return null;
                }

                adventureMotor.orbitSpeed = 15.00f;
            }

            yield return 0;
        }

        private IEnumerator DashValueRoutine(CharacterMelee melee, bool value, float duration)
        {
            yield return new WaitForSeconds(duration * 0.90f);
            
           melee.Character.SetCharacterDashing(false);
        }

        private IEnumerator ExecuteHitPause(float duration)
        {
            if (this.hitPauseCoroutine != null)
            {
                CoroutinesManager.Instance.StopCoroutine(this.hitPauseCoroutine);
            }

            this.hitPauseCoroutine = new WaitForSecondsRealtime(duration);
            yield return this.hitPauseCoroutine;

            TimeManager.Instance.SetTimeScale(1f, HITPAUSE_TIME_LAYER);
            this.hitPauseCoroutine = null;
        }
    }
}