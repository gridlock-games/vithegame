using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.Weapons;

namespace Vi.Core.VFX
{
    public class GameInteractiveActionVFX : ActionVFX, IHittable
    {
        public enum SpellType
        {
            NotASpell,
            GroundSpell,
            AerialSpell
        }

        public SpellType GetSpellType() { return spellType; }

#if UNITY_EDITOR
        public FollowUpVFX[] GetFollowUpVFX()
        {
            return followUpVFXToPlayOnDestroy;
        }
#endif

        [SerializeField] protected SpellType spellType = SpellType.NotASpell;
        [SerializeField] private FollowUpVFX[] followUpVFXToPlayOnDestroy;
        [SerializeField] private bool shouldBlockProjectiles;
        [SerializeField] private bool shouldDestroyOnEnemyHit;
        [SerializeField] private ActionClip.StatusPayload[] enemyStatuses = new ActionClip.StatusPayload[0];
        [SerializeField] private ActionClip.StatusPayload[] friendlyStatuses = new ActionClip.StatusPayload[0];

        public bool ShouldBlockProjectiles() { return shouldBlockProjectiles; }

        private CombatAgent attacker;
        private ActionClip attack;
        private NetworkVariable<ulong> attackerNetworkObjectId = new NetworkVariable<ulong>();

        public CombatAgent GetAttacker()
        {
            if (attacker)
            {
                return attacker;
            }
            else
            {
                if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(attackerNetworkObjectId.Value, out NetworkObject networkObject))
                {
                    if (networkObject) { return networkObject.GetComponent<CombatAgent>(); }
                }
            }
            return null;
        }

        public ActionClip GetAttack()
        {
            if (!IsServer) { Debug.LogError("GameInteractiveActionVFX.GetAttack() should only be called on the server!"); }
            if (!attack) { Debug.LogError(this + " has no attack!"); }
            return attack;
        }

        [SerializeField] private ParticleSystem[] teamColorParticleSystems = new ParticleSystem[0];

        public virtual void InitializeVFX(CombatAgent attacker, ActionClip attack)
        {
            if (!NetworkManager.Singleton.IsServer) { Debug.LogError("GameInteractiveActionVFX.InitializeVFX() should only be called on the server!"); }
            if (!attack.IsAttack()) { Debug.LogError("Initializing " + this + " without an attack! " + attack + " " + attack.GetClipType()); }
            this.attacker = attacker;
            this.attack = attack;
            attackerNetworkObjectId.Value = attacker.NetworkObjectId;
        }

        private List<ParticleSystem.MinMaxGradient> originalColors = new List<ParticleSystem.MinMaxGradient>();
        protected override void Awake()
        {
            base.Awake();
            foreach (ParticleSystem ps in teamColorParticleSystems)
            {
                var main = ps.main;
                originalColors.Add(main.startColor);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            CombatAgent attacker = GetAttacker();
            if (attacker)
            {
                if (!attacker.IsLocalPlayer)
                {
                    if (PlayerDataManager.CanHit(attacker.GetTeam(), PlayerDataManager.Singleton.LocalPlayerData.team))
                    {
                        for (int i = 0; i < teamColorParticleSystems.Length; i++)
                        {
                            var main = teamColorParticleSystems[i].main;
                            main.startColor = PlayerDataManager.Singleton.GetRelativeTeamColor(GetAttacker().GetTeam());
                            teamColorParticleSystems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                            teamColorParticleSystems[i].Play(false);
                        }
                    }
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
            {
                foreach (FollowUpVFX prefab in followUpVFXToPlayOnDestroy)
                {
                    PooledObject pooledObject = ObjectPoolingManager.SpawnObject(prefab.GetComponent<PooledObject>(), transform.position, transform.rotation);
                    if (pooledObject.TryGetComponent(out FollowUpVFX vfx)) { vfx.InitializeVFX(attacker, attack); }
                    if (pooledObject.TryGetComponent(out NetworkObject netObj)) { netObj.Spawn(true); }
                }
            }

            attacker = null;
            attack = null;
            if (IsServer) { attackerNetworkObjectId.Value = default; }

            for (int i = 0; i < teamColorParticleSystems.Length; i++)
            {
                var main = teamColorParticleSystems[i].main;
                main.startColor = originalColors[i];
            }

            if (IsServer)
            {
                RemoveAllCollisionStatuses();
            }
        }

        protected virtual bool OnHit(CombatAgent attacker)
        {
            if (!IsSpawned) { return false; }
            if (shouldDestroyOnEnemyHit)
            {
                if (PlayerDataManager.Singleton.CanHit(attacker, this.attacker))
                {
                    NetworkObject.Despawn(true);
                    return true;
                }
            }
            return false;
        }

        public bool ProcessMeleeHit(CombatAgent attacker, NetworkObject attackingNetworkObject, ActionClip attack, RuntimeWeapon runtimeWeapon, Vector3 impactPosition, Vector3 hitSourcePosition)
        {
            return OnHit(attacker);
        }

        public bool ProcessProjectileHit(CombatAgent attacker, NetworkObject attackingNetworkObject, RuntimeWeapon runtimeWeapon, Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter, ActionClip attack, Vector3 impactPosition, Vector3 hitSourcePosition, float damageMultiplier = 1)
        {
            return OnHit(attacker);
        }

        public bool ProcessEnvironmentDamage(float damage, NetworkObject attackingNetworkObject, bool ignoresArmor = false) { return false; }
        public bool ProcessEnvironmentDamageWithHitReaction(float damage, NetworkObject attackingNetworkObject) { return false; }

        private Dictionary<HittableAgent, List<int>> collisionStatusTracker = new Dictionary<HittableAgent, List<int>>();

        private void RegisterCollisionStatuses(HittableAgent hittableAgent)
        {
            if (collisionStatusTracker.ContainsKey(hittableAgent)) { return; }

            foreach (ActionClip.StatusPayload statusPayload in (PlayerDataManager.Singleton.CanHit(GetAttacker(), hittableAgent) ? enemyStatuses : friendlyStatuses))
            {
                if (collisionStatusTracker.ContainsKey(hittableAgent))
                {
                    (bool, int) tuple = hittableAgent.StatusAgent.AddConditionalStatus(statusPayload);
                    if (tuple.Item1)
                    {
                        collisionStatusTracker[hittableAgent].Add(tuple.Item2);
                    }
                }
                else
                {
                    (bool, int) tuple = hittableAgent.StatusAgent.AddConditionalStatus(statusPayload);
                    if (tuple.Item1)
                    {
                        collisionStatusTracker.Add(hittableAgent, new List<int>() { tuple.Item2 });
                    }
                }
            }
        }

        private void RemoveCollisionStatus(HittableAgent hittableAgent, bool removeFromTracker = true)
        {
            if (collisionStatusTracker.ContainsKey(hittableAgent))
            {
                foreach (int statusEventId in collisionStatusTracker[hittableAgent])
                {
                    hittableAgent.StatusAgent.RemoveConditionalStatus(statusEventId);
                }
            }

            if (removeFromTracker)
            {
                collisionStatusTracker.Remove(hittableAgent);
            }
        }

        private void RemoveAllCollisionStatuses()
        {
            foreach (HittableAgent hittableAgent in collisionStatusTracker.Keys)
            {
                RemoveCollisionStatus(hittableAgent, false);
            }
            collisionStatusTracker.Clear();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                RegisterCollisionStatuses(networkCollider.CombatAgent);
            }
            else if (other.transform.root.TryGetComponent(out HittableAgent hittableAgent))
            {
                RegisterCollisionStatuses(hittableAgent);
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                RemoveCollisionStatus(networkCollider.CombatAgent);
            }
            else if (other.transform.root.TryGetComponent(out HittableAgent hittableAgent))
            {
                RemoveCollisionStatus(hittableAgent);
            }
        }
    }
}