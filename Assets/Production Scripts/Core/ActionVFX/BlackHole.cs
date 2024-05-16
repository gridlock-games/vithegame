using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    [RequireComponent(typeof(FollowUpVFX))]
    public class BlackHole : MonoBehaviour
    {
        [SerializeField] private float radius = 2;
        [SerializeField] private float forceMultiplier = 10;
        [SerializeField] private GameObject[] VFXToPlayOnDestroy;

        private FollowUpVFX vfx;
        private void Start()
        {
            vfx = GetComponent<FollowUpVFX>();
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
                        MovementHandler movementHandler = networkCollider.Attributes.GetComponent<MovementHandler>();
                        movementHandler.AddForce((transform.position - movementHandler.transform.position) * forceMultiplier);
                    }
                }
                else if (colliders[i].TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce((transform.position - rb.position) * forceMultiplier, ForceMode.VelocityChange);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (GameObject prefab in VFXToPlayOnDestroy)
            {
                PlayerDataManager.Singleton.StartCoroutine(WeaponHandler.DestroyVFXWhenFinishedPlaying(Instantiate(prefab, transform.position, transform.rotation)));
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}