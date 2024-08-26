using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;

namespace Vi.Core.VFX.Axe
{
    public class BlackHole : FollowUpVFX
    {
        private const float duration = 3;
        private const float radius = 5;
        private static readonly ActionClip.Ailment ailmentToTriggerOnEnd = ActionClip.Ailment.Knockdown;

        private float startTime;
        private ParticleSystem ps;
        private new void Awake()
        {
            base.Awake();
            ps = GetComponent<ParticleSystem>();
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

        Collider[] colliders = new Collider[20];
        private void FixedUpdate()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, colliders, LayerMask.GetMask(new string[] { "NetworkPrediction", "Projectile" }), QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (ShouldAffect(networkCollider.CombatAgent))
                    {
                        MovementHandler movementHandler = networkCollider.MovementHandler;
                        Vector3 rel = transform.position - movementHandler.GetPosition();
                        movementHandler.AddForce(rel - movementHandler.GetVelocity());
                    }
                }
                else if (!colliders[i].transform.root.GetComponent<ActionVFX>() & colliders[i].transform.root.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce(transform.position - rb.position, ForceMode.VelocityChange);
                }
            }
        }

        private new void OnDisable()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, colliders, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (ShouldAffect(networkCollider.CombatAgent))
                    {
                        MovementHandler movementHandler = networkCollider.MovementHandler;
                        movementHandler.AddForce(4 * Physics.gravity);

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
                else if (colliders[i].transform.root.GetComponent<Projectile>())
                {
                    if (colliders[i].TryGetComponent(out Rigidbody rb))
                    {
                        rb.AddForce(transform.position - rb.position, ForceMode.VelocityChange);
                    }
                }
            }
            base.OnDisable();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}