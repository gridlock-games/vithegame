using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    public abstract class PhysicsMovementHandler : MovementHandler
    {
        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            if (rb)
            {
                rb.position = newPosition;
                rb.velocity = Vector3.zero;
            }
            base.SetOrientation(newPosition, newRotation);
        }

        public override Vector3 GetPosition() { return rb.position; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            rb.interpolation = IsClient ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            rb.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
        }

        public Rigidbody Rigidbody { get { return rb; } }
        private Rigidbody rb;
        protected CombatAgent combatAgent;
        protected override void Awake()
        {
            base.Awake();
            rb = GetComponentInChildren<Rigidbody>();
            combatAgent = GetComponent<CombatAgent>();
        }

        protected override void OnEnable()
        {
            if (!GetComponent<ActionVFX>() & rb) { NetworkPhysicsSimulation.AddRigidbody(rb); }
        }

        protected override void OnDisable()
        {
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

        public float GetRunSpeed()
        {
            return Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - combatAgent.StatusAgent.GetMovementSpeedDecreaseAmount()) + combatAgent.StatusAgent.GetMovementSpeedIncreaseAmount();
        }

        protected float GetAnimatorSpeed()
        {
            return (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - combatAgent.StatusAgent.GetMovementSpeedDecreaseAmount()) + combatAgent.StatusAgent.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * (combatAgent.AnimationHandler.IsAtRest() ? 1 : (weaponHandler.IsInRecovery ? weaponHandler.CurrentActionClip.recoveryAnimationSpeed : weaponHandler.CurrentActionClip.animationSpeed));
        }
    }
}