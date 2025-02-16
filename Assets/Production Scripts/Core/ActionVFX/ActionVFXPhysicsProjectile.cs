using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Vi.Utility;
using Vi.Core.CombatAgents;
using Unity.Netcode.Components;
using Vi.Core.Weapons;

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

        private Quaternion originalRotation;
        public override void InitializeVFX(CombatAgent attacker, ActionClip attack)
        {
            base.InitializeVFX(attacker, attack);
            startPosition = attacker.MovementHandler.GetPosition();
            originalRotation = transform.rotation;
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

            rb.interpolation = IsClient ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            rb.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
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

        [SerializeField] private AudioClip[] whooshNearbySound = new AudioClip[0];

        protected override void OnDisable()
        {
            base.OnDisable();
            nearbyWhooshPlayed = false;
            originalRotation = default;
            rb.Sleep();
        }

        private bool nearbyWhooshPlayed;
        private void Update()
        {
            if (IsClient)
            {
                if (whooshNearbySound.Length > 0)
                {
                    if (!nearbyWhooshPlayed)
                    {
                        KeyValuePair<int, Attributes> localPlayerKvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
                        if (localPlayerKvp.Value)
                        {
                            if (Vector3.Distance(localPlayerKvp.Value.transform.position, transform.position) < Weapon.projectileNearbyWhooshDistanceThreshold)
                            {
                                AudioSource audioSource = AudioManager.Singleton.PlayClipOnTransform(transform, whooshNearbySound[Random.Range(0, whooshNearbySound.Length)], false, Weapon.projectileNearbyWhooshVolume);
                                if (audioSource) { audioSource.maxDistance = 20; }
                                nearbyWhooshPlayed = true;
                            }
                        }
                    }
                }
            }

            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (!GetAttacker()) { return; }
            if (Vector3.Distance(transform.position, startPosition) > killDistance) { NetworkObject.Despawn(true); }
        }

        private void FixedUpdate()
        {
            if (!IsServer) { return; }
            rb.MoveRotation(rb.linearVelocity == Vector3.zero ? originalRotation : Quaternion.LookRotation(rb.linearVelocity));
        }

        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (!GetAttacker()) { return; }

            bool shouldDestroy = false;
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (other.isTrigger) { return; }
                if (networkCollider.CombatAgent == GetAttacker()) { return; }

                bool canHit = true;
                if (spellType == SpellType.GroundSpell)
                {
                    if (networkCollider.CombatAgent.StatusAgent.IsImmuneToGroundSpells()) { canHit = false; }
                }

                if (canHit)
                {
                    bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(GetAttacker(), NetworkObject, null, new Dictionary<IHittable, RuntimeWeapon.HitCounterData>(), GetAttack(), other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce);
                    if (!hitSuccess & networkCollider.CombatAgent.GetAilment() == ActionClip.Ailment.Knockdown) { return; }
                }
            }
            else if (other.transform.root.TryGetComponent(out IHittable hittable))
            {
                if ((Object)hittable == GetAttacker()) { return; }

                shouldDestroy = hittable.ShouldBlockProjectiles();
                hittable.ProcessProjectileHit(GetAttacker(), NetworkObject, null, new Dictionary<IHittable, RuntimeWeapon.HitCounterData>(), GetAttack(), other.ClosestPointOnBounds(transform.position), transform.position - transform.rotation * projectileForce);
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