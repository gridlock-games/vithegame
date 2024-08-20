using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Vi.Utility;
using Vi.Core.GameModeManagers;
using Unity.Netcode.Components;

namespace Vi.Core.VFX
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkRigidbody))]
    public class ActionVFXPhysicsProjectile : GameInteractiveActionVFX
    {
        [SerializeField] private Vector3 projectileForce = new Vector3(0, 0, 3);
        [SerializeField] private float timeToActivateGravity = 0;
        [SerializeField] private float killDistance = 50;

        private bool initialized;

        public override void InitializeVFX(CombatAgent attacker, ActionClip attack)
        {
            ClearInitialization();

            this.attacker = attacker;
            this.attack = attack;
            initialized = true;
        }

        private void ClearInitialization()
        {
            attacker = null;
            attack = null;
            initialized = false;
        }

        private Rigidbody rb;
        private new void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
        }

        private new void OnEnable()
        {
            base.OnEnable();
            StartCoroutine(ActivateGravityCoroutine());
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            StartCoroutine(AddForceAfter1Frame());

            Collider[] colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) { Debug.LogError("No collider attached to: " + this); }
            foreach (Collider col in colliders)
            {
                if (!col.isTrigger) { Debug.LogError("Make sure all colliders on projectiles are triggers! " + this); }
                col.enabled = IsServer;
            }

            if (gameObject.layer != LayerMask.NameToLayer("Projectile")) { Debug.LogError("Make sure projectiles are in the Projectile Layer!"); }
        }

        private IEnumerator AddForceAfter1Frame()
        {
            yield return null;
            rb.AddForce(transform.rotation * projectileForce, ForceMode.VelocityChange);
        }

        private IEnumerator ActivateGravityCoroutine()
        {
            rb.useGravity = false;
            if (timeToActivateGravity < 0) { yield break; }
            yield return new WaitForSeconds(timeToActivateGravity);
            rb.useGravity = true;
        }

        private Vector3 startPosition;
        private void Start()
        {
            startPosition = transform.position;
        }

        private bool despawnCalled;
        private void Update()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (despawnCalled) { return; }
            if (Vector3.Distance(transform.position, startPosition) > killDistance) { NetworkObject.Despawn(true); despawnCalled = true; }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized) { return; }
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            bool shouldDestroy = false;
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent == attacker) { return; }

                bool canHit = true;
                if (spellType == SpellType.GroundSpell)
                {
                    if (networkCollider.CombatAgent.StatusAgent.IsImmuneToGroundSpells()) { canHit = false; }
                }

                if (canHit)
                {
                    bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(attacker, null, new Dictionary<IHittable, RuntimeWeapon.HitCounterData>(), attack, other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce);
                    if (!hitSuccess & networkCollider.CombatAgent.GetAilment() == ActionClip.Ailment.Knockdown) { return; }
                }
            }
            else if (other.transform.root.TryGetComponent(out IHittable hittable))
            {
                shouldDestroy = hittable.ShouldBlockProjectiles();
                hittable.ProcessProjectileHit(attacker, null, new Dictionary<IHittable, RuntimeWeapon.HitCounterData>(), attack, other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce);
            }
            else if (other.transform.root.TryGetComponent(out ActionVFXPhysicsProjectile otherProjectile))
            {
                // Dont despawn projectiles that come from the same attacker
                if (otherProjectile.attacker == attacker) { return; }
            }

            if (!other.isTrigger | shouldDestroy)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}