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

        private Rigidbody rb;
        private new void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody>();
        }

        public override void InitializeVFX(CombatAgent attacker, ActionClip attack)
        {
            base.InitializeVFX(attacker, attack);
            startPosition = attacker.MovementHandler.GetPosition();
        }

        private Vector3 startPosition;
        private new void OnEnable()
        {
            base.OnEnable();
            rb.useGravity = false;
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

        private void Update()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (!GetAttacker()) { return; }
            if (Vector3.Distance(transform.position, startPosition) > killDistance) { NetworkObject.Despawn(true); }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (!GetAttacker()) { return; }

            bool shouldDestroy = false;
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent == GetAttacker()) { return; }

                bool canHit = true;
                if (spellType == SpellType.GroundSpell)
                {
                    if (networkCollider.CombatAgent.StatusAgent.IsImmuneToGroundSpells()) { canHit = false; }
                }

                if (canHit)
                {
                    bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(GetAttacker(), null, new Dictionary<IHittable, RuntimeWeapon.HitCounterData>(), GetAttack(), other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce);
                    if (!hitSuccess & networkCollider.CombatAgent.GetAilment() == ActionClip.Ailment.Knockdown) { return; }
                }
            }
            else if (other.transform.root.TryGetComponent(out IHittable hittable))
            {
                if ((Object)hittable == GetAttacker()) { return; }

                shouldDestroy = hittable.ShouldBlockProjectiles();
                hittable.ProcessProjectileHit(GetAttacker(), null, new Dictionary<IHittable, RuntimeWeapon.HitCounterData>(), GetAttack(), other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce);
            }
            else if (other.transform.root.TryGetComponent(out ActionVFXPhysicsProjectile otherProjectile))
            {
                // Dont despawn projectiles that come from the same attacker
                if (otherProjectile.GetAttacker() == GetAttacker()) { return; }
            }

            if (!other.isTrigger | shouldDestroy) { NetworkObject.Despawn(true); }
        }
    }
}