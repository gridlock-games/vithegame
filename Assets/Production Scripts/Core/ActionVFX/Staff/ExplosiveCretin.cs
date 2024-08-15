using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core.VFX.Staff
{
    public class ExplosiveCretin : GameInteractiveActionVFX
    {
        private float serverSpawnTime;
        private const float cretinDuration = 5;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { serverSpawnTime = Time.time; }
        }

        private void Update()
        {
            if (IsServer)
            {
                if (Time.time - serverSpawnTime > cretinDuration) { NetworkObject.Despawn(true); }
            }
        }

        private const float explosionRadius = 2.5f;
        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            System.Array.Sort(colliders, (x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (networkCollider.CombatAgent == attacker) { continue; }

                    bool shouldAffect = PlayerDataManager.Singleton.CanHit(attacker, networkCollider.CombatAgent);
                    if (shouldAffect)
                    {
                        if (spellType == SpellType.GroundSpell)
                        {
                            if (networkCollider.CombatAgent.StatusAgent.IsImmuneToGroundSpells())
                            {
                                shouldAffect = false;
                            }
                            else
                            {
                                bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(attacker, null, new Dictionary<CombatAgent, RuntimeWeapon.HitCounterData>(),
                                    attack, networkCollider.CombatAgent.transform.position, transform.position);
                            }
                        }
                        else
                        {
                            bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(attacker, null, new Dictionary<CombatAgent, RuntimeWeapon.HitCounterData>(),
                                    attack, networkCollider.CombatAgent.transform.position, transform.position);
                        }
                        NetworkObject.Despawn(true);
                    }
                }
            }
        }
    }
}