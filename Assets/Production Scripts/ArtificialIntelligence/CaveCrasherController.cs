using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.AI;
using Unity.Netcode;

namespace Vi.ArtificialIntelligence
{
    public class CaveCrasherController : MovementHandler
    {
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private new void Awake()
        {
            base.Awake();
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
        }

        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>();

        private const float runAnimationTransitionSpeed = 4;
        private const float rotationSpeed = 120;

        private new void Update()
        {
            base.Update();
            if (IsServer)
            {
                if (NetworkManager.LocalClient.PlayerObject)
                {
                    navMeshAgent.destination = NetworkManager.LocalClient.PlayerObject.transform.position;
                }

                if (Vector3.Distance(transform.position, navMeshAgent.destination) <= navMeshAgent.stoppingDistance - 0.5f)
                {
                    //float walkRadius = 500;
                    //Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
                    //randomDirection += transform.position;
                    //NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, walkRadius, 1);
                    //navMeshAgent.destination = hit.position;
                }
                else
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, navMeshAgent.velocity == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(navMeshAgent.velocity.normalized), rotationSpeed * Time.deltaTime);
                }

                Vector3 animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(navMeshAgent.velocity, 1));
                moveForwardTarget.Value = animDir.z;
                moveSidesTarget.Value = animDir.x;
            }
            animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            animator.SetFloat("MoveSides", Mathf.MoveTowards(animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) { return; }
            if (!navMeshAgent.hasPath) { return; }
            Gizmos.color = Color.magenta;
            for (int i = 0; i < navMeshAgent.path.corners.Length; i++)
            {
                Gizmos.DrawSphere(navMeshAgent.path.corners[i], 0.5f);
                if (i == 0)
                {
                    Gizmos.DrawLine(transform.position, navMeshAgent.path.corners[i]);
                }
                else
                {
                    Gizmos.DrawLine(navMeshAgent.path.corners[i - 1], navMeshAgent.path.corners[i]);
                }
            }
        }
    }
}