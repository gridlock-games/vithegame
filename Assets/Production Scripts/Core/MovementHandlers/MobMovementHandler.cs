using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.Structures;
using Vi.ScriptableObjects;
using UnityEngine.AI;
using Unity.Netcode;
using Vi.Utility;
using Vi.Core.CombatAgents;
using Vi.Core.GameModeManagers;
using System.Linq;

namespace Vi.Core.MovementHandlers
{
    [RequireComponent(typeof(GameplayTargetFinder))]
    public class MobMovementHandler : PhysicsMovementHandler
    {
        private GameplayTargetFinder targetFinder;
        private Mob mob;
        protected override void Awake()
        {
            base.Awake();
            targetFinder = GetComponent<GameplayTargetFinder>();
            mob = GetComponent<Mob>();

            if (!mob.GetWeaponOption().weapon.GetAbility1()) { canUseAbility1 = false; }
            if (!mob.GetWeaponOption().weapon.GetAbility2()) { canUseAbility2 = false; }
        }

        protected override void Update()
        {
            base.Update();
            if (combatAgent.GetAilment() != ActionClip.Ailment.Death & CanMove())
            {
                transform.position = Rigidbody.transform.position;
                transform.rotation = EvaluateRotation();
            }
            SetAnimationMoveInput(IsGrounded() ? GetPathMoveInput(true) : Vector2.zero);

            if (IsServer & IsSpawned)
            {
                if (canRage)
                {
                    if (!mob.IsRaging)
                    {
                        if (mob.GetHP() / mob.GetMaxHP() < HPRagePercent)
                        {
                            mob.ActivateRageWithoutCheckingRageParam();
                        }
                    }
                }
            }
        }

        private bool disableBots;
        protected override void RefreshStatus()
        {
            base.RefreshStatus();
            disableBots = FasterPlayerPrefs.Singleton.GetBool("DisableBots");
        }

        private Quaternion EvaluateRotation()
        {
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { return transform.rotation; }

            Vector3 camDirection = (NextPosition - Rigidbody.position).normalized;

            if (combatAgent.ShouldApplyAilmentRotation())
                return combatAgent.GetAilmentRotation();
            else if (combatAgent.IsGrabbing)
                return transform.rotation;
            else if (combatAgent.IsGrabbed)
            {
                CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                if (grabAssailant)
                {
                    Vector3 rel = grabAssailant.MovementHandler.GetPosition() - GetPosition();
                    return IsolateYRotation(Quaternion.LookRotation(rel, Vector3.up));
                }
            }
            else if (!combatAgent.ShouldPlayHitStop() & !disableBots)
            {
                if (!targetFinder.GetTarget() & targetingConstrainedByDistance) { return transform.rotation; }
                
                if (targetFinder.GetTarget())
                {
                    if (Vector3.Distance(GetPosition(), Destination) < stoppingDistance)
                    {
                        return Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetFinder.GetTarget().transform.position - GetPosition()), Time.deltaTime * 3);
                    }
                }
                
                return Quaternion.Lerp(transform.rotation, camDirection == Vector3.zero ? Quaternion.identity : IsolateYRotation(Quaternion.LookRotation(camDirection)), Time.deltaTime * 3);
            }

            return transform.rotation;
        }

        [Header("Mob Movement Handler")]
        [SerializeField] private TargetingType targetingType = TargetingType.StructuresThenPlayers;
        [SerializeField] private float targetingSwitchDistance = 11;

        [SerializeField] private bool targetingConstrainedByDistance;
        [SerializeField] private float maxTargetDistance;

        [Header("Flying")]
        [SerializeField] private bool canFly;
        [SerializeField] private AnimationCurve flightMovement = new AnimationCurve();
        private enum TargetingType
        {
            Players,
            Structures,
            StructuresThenPlayers,
            PlayersThenStructures,
            HighestKillPlayer,
            HighestDamageInflictedToSelf
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            EvaluateTargetingLogic();
            if (IsServer & IsSpawned)
            {
                EvaluateAction();
                Move();
            }
        }

        private void EvaluateTargetingLogic()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            targetFinder.ClearTarget();

            if (disableBots) { SetDestination(GetPosition()); return; }

            if (mob.Master)
            {
                if (mob.Master.GetLastAttackingCombatAgent())
                {
                    targetFinder.SetTarget(mob.Master.GetLastAttackingCombatAgent());
                    targetFinder.SetDestination(this);
                    return;
                }
            }

            switch (targetingType)
            {
                case TargetingType.Players:
                    foreach (CombatAgent combatAgent in targetFinder.ActiveCombatAgents.OrderBy(item => Vector3.Distance(item.NetworkCollider.GetClosestPoint(GetPosition()), GetPosition())))
                    {
                        if (combatAgent == this.combatAgent) { continue; }
                        if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(this.combatAgent, combatAgent)) { continue; }

                        if (targetingConstrainedByDistance)
                        {
                            if (Vector3.Distance(combatAgent.NetworkCollider.GetClosestPoint(GetPosition()), roamStartPosition) > maxTargetDistance)
                            {
                                continue;
                            }
                        }

                        targetFinder.SetTarget(combatAgent);
                        break;
                    }
                    break;
                case TargetingType.Structures:
                    foreach (Structure structure in targetFinder.ActiveStructures.OrderBy(item => Vector3.Distance(item.GetClosestPoint(GetPosition()), GetPosition())))
                    {
                        if (structure.IsDead) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(combatAgent, structure)) { continue; }

                        if (targetingConstrainedByDistance)
                        {
                            if (Vector3.Distance(structure.GetClosestPoint(GetPosition()), roamStartPosition) > maxTargetDistance)
                            {
                                continue;
                            }
                        }

                        targetFinder.SetTarget(structure);
                        break;
                    }
                    break;
                case TargetingType.StructuresThenPlayers:
                    float distanceToStructure = Mathf.Infinity;
                    foreach (Structure structure in targetFinder.ActiveStructures.OrderBy(item => Vector3.Distance(item.GetClosestPoint(GetPosition()), GetPosition())))
                    {
                        if (structure.IsDead) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(combatAgent, structure)) { continue; }

                        distanceToStructure = Vector3.Distance(structure.GetClosestPoint(GetPosition()), GetPosition());
                        if (targetingConstrainedByDistance)
                        {
                            if (Vector3.Distance(structure.GetClosestPoint(GetPosition()), roamStartPosition) > maxTargetDistance)
                            {
                                continue;
                            }
                        }

                        targetFinder.SetTarget(structure);
                        break;
                    }

                    foreach (CombatAgent combatAgent in targetFinder.ActiveCombatAgents.OrderBy(item => Vector3.Distance(item.NetworkCollider.GetClosestPoint(GetPosition()), GetPosition())))
                    {
                        if (combatAgent == this.combatAgent) { continue; }
                        if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(this.combatAgent, combatAgent)) { continue; }
                        float dist = Vector3.Distance(combatAgent.NetworkCollider.GetClosestPoint(GetPosition()), GetPosition());

                        if (targetingConstrainedByDistance)
                        {
                            if (Vector3.Distance(combatAgent.NetworkCollider.GetClosestPoint(GetPosition()), roamStartPosition) > maxTargetDistance)
                            {
                                continue;
                            }
                        }

                        if (dist > targetingSwitchDistance | dist > distanceToStructure) { continue; }
                        targetFinder.SetTarget(combatAgent);
                        break;
                    }
                    break;
                case TargetingType.PlayersThenStructures:
                    float distanceToAgent = Mathf.Infinity;
                    foreach (CombatAgent combatAgent in targetFinder.ActiveCombatAgents.OrderBy(item => Vector3.Distance(item.NetworkCollider.GetClosestPoint(GetPosition()), GetPosition())))
                    {
                        if (combatAgent == this.combatAgent) { continue; }
                        if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(this.combatAgent, combatAgent)) { continue; }
                        distanceToAgent = Vector3.Distance(combatAgent.NetworkCollider.GetClosestPoint(GetPosition()), GetPosition());

                        if (targetingConstrainedByDistance)
                        {
                            if (Vector3.Distance(combatAgent.NetworkCollider.GetClosestPoint(GetPosition()), roamStartPosition) > maxTargetDistance)
                            {
                                continue;
                            }
                        }

                        targetFinder.SetTarget(combatAgent);
                        break;
                    }

                    foreach (Structure structure in targetFinder.ActiveStructures.OrderBy(item => Vector3.Distance(item.GetClosestPoint(GetPosition()), GetPosition())))
                    {
                        if (structure.IsDead) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(combatAgent, structure)) { continue; }
                        float dist = Vector3.Distance(structure.GetClosestPoint(GetPosition()), GetPosition());

                        if (targetingConstrainedByDistance)
                        {
                            if (Vector3.Distance(structure.GetClosestPoint(GetPosition()), roamStartPosition) > maxTargetDistance)
                            {
                                continue;
                            }
                        }

                        if (dist > targetingSwitchDistance | dist > distanceToAgent) { continue; }
                        targetFinder.SetTarget(structure);
                        break;
                    }
                    break;
                case TargetingType.HighestKillPlayer:
                    foreach (Attributes attributes in targetFinder.ActivePlayers.OrderByDescending(item => GameModeManager.Singleton.GetPlayerScore(item.GetPlayerDataId()).killsThisRound).ThenBy(item => Vector3.Distance(item.NetworkCollider.GetClosestPoint(GetPosition()), GetPosition())))
                    {
                        if (attributes == combatAgent) { continue; }
                        if (attributes.GetAilment() == ActionClip.Ailment.Death) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(combatAgent, attributes)) { continue; }

                        if (targetingConstrainedByDistance)
                        {
                            if (Vector3.Distance(attributes.NetworkCollider.GetClosestPoint(GetPosition()), roamStartPosition) > maxTargetDistance)
                            {
                                continue;
                            }
                        }

                        targetFinder.SetTarget(attributes);
                        break;
                    }
                    break;
                case TargetingType.HighestDamageInflictedToSelf:
                    foreach (KeyValuePair<CombatAgent, float> kvp in combatAgent.GetDamageMappingThisLifeFromAliveAgents().OrderByDescending(item => item.Value))
                    {
                        if (kvp.Key == combatAgent) { continue; }
                        if (kvp.Key.GetAilment() == ActionClip.Ailment.Death) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(combatAgent, kvp.Key)) { continue; }

                        if (targetingConstrainedByDistance)
                        {
                            if (Vector3.Distance(kvp.Key.NetworkCollider.GetClosestPoint(GetPosition()), roamStartPosition) > maxTargetDistance)
                            {
                                continue;
                            }
                        }

                        targetFinder.SetTarget(kvp.Key);
                        break;
                    }
                    break;
                default:
                    Debug.LogError("Unsure how to handle targeting type " + targetingType);
                    break;
            }

            if (targetFinder.GetTarget())
            {
                targetFinder.SetDestination(this);
            }
            else if (ShouldStop)
            {
                SetDestination(GetRandomDestination());
            }
        }

        private bool ShouldStop { get { return Vector3.Distance(GetPosition(), Destination) < stoppingDistance; } }


        private NetworkVariable<bool> lastMovementWasZeroSynced = new NetworkVariable<bool>();

        private void OnLastMovementWasZeroSyncedChanged(bool prev, bool current)
        {
            LastMovementWasZero = current;
        }

        private void SetLastMovement(Vector3 lastMovement)
        {
            bool value = lastMovement == Vector3.zero;

            LastMovementWasZero = value;

            if (IsServer)
            {
                lastMovementWasZeroSynced.Value = value;
            }
        }

        private void Move()
        {
            Vector3 rootMotion = combatAgent.AnimationHandler.ApplyRootMotion();
            if (combatAgent.StatusAgent.IsRooted())
            {
                rootMotion.x = 0;
                rootMotion.z = 0;
            }

            if (!IsSpawned)
            {
                SetLastMovement(Vector3.zero);
                return;
            }

            CalculatePath(Rigidbody.position);

            if (!CanMove())
            {
                transform.position = Rigidbody.position;
                Rigidbody.Sleep();
                SetLastMovement(Vector3.zero);
                return;
            }
            else if (combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                Rigidbody.Sleep();
                SetLastMovement(Vector3.zero);
                return;
            }

            if (IsAffectedByExternalForce & !combatAgent.IsGrabbed & !combatAgent.IsGrabbing)
            {
                Rigidbody.isKinematic = false;
                SetLastMovement(Vector3.zero);
                return;
            }

            Vector2 moveInput = GetPathMoveInput(false);
            Quaternion newRotation = transform.rotation;

            // Apply movement
            Vector3 movement = Vector3.zero;
            if (combatAgent.IsGrabbing)
            {
                Rigidbody.isKinematic = true;
                SetLastMovement(Vector3.zero);
                return;
            }
            else if (combatAgent.IsGrabbed & combatAgent.GetAilment() == ActionClip.Ailment.None)
            {
                CombatAgent grabAssailant = combatAgent.GetGrabAssailant();
                if (grabAssailant)
                {
                    Rigidbody.isKinematic = true;
                    Rigidbody.MovePosition(grabAssailant.MovementHandler.GetPosition() + (grabAssailant.MovementHandler.GetRotation() * Vector3.forward));
                    SetLastMovement(Vector3.zero);
                    return;
                }
            }
            else if (combatAgent.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (combatAgent.IsPulled)
            {
                movement = combatAgent.GetPullAssailantPosition() - GetPosition();
            }
            else if (combatAgent.AnimationHandler.ShouldApplyRootMotion())
            {
                if (combatAgent.StatusAgent.IsRooted() & combatAgent.GetAilment() != ActionClip.Ailment.Knockup & combatAgent.GetAilment() != ActionClip.Ailment.Knockdown)
                {
                    movement = Vector3.zero;
                }
                else
                {
                    movement = newRotation * rootMotion;
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
            Vector3 startPos = Rigidbody.position + newRotation * stairRaycastingStartOffset;
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

            bool evaluateForce = true;
            if (weaponHandler.CurrentActionClip.shouldIgnoreGravity & combatAgent.AnimationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip))
            {
                if (movement.y >= 0)
                {
                    Rigidbody.AddForce(movement - Rigidbody.linearVelocity, ForceMode.VelocityChange);
                }
                else
                {
                    Rigidbody.AddForce(movement - new Vector3(Rigidbody.linearVelocity.x, 0, Rigidbody.linearVelocity.z), ForceMode.VelocityChange);
                }
                evaluateForce = false;
            }

            if (evaluateForce)
            {
                if (IsGrounded() | canFly)
                {
                    Rigidbody.AddForce(new Vector3(movement.x, 0, movement.z) - new Vector3(Rigidbody.linearVelocity.x, 0, Rigidbody.linearVelocity.z), ForceMode.VelocityChange);
                    if (Rigidbody.linearVelocity.y > 0 & Mathf.Approximately(stairMovement, 0)) // This is to prevent slope bounce
                    {
                        Rigidbody.AddForce(new Vector3(0, -Rigidbody.linearVelocity.y, 0), ForceMode.VelocityChange);
                    }

                    if (canFly)
                    {
                        if (Vector2.Distance(new Vector2(Destination.x, Destination.z), new Vector2(Rigidbody.position.x, Rigidbody.position.z)) > LightAttackDistance + 1)
                        {
                            Rigidbody.AddForce(new Vector3(0, Random.Range(0, flightMovement.EvaluateNormalizedTime(flightTime)), 0), ForceMode.VelocityChange);
                            flightTime += Time.fixedDeltaTime;
                            if (flightTime > 1) { flightTime = 0; }
                        }
                    }
                }
                else // Decelerate horizontal movement while aiRigidbodyorne
                {
                    Vector3 counterForce = Vector3.Slerp(Vector3.zero, new Vector3(-Rigidbody.linearVelocity.x, 0, -Rigidbody.linearVelocity.z), airborneHorizontalDragMultiplier);
                    Rigidbody.AddForce(counterForce, ForceMode.VelocityChange);
                }
            }
            Rigidbody.AddForce(new Vector3(0, stairMovement * stairStepForceMultiplier, 0), ForceMode.VelocityChange);
            Rigidbody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
            SetLastMovement(movement);
        }

        private float flightTime;

        private float LightAttackDistance { get { return stoppingDistance + 2.5f; } }

        [Header("Ability1")]
        [SerializeField] private bool canUseAbility1 = true;
        [SerializeField] private bool ability1IsConstrainedByDistance = true;
        [SerializeField] private float ability1DistanceMin = 8;
        [SerializeField] private float ability1DistanceMax = 10;

        [Header("Ability2")]
        [SerializeField] private bool canUseAbility2 = false;
        [SerializeField] private bool ability2IsConstrainedByDistance = true;
        [SerializeField] private float ability2DistanceMin = 2;
        [SerializeField] private float ability2DistanceMax = 4;

        [Header("Suicide")]
        [SerializeField] private bool canSuicide;
        [SerializeField] private float suicideDistance;
        [SerializeField] private bool suicideIsConstrainedByTime;
        [SerializeField] private float suicideTimer;

        [Header("Rage")]
        [SerializeField] private bool canRage;
        [SerializeField] private float HPRagePercent = 0.5f;

        private float spawnFixedTime = Mathf.NegativeInfinity;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            spawnFixedTime = Time.fixedTime;

            if (mob.Master)
            {
                roamStartPosition = mob.Master.transform.position;
            }
            else
            {
                roamStartPosition = transform.position;
            }

            if (!IsServer & !IsOwner)
            {
                lastMovementWasZeroSynced.OnValueChanged += OnLastMovementWasZeroSyncedChanged;
            }
        }

        private Vector3 roamStartPosition;

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            spawnFixedTime = Mathf.NegativeInfinity;

            if (!IsServer & !IsOwner)
            {
                lastMovementWasZeroSynced.OnValueChanged -= OnLastMovementWasZeroSyncedChanged;
            }
        }

        private void EvaluateAction()
        {
            if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { return; }
            if (combatAgent.StatusAgent.IsFeared()) { return; }

            if (canSuicide & suicideIsConstrainedByTime)
            {
                if (Time.fixedTime - spawnFixedTime >= suicideTimer)
                {
                    combatAgent.ProcessEnvironmentDamage(-combatAgent.GetHP(), NetworkObject);
                    return;
                }
            }

            if (targetFinder.GetTarget())
            {
                float dist = Vector3.Distance(Destination, transform.position);
                if (dist < suicideDistance & canSuicide)
                {
                    combatAgent.ProcessEnvironmentDamage(-combatAgent.GetHP(), NetworkObject);
                    return;
                }

                if (canUseAbility1)
                {
                    if (combatAgent.AnimationHandler.CanPlayActionClip(weaponHandler.GetWeapon().GetAbility1(), false))
                    {
                        if (!ability1IsConstrainedByDistance)
                        {
                            weaponHandler.Ability1(true);
                            return;
                        }
                        else if (dist < ability1DistanceMax & dist > ability1DistanceMin)
                        {
                            weaponHandler.Ability1(true);
                            return;
                        }
                    }
                }

                if (canUseAbility2)
                {
                    if (combatAgent.AnimationHandler.CanPlayActionClip(weaponHandler.GetWeapon().GetAbility2(), false))
                    {
                        if (!ability2IsConstrainedByDistance)
                        {
                            weaponHandler.Ability2(true);
                            return;
                        }
                        else if (dist < ability2DistanceMax & dist > ability2DistanceMin)
                        {
                            weaponHandler.Ability2(true);
                            return;
                        }
                    }
                }

                if (combatAgent.WeaponHandler.IsInRecovery)
                {
                    if (dist < LightAttackDistance)
                    {
                        weaponHandler.LightAttack(true);
                        return;
                    }
                }
                else if (dist < stoppingDistance)
                {
                    weaponHandler.LightAttack(true);
                    return;
                }
            }
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (targetingConstrainedByDistance)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(Application.isPlaying ? roamStartPosition : transform.position, maxTargetDistance);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + BodyHeightOffset / 2, transform.forward * LightAttackDistance);
            if (Application.isPlaying)
            {
                if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { return; }

                Gizmos.color = Color.white;
                float dist = Vector3.Distance(Destination, transform.position);
                if (combatAgent.WeaponHandler.IsInRecovery)
                {
                    if (dist < LightAttackDistance)
                    {
                        Gizmos.color = Color.red;
                    }
                }
                else if (dist < stoppingDistance)
                {
                    Gizmos.color = Color.red;
                }
                Gizmos.DrawSphere(transform.position + BodyHeightOffset, 0.3f);
            }
        }
    }
}