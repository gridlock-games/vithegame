using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Vi.Core.MovementHandlers;
using Vi.Core.Weapons;
using Vi.ScriptableObjects;

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
        [SerializeField] private bool latchToFirstTarget;
        [SerializeField] private Vector3 latchPositionOffset;

        private NetworkTransform networkTransform;
        private new void Awake()
        {
            base.Awake();
            GetComponent<Rigidbody>().useGravity = false;
            networkTransform = GetComponent<NetworkTransform>();

            if (particleSystemType == ParticleSystemType.ParticleCollisions)
            {
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

        private float DivideBounds(float originalBounds, float newBounds)
        {
            if (Mathf.Approximately(originalBounds, 0)) { return 1; }
            return newBounds / originalBounds;
        }

        RaycastHit[] allHits = new RaycastHit[10];
        private new void OnEnable()
        {
            base.OnEnable();
            if (scaleVFXBasedOnEdges)
            {
                Vector3 endBoundsPoint = boundsPoint;
                while (endBoundsPoint != Vector3.zero)
                {
                    int allHitsCount = Physics.RaycastNonAlloc(transform.position + (transform.rotation * endBoundsPoint),
                        (transform.rotation * boundsLocalAxis).normalized, allHits, 1,
                        LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore);

                    #if UNITY_EDITOR
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

        private bool CanHit(HittableAgent hittableAgent)
        {
            if (!IsSpawned) { return false; }
            if (!IsServer) { Debug.LogError("ActionVFXParticleSystem.CanHit() should only be called on the server!"); return false; }

            if (!ShouldAffect(hittableAgent)) { return false; }

            bool canHit = true;
            if (hitCounter.ContainsKey(hittableAgent))
            {
                if (hitCounter[hittableAgent].hitNumber >= (shouldOverrideMaxHits ? maxHitOverride : GetAttack().maxHitLimit)) { canHit = false; }
                if (Time.time - hitCounter[hittableAgent].timeOfHit < GetAttack().GetTimeBetweenHits(1)) { canHit = false; }
            }

            if (spellType == SpellType.GroundSpell)
            {
                if (PlayerDataManager.Singleton.CanHit(GetAttacker(), hittableAgent))
                {
                    if (hittableAgent.StatusAgent.IsImmuneToGroundSpells()) { canHit = false; }
                }
            }
            return canHit;
        }

        private void ProcessHit(HittableAgent hittable, Vector3 impactPosition)
        {
            if (hittable.ProcessProjectileHit(GetAttacker(), NetworkObject, null, hitCounter, GetAttack(), impactPosition, shouldUseAttackerPositionForHitAngles ? GetAttacker().transform.position : transform.position))
            {
                if (!hitCounter.ContainsKey(hittable))
                {
                    hitCounter.Add(hittable, new(1, Time.time));
                }
                else
                {
                    hitCounter[hittable] = new(hitCounter[hittable].hitNumber + 1, Time.time);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                latchTargetNetObjId.Value = default;
            }
        }

        private NetworkVariable<ulong> latchTargetNetObjId = new NetworkVariable<ulong>();

        private void LateUpdate()
        {
            if (!IsSpawned) { return; }

            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(latchTargetNetObjId.Value, out NetworkObject latchTarget))
            {
                networkTransform.SyncPositionX = false;
                networkTransform.SyncPositionY = false;
                networkTransform.SyncPositionZ = false;

                networkTransform.SyncRotAngleX = false;
                networkTransform.SyncRotAngleY = false;
                networkTransform.SyncRotAngleZ = false;

                transform.rotation = Quaternion.identity;
                transform.position = latchTarget.transform.position + transform.rotation * latchPositionOffset;
            }
            else
            {
                networkTransform.SyncPositionX = true;
                networkTransform.SyncPositionY = true;
                networkTransform.SyncPositionZ = true;

                networkTransform.SyncRotAngleX = true;
                networkTransform.SyncRotAngleY = true;
                networkTransform.SyncRotAngleZ = true;
            }
        }

        private const string layersToHit = "NetworkPrediction";

        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (other.gameObject.layer != LayerMask.NameToLayer(layersToHit)) { return; }

            if (particleSystemType == ParticleSystemType.ParticleCollisions)
            {
                if (other.isTrigger) { return; }
                if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider) | other.transform.root.TryGetComponent(out IHittable hittable))
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

                    if (latchToFirstTarget & !latchAssigned)
                    {
                        if (networkCollider)
                        {
                            if (!NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(latchTargetNetObjId.Value))
                            {
                                if (ShouldAffect(networkCollider.CombatAgent))
                                {
                                    latchTargetNetObjId.Value = networkCollider.CombatAgent.NetworkObjectId;
                                }
                            }
                        }
                    }
                }
            }
            else if (particleSystemType == ParticleSystemType.GenericCollisions)
            {
                if (other.isTrigger) { return; }
                if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (latchToFirstTarget & !latchAssigned)
                    {
                        if (!NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(latchTargetNetObjId.Value))
                        {
                            if (ShouldAffect(networkCollider.CombatAgent))
                            {
                                latchTargetNetObjId.Value = networkCollider.CombatAgent.NetworkObjectId;
                            }
                        }
                    }
                    
                    if (CanHit(networkCollider.CombatAgent))
                    {
                        ProcessHit(networkCollider.CombatAgent, other.ClosestPointOnBounds(transform.position));
                    }
                }
                else if (other.transform.root.TryGetComponent(out HittableAgent hittable))
                {
                    if (CanHit(hittable))
                    {
                        ProcessHit(hittable, other.ClosestPointOnBounds(transform.position));
                    }
                }
            }
        }

        private bool latchAssigned;

        protected void OnTriggerStay(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (other.gameObject.layer != LayerMask.NameToLayer(layersToHit)) { return; }

            if (particleSystemType == ParticleSystemType.GenericCollisions)
            {
                if (other.isTrigger) { return; }
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
                else if (other.transform.root.TryGetComponent(out HittableAgent hittable))
                {
                    if (CanHit(hittable))
                    {
                        ProcessHit(hittable, other.ClosestPointOnBounds(transform.position));
                    }
                }
            }
        }

        private Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounter = new Dictionary<IHittable, RuntimeWeapon.HitCounterData>();

        public void AddToHitCounter(Dictionary<IHittable, RuntimeWeapon.HitCounterData> hitCounterToAdd)
        {
            if (!IsSpawned) { Debug.LogError("Trying to add to hit counter while not spawned " + this); }
            if (!IsServer) { Debug.LogWarning("Trying to add to hit counter when we are not the server, this will have no effect " + this); }
            foreach (KeyValuePair<IHittable, RuntimeWeapon.HitCounterData> kvp in hitCounterToAdd)
            {
                if (hitCounter.ContainsKey(kvp.Key))
                {
                    hitCounter[kvp.Key] = new RuntimeWeapon.HitCounterData(hitCounter[kvp.Key].hitNumber + kvp.Value.hitNumber, Time.time);
                }
                else
                {
                    hitCounter.Add(kvp.Key, new RuntimeWeapon.HitCounterData(1, Time.time));
                }
            }
        }

        protected new void OnDisable()
        {
            base.OnDisable();
            hitCounter.Clear();
            foreach (ParticleSystem ps in particleSystems)
            {
                for (int i = 0; i < ps.trigger.colliderCount; i++)
                {
                    ps.trigger.RemoveCollider(i);
                }
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            latchAssigned = default;
        }

        List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();
        List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();
        public void ProcessOnParticleEnterMessage(ParticleSystem ps)
        {
            if (!IsSpawned)
            if (!IsServer) { return; }

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
                    else if (col.transform.root.TryGetComponent(out HittableAgent hittableAgent))
                    {
                        if (CanHit(hittableAgent))
                        {
                            ProcessHit(hittableAgent, col.ClosestPointOnBounds(enter[particleIndex].position));
                        }
                    }
                }
            }

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
                    else if (col.transform.root.TryGetComponent(out HittableAgent hittableAgent))
                    {
                        if (CanHit(hittableAgent))
                        {
                            ProcessHit(hittableAgent, col.ClosestPointOnBounds(inside[particleIndex].position));
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