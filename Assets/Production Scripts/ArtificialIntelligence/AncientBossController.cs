using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Vi.Core;
using Unity.Netcode;

namespace Vi.ArtificialIntelligence
{
    public class AncientBossController : MovementHandler
    {
        private const float roamRadius = 5;

        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private Vector3 startingPosition;
        private new void Awake()
        {
            base.Awake();
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            startingPosition = transform.position;
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;
        }

        public override void OnNetworkSpawn()
        {
            currentPosition.Value = transform.position;
            currentRotation.Value = transform.rotation;
            if (IsServer) { NetworkManager.NetworkTickSystem.Tick += ProcessMovementTick; }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) { NetworkManager.NetworkTickSystem.Tick -= ProcessMovementTick; }
        }

        private const float angularSpeed = 540;
        private const float runSpeed = 5;
        private const float gravitySphereCastRadius = 0.6f;
        private const float runAnimationTransitionSpeed = 5;
        private readonly Vector3 gravitySphereCastPositionOffset = new Vector3(0, 0.6f, 0);

        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> isGrounded = new NetworkVariable<bool>();

        private void ProcessMovementTick()
        {
            Vector3 inputDir = transform.InverseTransformDirection(navMeshAgent.nextPosition - currentPosition.Value).normalized;

            if (Vector3.Distance(navMeshAgent.destination, currentPosition.Value) < navMeshAgent.stoppingDistance)
            {
                inputDir = Vector3.zero;
            }
            //Debug.Log(Vector3.Distance(navMeshAgent.destination, currentPosition.Value));

            Vector3 lookDirection = (navMeshAgent.nextPosition - currentPosition.Value).normalized;
            lookDirection.Scale(HORIZONTAL_PLANE);

            //Quaternion newRotation = currentRotation.Value;
            //if (attributes.ShouldApplyAilmentRotation())
            //    newRotation = attributes.GetAilmentRotation();
            //if (weaponHandler.IsAiming() & !attributes.ShouldPlayHitStop())
            //    newRotation = lookDirection != Vector3.zero ? Quaternion.LookRotation(lookDirection) : currentRotation.Value;
            //else if (!attributes.ShouldPlayHitStop())
            //    newRotation = lookDirection != Vector3.zero ? Quaternion.RotateTowards(currentRotation.Value, Quaternion.LookRotation(lookDirection), 1f / NetworkManager.NetworkTickSystem.TickRate * angularSpeed) : currentRotation.Value;

            // Handle gravity
            Vector3 gravity = Vector3.zero;
            RaycastHit[] allHits = Physics.SphereCastAll(currentPosition.Value + currentRotation.Value * gravitySphereCastPositionOffset,
                gravitySphereCastRadius, Physics.gravity,
                gravitySphereCastPositionOffset.magnitude, LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            bool bHit = false;
            foreach (RaycastHit gravityHit in allHits)
            {
                gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Mathf.Clamp01(gravityHit.distance) * Physics.gravity;
                bHit = true;
                break;
            }

            if (bHit)
            {
                isGrounded.Value = true;
            }
            else // If no sphere cast hit
            {
                if (Physics.Raycast(currentPosition.Value + currentRotation.Value * gravitySphereCastPositionOffset,
                    Physics.gravity, 1, LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
                {
                    isGrounded.Value = true;
                }
                else
                {
                    isGrounded.Value = false;
                    gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Physics.gravity;
                }
            }

            Vector3 animDir = Vector3.zero;
            // Apply movement
            Vector3 movement;

            Vector3 targetDirection = transform.rotation * new Vector3(inputDir.x, 0, inputDir.z);
            targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
            targetDirection *= isGrounded.Value ? 1 : 0;
            movement = 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection;
            animDir = new Vector3(targetDirection.x, 0, targetDirection.z);

            float stairMovement = 0;
            float yOffset = 0.2f;
            Vector3 startPos = currentPosition.Value;
            startPos.y += yOffset;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }

                Debug.DrawRay(startPos, movement.normalized, Color.cyan, 1f / NetworkManager.NetworkTickSystem.TickRate);
                startPos.y += yOffset;
                stairMovement = startPos.y - currentPosition.Value.y - yOffset;

                if (stairMovement > 0.5f)
                {
                    stairMovement = 0;
                    break;
                }
            }

            movement.y += stairMovement;

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            if (IsOwner)
            {
                moveForwardTarget.Value = animDir.z;
                moveSidesTarget.Value = animDir.x;
            }

            currentPosition.Value += movement + gravity;
            currentRotation.Value = transform.rotation;
            navMeshAgent.nextPosition = currentPosition.Value;
        }

        private void Update()
        {
            if (navMeshAgent.isOnNavMesh)
            {
                if (Vector3.Distance(navMeshAgent.destination, transform.position) <= navMeshAgent.stoppingDistance)
                {
                    navMeshAgent.destination = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), transform.position.y, Random.Range(-roamRadius, roamRadius));
                }

                //transform.position = navMeshAgent.nextPosition;

                //Vector3 inputDir = transform.InverseTransformDirection(navMeshAgent.nextPosition - transform.position).normalized;
                //Vector3 targetDirection = transform.rotation * new Vector3(inputDir.x, 0, inputDir.z);
                //Vector3 animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
                //animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
                //if (IsOwner)
                //{
                //    moveForwardTarget.Value = animDir.z;
                //    moveSidesTarget.Value = animDir.x;
                //}

                //animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), animDir.z, Time.deltaTime * runAnimationTransitionSpeed));
                //animator.SetFloat("MoveSides", Mathf.MoveTowards(animator.GetFloat("MoveSides"), animDir.x, Time.deltaTime * runAnimationTransitionSpeed));
            }

            animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            animator.SetFloat("MoveSides", Mathf.MoveTowards(animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            //animator.SetBool("IsGrounded", isGrounded.Value);
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentPosition.Value, 0.5f);

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(navMeshAgent.destination, 0.25f);
            }
        }
    }
}