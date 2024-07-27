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

            animator.cullingMode = WebRequestManager.IsServerBuild() | NetworkManager.Singleton.IsServer ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullCompletely;
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

        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            Collider[] colliders = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            System.Array.Sort(colliders, (x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
            for (int i = 0; i < colliders.Length; i++)
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
                        navMeshAgent.destination = networkCollider.MovementHandler.GetPosition();
                        if (Vector3.Distance(navMeshAgent.destination, transform.position) < navMeshAgent.stoppingDistance)
                        {
                            bool hitSuccess = networkCollider.Attributes.ProcessProjectileHit(attacker, null, new Dictionary<Attributes, RuntimeWeapon.HitCounterData>(),
                                attack, networkCollider.Attributes.transform.position, transform.position);

                            if (hitSuccess)
                            {
                                NetworkObject.Despawn(true);
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}