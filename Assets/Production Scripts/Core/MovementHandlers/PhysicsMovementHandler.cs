using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;
using Unity.Netcode.Components;
using System.Linq;

namespace Vi.Core.MovementHandlers
{
    [RequireComponent(typeof(NetworkTransform))]
    public abstract class PhysicsMovementHandler : MovementHandler
    {
        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            if (rb)
            {
                rb.position = newPosition;
                rb.Sleep();
                networkTransform.Interpolate = false;
            }
            base.SetOrientation(newPosition, newRotation);
        }

        protected override void TeleportPositionRpc(Vector3 newPosition)
        {
            if (rb)
            {
                rb.position = newPosition;
                rb.Sleep();
            }
            base.TeleportPositionRpc(newPosition);
        }

        public override Vector3 GetPosition() { return rb.position; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            networkTransform.Interpolate = true;
            rb.interpolation = IsClient ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            rb.collisionDetectionMode = IsServer | IsOwner ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
            rb.isKinematic = !IsServer & !IsOwner;
            NetworkManager.NetworkTickSystem.Tick += Tick;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            NetworkManager.NetworkTickSystem.Tick -= Tick;
        }

        private bool interpolateReached;
        private void Tick()
        {
            if (networkTransform.Interpolate)
            {
                interpolateReached = false;
            }
            else if (interpolateReached)
            {
                networkTransform.Interpolate = true;
            }
            else
            {
                interpolateReached = true;
            }
        }

        public Rigidbody Rigidbody { get { return rb; } }
        private Rigidbody rb;
        protected CombatAgent combatAgent;
        protected NetworkTransform networkTransform;
        protected override void Awake()
        {
            base.Awake();
            rb = GetComponentInChildren<Rigidbody>();
            combatAgent = GetComponent<CombatAgent>();
            PooledObject pooledObject = GetComponent<PooledObject>();
            pooledObject.OnSpawnFromPool += OnSpawnFromPool;
            pooledObject.OnReturnToPool += OnReturnToPool;
            networkTransform = GetComponent<NetworkTransform>();
        }

        protected virtual void OnSpawnFromPool()
        {
            rb.transform.SetParent(null, true);
            rb.rotation = Quaternion.identity;
            rb.transform.rotation = Quaternion.identity;
            if (!GetComponent<ActionVFX>() & rb) { NetworkPhysicsSimulation.AddRigidbody(rb); }
        }
        
        protected virtual void OnReturnToPool()
        {
            rb.transform.SetParent(transform);
            rb.transform.localPosition = Vector3.zero;
            rb.transform.localRotation = Quaternion.identity;
            rb.Sleep();
            if (!GetComponent<ActionVFX>() & rb) { NetworkPhysicsSimulation.RemoveRigidbody(rb); }
            interpolateReached = default;
        }

        protected float GetTickRateDeltaTime()
        {
            return NetworkManager.NetworkTickSystem.LocalTime.FixedDeltaTime * Time.timeScale;
        }

        protected float GetRootMotionSpeed()
        {
            return Mathf.Clamp01(weaponHandler.GetWeapon().GetMovementSpeed(false, false) - combatAgent.StatusAgent.GetMovementSpeedDecreaseAmount() + combatAgent.StatusAgent.GetMovementSpeedIncreaseAmount());
        }

        protected float GetRunSpeed()
        {
            return Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking, weaponHandler.IsAiming()) - combatAgent.StatusAgent.GetMovementSpeedDecreaseAmount()) + combatAgent.StatusAgent.GetMovementSpeedIncreaseAmount();
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
            networkTransform.SetPositionMaximumInterpolationTime(combatAgent.AnimationHandler.ShouldApplyRootMotion() ? 0.05f : 0.1f);
        }

        protected virtual void LateUpdate()
        {
            if (combatAgent.ShouldShake()) { transform.position += Random.insideUnitSphere * (Time.deltaTime * CombatAgent.ShakeAmount); }
        }

        protected void UpdateAnimatorSpeed()
        {
            if (!combatAgent.AnimationHandler.Animator) { return; }

            if (weaponHandler.CurrentActionClip != null)
            {
                if (combatAgent.ShouldPlayHitStop())
                {
                    combatAgent.AnimationHandler.Animator.speed = 0;
                }
                else
                {
                    if (combatAgent.IsGrabbed)
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
            if (!combatAgent.AnimationHandler.Animator) { return; }
            Vector2 walkCycleAnims = IsSpawned ? GetWalkCycleAnimationParameters() : Vector2.zero;
            combatAgent.AnimationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(combatAgent.AnimationHandler.Animator.GetFloat("MoveForward"), walkCycleAnims.y, Time.deltaTime * runAnimationTransitionSpeed));
            combatAgent.AnimationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(combatAgent.AnimationHandler.Animator.GetFloat("MoveSides"), walkCycleAnims.x, Time.deltaTime * runAnimationTransitionSpeed));
            combatAgent.AnimationHandler.Animator.SetBool("IsGrounded", IsSpawned ? IsGrounded() : true);
            combatAgent.AnimationHandler.Animator.SetFloat("VerticalSpeed", Rigidbody.linearVelocity.y);
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

        public override void ReceiveOnCollisionEnterMessage(Collision collision)
        {
            EvaluateGroundCollider(collision);
        }

        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            EvaluateGroundCollider(collision);
        }

        List<Collider> groundColliders = new List<Collider>();
        List<Collider> stairColliders = new List<Collider>();
        ContactPoint[] groundColliderContacts = new ContactPoint[3];
        private void EvaluateGroundCollider(Collision collision)
        {
            if (collision.collider.isTrigger) { return; }
            if (!layersToAccountForInMovement.Contains(LayerMask.LayerToName(collision.collider.gameObject.layer))) { return; }
            int contactCount = collision.GetContacts(groundColliderContacts);
            for (int i = 0; i < contactCount; i++)
            {
                if (groundColliderContacts[i].normal.y >= 0.9f)
                {
                    if (!groundColliders.Contains(collision.collider)) { groundColliders.Add(collision.collider); }
                    break;
                }
                else if (groundColliderContacts[i].normal.y >= 0.8f) // This is stairs
                {
                    if (!stairColliders.Contains(collision.collider)) { stairColliders.Add(collision.collider); }
                    break;
                }
                else // Normal is not pointing up
                {
                    groundColliders.Remove(collision.collider);
                    stairColliders.Remove(collision.collider);
                }
            }
        }

        public override void ReceiveOnCollisionExitMessage(Collision collision)
        {
            groundColliders.Remove(collision.collider);
            stairColliders.Remove(collision.collider);
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

        protected int GetStairCollidersCount() { return stairColliders.Count; }

        protected const float stairStepHeight = 0.01f;

        [Header("Physics Locomotion Settings")]
        [SerializeField] protected Vector3 stairRaycastingStartOffset;
        [SerializeField] protected float stairStepForceMultiplier = 1;
        [SerializeField] protected float maxStairStepHeight = 0.5f;
        [SerializeField] protected float airborneHorizontalDragMultiplier = 0.1f;
        [SerializeField] protected float gravityScale = 2;

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position + transform.rotation * stairRaycastingStartOffset, 0.3f);
        }
    }
}