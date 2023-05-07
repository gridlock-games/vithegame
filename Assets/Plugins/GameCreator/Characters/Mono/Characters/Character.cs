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
        public class AilmentUpdateEvent : UnityEvent<CharacterLocomotion.CHARACTER_AILMENTS>{ }

        protected const string ERR_NOCAM = "No Main Camera found.";

        // PROPERTIES: ----------------------------------------------------------------------------

        public CharacterLocomotion characterLocomotion;

        public CharacterLocomotion.CHARACTER_AILMENTS characterAilment {get; private set;}

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

        // INITIALIZERS: --------------------------------------------------------------------------

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

        public bool isCharacterDashing() {
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
            else if (this.characterLocomotion.canRun && speed > this.characterLocomotion.runSpeed/2.0f)
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

        public bool Grab(CharacterLocomotion.OVERRIDE_FACE_DIRECTION direction, bool isControllable) {
            if (this.characterLocomotion == null) return false;
            this.characterLocomotion.UpdateDirectionControl(direction, isControllable);
            return true;
        }

        public bool Stun() {
            if (this.characterLocomotion == null) return false;
            this.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
            this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsStunned, null);
            return true;
        }

        public bool Knockdown() {
            if (this.characterLocomotion == null) return false;
            this.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
            this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown, null);
            return true;
        }

        public bool CancelAilment() {
            if (this.characterLocomotion == null) return false;
            this.UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, null);
            return true;
        }

        public void UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS ailment, CharacterState assignState) {
            CharacterLocomotion.CHARACTER_AILMENTS prevAilment = this.characterAilment;
            this.characterAilment = this.characterLocomotion.Ailment(ailment);
            CharacterMelee melee = this.GetComponent<CharacterMelee>();

            
            float recoveryAnimDuration = 0f;

            switch(ailment) {
                case CharacterLocomotion.CHARACTER_AILMENTS.Reset: // ONLY USE RESET IF COMING FROM GRAB OR KNOCKDOWN
                    melee.currentWeapon.recoveryStandUp.Play(melee);
                    recoveryAnimDuration = melee.currentWeapon.recoveryStandUp.animationClip.length * 1.25f; // 125% of the length
                    CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(melee.currentWeapon.recoveryStandUp.animationClip.length, melee));
                break;

                case CharacterLocomotion.CHARACTER_AILMENTS.None: 
                    if(prevAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned) {
                        melee.currentWeapon.recoveryStun.Play(melee);
                        recoveryAnimDuration = melee.currentWeapon.recoveryStandUp.animationClip.length * 1.15f;
                    }
                    CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(recoveryAnimDuration, melee));
                break;
            }

            this.characterAilment = this.characterAilment != ailment ? this.characterLocomotion.Ailment(ailment) : this.characterAilment;
            this.onAilmentEvent.Invoke(ailment);

        }

        private IEnumerator ResetDefaultState(float duration,CharacterMelee melee) {
            float initTime = Time.time;

            while (initTime + (duration * 0.80) >= Time.time) {
                yield return null;
            }

            melee.ChangeState(
                melee.currentWeapon.characterState,
                melee.currentWeapon.characterMask,
                MeleeWeapon.LAYER_STANCE,
                this.GetCharacterAnimator()
            );
            
            this.characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.CameraDirection, true);
            this.characterAilment = CharacterLocomotion.CHARACTER_AILMENTS.None;

            yield return 0;
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
}
