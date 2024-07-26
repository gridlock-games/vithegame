using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

namespace Vi.Core
{
    public class ExplosiveCretin : GameInteractiveActionVFX
    {
        private Rigidbody rb;
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
        }

        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();

        private const float runAnimationTransitionSpeed = 5;

        private void Update()
        {
            if (IsServer)
            {
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

        private NetworkCollider target;
        private Collider[] colliders = new Collider[20];
        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            target = null;
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, colliders, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].TryGetComponent(out NetworkCollider networkCollider))
                {
                    bool shouldAffect = PlayerDataManager.Singleton.CanHit(networkCollider.Attributes, attacker);
                    if (spellType == SpellType.GroundSpell)
                    {
                        if (networkCollider.Attributes.IsImmuneToGroundSpells()) { shouldAffect = false; }
                    }

                    if (shouldAffect)
                    {
                        target = networkCollider;
                        navMeshAgent.destination = networkCollider.MovementHandler.GetPosition();
                        if (Vector3.Distance(navMeshAgent.destination, transform.position) < navMeshAgent.stoppingDistance)
                        {
                            NetworkObject.Despawn(true);
                        }
                        break;
                    }
                }
            }
        }
    }
}