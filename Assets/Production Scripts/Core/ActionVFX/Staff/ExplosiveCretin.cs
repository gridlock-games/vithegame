using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core.Weapons;

namespace Vi.Core.VFX.Staff
{
    public class ExplosiveCretin : GameInteractiveActionVFX
    {
        private float serverSpawnTime;
        private const float cretinDuration = 5;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                serverSpawnTime = Time.time;
                networkCollidersEvaluated.Clear();
            }
        }

        private const float explosionRadius = 2.5f;

        private List<NetworkCollider> networkCollidersEvaluated = new List<NetworkCollider>();
        private void Update()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            bool shouldDespawn = false;
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            System.Array.Sort(colliders, (x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (networkCollider.CombatAgent == GetAttacker()) { continue; }

                    if (networkCollidersEvaluated.Contains(networkCollider)) { continue; }
                    networkCollidersEvaluated.Add(networkCollider);

                    bool shouldAffect = PlayerDataManager.Singleton.CanHit(GetAttacker(), networkCollider.CombatAgent);
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
                                bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(GetAttacker(), NetworkObject, null, new Dictionary<IHittable, RuntimeWeapon.HitCounterData>(),
                                    GetAttack(), networkCollider.CombatAgent.transform.position, transform.position);
                            }
                        }
                        else
                        {
                            bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(GetAttacker(), NetworkObject, null, new Dictionary<IHittable, RuntimeWeapon.HitCounterData>(),
                                    GetAttack(), networkCollider.CombatAgent.transform.position, transform.position);
                        }
                        shouldDespawn = true;
                    }
                }
            }
            if (shouldDespawn | Time.time - serverSpawnTime > cretinDuration) { NetworkObject.Despawn(true); }
        }
    }
}