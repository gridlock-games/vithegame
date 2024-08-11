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
        [SerializeField] private float forceMultiplier = 10;
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
                if (colliders[i].TryGetComponent(out NetworkCollider networkCollider))
                {
                    bool shouldAffect = false;
                    if (networkCollider.Attributes == GetAttacker())
                    {
                        if (shouldAffectSelf) { shouldAffect = true; }
                    }
                    else
                    {
                        bool canHit = PlayerDataManager.Singleton.CanHit(networkCollider.Attributes, GetAttacker());
                        if (shouldAffectEnemies & canHit) { shouldAffect = true; }
                        if (shouldAffectTeammates & !canHit) { shouldAffect = true; }
                    }

                    if (spellType == SpellType.GroundSpell)
                    {
                        if (networkCollider.Attributes.StatusAgent.IsImmuneToGroundSpells()) { shouldAffect = false; }
                    }

                    if (shouldAffect)
                    {
                        MovementHandler movementHandler = networkCollider.MovementHandler;
                        movementHandler.AddForce((transform.position - movementHandler.transform.position) * forceMultiplier);
                    }
                }
                else if (colliders[i].GetComponent<Projectile>())
                {
                    if (colliders[i].TryGetComponent(out Rigidbody rb))
                    {
                        rb.AddForce((transform.position - rb.position) * forceMultiplier, ForceMode.VelocityChange);
                    }
                }
            }
        }

        private new void OnDisable()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, colliders, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].TryGetComponent(out NetworkCollider networkCollider))
                {
                    bool shouldAffect = false;
                    if (networkCollider.Attributes == GetAttacker())
                    {
                        if (shouldAffectSelf) { shouldAffect = true; }
                    }
                    else
                    {
                        bool canHit = PlayerDataManager.Singleton.CanHit(networkCollider.Attributes, GetAttacker());
                        if (shouldAffectEnemies & canHit) { shouldAffect = true; }
                        if (shouldAffectTeammates & !canHit) { shouldAffect = true; }
                    }

                    if (shouldAffect)
                    {
                        MovementHandler movementHandler = networkCollider.MovementHandler;
                        movementHandler.AddForce((transform.position - movementHandler.transform.position) * forceMultiplier);

                        ActionClip copy = Instantiate(GetAttack());
                        copy.name = GetAttack().name;
                        copy.ailment = ailmentToTriggerOnEnd;

                        if (NetworkManager.Singleton.IsServer)
                        {
                            networkCollider.Attributes.ProcessProjectileHit(GetAttacker(), null, new Dictionary<Attributes, RuntimeWeapon.HitCounterData>(),
                                copy, networkCollider.Attributes.transform.position, transform.position);
                        }
                    }
                }
                else if (colliders[i].TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce((transform.position - rb.position) * forceMultiplier, ForceMode.VelocityChange);
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