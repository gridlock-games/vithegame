using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;

namespace Vi.Core.VFX.Axe
{
    public class BlackHole : FollowUpVFX
    {
        private const float duration = 9999;
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

        private const float GRAVITY_PULL = 0.78f;

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
                        float dist = Vector3.Distance(transform.position, movementHandler.GetPosition());
                        if (dist > 0.3f)
                        {
                            Debug.Log(dist);
                            float gravityIntensity = Mathf.Clamp(radius - (dist / radius), 0, Mathf.Infinity);
                            float mass = 1;
                            Vector3 force = GRAVITY_PULL * gravityIntensity * mass * Time.fixedDeltaTime * (transform.position - movementHandler.GetPosition()).normalized;

                            movementHandler.AddForce(force);
                        }
                        
                        //MovementHandler movementHandler = networkCollider.MovementHandler;
                        //Vector3 force = transform.position - movementHandler.transform.position;
                        //force = force.normalized * 10;
                        //force *= Time.fixedDeltaTime;
                        //force -= movementHandler.GetVelocity();
                        //movementHandler.AddForce(force);
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
                        //movementHandler.AddForce(transform.position - movementHandler.transform.position);

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