using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Vi.Utility;

namespace Vi.Core
{
    [RequireComponent(typeof(FollowUpVFX))]
    public class BlackHole : MonoBehaviour
    {
        [SerializeField] private float duration = 3;
        [SerializeField] private float radius = 2;
        [SerializeField] private float forceMultiplier = 10;
        [SerializeField] private GameObject[] VFXToPlayOnDestroy;
        [SerializeField] private ActionClip.Ailment ailmentToTriggerOnEnd = ActionClip.Ailment.Knockdown;

        private float startTime;
        private ParticleSystem ps;
        private FollowUpVFX vfx;
        private void Awake()
        {
            vfx = GetComponent<FollowUpVFX>();
            ps = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
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
                    if (networkCollider.Attributes == vfx.Attacker)
                    {
                        if (vfx.shouldAffectSelf) { shouldAffect = true; }
                    }
                    else
                    {
                        bool canHit = PlayerDataManager.Singleton.CanHit(networkCollider.Attributes, vfx.Attacker);
                        if (vfx.shouldAffectEnemies & canHit) { shouldAffect = true; }
                        if (vfx.shouldAffectTeammates & !canHit) { shouldAffect = true; }
                    }

                    if (shouldAffect)
                    {
                        MovementHandler movementHandler = networkCollider.MovementHandler;
                        movementHandler.AddForce((transform.position - movementHandler.transform.position) * forceMultiplier);
                    }
                }
                else if (colliders[i].TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce((transform.position - rb.position) * forceMultiplier, ForceMode.VelocityChange);
                }
            }
        }

        private void OnDisable()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, colliders, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].TryGetComponent(out NetworkCollider networkCollider))
                {
                    bool shouldAffect = false;
                    if (networkCollider.Attributes == vfx.Attacker)
                    {
                        if (vfx.shouldAffectSelf) { shouldAffect = true; }
                    }
                    else
                    {
                        bool canHit = PlayerDataManager.Singleton.CanHit(networkCollider.Attributes, vfx.Attacker);
                        if (vfx.shouldAffectEnemies & canHit) { shouldAffect = true; }
                        if (vfx.shouldAffectTeammates & !canHit) { shouldAffect = true; }
                    }

                    if (shouldAffect)
                    {
                        MovementHandler movementHandler = networkCollider.MovementHandler;
                        movementHandler.AddForce((transform.position - movementHandler.transform.position) * forceMultiplier);

                        ActionClip copy = Instantiate(vfx.ActionClip);
                        copy.name = vfx.ActionClip.name;
                        copy.ailment = ailmentToTriggerOnEnd;

                        if (NetworkManager.Singleton.IsServer)
                        {
                            networkCollider.Attributes.ProcessProjectileHit(vfx.Attacker, null, new Dictionary<Attributes, RuntimeWeapon.HitCounterData>(),
                                copy, networkCollider.Attributes.transform.position, transform.position);
                        }
                    }
                }
                else if (colliders[i].TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce((transform.position - rb.position) * forceMultiplier, ForceMode.VelocityChange);
                }
            }

            foreach (GameObject prefab in VFXToPlayOnDestroy)
            {
                PlayerDataManager.Singleton.StartCoroutine(WeaponHandler.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(prefab, transform.position, transform.rotation)));
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}