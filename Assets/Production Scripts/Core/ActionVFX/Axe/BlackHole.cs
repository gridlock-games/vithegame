using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Vi.Core.MovementHandlers;

namespace Vi.Core.VFX.Axe
{
    public class BlackHole : FollowUpVFX
    {
        private const float duration = 3;
        private static readonly ActionClip.Ailment ailmentToTriggerOnEnd = ActionClip.Ailment.Knockdown;

        private float startTime;
        private ParticleSystem ps;
        private SphereCollider sphereCollider;
        private new void Awake()
        {
            base.Awake();
            ps = GetComponent<ParticleSystem>();
            sphereCollider = GetComponent<SphereCollider>();
        }

        private new void OnEnable()
        {
            base.OnEnable();
            startTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - startTime > duration)
            {
                ps.Pause(false);
            }
        }

        private const float pullStrength = 6;

        List<NetworkCollider> networkCollidersEvaluatedThisPhysicsUpdate = new List<NetworkCollider>();
        private void OnTriggerStay(Collider other)
        {
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollidersEvaluatedThisPhysicsUpdate.Contains(networkCollider)) { return; }
                networkCollidersEvaluatedThisPhysicsUpdate.Add(networkCollider);

                if (ShouldAffect(networkCollider.CombatAgent))
                {
                    PhysicsMovementHandler movementHandler = networkCollider.MovementHandler;
                    movementHandler.ExternalForceAffecting();
                    Vector3 rel = transform.position - movementHandler.GetPosition();
                    movementHandler.Rigidbody.AddForce(rel * pullStrength - movementHandler.Rigidbody.velocity, ForceMode.VelocityChange);
                }
            }
            else if (other.transform.root.GetComponent<Projectile>())
            {
                if (other.transform.root.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce(transform.position - rb.position, ForceMode.VelocityChange);
                }
            }
        }

        private bool clearListNextUpdate;
        private void FixedUpdate()
        {
            if (clearListNextUpdate) { networkCollidersEvaluatedThisPhysicsUpdate.Clear(); }
            clearListNextUpdate = networkCollidersEvaluatedThisPhysicsUpdate.Count > 0;
        }

        private const float explosionForce = 50;

        private Collider[] overlapSphereColliders = new Collider[20];

        public override void OnNetworkDespawn()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, sphereCollider.radius, overlapSphereColliders, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            List<NetworkCollider> networkCollidersEvaluated = new List<NetworkCollider>();
            for (int i = 0; i < count; i++)
            {
                if (overlapSphereColliders[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (networkCollidersEvaluated.Contains(networkCollider)) { continue; }
                    networkCollidersEvaluated.Add(networkCollider);

                    if (ShouldAffect(networkCollider.CombatAgent))
                    {
                        PhysicsMovementHandler movementHandler = networkCollider.MovementHandler;
                        movementHandler.Rigidbody.AddExplosionForce(explosionForce, new Vector3(transform.position.x, movementHandler.GetPosition().y + 2, transform.position.z), sphereCollider.radius, -1, ForceMode.VelocityChange);

                        if (NetworkManager.Singleton.IsServer)
                        {
                            ActionClip copy = Instantiate(GetAttack());
                            copy.name = GetAttack().name;
                            copy.ailment = ailmentToTriggerOnEnd;

                            networkCollider.CombatAgent.ProcessProjectileHit(GetAttacker(), null, new Dictionary<IHittable, RuntimeWeapon.HitCounterData>(),
                                copy, networkCollider.CombatAgent.transform.position, transform.position);
                        }
                    }
                }
                else if (overlapSphereColliders[i].transform.root.GetComponent<Projectile>())
                {
                    if (overlapSphereColliders[i].TryGetComponent(out Rigidbody rb))
                    {
                        rb.AddForce(transform.position - rb.position, ForceMode.VelocityChange);
                    }
                }
            }
            base.OnNetworkDespawn();
        }
    }
}