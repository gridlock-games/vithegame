using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.AI;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Vi.Core.Structures;
using Vi.Utility;

namespace Vi.ArtificialIntelligence
{
    public class CaveCrasherController : MovementHandler
    {
        private Animator animator;
        private CombatAgent combatAgent;
        private new void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
            combatAgent = GetComponent<CombatAgent>();
            GetComponent<PooledObject>().OnSpawnFromPool += OnSpawnFromPool;
        }

        private void OnSpawnFromPool()
        {
            UpdateActivePlayersList();
            UpdateStructureList();
        }

        private List<CombatAgent> activePlayers = new List<CombatAgent>();
        private void UpdateActivePlayersList() { activePlayers = PlayerDataManager.Singleton.GetActiveCombatAgents(combatAgent); }

        private Structure[] structures = new Structure[0];
        private void UpdateStructureList() { structures = PlayerDataManager.Singleton.GetActiveStructures(); }

        HittableAgent targetObject;
        private void EvaluateBotLogic()
        {
            if (IsServer)
            {
                targetObject = null;
                float distanceToStructure = 100;
                foreach (Structure structure in structures)
                {
                    if (!structure) { Debug.LogError("There is a null strcture in the structures list!"); continue; }
                    if (!PlayerDataManager.Singleton.CanHit(combatAgent, structure)) { continue; }
                    targetObject = structure;
                    distanceToStructure = Vector3.Distance(transform.position, structure.transform.position);
                    break;
                }

                activePlayers.Sort((x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
                foreach (CombatAgent player in activePlayers)
                {
                    if (player.GetAilment() == ActionClip.Ailment.Death) { continue; }
                    if (!PlayerDataManager.Singleton.CanHit(combatAgent, player)) { continue; }

                    float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
                    if (distanceToPlayer < 11 & distanceToPlayer < distanceToStructure)
                    {
                        targetObject = player;
                    }
                    break;
                }

                if (targetObject)
                {
                    SetDestination(targetObject.transform.position, false);
                }
                else
                {
                    if (Vector3.Distance(Destination, transform.position) <= stoppingDistance)
                    {
                        float walkRadius = 500;
                        Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
                        randomDirection += transform.position;
                        NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, walkRadius, 1);
                        SetDestination(hit.position, false);
                    }
                }
                EvaluteAction();
            }
        }

        private const float lightAttackDistance = 2.5f;

        private const float ability1DistanceMin = 8;
        private const float ability1Distance = 10;
        private void EvaluteAction()
        {
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { return; }

            if (targetObject)
            {
                float dist = Vector3.Distance(Destination, transform.position);
                if (dist < lightAttackDistance)
                {
                    weaponHandler.LightAttack(true);
                }
                else if (dist < ability1Distance & dist > ability1DistanceMin)
                {
                    weaponHandler.Ability1(true);
                }
            }
        }

        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>();

        private NetworkVariable<bool> isGrounded = new NetworkVariable<bool>(true);

        private const float runAnimationTransitionSpeed = 4;
        private const float rotationSpeed = 120;

        private const float runSpeed = 4;

        private void ProcessMovement()
        {
            if (!IsServer) { return; }

            if (!CanMove() | combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                moveForwardTarget.Value = 0;
                moveSidesTarget.Value = 0;
                return;
            }

            if (!targetObject) { return; }
            Transform target = targetObject.transform;
            if (target)
            {
                SetDestination(target.position, false);
                CalculatePath(transform.position, NavMesh.AllAreas);
            }

            Vector3 inputDir = NextPosition - transform.position;
            inputDir.y = 0;
            inputDir = transform.InverseTransformDirection(inputDir).normalized;

            if (Vector3.Distance(Destination, transform.position) < stoppingDistance)
            {
                inputDir = Vector3.zero;
            }

            if (target)
            {
                Vector3 lookDirection = (target.position - transform.position).normalized;
                lookDirection.Scale(HORIZONTAL_PLANE);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lookDirection), rotationSpeed * Time.deltaTime);
            }

            Vector3 animDir = Vector3.zero;
            // Apply movement
            Vector3 rootMotion = combatAgent.AnimationHandler.ApplyRootMotion() * Mathf.Clamp01(runSpeed - combatAgent.StatusAgent.GetMovementSpeedDecreaseAmount() + combatAgent.StatusAgent.GetMovementSpeedIncreaseAmount());
            Vector3 movement;
            if (combatAgent.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (combatAgent.AnimationHandler.ShouldApplyRootMotion())
            {
                if (combatAgent.StatusAgent.IsRooted() & combatAgent.GetAilment() != ActionClip.Ailment.Knockup & combatAgent.GetAilment() != ActionClip.Ailment.Knockdown)
                {
                    movement = Vector3.zero;
                }
                else
                {
                    movement = rootMotion;
                }
            }
            else
            {
                //Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y) * (attributes.IsFeared() ? -1 : 1));
                Vector3 targetDirection = transform.rotation * (new Vector3(inputDir.x, 0, inputDir.z) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= isGrounded.Value ? Mathf.Max(0, runSpeed - combatAgent.StatusAgent.GetMovementSpeedDecreaseAmount()) + combatAgent.StatusAgent.GetMovementSpeedIncreaseAmount() : 0;
                movement = combatAgent.StatusAgent.IsRooted() | combatAgent.AnimationHandler.IsReloading() ? Vector3.zero : Time.deltaTime * targetDirection;
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }

            if (combatAgent.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            float yOffset = 0.2f;
            Vector3 startPos = transform.position;
            startPos.y += yOffset;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }

                if (Application.isEditor) { Debug.DrawRay(startPos, movement.normalized, Color.cyan, 1f / NetworkManager.NetworkTickSystem.TickRate); }
                startPos.y += yOffset;
                stairMovement = startPos.y - transform.position.y - yOffset;

                if (stairMovement > 0.5f)
                {
                    stairMovement = 0;
                    break;
                }
            }

            movement.y += stairMovement;

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            moveForwardTarget.Value = animDir.z;
            moveSidesTarget.Value = animDir.x;

            if (rb.velocity.magnitude <= 1)
            {
                transform.position += movement;
            }
        }

        private new void Update()
        {
            base.Update();
            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateActivePlayersList(); }
            if (PlayerDataManager.Singleton.StructuresListWasUpdatedThisFrame) { UpdateStructureList(); }
            ProcessMovement();
            EvaluateBotLogic();
            animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            animator.SetFloat("MoveSides", Mathf.MoveTowards(animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
        }
    }
}