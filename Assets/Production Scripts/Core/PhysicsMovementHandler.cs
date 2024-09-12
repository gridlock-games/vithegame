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
            rb.interpolation = IsClient ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            rb.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
        }

        public Rigidbody Rigidbody { get { return rb; } }
        private Rigidbody rb;
        protected override void Awake()
        {
            base.Awake();
            rb = GetComponentInChildren<Rigidbody>();
        }

        protected override void OnEnable()
        {
            if (!GetComponent<ActionVFX>() & rb) { NetworkPhysicsSimulation.AddRigidbody(rb); }
        }

        protected override void OnDisable()
        {
            if (!GetComponent<ActionVFX>() & rb) { NetworkPhysicsSimulation.RemoveRigidbody(rb); }
        }
    }
}