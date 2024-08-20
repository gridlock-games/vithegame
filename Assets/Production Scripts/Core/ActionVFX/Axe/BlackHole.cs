using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;

namespace Vi.Core.VFX.Axe
{
    public class BlackHole : FollowUpVFX
    {
        [SerializeField] private float duration = 3;
        [SerializeField] private float radius = 2;
        [SerializeField] private ActionClip.Ailment ailmentToTriggerOnEnd = ActionClip.Ailment.Knockdown;

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
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, colliders, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (ShouldAffect(networkCollider.CombatAgent))
                    {
                        MovementHandler movementHandler = networkCollider.MovementHandler;
                        movementHandler.AddForce(transform.position - movementHandler.transform.position);
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
                        movementHandler.AddForce(transform.position - movementHandler.transform.position);

                        ActionClip copy = Instantiate(GetAttack());
                        copy.name = GetAttack().name;
                        copy.ailment = ailmentToTriggerOnEnd;

                        if (NetworkManager.Singleton.IsServer)
                        {
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