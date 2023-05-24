namespace GameCreator.Characters
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.AI;
    using UnityEngine.SceneManagement;
    using GameCreator.Core;
    using GameCreator.Melee;
    using System;
    using Unity.Netcode;

    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Game Creator/Characters/Character", 100)]
    public class Character : GlobalID, IGameSave
    {
        [Serializable]
        public class State
        {
            public Vector3 forwardSpeed;
            public float sidesSpeed;
            public float pivotSpeed;
            public bool targetLock;
            public float isGrounded;
            public float isSliding;
            public float isDashing;
            public float verticalSpeed;
            public Vector3 normal;

            public State()
            {
                this.forwardSpeed = Vector3.zero;
                this.sidesSpeed = 0f;
                this.targetLock = false;
                this.isGrounded = 1.0f;
                this.isSliding = 0.0f;
                this.isDashing = 0.0f;
                this.verticalSpeed = 0f;
                this.normal = Vector3.zero;
            }

            public State(Vector3 forwardSpeed, float sidesSpeed, float pivotSpeed, bool targetLock, float isGrounded, float isSliding, float isDashing, float verticalSpeed, Vector3 normal)
            {
                this.forwardSpeed = forwardSpeed;
                this.sidesSpeed = sidesSpeed;
                this.pivotSpeed = pivotSpeed;
                this.targetLock = targetLock;
                this.isGrounded = isGrounded;
                this.isSliding = isSliding;
                this.isDashing = isDashing;
                this.verticalSpeed = verticalSpeed;
                this.normal = normal;
            }
        }

        public struct NetworkedState : INetworkSerializable
        {
            public Vector3 forwardSpeed;
            public float sidesSpeed;
            public float pivotSpeed;
            public bool targetLock;
            public float isGrounded;
            public float isSliding;
            public float isDashing;
            public float verticalSpeed;
            public Vector3 normal;

            public NetworkedState(State charState)
            {
                this.forwardSpeed = charState.forwardSpeed;
                this.sidesSpeed = charState.sidesSpeed;
                this.pivotSpeed = charState.pivotSpeed;
                this.targetLock = charState.targetLock;
                this.isGrounded = charState.isGrounded;
                this.isSliding = charState.isSliding;
                this.isDashing = charState.isDashing;
                this.verticalSpeed = charState.verticalSpeed;
                this.normal = charState.normal;
            }

            public State ConvertToState()
            {
                return new State(forwardSpeed, sidesSpeed, pivotSpeed, targetLock, isGrounded, isSliding, isDashing, verticalSpeed, normal);
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref forwardSpeed);
                serializer.SerializeValue(ref sidesSpeed);
                serializer.SerializeValue(ref pivotSpeed);
                serializer.SerializeValue(ref targetLock);
                serializer.SerializeValue(ref isGrounded);
                serializer.SerializeValue(ref isSliding);
                serializer.SerializeValue(ref isDashing);
                serializer.SerializeValue(ref verticalSpeed);
                serializer.SerializeValue(ref normal);
            }
        }

        [Serializable]
        public class SaveData
        {
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
        }

        [Serializable]
        public class OnLoadSceneData
        {
            public bool active { get; private set; }
            public Vector3 position { get; private set; }
            public Quaternion rotation { get; private set; }

            public OnLoadSceneData(Vector3 position, Quaternion rotation)
            {
                this.active = true;
                this.position = position;
                this.rotation = rotation;
            }

            public void Consume()
            {
                this.active = false;
            }
        }

        public class LandEvent : UnityEvent<float> { }
        public class JumpEvent : UnityEvent<int> { }
        public class DashEvent : UnityEvent { }
        public class StepEvent : UnityEvent<CharacterLocomotion.STEP> { }
        public class IsControllableEvent : UnityEvent<bool> { }
        public class AilmentUpdateEvent : UnityEvent<CharacterLocomotion.CHARACTER_AILMENTS> { }

        protected const string ERR_NOCAM = "No Main Camera found.";

        // PROPERTIES: ----------------------------------------------------------------------------

        public CharacterLocomotion characterLocomotion;

        public CharacterLocomotion.CHARACTER_AILMENTS characterAilment { get; private set; }
        private NetworkVariable<CharacterLocomotion.CHARACTER_AILMENTS> characterAilmentNetworked = new NetworkVariable<CharacterLocomotion.CHARACTER_AILMENTS>();

        public State characterState = new State();
        private CharacterAnimator animator;
        private CharacterRagdoll ragdoll;

        public JumpEvent onJump = new JumpEvent();
        public LandEvent onLand = new LandEvent();
        public DashEvent onDash = new DashEvent();
        public StepEvent onStep = new StepEvent();
        public AilmentUpdateEvent onAilmentEvent = new AilmentUpdateEvent();

        public IsControllableEvent onIsControllable = new IsControllableEvent();

        public bool save;
        protected SaveData initSaveData = new SaveData();

        public NetworkVariable<bool> disableActions = new NetworkVariable<bool>();

        private static readonly Vector3 PLANE = new Vector3(1, 0, 1);

        // INITIALIZERS: --------------------------------------------------------------------------

        private void OnAilmentChange(CharacterLocomotion.CHARACTER_AILMENTS prev, CharacterLocomotion.CHARACTER_AILMENTS current)
        {
            if (IsServer) { return; }

            StartCoroutine(ExecuteAilmentChange(current));
        }

        private IEnumerator ExecuteAilmentChange(CharacterLocomotion.CHARACTER_AILMENTS current)
        {
            yield return null;

            if (current == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown & characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp)
            {
                yield return new WaitUntil(() => characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.None);
            }
            
            switch (current)
            {
                case CharacterLocomotion.CHARACTER_AILMENTS.IsStunned:
                    Stun();
                    break;
                case CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown:
                    Knockdown(this, null);
                    break;
                case CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp:
                    Knockup(this, null);
                    break;
                case CharacterLocomotion.CHARACTER_AILMENTS.None:
                    if (characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned) { CancelAilment(); }
                    break;
            }
        }

        public override void OnNetworkSpawn() { characterAilmentNetworked.OnValueChanged += OnAilmentChange; }
        public override void OnNetworkDespawn() { characterAilmentNetworked.OnValueChanged -= OnAilmentChange; }

        protected override void Awake()
        {
            base.Awake();

            if (!Application.isPlaying) return;
            this.CharacterAwake();

            this.initSaveData = new SaveData()
            {
                position = transform.position,
                rotation = transform.rotation,
            };

            if (this.save)
            {
                SaveLoadManager.Instance.Initialize(this);
            }
        }

        protected void CharacterAwake()
        {
            if (!Application.isPlaying) return;
            this.animator = GetComponent<CharacterAnimator>();
            this.characterLocomotion.Setup(this);

            if (this.animator != null && this.animator.autoInitializeRagdoll)
            {
                this.InitializeRagdoll();
            }
        }

        protected void OnDestroy()
        {
            this.OnDestroyGID();
            if (!Application.isPlaying) return;

            if (this.save && !this.exitingApplication)
            {
                SaveLoadManager.Instance.OnDestroyIGameSave(this);
            }
        }

        // UPDATE: --------------------------------------------------------------------------------

        private void Update()
        {
            if (!Application.isPlaying) return;
            this.CharacterUpdate();
        }

        protected void CharacterUpdate()
        {
            if (this.ragdoll != null && this.ragdoll.GetState() != CharacterRagdoll.State.Normal) return;

            this.characterLocomotion.Update();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (this.ragdoll != null && this.ragdoll.GetState() != CharacterRagdoll.State.Normal)
            {
                this.ragdoll.Update();
            }
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public bool isCharacterDashing()
        {
            return this.characterLocomotion.isDashing;
        }

        private NetworkVariable<NetworkedState> networkedState = new NetworkVariable<NetworkedState>();
        public State GetCharacterState()
        {
            if (IsServer)
            {
                networkedState.Value = new NetworkedState(characterState);
            }
            return networkedState.Value.ConvertToState();
        }

        public void SetRagdoll(bool active, bool autoStand = false)
        {
            if (active && this.ragdoll.GetState() != CharacterRagdoll.State.Normal) return;
            if (!active && this.ragdoll.GetState() == CharacterRagdoll.State.Normal) return;

            this.characterLocomotion.characterController.enabled = !active;
            this.animator.animator.enabled = !active;

            Transform model = this.animator.animator.transform;
            switch (active)
            {
                case true:
                    this.ragdoll.Ragdoll(true, autoStand);
                    model.SetParent(null, true);
                    break;

                case false:
                    model.SetParent(transform, true);
                    this.ragdoll.Ragdoll(false, autoStand);
                    break;
            }
        }

        public void InitializeRagdoll()
        {
            this.ragdoll = new CharacterRagdoll(this);
        }

        // GETTERS: -------------------------------------------------------------------------------

        public bool IsControllable()
        {
            if (this.characterLocomotion == null) return false;
            return this.characterLocomotion.isControllable;
        }

        public bool IsRagdoll()
        {
            return (this.ragdoll != null && this.ragdoll.GetState() != CharacterRagdoll.State.Normal);
        }

        public int GetCharacterMotion()
        {
            //if (this.characterState == null) return 0;
            if (this.characterLocomotion == null) return 0;

            float speed = Mathf.Abs(this.characterState.forwardSpeed.magnitude);
            if (Mathf.Approximately(speed, 0.0f)) return 0;
            else if (this.characterLocomotion.canRun && speed > this.characterLocomotion.runSpeed / 2.0f)
            {
                return 2;
            }

            return 1;
        }

        public bool IsGrounded()
        {
            //if (this.characterState == null) return true;
            return Mathf.Approximately(this.characterState.isGrounded, 1.0f);
        }

        public CharacterAnimator GetCharacterAnimator()
        {
            return this.animator;
        }

        // JUMP: ----------------------------------------------------------------------------------

        public bool Dash(Vector3 direction, float impulse, float duration, float drag = 10f)
        {
            if (this.characterLocomotion.isBusy) return false;

            this.characterLocomotion.Dash(direction, impulse, duration, drag);
            if (this.onDash != null) this.onDash.Invoke();
            return true;
        }

        public bool Grab(CharacterLocomotion.OVERRIDE_FACE_DIRECTION direction, bool isControllable)
        {
            if (this.characterLocomotion == null || this.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None) return false;
            this.characterLocomotion.UpdateDirectionControl(direction, isControllable);
            return true;
        }

        public bool Stun()
        {
            if (this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.None ||
                this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp)
            {

                if (this.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None ||
                    this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp)
                {
                    this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, null);
                    StartCoroutine(StartKnockdownAfterDuration(0f));
                }
                else
                {
                    this.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
                    this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsStunned, null);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Knockup(Character attacker, Character target)
        {
            if (this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.None ||
                this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned ||
                this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp)
            {
                if (target)
                {
                    PreserveRotation rotationConfig = Rotation(attacker.gameObject, target);
                    target.UpdateRotationClientRpc(rotationConfig.quaternion, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { target.OwnerClientId } } });
                }

                if (this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned)
                {
                    this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, null);
                    StartCoroutine(StartKnockupAfterDuration(0f));
                }
                else if (this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp)
                {
                    this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, null);
                    StartCoroutine(StartKnockupAfterDuration(.05f));
                }
                else
                {
                    this.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
                    this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp, null);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Knockdown(Character attacker, Character target)
        {
            if (this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.None ||
            this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned ||
            this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp)
            {
                // If the target is null, that means we are calling this from a client, rather than the server
                if (target)
                {
                    PreserveRotation rotationConfig = Rotation(attacker.gameObject, target);
                    target.UpdateRotationClientRpc(rotationConfig.quaternion, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { target.OwnerClientId } } });
                }

                if (this.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None)
                {
                    this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, null);
                    StartCoroutine(StartKnockdownAfterDuration(0f));
                }
                else
                {
                    this.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
                    this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown, null);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        [ClientRpc] public void UpdateRotationClientRpc(Quaternion newRotation, ClientRpcParams clientRpcParams) { transform.rotation = newRotation; }

        /* Pause for a given duration if the target character is coming from another ailment.
        Ailments: Stun, Knockup*/
        private IEnumerator StartKnockdownAfterDuration(float duration)
        {
            float initTime = Time.time;
            while (initTime + duration >= Time.time)
            {
                yield return null;
            }
            this.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
            this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown, null);
        }

        /* Pause for a given duration if the target character is coming from another ailment
        Ailments: Stun*/
        private IEnumerator StartKnockupAfterDuration(float duration)
        {
            float initTime = Time.time;
            while (initTime + duration >= Time.time)
            {
                yield return null;
            }
            this.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
            this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp, null);
        }

        /* This is needed so that the character won't immediatley go through 
        the stand up sequence when the stun duration is over*/
        private IEnumerator RecoverFromKnockupAfterDuration(float duration, CharacterMelee melee)
        {
            float initTime = Time.time;
            melee.SetInvincibility(duration);
            while (initTime + duration >= Time.time)
            {
                yield return null;
            }
            melee.currentWeapon.recoveryStandUp.Play(melee);
            float recoveryAnimDuration = melee.currentWeapon.recoveryStandUp.animationClip.length * 1.25f;
            CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(recoveryAnimDuration, melee));
        }

        public bool CancelAilment()
        {
            if (this.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None)
            {
                this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, null);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS ailment, CharacterState assignState)
        {
            CharacterLocomotion.CHARACTER_AILMENTS prevAilment = this.characterAilment;
            CharacterMelee melee = this.GetComponent<CharacterMelee>();

            float recoveryAnimDuration = 0f;

            switch (ailment)
            {
                // All Ailments should end with reset except Stun which can be cancelled
                case CharacterLocomotion.CHARACTER_AILMENTS.Reset:
                    /*Stun has an if handling since it has its own standup sequence*/
                    if (prevAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned)
                    {
                        melee.currentWeapon.recoveryStun.Play(melee);
                        recoveryAnimDuration = melee.currentWeapon.recoveryStun.animationClip.length * 1.25f;
                        CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(recoveryAnimDuration, melee));
                    }
                    else
                    {
                        /*Knockup has an if handling to prevent character from immediately standing up*/
                        if (prevAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp)
                        {
                            StartCoroutine(RecoverFromKnockupAfterDuration(1.5f, melee));
                        }
                        else
                        {
                            melee.currentWeapon.recoveryStandUp.Play(melee);
                            recoveryAnimDuration = melee.currentWeapon.recoveryStandUp.animationClip.length * 1.25f;
                            CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(recoveryAnimDuration, melee));
                        }
                    }
                    break;

                case CharacterLocomotion.CHARACTER_AILMENTS.None:
                    CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(recoveryAnimDuration, melee));
                    break;

                case CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown:
                    if (melee.currentWeapon.knockbackReaction[0])
                        melee.currentWeapon.knockbackReaction[0].Play(melee);
                    melee.SetInvincibility(6.50f);
                    break;

                case CharacterLocomotion.CHARACTER_AILMENTS.IsWallBound:
                    melee.StopAttack();
                    if (melee.currentWeapon.knockbackReaction[1])
                        melee.currentWeapon.knockbackReaction[1].Play(melee);

                    this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, null);
                    StartCoroutine(StartKnockupAfterDuration(0.5f));
                    break;
            }

            this.characterAilment = prevAilment != ailment ? this.characterLocomotion.Ailment(ailment) : prevAilment;
            if (IsServer) { characterAilmentNetworked.Value = characterAilment; }
            this.onAilmentEvent.Invoke(ailment);
        }

        private IEnumerator ResetDefaultState(float duration, CharacterMelee melee)
        {
            float initTime = Time.time;
            CharacterLocomotion.CHARACTER_AILMENTS prevAilment = this.characterAilment;

            while (initTime + (duration * 0.80f) >= Time.time)
            {
                yield return null;
            }

            melee.ChangeState(
                melee.currentWeapon.characterState,
                melee.currentWeapon.characterMask,
                MeleeWeapon.LAYER_STANCE,
                this.GetCharacterAnimator()
            );

            if (prevAilment != CharacterLocomotion.CHARACTER_AILMENTS.IsStunned)
            {
                melee.SetInvincibility(1.0f);
            }

            if (prevAilment != CharacterLocomotion.CHARACTER_AILMENTS.None)
            {
                this.characterAilment = CharacterLocomotion.CHARACTER_AILMENTS.None;
                //if (IsServer) { characterAilmentNetworked.Value = characterAilment; }
            }

            if (prevAilment != CharacterLocomotion.CHARACTER_AILMENTS.IsStunned)
            {
            }

            this.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.CameraDirection, true);

            if (IsServer) { melee.knockedUpHitCount.Value = 0; }

            this.onAilmentEvent.Invoke(this.characterAilment);

            yield return 0;
        }

        private PreserveRotation Rotation(GameObject anchor, Character targetChar)
        {

            Vector3 rotationDirection = (
                anchor.transform.position - targetChar.gameObject.transform.position
            );

            Quaternion targetRotation = Quaternion.LookRotation(rotationDirection, anchor.transform.up);
            rotationDirection = Vector3.Scale(rotationDirection, PLANE).normalized;
            targetChar.characterLocomotion.SetRotation(rotationDirection);

            PreserveRotation preserveRotation = new PreserveRotation(targetRotation, rotationDirection);

            return preserveRotation;

        }

        public void RootMovement(float impulse, float duration, float gravityInfluence,
            AnimationCurve acForward, AnimationCurve acSides, AnimationCurve acVertical)
        {
            this.characterLocomotion.RootMovement(
                impulse, duration, gravityInfluence,
                acForward, acSides, acVertical
            );
        }

        public void Jump(float force)
        {
            // int jumpChain = this.characterLocomotion.Jump(force);
            // if (jumpChain >= 0 && this.animator != null)
            // {
            //     this.animator.Jump();
            // }
            StartCoroutine(this.DelayJump(0f, force));
        }

        public void Jump()
        {
            // int jumpChain = this.characterLocomotion.Jump();
            // if (jumpChain >= 0 && this.animator != null)
            // {
            //     this.animator.Jump(jumpChain);
            // }
            StartCoroutine(this.DelayJump(0f, this.characterLocomotion.jumpForce));
        }

        public IEnumerator DelayJump(float seconds, float force)
        {
            WaitForSeconds wait = new WaitForSeconds(seconds);
            yield return wait;

            int jumpChain = this.characterLocomotion.Jump(force);
            if (jumpChain >= 0 && this.animator != null)
            {
                this.animator.Jump(jumpChain);
            }
        }

        // HEAD TRACKER: --------------------------------------------------------------------------

        public CharacterHeadTrack GetHeadTracker()
        {
            if (this.animator == null) return null;
            return this.animator.GetHeadTracker();
        }

        // FLOOR COLLISION: -----------------------------------------------------------------------

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!Application.isPlaying) return;

            float coefficient = this.characterLocomotion.pushForce;
            if (coefficient < float.Epsilon) return;

            float angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle < 90f) this.characterLocomotion.terrainNormal = hit.normal;

            Rigidbody hitRigidbody = hit.collider.attachedRigidbody;
            if (angle <= 90f && angle >= 5f && hitRigidbody != null && !hitRigidbody.isKinematic)
            {
                Vector3 force = hit.controller.velocity * coefficient / Time.fixedDeltaTime;
                hitRigidbody.AddForceAtPosition(force, hit.point, ForceMode.Force);
            }
        }

        // GIZMOS: --------------------------------------------------------------------------------

        private void OnDrawGizmos()
        {
            if (this.ragdoll != null) this.ragdoll.OnDrawGizmos();
        }

        // GAME SAVE: -----------------------------------------------------------------------------

        public string GetUniqueName()
        {
            string uniqueName = string.Format(
                "character:{0}",
                this.GetUniqueCharacterID()
            );

            return uniqueName;
        }

        protected virtual string GetUniqueCharacterID()
        {
            return this.GetID();
        }

        public Type GetSaveDataType()
        {
            return typeof(SaveData);
        }

        public object GetSaveData()
        {
            return new SaveData()
            {
                position = transform.position,
                rotation = transform.rotation
            };
        }

        public void ResetData()
        {
            transform.position = this.initSaveData.position;
            transform.rotation = this.initSaveData.rotation;
        }

        public void OnLoad(object generic)
        {
            SaveData container = generic as SaveData;
            if (container == null) return;

            transform.position = container.position;
            transform.rotation = container.rotation;
        }
    }

    public class PreserveRotation
    {
        public PreserveRotation(Quaternion targetRotation, Vector3 rotationDirection)
        {
            this.quaternion = targetRotation;
            this.vector3 = rotationDirection;
        }
        public Quaternion quaternion { get; private set; }
        public Vector3 vector3 { get; private set; }
    }
}
