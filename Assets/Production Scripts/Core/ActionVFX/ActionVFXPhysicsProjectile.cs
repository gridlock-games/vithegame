using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Vi.Utility;

namespace Vi.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class ActionVFXPhysicsProjectile : GameInteractiveActionVFX
    {
        [SerializeField] private Vector3 projectileForce = new Vector3(0, 0, 3);
        [SerializeField] private float timeToActivateGravity = 0;
        [SerializeField] private float killDistance = 50;

        private bool initialized;

        public void InitializeVFX(Attributes attacker, ActionClip attack)
        {
            if (initialized) { Debug.LogError("ActionVFXPhysicsProjectile.Initialize() already called, why are you calling it again idiot?"); return; }

            this.attacker = attacker;
            this.attack = attack;
            initialized = true;

            GetComponent<Rigidbody>().AddForce(transform.rotation * projectileForce, ForceMode.VelocityChange);
        }

        private void Awake()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) { Debug.LogError("No collider attached to: " + this); }
            foreach (Collider col in colliders)
            {
                if (!col.isTrigger) { Debug.LogError("Make sure all colliders on projectiles are triggers! " + this); }
            }

            if (gameObject.layer != LayerMask.NameToLayer("Projectile")) { Debug.LogError("Make sure projectiles are in the Projectile Layer!"); }

            StartCoroutine(ActivateGravityCoroutine());
        }

        private IEnumerator ActivateGravityCoroutine()
        {
            GetComponent<Rigidbody>().useGravity = false;
            yield return new WaitForSeconds(timeToActivateGravity);
            GetComponent<Rigidbody>().useGravity = true;
        }

        private Vector3 startPosition;
        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            if (Vector3.Distance(transform.position, startPosition) > killDistance) { Destroy(gameObject); }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized) { return; }
            if (other.isTrigger) { return; }

            if (other.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.Attributes == attacker) { return; }
                if (NetworkManager.Singleton.IsServer) networkCollider.Attributes.ProcessProjectileHit(attacker, null, new Dictionary<Attributes, RuntimeWeapon.HitCounterData>(), attack, other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce);
            }

            Destroy(gameObject);
        }
    }
}