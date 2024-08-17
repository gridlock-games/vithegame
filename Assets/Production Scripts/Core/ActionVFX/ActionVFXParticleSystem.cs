using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace Vi.Core.VFX
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkRigidbody))]
    public class ActionVFXParticleSystem : FollowUpVFX
    {
        private enum ParticleSystemType
        {
            ParticleCollisions,
            GenericCollisions
        }

        [SerializeField] private ParticleSystemType particleSystemType = ParticleSystemType.ParticleCollisions;
        [SerializeField] private bool shouldUseAttackerPositionForHitAngles;
        [SerializeField] private bool shouldOverrideMaxHits;
        [SerializeField] private int maxHitOverride = 1;
        [SerializeField] private bool scaleVFXBasedOnEdges;
        [SerializeField] private Vector3 boundsPoint = new Vector3(0, 0, 2.5f);
        [SerializeField] private Vector3 boundsLocalAxis = new Vector3(0, -1, 0);

        private ParticleSystem[] particleSystems;
        private Collider[] colliders;
        private new void Awake()
        {
            base.Awake();
            GetComponent<Rigidbody>().useGravity = false;

            if (particleSystemType == ParticleSystemType.ParticleCollisions)
            {
                particleSystems = GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in particleSystems)
                {
                    if (ps.trigger.enabled)
                    {
                        ps.gameObject.AddComponent<ActionVFXChildTriggerParticleSystem>();
                    }
                }
            }

            colliders = GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) { Debug.LogError("No collider attached to: " + this); }
            foreach (Collider col in colliders)
            {
                if (!col.isTrigger) { Debug.LogError("Make sure all colliders on particle systems are triggers! " + this); }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            foreach (Collider col in colliders)
            {
                col.enabled = IsServer;
            }
        }

        private float DivideBounds(float originalBounds, float newBounds)
        {
            if (Mathf.Approximately(originalBounds, 0)) { return 1; }
            return newBounds / originalBounds;
        }

        RaycastHit[] allHits = new RaycastHit[10];
        private void Start()
        {
            if (scaleVFXBasedOnEdges)
            {
                Vector3 endBoundsPoint = boundsPoint;
                while (endBoundsPoint != Vector3.zero)
                {
                    int allHitsCount = Physics.RaycastNonAlloc(transform.position + (transform.rotation * endBoundsPoint),
                        (transform.rotation * boundsLocalAxis).normalized, allHits, 1,
                        LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore);

                    # if UNITY_EDITOR
                    Debug.DrawRay(transform.position + (transform.rotation * endBoundsPoint), transform.rotation * boundsLocalAxis, Color.yellow, 3);
                    #endif

                    bool bHit = allHitsCount > 0;
                    if (bHit) { break; }

                    endBoundsPoint = Vector3.MoveTowards(endBoundsPoint, Vector3.zero, 0.1f);
                }
                Vector3 newScale = new Vector3(DivideBounds(boundsPoint.x, endBoundsPoint.x), DivideBounds(boundsPoint.y, endBoundsPoint.y), DivideBounds(boundsPoint.z, endBoundsPoint.z));
                transform.localScale = Vector3.Scale(transform.localScale, newScale);
            }
        }

        private bool CanHit(CombatAgent combatAgent)
        {
            if (!IsSpawned) { return false; }
            if (!IsServer) { Debug.LogError("ActionVFXParticleSystem.CanHit() should only be called on the server!"); return false; }

            if (!ShouldAffect(combatAgent)) { return false; }

            bool canHit = true;
            if (hitCounter.ContainsKey(combatAgent))
            {
                if (hitCounter[combatAgent].hitNumber >= (shouldOverrideMaxHits ? maxHitOverride : attack.maxHitLimit)) { canHit = false; }
                if (Time.time - hitCounter[combatAgent].timeOfHit < attack.GetTimeBetweenHits(1)) { canHit = false; }
            }

            if (spellType == SpellType.GroundSpell)
            {
                if (PlayerDataManager.Singleton.CanHit(attacker, combatAgent))
                {
                    if (combatAgent.StatusAgent.IsImmuneToGroundSpells()) { canHit = false; }
                }
            }
            return canHit;
        }

        private void ProcessHit(CombatAgent combatAgent, Vector3 impactPosition)
        {
            if (combatAgent.ProcessProjectileHit(attacker, null, hitCounter, attack, impactPosition, shouldUseAttackerPositionForHitAngles ? attacker.transform.position : transform.position))
            {
                if (!hitCounter.ContainsKey(combatAgent))
                {
                    hitCounter.Add(combatAgent, new(1, Time.time));
                }
                else
                {
                    hitCounter[combatAgent] = new(hitCounter[combatAgent].hitNumber + 1, Time.time);
                }
            }
        }

        private const string layersToHit = "NetworkPrediction";

        protected void OnTriggerEnter(Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }
            if (other.gameObject.layer != LayerMask.NameToLayer(layersToHit)) { return; }

            if (particleSystemType == ParticleSystemType.ParticleCollisions)
            {
                if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    foreach (ParticleSystem ps in particleSystems)
                    {
                        bool skip = false;
                        for (int i = 0; i < ps.trigger.colliderCount; i++)
                        {
                            if (ps.trigger.GetCollider(i) == other)
                            {
                                skip = true;
                                break;
                            }
                        }

                        if (!skip)
                        {
                            ps.trigger.AddCollider(other);
                        }
                    }
                }
            }
            else if (particleSystemType == ParticleSystemType.GenericCollisions)
            {
                if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (networkCollider.CombatAgent)
                    {
                        if (CanHit(networkCollider.CombatAgent))
                        {
                            ProcessHit(networkCollider.CombatAgent, other.ClosestPointOnBounds(transform.position));
                        }
                    }
                }
            }
        }

        protected void OnTriggerStay(Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }
            if (other.gameObject.layer != LayerMask.NameToLayer(layersToHit)) { return; }

            if (particleSystemType == ParticleSystemType.GenericCollisions)
            {
                if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (networkCollider.CombatAgent)
                    {
                        if (CanHit(networkCollider.CombatAgent))
                        {
                            ProcessHit(networkCollider.CombatAgent, other.ClosestPointOnBounds(transform.position));
                        }
                    }
                }
            }
        }

        private Dictionary<CombatAgent, RuntimeWeapon.HitCounterData> hitCounter = new Dictionary<CombatAgent, RuntimeWeapon.HitCounterData>();

        protected new void OnDisable()
        {
            base.OnDisable();
            hitCounter.Clear();
        }

        private bool particleEnterCalledThisFrame;
        protected void LateUpdate()
        {
            particleEnterCalledThisFrame = false;
        }

        public void ProcessOnParticleEnterMessage(ParticleSystem ps)
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            if (particleEnterCalledThisFrame) { return; }
            particleEnterCalledThisFrame = true;

            List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();
            int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter, out ParticleSystem.ColliderData enterColliderData);

            for (int particleIndex = 0; particleIndex < numEnter; particleIndex++)
            {
                for (int colliderIndex = 0; colliderIndex < enterColliderData.GetColliderCount(particleIndex); colliderIndex++)
                {
                    Collider col = (Collider)enterColliderData.GetCollider(particleIndex, colliderIndex);
                    if (col.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                    {
                        if (networkCollider.CombatAgent)
                        {
                            if (CanHit(networkCollider.CombatAgent))
                            {
                                ProcessHit(networkCollider.CombatAgent, col.ClosestPointOnBounds(enter[particleIndex].position));
                            }
                        }
                    }
                }
            }

            List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();
            int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside, out ParticleSystem.ColliderData insideColliderData);

            for (int particleIndex = 0; particleIndex < numInside; particleIndex++)
            {
                for (int colliderIndex = 0; colliderIndex < insideColliderData.GetColliderCount(particleIndex); colliderIndex++)
                {
                    Collider col = (Collider)insideColliderData.GetCollider(particleIndex, colliderIndex);
                    if (col.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                    {
                        if (networkCollider.CombatAgent)
                        {
                            if (CanHit(networkCollider.CombatAgent))
                            {
                                ProcessHit(networkCollider.CombatAgent, col.ClosestPointOnBounds(inside[particleIndex].position));
                            }
                        }
                    }
                }
            }

            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.rotation * boundsPoint, transform.rotation * boundsLocalAxis);
            }
        }
    }
}