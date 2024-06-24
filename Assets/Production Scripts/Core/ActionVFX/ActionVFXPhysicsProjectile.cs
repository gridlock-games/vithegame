using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Vi.Utility;
using Vi.Core.GameModeManagers;

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

            rb.AddForce(transform.rotation * projectileForce, ForceMode.VelocityChange);
        }

        private void ClearInitialization()
        {
            attacker = null;
            attack = null;
            initialized = false;
        }

        private Rigidbody rb;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
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
            rb.useGravity = false;
            yield return new WaitForSeconds(timeToActivateGravity);
            rb.useGravity = true;
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

            bool shouldDestroy = false;
            if (other.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.Attributes == attacker) { return; }
                if (NetworkManager.Singleton.IsServer)
                {
                    bool hitSuccess = networkCollider.Attributes.ProcessProjectileHit(attacker, null, new Dictionary<Attributes, RuntimeWeapon.HitCounterData>(), attack, other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce);
                    if (!hitSuccess & networkCollider.Attributes.GetAilment() == ActionClip.Ailment.Knockdown) { return; }
                }
            }
            else if (other.transform.root.TryGetComponent(out GameInteractiveActionVFX actionVFX))
            {
                shouldDestroy = true;
                actionVFX.OnHit(attacker);
            }
            else if (other.transform.root.TryGetComponent(out GameItem gameItem))
            {
                shouldDestroy = true;
                gameItem.OnHit(attacker);
            }
            else
            {
                // Dont despawn projectiles that come from the same attacker
                if (other.transform.root.TryGetComponent(out ActionVFXPhysicsProjectile otherProjectile))
                {
                    if (otherProjectile.attacker == attacker) { return; }
                }
            }

            if (!other.isTrigger | shouldDestroy)
            {
                ClearInitialization();
                ObjectPoolingManager.ReturnObjectToPool(gameObject);
            }
        }
    }
}