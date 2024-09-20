using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core.MovementHandlers
{
    public abstract class PhysicsMovementHandler : MovementHandler
    {
        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            if (rb)
            {
                rb.position = newPosition;
                rb.Sleep();
            }
            base.SetOrientation(newPosition, newRotation);
        }

        public override Vector3 GetPosition() { return rb.position; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            rb.interpolation = IsClient ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            rb.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
            rb.isKinematic = !IsServer & !IsOwner;
        }

        public Rigidbody Rigidbody { get { return rb; } }
        private Rigidbody rb;
        protected CombatAgent combatAgent;
        protected override void Awake()
        {
            base.Awake();
            rb = GetComponentInChildren<Rigidbody>();
            combatAgent = GetComponent<CombatAgent>();
            PooledObject pooledObject = GetComponent<PooledObject>();
            pooledObject.OnSpawnFromPool += OnSpawnFromPool;
            pooledObject.OnReturnToPool += OnReturnToPool;
        }

        protected virtual void OnSpawnFromPool()
        {
            rb.transform.SetParent(null, true);
            rb.rotation = Quaternion.identity;
            rb.transform.rotation = Quaternion.identity;
        }
        
        protected virtual void OnReturnToPool()
        {
            rb.transform.SetParent(transform);
            rb.transform.localPosition = Vector3.zero;
            rb.transform.localRotation = Quaternion.identity;
            rb.Sleep();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!GetComponent<ActionVFX>() & rb) { NetworkPhysicsSimulation.AddRigidbody(rb); }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!GetComponent<ActionVFX>() & rb) { NetworkPhysicsSimulation.RemoveRigidbody(rb); }
        }

        protected float GetTickRateDeltaTime()
        {
            return NetworkManager.NetworkTickSystem.LocalTime.FixedDeltaTime * Time.timeScale;
        }

        protected float GetRootMotionSpeed()
        {
            return Mathf.Clamp01(weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - combatAgent.StatusAgent.GetMovementSpeedDecreaseAmount() + combatAgent.StatusAgent.GetMovementSpeedIncreaseAmount());
        }

        protected float GetRunSpeed()
        {
            return Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - combatAgent.StatusAgent.GetMovementSpeedDecreaseAmount()) + combatAgent.StatusAgent.GetMovementSpeedIncreaseAmount();
        }

        protected float GetAnimatorSpeed()
        {
            return (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - combatAgent.StatusAgent.GetMovementSpeedDecreaseAmount()) + combatAgent.StatusAgent.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * (combatAgent.AnimationHandler.IsAtRest() ? 1 : (weaponHandler.IsInRecovery ? weaponHandler.CurrentActionClip.recoveryAnimationSpeed : weaponHandler.CurrentActionClip.animationSpeed));
        }

        protected override void Update()
        {
            base.Update();
            UpdateAnimatorSpeed();
            UpdateAnimatorParameters();
        }

        protected void UpdateAnimatorSpeed()
        {
            if (weaponHandler.CurrentActionClip != null)
            {
                if (combatAgent.ShouldPlayHitStop())
                {
                    combatAgent.AnimationHandler.Animator.speed = 0;
                }
                else
                {
                    if (combatAgent.IsGrabbed())
                    {
                        CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                        if (grabAssailant)
                        {
                            if (grabAssailant.AnimationHandler)
                            {
                                combatAgent.AnimationHandler.Animator.speed = grabAssailant.AnimationHandler.Animator.speed;
                            }
                        }
                    }
                    else
                    {
                        combatAgent.AnimationHandler.Animator.speed = GetAnimatorSpeed();
                    }
                }
            }
        }

        [Header("Physics Movement Handler")]
        [SerializeField] private float runAnimationTransitionSpeed = 5;

        Vector2 animationMoveInput;
        protected void SetAnimationMoveInput(Vector2 moveInput)
        {
            animationMoveInput = moveInput;
        }

        private void UpdateAnimatorParameters()
        {
            Vector2 walkCycleAnims = IsSpawned ? GetWalkCycleAnimationParameters() : Vector2.zero;
            combatAgent.AnimationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(combatAgent.AnimationHandler.Animator.GetFloat("MoveForward"), walkCycleAnims.y, Time.deltaTime * runAnimationTransitionSpeed));
            combatAgent.AnimationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(combatAgent.AnimationHandler.Animator.GetFloat("MoveSides"), walkCycleAnims.x, Time.deltaTime * runAnimationTransitionSpeed));
            combatAgent.AnimationHandler.Animator.SetBool("IsGrounded", IsSpawned ? IsGrounded() : true);
            combatAgent.AnimationHandler.Animator.SetFloat("VerticalSpeed", Rigidbody.velocity.y);
        }

        private Vector2 GetWalkCycleAnimationParameters()
        {
            if (combatAgent.AnimationHandler.ShouldApplyRootMotion())
            {
                return Vector2.zero;
            }
            else if (!CanMove() | combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                return Vector2.zero;
            }
            else
            {
                Vector2 animDir = new Vector2(animationMoveInput.x, animationMoveInput.y) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1);
                animDir = Vector2.ClampMagnitude(animDir, 1);

                if (combatAgent.WeaponHandler.IsBlocking)
                {
                    switch (combatAgent.WeaponHandler.GetWeapon().GetBlockingLocomotion())
                    {
                        case Weapon.BlockingLocomotion.NoMovement:
                            animDir = Vector2.zero;
                            break;
                        case Weapon.BlockingLocomotion.CanWalk:
                            animDir /= 2;
                            break;
                        case Weapon.BlockingLocomotion.CanRun:
                            break;
                        default:
                            Debug.LogError("Unsure how to handle blocking locomotion type: " + combatAgent.WeaponHandler.GetWeapon().GetBlockingLocomotion());
                            break;
                    }
                }
                return animDir;
            }
        }

        List<Collider> groundColliders = new List<Collider>();
        ContactPoint[] stayContacts = new ContactPoint[3];
        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            int contactCount = collision.GetContacts(stayContacts);
            for (int i = 0; i < contactCount; i++)
            {
                if (stayContacts[i].normal.y >= 0.9f)
                {
                    if (!groundColliders.Contains(collision.collider)) { groundColliders.Add(collision.collider); }
                    break;
                }
                else // Normal is not pointing up
                {
                    if (groundColliders.Contains(collision.collider)) { groundColliders.Remove(collision.collider); }
                }
            }
        }

        public override void ReceiveOnCollisionExitMessage(Collision collision)
        {
            if (groundColliders.Contains(collision.collider))
            {
                groundColliders.Remove(collision.collider);
            }
        }

        [SerializeField] private float isGroundedSphereCheckRadius = 0.6f;
        protected virtual bool IsGrounded()
        {
            if (groundColliders.Count > 0)
            {
                return true;
            }
            else
            {
                return Physics.CheckSphere(Rigidbody.position, isGroundedSphereCheckRadius, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
            }
        }

        protected const float stairStepHeight = 0.01f;

        [Header("Physics Locomotion Settings")]
        [SerializeField] protected float maxStairStepHeight = 0.5f;
        [SerializeField] protected float airborneHorizontalDragMultiplier = 0.1f;
        [SerializeField] protected float gravityScale = 2;
    }
}