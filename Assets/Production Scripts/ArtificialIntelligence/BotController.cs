using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.ScriptableObjects;
using UnityEngine.AI;
using Vi.Utility;
using Vi.Core.MovementHandlers;

namespace Vi.ArtificialIntelligence
{
    public class BotController : PhysicsMovementHandler
    {
        private GameplayTargetFinder targetFinder;
        protected override void Awake()
        {
            base.Awake();
            targetFinder = GetComponent<GameplayTargetFinder>();
        }

        protected override void OnDisable()
        {
            isHeavyAttacking = default;
        }

        protected override void Update()
        {
            base.Update();

            if (!IsSpawned) { return; }

            if (IsServer)
            {
                if (combatAgent.GetAilment() == ActionClip.Ailment.Death) { SetDestination(Rigidbody.position); }
            }

            if (combatAgent.GetAilment() != ActionClip.Ailment.Death & CanMove())
            {
                transform.position = Rigidbody.transform.position;
                transform.rotation = EvaluateRotation();
            }

            SetAnimationMoveInput(GetPathMoveInput(true));
        }

        private Quaternion EvaluateRotation()
        {
            if (IsServer)
            {
                Vector3 camDirection = (NextPosition - Rigidbody.position).normalized;
                camDirection.Scale(HORIZONTAL_PLANE);

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
                        rel = Vector3.Scale(rel, HORIZONTAL_PLANE);
                        return Quaternion.LookRotation(rel, Vector3.up);
                    }
                }
                else if (!combatAgent.ShouldPlayHitStop() & !disableBots)
                    return Quaternion.Lerp(transform.rotation, camDirection == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(camDirection), Time.deltaTime * 3);

                return transform.rotation;
            }
            else
            {
                return transform.rotation;
            }
        }

        private bool disableBots;
        private bool canOnlyLightAttack;
        protected override void RefreshStatus()
        {
            base.RefreshStatus();
            disableBots = FasterPlayerPrefs.Singleton.GetBool("DisableBots");
            canOnlyLightAttack = FasterPlayerPrefs.Singleton.GetBool("BotsCanOnlyLightAttack");
        }

        private void EvaluateBotLogic()
        {
            if (IsServer)
            {
                if (disableBots) { SetDestination(Rigidbody.position); return; }

                targetFinder.ActiveCombatAgents.Sort((x, y) => Vector3.Distance(x.transform.position, Rigidbody.position).CompareTo(Vector3.Distance(y.transform.position, Rigidbody.position)));

                targetFinder.ClearTarget();
                foreach (CombatAgent player in targetFinder.ActiveCombatAgents)
                {
                    if (player.GetAilment() == ActionClip.Ailment.Death) { continue; }
                    if (!PlayerDataManager.Singleton.CanHit(combatAgent, player)) { continue; }

                    targetFinder.SetTarget(player);
                    break;
                }

                if (targetFinder.GetTarget())
                {
                    targetFinder.SetDestination(this);
                    EvaluateAction();
                }
                else if (Vector3.Distance(GetPosition(), Destination) < stoppingDistance)
                {
                    SetDestination(GetRandomDestination());
                }
            }
        }

        private const float lightAttackDistance = 3;
        private const float heavyAttackDistance = 7;

        private const float chargeAttackDuration = 1;
        private const float chargeWaitDuration = 2;
        private float lastChargeAttackTime;

        private const float dodgeWaitDuration = 5;
        private float lastDodgeTime;

        private const float weaponSwapDuration = 20;
        private float lastWeaponSwapTime;

        private const float abilityWaitDuration = 3;
        private float lastAbilityTime;

        private void EvaluateAction()
        {
            if (combatAgent.AnimationHandler.WaitingForActionClipToPlay) { return; }
            if (combatAgent.StatusAgent.IsFeared()) { return; }

            if (canOnlyLightAttack)
            {
                if (Vector3.Distance(Destination, transform.position) < lightAttackDistance)
                {
                    if (weaponHandler.CanAim) { weaponHandler.HeavyAttack(true); }
                    else { weaponHandler.HeavyAttack(false); }

                    weaponHandler.LightAttack(true);
                }
                return;
            }

            if (Time.time - lastWeaponSwapTime > weaponSwapDuration | combatAgent.LoadoutManager.WeaponNameThatCanFlashAttack != null)
            {
                combatAgent.LoadoutManager.SwitchWeapon();
                lastWeaponSwapTime = Time.time;
            }

            if (targetFinder.GetTarget())
            {
                if (weaponHandler.CanADS)
                {
                    weaponHandler.AimDownSights(true);
                    if (Vector3.Distance(Destination, transform.position) < heavyAttackDistance)
                    {
                        EvaluateAbility();
                        weaponHandler.LightAttack(true);
                    }
                }
                else
                {
                    if (Vector3.Distance(Destination, transform.position) < lightAttackDistance)
                    {
                        if (!isHeavyAttacking)
                        {
                            EvaluateAbility();
                            weaponHandler.LightAttack(true);
                        }
                    }
                    else if (Vector3.Distance(Destination, transform.position) < heavyAttackDistance)
                    {
                        EvaluateAbility();
                        if (!weaponHandler.CanADS)
                        {
                            if (!isHeavyAttacking & Time.time - lastChargeAttackTime > chargeWaitDuration) { StartCoroutine(HeavyAttack()); }
                        }
                    }
                }
            }

            if (Time.time - lastDodgeTime > dodgeWaitDuration)
            {
                OnDodge();
                lastDodgeTime = Time.time;
            }
        }

        private bool isHeavyAttacking;

        private IEnumerator HeavyAttack()
        {
            if (isHeavyAttacking) { yield break; }
            isHeavyAttacking = true;

            weaponHandler.HeavyAttack(true);

            yield return new WaitForSeconds(chargeAttackDuration);

            lastChargeAttackTime = Time.time;
            weaponHandler.HeavyAttack(false);

            isHeavyAttacking = false;
        }

        private void EvaluateAbility()
        {
            if (Time.time - lastAbilityTime > abilityWaitDuration)
            {
                if (combatAgent.GetRage() / combatAgent.GetMaxRage() >= 1)
                {
                    combatAgent.OnActivateRage();
                    lastAbilityTime = Time.time;
                    return;
                }

                List<int> abilitiesOffCooldown = new List<int>();
                for (int i = 1; i < 5; i++)
                {
                    switch (i)
                    {
                        case 1:
                            if (Mathf.Approximately(weaponHandler.GetWeapon().GetAbilityCooldownProgress(weaponHandler.GetWeapon().GetAbility1()), 1)) { abilitiesOffCooldown.Add(i); }
                            break;
                        case 2:
                            if (Mathf.Approximately(weaponHandler.GetWeapon().GetAbilityCooldownProgress(weaponHandler.GetWeapon().GetAbility2()), 1)) { abilitiesOffCooldown.Add(i); }
                            break;
                        case 3:
                            if (Mathf.Approximately(weaponHandler.GetWeapon().GetAbilityCooldownProgress(weaponHandler.GetWeapon().GetAbility3()), 1)) { abilitiesOffCooldown.Add(i); }
                            break;
                        case 4:
                            if (Mathf.Approximately(weaponHandler.GetWeapon().GetAbilityCooldownProgress(weaponHandler.GetWeapon().GetAbility4()), 1)) { abilitiesOffCooldown.Add(i); }
                            break;
                        default:
                            Debug.LogError("Unsure how to handle ability num " + i);
                            break;
                    }
                }

                if (abilitiesOffCooldown.Count == 0)
                {
                    lastAbilityTime = Time.time;
                    return;
                }

                int abilityNum = abilitiesOffCooldown[Random.Range(0, abilitiesOffCooldown.Count)];
                if (abilityNum == 1)
                {
                    weaponHandler.Ability1(true);
                }
                else if (abilityNum == 2)
                {
                    weaponHandler.Ability2(true);
                }
                else if (abilityNum == 3)
                {
                    weaponHandler.Ability3(true);
                }
                else if (abilityNum == 4)
                {
                    weaponHandler.Ability4(true);
                }
                else
                {
                    Debug.LogError("Unsure how to handle ability num of - " + abilityNum);
                }
                lastAbilityTime = Time.time;
            }
        }

        void FixedUpdate()
        {
            if (IsServer)
            {
                EvaluateBotLogic();
                Move();
            }
            else
            {
                Rigidbody.MovePosition(networkTransform.GetSpaceRelativePosition(true));
            }
        }

        private void Move()
        {
            Vector3 rootMotion = combatAgent.AnimationHandler.ApplyRootMotion();

            if (!IsSpawned) { return; }

            CalculatePath(Rigidbody.position);

            if (!CanMove())
            {
                transform.position = Rigidbody.position;
                Rigidbody.Sleep();
                return;
            }
            else if (combatAgent.GetAilment() == ActionClip.Ailment.Death)
            {
                Rigidbody.Sleep();
                return;
            }

            if (IsAffectedByExternalForce & !combatAgent.IsGrabbed & !combatAgent.IsGrabbing) { Rigidbody.isKinematic = false; return; }

            Vector2 moveInput = GetPathMoveInput(false);
            Quaternion newRotation = transform.rotation;

            // Apply movement
            Vector3 movement = Vector3.zero;
            if (combatAgent.IsGrabbing)
            {
                Rigidbody.isKinematic = true;
                return;
            }
            else if (combatAgent.IsGrabbed & combatAgent.GetAilment() == ActionClip.Ailment.None)
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
            else if (combatAgent.IsPulled)
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
                if (IsGrounded())
                {
                    Rigidbody.AddForce(new Vector3(movement.x, 0, movement.z) - new Vector3(Rigidbody.linearVelocity.x, 0, Rigidbody.linearVelocity.z), ForceMode.VelocityChange);
                    if (Rigidbody.linearVelocity.y > 0 & Mathf.Approximately(stairMovement, 0)) // This is to prevent slope bounce
                    {
                        Rigidbody.AddForce(new Vector3(0, -Rigidbody.linearVelocity.y, 0), ForceMode.VelocityChange);
                    }
                }
                else // Decelerate horizontal movement while aiRigidbodyorne
                {
                    Vector3 counterForce = Vector3.Slerp(Vector3.zero, new Vector3(-Rigidbody.linearVelocity.x, 0, -Rigidbody.linearVelocity.z), airborneHorizontalDragMultiplier);
                    Rigidbody.AddForce(counterForce, ForceMode.VelocityChange);
                }
            }
            Rigidbody.AddForce(new Vector3(0, stairMovement * stairStepForceMultiplier, 0), ForceMode.VelocityChange);
            if (GetGroundCollidersCount() == 0 | !IsGrounded()) { Rigidbody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration); }
        }

        private const float bodyRadius = 0.5f;

        void OnDodge()
        {
            Vector2 moveInput = GetPathMoveInput(false);
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * (combatAgent.StatusAgent.IsFeared() ? -1 : 1), transform.forward, Vector3.up);
            combatAgent.AnimationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }
    }
}