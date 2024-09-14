using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.Structures;
using Vi.ScriptableObjects;
using UnityEngine.AI;
using Unity.Netcode;
using Vi.Utility;

namespace Vi.Core.MovementHandlers
{
    [RequireComponent(typeof(GameplayTargetFinder))]
    public class MobMovementHandler : PhysicsMovementHandler
    {
        private GameplayTargetFinder targetFinder;
        protected override void Awake()
        {
            base.Awake();
            targetFinder = GetComponent<GameplayTargetFinder>();
        }

        protected override void Update()
        {
            base.Update();
            SetAnimationMoveInput(Vector3.Distance(Destination, GetPosition()) < 0.5f ? Vector2.zero : GetPathMoveInput());
            transform.position = Rigidbody.transform.position;
            transform.rotation = EvaluateRotation();
        }

        private bool disableBots;
        protected override void RefreshStatus()
        {
            base.RefreshStatus();
            disableBots = FasterPlayerPrefs.Singleton.GetBool("DisableBots");
        }

        private Quaternion EvaluateRotation()
        {
            Vector3 camDirection = targetFinder.GetTarget() ? (targetFinder.GetTarget().transform.position - Rigidbody.position).normalized : (NextPosition - Rigidbody.position).normalized;
            camDirection.Scale(HORIZONTAL_PLANE);

            if (combatAgent.ShouldApplyAilmentRotation())
                return combatAgent.GetAilmentRotation();
            else if (combatAgent.IsGrabbing())
                return transform.rotation;
            else if (combatAgent.IsGrabbed())
            {
                CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                if (grabAssailant)
                {
                    Vector3 rel = grabAssailant.MovementHandler.GetPosition() - GetPosition();
                    rel = Vector3.Scale(rel, HORIZONTAL_PLANE);
                    return Quaternion.LookRotation(rel, Vector3.up);
                }
            }
            else if (!combatAgent.ShouldPlayHitStop())
                return Quaternion.LerpUnclamped(transform.rotation, camDirection == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(camDirection), Time.deltaTime * Random.Range(0.1f, 5));

            return transform.rotation;
        }

        [Header("Mob Movement Handler")]
        [SerializeField] private TargetingType targetingType = TargetingType.StructuresThenPlayers;
        private enum TargetingType
        {
            Players,
            Structures,
            StructuresThenPlayers,
            PlayersThenStructures
        }

        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();

        private void FixedUpdate()
        {
            EvaluateTargetingLogic();
            if (IsServer)
            {
                Move();
                currentPosition.Value = Rigidbody.position;
            }
            else
            {
                Rigidbody.MovePosition(currentPosition.Value);
            }
        }

        private void EvaluateTargetingLogic()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            targetFinder.ClearTarget();
            switch (targetingType)
            {
                case TargetingType.Players:
                    targetFinder.ActiveCombatAgents.Sort((x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
                    foreach (CombatAgent combatAgent in targetFinder.ActiveCombatAgents)
                    {
                        if (!PlayerDataManager.Singleton.CanHit(this.combatAgent, combatAgent)) { continue; }
                        targetFinder.SetTarget(combatAgent);
                    }
                    break;
                case TargetingType.Structures:
                    System.Array.Sort(targetFinder.ActiveStructures, (x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
                    foreach (Structure structure in targetFinder.ActiveStructures)
                    {
                        if (!PlayerDataManager.Singleton.CanHit(combatAgent, structure)) { continue; }
                        targetFinder.SetTarget(structure);
                        break;
                    }
                    break;
                case TargetingType.StructuresThenPlayers:
                    System.Array.Sort(targetFinder.ActiveStructures, (x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
                    foreach (Structure structure in targetFinder.ActiveStructures)
                    {
                        if (!PlayerDataManager.Singleton.CanHit(combatAgent, structure)) { continue; }
                        targetFinder.SetTarget(structure);
                        break;
                    }

                    targetFinder.ActiveCombatAgents.Sort((x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
                    foreach (CombatAgent combatAgent in targetFinder.ActiveCombatAgents)
                    {
                        if (!PlayerDataManager.Singleton.CanHit(this.combatAgent, combatAgent)) { continue; }
                        targetFinder.SetTarget(combatAgent);
                    }
                    break;
                case TargetingType.PlayersThenStructures:
                    targetFinder.ActiveCombatAgents.Sort((x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
                    foreach (CombatAgent combatAgent in targetFinder.ActiveCombatAgents)
                    {
                        if (!PlayerDataManager.Singleton.CanHit(this.combatAgent, combatAgent)) { continue; }
                        targetFinder.SetTarget(combatAgent);
                    }

                    System.Array.Sort(targetFinder.ActiveStructures, (x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
                    foreach (Structure structure in targetFinder.ActiveStructures)
                    {
                        if (!PlayerDataManager.Singleton.CanHit(combatAgent, structure)) { continue; }
                        targetFinder.SetTarget(structure);
                        break;
                    }
                    break;
                default:
                    Debug.LogError("Unsure how to handle targeting type " + targetingType);
                    break;
            }

            if (targetFinder.GetTarget() & !disableBots)
            {
                targetFinder.SetDestination(this);
            }
            else
            {
                SetDestination(GetPosition());
            }
        }

        private void Move()
        {
            Vector3 rootMotion = combatAgent.AnimationHandler.ApplyRootMotion();

            if (!IsSpawned) { return; }

            CalculatePath(Rigidbody.position, NavMesh.AllAreas);

            if (!CanMove() | combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                Rigidbody.velocity = Vector3.zero;
                return;
            }

            if (IsAffectedByExternalForce & !combatAgent.IsGrabbed() & !combatAgent.IsGrabbing()) { Rigidbody.isKinematic = false; return; }

            Vector2 moveInput = GetPathMoveInput();
            Quaternion newRotation = transform.rotation;

            // Apply movement
            Vector3 movement = Vector3.zero;
            if (combatAgent.IsGrabbing())
            {
                Rigidbody.isKinematic = true;
                return;
            }
            else if (combatAgent.IsGrabbed() & combatAgent.GetAilment() == ActionClip.Ailment.None)
            {
                CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                if (grabAssailant)
                {
                    Rigidbody.isKinematic = true;
                    Rigidbody.MovePosition(grabAssailant.MovementHandler.GetPosition() + (grabAssailant.MovementHandler.GetRotation() * Vector3.forward));
                    return;
                }
            }
            else if (combatAgent.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (combatAgent.IsPulled())
            {
                CombatAgent pullAssailant = combatAgent.GetPullAssailant();
                if (pullAssailant)
                {
                    movement = pullAssailant.MovementHandler.GetPosition() - GetPosition();
                }
            }
            else if (combatAgent.AnimationHandler.ShouldApplyRootMotion())
            {
                if (combatAgent.StatusAgent.IsRooted() & combatAgent.GetAilment() != ActionClip.Ailment.Knockup & combatAgent.GetAilment() != ActionClip.Ailment.Knockdown)
                {
                    movement = Vector3.zero;
                }
                else
                {
                    movement = newRotation * rootMotion * GetRootMotionSpeed();
                }
            }
            else if (combatAgent.AnimationHandler.IsAtRest())
            {
                Vector3 targetDirection = newRotation * (new Vector3(moveInput.x, 0, moveInput.y) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= GetRunSpeed();
                movement = combatAgent.StatusAgent.IsRooted() | combatAgent.AnimationHandler.IsReloading() ? Vector3.zero : targetDirection;
            }

            Rigidbody.isKinematic = false;

            if (combatAgent.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            Vector3 startPos = Rigidbody.position;
            startPos.y += stairStepHeight;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }
#if UNITY_EDITOR
                Debug.DrawRay(startPos, movement.normalized, Color.cyan, GetTickRateDeltaTime());
#endif
                startPos.y += stairStepHeight;
                stairMovement += stairStepHeight;

                if (stairMovement > maxStairStepHeight)
                {
                    stairMovement = 0;
                    break;
                }
            }

            if (Physics.CapsuleCast(Rigidbody.position, Rigidbody.position + bodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude * Time.fixedDeltaTime, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore))
            {
                bool collidersIgnoreEachOther = false;
                foreach (Collider c in combatAgent.NetworkCollider.Colliders)
                {
                    if (Physics.GetIgnoreCollision(playerHit.collider, c))
                    {
                        collidersIgnoreEachOther = true;
                        break;
                    }
                }

                if (!collidersIgnoreEachOther)
                {
                    Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - Rigidbody.position, Vector3.up);
                    float angle = targetRot.eulerAngles.y - Quaternion.LookRotation(movement, Vector3.up).eulerAngles.y;

                    if (angle > 180) { angle -= 360; }

                    if (angle > -20 & angle < 20)
                    {
                        movement = Vector3.zero;
                    }
                }
            }

            bool evaluateForce = true;
            if (weaponHandler.CurrentActionClip.shouldIgnoreGravity)
            {
                if (combatAgent.AnimationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip))
                {
                    Rigidbody.AddForce(movement - Rigidbody.velocity, ForceMode.VelocityChange);
                    evaluateForce = false;
                }
            }

            if (evaluateForce)
            {
                if (IsGrounded())
                {
                    Rigidbody.AddForce(new Vector3(movement.x, 0, movement.z) - new Vector3(Rigidbody.velocity.x, 0, Rigidbody.velocity.z), ForceMode.VelocityChange);
                    if (Rigidbody.velocity.y > 0 & Mathf.Approximately(stairMovement, 0)) // This is to prevent slope bounce
                    {
                        Rigidbody.AddForce(new Vector3(0, -Rigidbody.velocity.y, 0), ForceMode.VelocityChange);
                    }
                }
                else // Decelerate horizontal movement while aiRigidbodyorne
                {
                    Vector3 counterForce = Vector3.Slerp(Vector3.zero, new Vector3(-Rigidbody.velocity.x, 0, -Rigidbody.velocity.z), aiRigidbodyorneHorizontalDragMultiplier);
                    Rigidbody.AddForce(counterForce, ForceMode.VelocityChange);
                }
            }
            Rigidbody.AddForce(new Vector3(0, stairMovement, 0), ForceMode.VelocityChange);
            Rigidbody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        }

        protected void EvaluateAction()
        {

        }
    }
}