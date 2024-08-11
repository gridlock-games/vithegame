using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

namespace Vi.Core.VFX.Staff
{
    public class ExplosiveCretin : GameInteractiveActionVFX
    {
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private new void Awake()
        {
            base.Awake();
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            animator.cullingMode = WebRequestManager.IsServerBuild() | NetworkManager.Singleton.IsServer ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullCompletely;
        }

        private float serverSpawnTime;
        private const float cretinDuration = 5;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { serverSpawnTime = Time.time; }
        }

        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();

        private const float runAnimationTransitionSpeed = 5;

        private bool despawnCalled;

        private void Update()
        {
            if (despawnCalled) { return; }
            if (IsServer)
            {
                if (Time.time - serverSpawnTime > cretinDuration) { despawnCalled = true; NetworkObject.Despawn(true); }
                Vector3 inputDir;
                if (Vector3.Distance(navMeshAgent.destination, transform.position) < navMeshAgent.stoppingDistance)
                {
                    inputDir = Vector3.zero;
                }
                else
                {
                    inputDir = navMeshAgent.destination - transform.position;
                    inputDir.y = 0;
                    inputDir = transform.InverseTransformDirection(inputDir).normalized;
                }
                moveForwardTarget.Value = inputDir.z;
            }
            animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
        }

        private const float radius = 10;

        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }
            if (despawnCalled) { return; }

            Collider[] colliders = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            System.Array.Sort(colliders, (x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
            int networkColliderCount = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (networkCollider.CombatAgent == attacker) { continue; }

                    networkColliderCount++;
                    bool shouldAffect = PlayerDataManager.Singleton.CanHit(attacker, networkCollider.CombatAgent);
                    if (shouldAffect)
                    {
                        if (spellType == SpellType.GroundSpell)
                        {
                            if (networkCollider.CombatAgent.StatusAgent.IsImmuneToGroundSpells())
                            {
                                shouldAffect = false;
                                despawnCalled = true;
                                NetworkObject.Despawn(true);
                            }
                        }
                    }
                    
                    if (shouldAffect)
                    {
                        if (networkColliderCount == 1)
                        {
                            Vector3 targetPosition = networkCollider.MovementHandler.GetPosition();
                            if (new Vector2(navMeshAgent.destination.x, navMeshAgent.destination.z) != new Vector2(targetPosition.x, targetPosition.z)) { navMeshAgent.destination = networkCollider.MovementHandler.GetPosition(); }
                        }

                        if (Vector3.Distance(networkCollider.transform.position, transform.position) <= navMeshAgent.stoppingDistance + 0.5f)
                        {
                            bool hitSuccess = networkCollider.CombatAgent.ProcessProjectileHit(attacker, null, new Dictionary<CombatAgent, RuntimeWeapon.HitCounterData>(),
                                attack, networkCollider.CombatAgent.transform.position, transform.position);

                            despawnCalled = true;
                            NetworkObject.Despawn(true);
                        }
                    }
                }
            }
        }
    }
}