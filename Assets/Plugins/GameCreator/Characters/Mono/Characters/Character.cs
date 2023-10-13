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
    using GameCreator.Variables;

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
        public CharacterStatusManager characterStatusManager { get; private set; }

        public CharacterLocomotion.CHARACTER_AILMENTS characterAilment { get; private set; }
        public CharacterStatusManager.CHARACTER_STATUS characterStatus { get; set; }
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

        public bool didDodgeCancelAilment = false;

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
                    Stun(null, this);
                    break;
                case CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown:
                    Knockdown(null, this);
                    break;
                case CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp:
                    Knockup(null, this);
                    break;
                case CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered:
                    Stagger(null, this);
                    break;
                case CharacterLocomotion.CHARACTER_AILMENTS.Dead:
                    UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.Dead, null);
                    break;
                case CharacterLocomotion.CHARACTER_AILMENTS.IsPulled:
                    UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsPulled, null);
                    break;
                case CharacterLocomotion.CHARACTER_AILMENTS.None:
                    CancelAilment(null);
                    break;
            }
        }

        public NetworkVariable<bool> isControllable;

        private void OnIsControllableChange(bool prev, bool current)
        {
            characterLocomotion.isControllable = current;
            characterLocomotion.isBusy = !current;
        }

        public NetworkVariable<CharacterLocomotion.OVERRIDE_FACE_DIRECTION> overrideFaceDirection;

        private void OnOverrideFaceDirectionChange(CharacterLocomotion.OVERRIDE_FACE_DIRECTION prev, CharacterLocomotion.OVERRIDE_FACE_DIRECTION current)
        {
            characterLocomotion.overrideFaceDirection = GetComponent<PlayerCharacter>() ? current : CharacterLocomotion.OVERRIDE_FACE_DIRECTION.None;
        }

        public override void OnNetworkSpawn()
        {
            characterAilmentNetworked.OnValueChanged += OnAilmentChange;
            isControllable.OnValueChanged += OnIsControllableChange;
            overrideFaceDirection.OnValueChanged += OnOverrideFaceDirectionChange;
            dead.OnValueChanged += OnDeadChange;

            characterStatusManager = this.GetComponentInChildren<CharacterStatusManager>();

            if (IsServer)
            {
                //characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.CameraDirection, true);
                characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.None, true);
            }
        }

        public override void OnNetworkDespawn()
        {
            characterAilmentNetworked.OnValueChanged -= OnAilmentChange;
            isControllable.OnValueChanged -= OnIsControllableChange;
            overrideFaceDirection.OnValueChanged -= OnOverrideFaceDirectionChange;
            dead.OnValueChanged -= OnDeadChange;
        }

        private void OnDeadChange(bool prev, bool current)
        {
            characterLocomotion.characterController.enabled = !current;
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                c.enabled = !current;
            }
        }

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


        protected new void OnDestroy()
        {
            base.OnDestroy();
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

            if (IsServer)
            {
                if (TryGetComponent(out LocalVariables variables))
                {
                    if (variables.Get("isDodging") != null)
                    {
                        bool isDodging = (bool)variables.Get("isDodging").Get();
                        isDashing.Value = isDodging;
                    }
                }
            }
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

        private NetworkVariable<bool> isDashing = new NetworkVariable<bool>();
        public bool isCharacterDashing()
        {
            return isDashing.Value;
        }

        public void SetCharacterDashing(bool value)
        {
            LocalVariables variables = this.gameObject.GetComponent<LocalVariables>();
            variables.Get("isDodging").Update(value);
        }

        public State GetCharacterState()
        {
            return characterState;
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
            if (disableActions.Value) return false;

            if (IsSpawned)
                return isControllable.Value;
            else
                return characterLocomotion.isControllable;
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

        // Ailments: ----------------------------------------------------------------------------------

        #region Ailments
        private List<CharacterLocomotion.CHARACTER_AILMENTS> allowedKnockupEntries = new List<CharacterLocomotion.CHARACTER_AILMENTS>(){
            CharacterLocomotion.CHARACTER_AILMENTS.None,
            CharacterLocomotion.CHARACTER_AILMENTS.IsStunned,
            CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp,
            CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered
        };

        private List<CharacterLocomotion.CHARACTER_AILMENTS> allowedStaggerEntries = new List<CharacterLocomotion.CHARACTER_AILMENTS>(){
            CharacterLocomotion.CHARACTER_AILMENTS.None,
            CharacterLocomotion.CHARACTER_AILMENTS.IsStunned,
            CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp,
            CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered
        };

        private List<CharacterLocomotion.CHARACTER_AILMENTS> allowedStunEntries = new List<CharacterLocomotion.CHARACTER_AILMENTS>(){
            CharacterLocomotion.CHARACTER_AILMENTS.None,
            CharacterLocomotion.CHARACTER_AILMENTS.IsStunned,
            CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp,
            CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered
        };

        public Character grabAssailant { get; private set; }
        public bool Grab(Character attacker, float duration)
        {
            if (dead.Value) { return false; }

            if (characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None) return false;

            UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.WasGrabbed, attacker);
            grabAssailant = attacker;
            StartCoroutine(EndGrab(duration));
            return true;
        }

        private IEnumerator EndGrab(float duration)
        {
            yield return new WaitForSeconds(duration);
            UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, null);
        }

        public bool Stun(Character attacker, Character target)
        {
            if (dead.Value) { return false; }

            if (allowedStunEntries.Contains(this.characterAilment))
            {
                CharacterLocomotion.CHARACTER_AILMENTS prevAilment = this.characterAilment;

                switch (prevAilment)
                {
                    case CharacterLocomotion.CHARACTER_AILMENTS.IsStunned:
                    case CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp:
                        UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, attacker);
                        StartCoroutine(StartKnockdownAfterDuration(0, attacker));
                        break;

                    case CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered:
                        UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, attacker);
                        StartCoroutine(StartSunAfterDuration(0.15f, attacker));
                        break;

                    default:
                        if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }
                        UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsStunned, attacker);
                        break;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Pull(Character attacker, Character target)
        {
            if (dead.Value) { return false; }

            if (allowedStunEntries.Contains(this.characterAilment))
            {
                CharacterLocomotion.CHARACTER_AILMENTS prevAilment = this.characterAilment;

                UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, attacker);
                StartCoroutine(StartPullAfterDuration(0f, attacker));

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Knockup(Character attacker, Character target)
        {
            if (dead.Value) { return false; }

            if (this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.None ||
                this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned ||
                this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp ||
                this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered)
            {
                // This is to make sure the target's ailment is reset before applying the knockup ailment
                if (this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp ||
                    this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned ||
                    this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered)
                {
                    UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, attacker);
                    StartCoroutine(StartKnockupAfterDuration(.05f, attacker));
                }
                else
                {
                    if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }
                    UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp, attacker);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Stagger(Character attacker, Character target)
        {
            if (dead.Value) { return false; }

            if (allowedStaggerEntries.Contains(this.characterAilment))
            {
                // This is to make sure the target's ailment is reset before applying the Stager ailment
                CharacterLocomotion.CHARACTER_AILMENTS prevAilment = this.characterAilment;
                switch (prevAilment)
                {
                    case CharacterLocomotion.CHARACTER_AILMENTS.IsStunned:
                        UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, attacker);
                        StartCoroutine(StartKnockupAfterDuration(.05f, attacker));
                        break;
                    case CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered:
                    case CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp:
                        UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, attacker);
                        StartCoroutine(StartKnockdownAfterDuration(.05f, attacker));
                        break;
                    default:
                        if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }
                        UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered, attacker);
                        break;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private NetworkVariable<bool> dead = new NetworkVariable<bool>();

        public bool IsDead()
        {
            return dead.Value;
        }

        public bool Die(Character killer)
        {
            if (dead.Value) { return false; }
            dead.Value = true;
            UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.Dead, killer);

            if (resetDefaultStateCoroutine != null)
            {
                StopCoroutine(resetDefaultStateCoroutine);
            }

            return true;
        }

        public bool CancelDeath()
        {
            if (!dead.Value) { return false; }

            dead.Value = false;
            UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, null);

            return true;
        }

        public bool Knockdown(Character attacker, Character target)
        {
            if (dead.Value) { return false; }

            if (this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.None ||
            this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned ||
            this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp ||
            this.characterAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered)
            {
                // If the target is null, that means we are calling this from a client, rather than the server
                if (this.characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None)
                {
                    UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, attacker);
                    StartCoroutine(StartKnockdownAfterDuration(0, attacker));
                }
                else
                {
                    if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }
                    UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown, attacker);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        [ClientRpc] public void UpdatePositionClientRpc(Vector3 targetPosition, ClientRpcParams clientRpcParams) { transform.position = targetPosition; }

        [ClientRpc] public void UpdateRotationClientRpc(Quaternion targetRotation, ClientRpcParams clientRpcParams) { transform.rotation = targetRotation; }

        [ClientRpc]
        private void UpdateAilmentRotationClientRpc(Quaternion rotation, ClientRpcParams clientRpcParams)
        {
            if (TryGetComponent(out PlayerCharacterNetworkTransform networkTransform))
            {
                networkTransform.SetRotation(rotation);
            }
            else
            {
                transform.rotation = rotation;
            }
            AilmentRotationHasBeenRecievedServerRpc(rotation);
        }


        private bool ailmentRotationRecieved;
        [ServerRpc] private void AilmentRotationHasBeenRecievedServerRpc(Quaternion rotation) { ailmentRotationRecieved = true; }

        /* Pause for a given duration if the target character is coming from another ailment.
        Ailments: Stun, Knockup*/
        private IEnumerator StartKnockdownAfterDuration(float duration, Character attacker)
        {
            float initTime = Time.time;
            while (initTime + duration >= Time.time)
            {
                yield return null;
            }
            if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }
            UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown, attacker);
        }

        /* Pause for a given duration if the target character is coming from another ailment
        Ailments: Stun*/
        private IEnumerator StartKnockupAfterDuration(float duration, Character attacker)
        {
            float initTime = Time.time;
            while (initTime + duration >= Time.time)
            {
                yield return null;
            }
            if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }
            UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp, attacker);
        }

        /* Pause for a given duration if the target character is coming from another ailment
        Ailments: Stun*/
        private IEnumerator StartStaggerAfterDuration(float duration, Character attacker)
        {
            float initTime = Time.time;
            while (initTime + duration >= Time.time)
            {
                yield return null;
            }
            if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }
            UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered, attacker);
        }

        /* Pause for a given duration if the target character is coming from another ailment
        Ailments: Pull*/
        private IEnumerator StartPullAfterDuration(float duration, Character attacker)
        {
            float initTime = Time.time;
            while (initTime + duration >= Time.time)
            {
                yield return null;
            }
            if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }
            UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsPulled, attacker);
        }

        /* Pause for a given duration if the target character is coming from another ailment
        Ailments: Stun*/
        private IEnumerator StartSunAfterDuration(float duration, Character attacker)
        {
            float initTime = Time.time;
            while (initTime + duration >= Time.time)
            {
                yield return null;
            }
            if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }
            UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.IsStunned, attacker);
        }

        /* This is needed so that the character won't immediatley go through 
        the stand up sequence when the knockup duration is over*/
        private IEnumerator RecoverFromKnockupAfterDuration(float duration, CharacterMelee melee)
        {
            float initTime = Time.time;
            melee.SetInvincibility(duration);
            while (initTime + duration >= Time.time)
            {
                yield return null;
            }

            if (!didDodgeCancelAilment)
            {
                MeleeClip standRecovery = melee.currentWeapon.recoveryStandUp;
                standRecovery.PlayNetworked(melee);

                float recoveryAnimDuration = melee.currentWeapon.recoveryStandUp.animationClip.length * 1.25f;
                resetDefaultStateCoroutine = CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(recoveryAnimDuration, melee));
            }
            else if (didDodgeCancelAilment) { didDodgeCancelAilment = false; }
        }

        public bool CancelAilment(Character attacker)
        {
            if (dead.Value) { return false; }

            if (characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.None)
            {
                UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS.None, attacker);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ShouldApplyAilmentRotation()
        {
            return applyAilmentRotation.Value;
        }

        public Quaternion GetAilmentRotation()
        {
            return ailmentRotation.Value;
        }

        private NetworkVariable<bool> applyAilmentRotation = new NetworkVariable<bool>();
        private NetworkVariable<Quaternion> ailmentRotation = new NetworkVariable<Quaternion>();

        public void UpdateAilment(CharacterLocomotion.CHARACTER_AILMENTS ailment, Character attacker)
        {
            if (dead.Value & ailment != CharacterLocomotion.CHARACTER_AILMENTS.Dead) { return; }

            List<CharacterLocomotion.CHARACTER_AILMENTS> rotationAilments = new List<CharacterLocomotion.CHARACTER_AILMENTS>()
            {
                CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown,
                CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp,
                CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered
            };

            if (IsServer)
            {
                applyAilmentRotation.Value = rotationAilments.Contains(ailment);
                if (applyAilmentRotation.Value) { ailmentRotation.Value = CalculateAilmentRotation(attacker); }
            }
            
            CharacterLocomotion.CHARACTER_AILMENTS prevAilment = this.characterAilment;
            CharacterMelee melee = this.GetComponent<CharacterMelee>();

            melee.SetUninterruptable(0f);
            if (IsServer) { melee.RevertAbilityCastingStatus(); }
            bool isDodging = this.isCharacterDashing();

            if (IsServer) { characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false); }

            float recoveryAnimDuration = 0f;

            switch (ailment)
            {
                // All Ailments should end with reset except Stun which can be cancelled
                case CharacterLocomotion.CHARACTER_AILMENTS.Reset:
                    /*Stun has an if handling since it has its own standup sequence*/
                    if (prevAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsStunned)
                    {
                        melee.currentWeapon.recoveryStun.PlayNetworked(melee);
                        recoveryAnimDuration = melee.currentWeapon.recoveryStun.animationClip.length * 1.10f;
                        resetDefaultStateCoroutine = CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(recoveryAnimDuration, melee));
                    }
                    else
                    {
                        /*Knockup has an if handling to prevent character from immediately standing up*/
                        if (prevAilment == CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp)
                        {
                            StartCoroutine(RecoverFromKnockupAfterDuration(1.10f, melee));
                        }
                        else
                        {
                            if (!isDodging) melee.currentWeapon.recoveryStandUp.PlayNetworked(melee);
                            recoveryAnimDuration = isDodging ? 0.05f : melee.currentWeapon.recoveryStandUp.animationClip.length * 1.10f;
                            if (!isDodging) resetDefaultStateCoroutine = CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(recoveryAnimDuration, melee));
                            if (didDodgeCancelAilment) { didDodgeCancelAilment = false; }
                        }
                    }
                    break;

                case CharacterLocomotion.CHARACTER_AILMENTS.None:
                    resetDefaultStateCoroutine = CoroutinesManager.Instance.StartCoroutine(ResetDefaultState(recoveryAnimDuration, melee));
                    break;

                case CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedUp:
                    if (melee.currentWeapon.knockupF)
                        melee.currentWeapon.knockupF.PlayNetworked(melee);
                    break;

                case CharacterLocomotion.CHARACTER_AILMENTS.IsKnockedDown:
                    if (melee.currentWeapon.knockbackF)
                        melee.currentWeapon.knockbackF.PlayNetworked(melee);
                    melee.SetInvincibility(6.50f);
                    break;

                case CharacterLocomotion.CHARACTER_AILMENTS.IsStaggered:
                    if (melee.currentWeapon.staggerF)
                        melee.currentWeapon.staggerF.PlayNetworked(melee);
                    break;

                case CharacterLocomotion.CHARACTER_AILMENTS.IsPulled:
                    // if (melee.currentWeapon.staggerF)
                    //     melee.currentWeapon.staggerF.PlayNetworked(melee);
                    break;

                case CharacterLocomotion.CHARACTER_AILMENTS.WasGrabbed:
                    characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.MovementDirection, false);
                    break;
            }

            characterAilment = prevAilment != ailment ? characterLocomotion.Ailment(ailment) : prevAilment;
            if (IsServer) { characterAilmentNetworked.Value = characterAilment; }
            onAilmentEvent.Invoke(ailment);
        }

        private Coroutine resetDefaultStateCoroutine;
        private IEnumerator ResetDefaultState(float duration, CharacterMelee melee)
        {
            CharacterLocomotion.CHARACTER_AILMENTS prevAilment = this.characterAilment;

            if (IsServer)
            {
                applyAilmentRotation.Value = false;
                characterLocomotion.UpdateDirectionControl(CharacterLocomotion.OVERRIDE_FACE_DIRECTION.CameraDirection, true);
            }

            yield return new WaitForSeconds(duration * 0.80f);

            // Reset State only if HP > 0
            if (characterAilment != CharacterLocomotion.CHARACTER_AILMENTS.Dead)
            {
                melee.ChangeState(
                    melee.currentWeapon.characterState,
                    melee.currentWeapon.characterMask,
                    MeleeWeapon.LAYER_STANCE,
                    this.GetCharacterAnimator()
                );

                if (prevAilment != CharacterLocomotion.CHARACTER_AILMENTS.None)
                {
                    this.characterAilment = CharacterLocomotion.CHARACTER_AILMENTS.None;
                }

                if (IsServer) { melee.knockedUpHitCount.Value = 0; }

                this.onAilmentEvent.Invoke(this.characterAilment);
            }
        }

        private Quaternion CalculateAilmentRotation(Character attacker)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = attacker.transform.position;
            startPos.y = 0;
            endPos.y = 0;

            return Quaternion.LookRotation(endPos - startPos, Vector3.up);
        }

        #endregion


        // STATUS: ----------------------------------------------------------------------------------
        public CharacterStatusManager.CHARACTER_STATUS Status(CharacterStatusManager.CHARACTER_STATUS characterStatus)
        {
            this.characterStatus = characterStatus;

            return this.characterStatus;
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
            StartCoroutine(this.DelayJump(0f, force));
        }

        public void Jump()
        {
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
