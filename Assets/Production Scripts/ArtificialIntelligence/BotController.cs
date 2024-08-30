using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.ScriptableObjects;
using UnityEngine.AI;
using Vi.Utility;
using Vi.Core.CombatAgents;

namespace Vi.ArtificialIntelligence
{
    public class BotController : MovementHandler
    {
        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            currentPosition.Value = newPosition;
            currentRotation.Value = newRotation;
            rb.position = newPosition;
        }

        public override Vector3 GetPosition() { return currentPosition.Value; }

        public override Quaternion GetRotation() { return currentRotation.Value; }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                currentPosition.Value = transform.position;
                currentRotation.Value = transform.rotation;
            }
            rb.useGravity = true;
            rb.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
        }

        private Attributes attributes;
        private new void Awake()
        {
            base.Awake();
            attributes = GetComponent<Attributes>();
            RefreshStatus();
        }

        private void Start()
        {
            rb.transform.SetParent(null, true);
            UpdateActivePlayersList();
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (rb) { Destroy(rb.gameObject); }
        }

        private float GetTickRateDeltaTime()
        {
            return NetworkManager.NetworkTickSystem.LocalTime.FixedDeltaTime * Time.timeScale;
        }

        private float GetRootMotionSpeed()
        {
            return Mathf.Clamp01(weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.StatusAgent.GetMovementSpeedDecreaseAmount() + attributes.StatusAgent.GetMovementSpeedIncreaseAmount());
        }

        private List<CombatAgent> activePlayers = new List<CombatAgent>();
        private void UpdateActivePlayersList() { activePlayers = PlayerDataManager.Singleton.GetActiveCombatAgents(attributes); }

        private CombatAgent targetAttributes;

        private const float runAnimationTransitionSpeed = 5;
        private new void Update()
        {
            base.Update();

            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateActivePlayersList(); }

            if (!IsSpawned) { return; }

            if (attributes.GetAilment() == ActionClip.Ailment.Death) { SetDestination(currentPosition.Value, true); }

            UpdateAnimatorParameters();
            UpdateAnimatorSpeed();
            EvaluateBotLogic();
        }

        private void UpdateAnimatorParameters()
        {
            Vector2 walkCycleAnims = GetWalkCycleAnimationParameters();
            attributes.AnimationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveForward"), walkCycleAnims.y, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveSides"), walkCycleAnims.x, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetBool("IsGrounded", IsGrounded());
            attributes.AnimationHandler.Animator.SetFloat("VerticalSpeed", rb.velocity.y);
        }

        private Vector2 GetWalkCycleAnimationParameters()
        {
            if (attributes.AnimationHandler.ShouldApplyRootMotion())
            {
                return Vector2.zero;
            }
            else if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                return Vector2.zero;
            }
            else
            {
                Vector2 moveInput = GetPathMoveInput();
                Vector2 animDir = (new Vector2(moveInput.x, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1));
                animDir = Vector2.ClampMagnitude(animDir, 1);

                if (attributes.WeaponHandler.IsBlocking)
                {
                    switch (attributes.WeaponHandler.GetWeapon().GetBlockingLocomotion())
                    {
                        case Weapon.BlockingLocomotion.NoMovement:
                            animDir = Vector2.zero;
                            break;
                        case Weapon.BlockingLocomotion.CanWalk:
                            animDir /= 2;
                            break;
                        case Weapon.BlockingLocomotion.CanRun:
                            break;
                        default:
                            Debug.LogError("Unsure how to handle blocking locomotion type: " + attributes.WeaponHandler.GetWeapon().GetBlockingLocomotion());
                            break;
                    }
                }
                return animDir;
            }
        }

        private void UpdateAnimatorSpeed()
        {
            if (weaponHandler.CurrentActionClip != null)
            {
                if (attributes.ShouldPlayHitStop())
                {
                    attributes.AnimationHandler.Animator.speed = 0;
                }
                else
                {
                    if (attributes.IsGrabbed())
                    {
                        CombatAgent grabAssailant = attributes.GetGrabAssailant();
                        if (grabAssailant)
                        {
                            if (grabAssailant.AnimationHandler)
                            {
                                attributes.AnimationHandler.Animator.speed = grabAssailant.AnimationHandler.Animator.speed;
                            }
                        }
                    }
                    else
                    {
                        attributes.AnimationHandler.Animator.speed = GetAnimatorSpeed();
                    }
                }
            }
        }

        private bool disableBots;
        private bool canOnlyLightAttack;
        private void RefreshStatus()
        {
            disableBots = FasterPlayerPrefs.Singleton.GetBool("DisableBots");
            canOnlyLightAttack = FasterPlayerPrefs.Singleton.GetBool("BotsCanOnlyLightAttack");
        }

        private void LateUpdate()
        {
            transform.position = rb.transform.position;

            if (attributes.ShouldShake()) { transform.position += Random.insideUnitSphere * (Time.deltaTime * CombatAgent.ShakeAmount); }

            Vector3 camDirection = targetAttributes ? (targetAttributes.transform.position - currentPosition.Value).normalized : (NextPosition - currentPosition.Value).normalized;
            camDirection.Scale(HORIZONTAL_PLANE);

            if (attributes.ShouldApplyAilmentRotation())
                transform.rotation = attributes.GetAilmentRotation();
            else if (attributes.AnimationHandler.IsGrabAttacking())
                transform.rotation = currentRotation.Value;
            else if (!attributes.ShouldPlayHitStop())
                transform.rotation = Quaternion.LookRotation(camDirection);
        }

        private void EvaluateBotLogic()
        {
            if (IsServer)
            {
                activePlayers.Sort((x, y) => Vector3.Distance(x.transform.position, currentPosition.Value).CompareTo(Vector3.Distance(y.transform.position, currentPosition.Value)));

                targetAttributes = null;
                foreach (CombatAgent player in activePlayers)
                {
                    if (player.GetAilment() == ActionClip.Ailment.Death) { continue; }
                    if (!PlayerDataManager.Singleton.CanHit(attributes, player)) { continue; }
                    if (SetDestination(player.transform.position, true)) { targetAttributes = player; }
                    break;
                }

                if (disableBots)
                {
                    SetDestination(currentPosition.Value, true);
                }
                else
                {
                    if (!targetAttributes)
                    {
                        if (Vector3.Distance(Destination, transform.position) <= stoppingDistance)
                        {
                            float walkRadius = 500;
                            Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
                            randomDirection += transform.position;
                            NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, walkRadius, 1);
                            SetDestination(hit.position, true);
                        }
                    }
                    EvaluteAction();
                }
            }
            else if (disableBots)
            {
                if (new Vector2(Destination.x, Destination.z) != new Vector2(currentPosition.Value.x, currentPosition.Value.z)) { SetDestination(currentPosition.Value, true); }
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

        private void EvaluteAction()
        {
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

            if (Time.time - lastWeaponSwapTime > weaponSwapDuration | attributes.LoadoutManager.WeaponNameThatCanFlashAttack != null)
            {
                attributes.LoadoutManager.SwitchWeapon();
                lastWeaponSwapTime = Time.time;
            }

            if (targetAttributes)
            {
                if (weaponHandler.CanADS)
                {
                    weaponHandler.AimDownSights(true);
                    if (Vector3.Distance(Destination, transform.position) < heavyAttackDistance)
                    {
                        weaponHandler.LightAttack(true);
                        EvaluateAbility();
                    }
                }
                else
                {
                    if (Vector3.Distance(Destination, transform.position) < lightAttackDistance)
                    {
                        if (!isHeavyAttacking)
                        {
                            weaponHandler.LightAttack(true);
                            EvaluateAbility();
                        }
                    }
                    else if (Vector3.Distance(Destination, transform.position) < heavyAttackDistance)
                    {
                        if (!weaponHandler.CanADS)
                        {
                            if (!isHeavyAttacking & Time.time - lastChargeAttackTime > chargeWaitDuration) { StartCoroutine(HeavyAttack()); }
                        }
                        EvaluateAbility();
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
                if (attributes.GetRage() / attributes.GetMaxRage() >= 1)
                {
                    attributes.OnActivateRage();
                    lastAbilityTime = Time.time;
                    return;
                }

                int abilityNum = Random.Range(1, 5);
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

        List<Collider> groundColliders = new List<Collider>();
        ContactPoint[] stayContacts = new ContactPoint[3];
        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            int contactCount = collision.GetContacts(stayContacts);
            for (int i = 0; i < contactCount; i++)
            {
                if (stayContacts[i].normal.y >= 0.9f)
                {
                    if (!groundColliders.Contains(collision.collider)) { groundColliders.Add(collision.collider); }
                    break;
                }
                else // Normal is not pointing up
                {
                    if (groundColliders.Contains(collision.collider)) { groundColliders.Remove(collision.collider); }
                }
            }
        }

        public override void ReceiveOnCollisionExitMessage(Collision collision)
        {
            if (groundColliders.Contains(collision.collider))
            {
                groundColliders.Remove(collision.collider);
            }
        }

        private const float isGroundedSphereCheckRadius = 0.6f;
        private bool IsGrounded()
        {
            if (groundColliders.Count > 0)
            {
                return true;
            }
            else
            {
                return Physics.CheckSphere(rb.position, isGroundedSphereCheckRadius, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
            }
        }

        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>();
        void FixedUpdate()
        {
            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                rb.velocity = Vector3.zero;
                return;
            }

            CalculatePath(currentPosition.Value, NavMesh.AllAreas);

            Vector2 moveInput = GetPathMoveInput();
            Quaternion inputRotation = rb.rotation;

            Quaternion newRotation = currentRotation.Value;
            Vector3 camDirection = targetAttributes ? (targetAttributes.transform.position - currentPosition.Value).normalized : (NextPosition - currentPosition.Value).normalized;
            camDirection.Scale(HORIZONTAL_PLANE);

            if (attributes.ShouldApplyAilmentRotation())
                newRotation = attributes.GetAilmentRotation();
            else if (attributes.AnimationHandler.IsGrabAttacking())
                newRotation = inputRotation;
            else if (!attributes.ShouldPlayHitStop())
                newRotation = Quaternion.LookRotation(camDirection);

            // Apply movement
            Vector3 rootMotion = newRotation * attributes.AnimationHandler.ApplyRootMotion() * GetRootMotionSpeed();
            Vector3 movement;
            if (attributes.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (attributes.AnimationHandler.ShouldApplyRootMotion())
            {
                if (attributes.StatusAgent.IsRooted() & attributes.GetAilment() != ActionClip.Ailment.Knockup & attributes.GetAilment() != ActionClip.Ailment.Knockdown)
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
                Vector3 targetDirection = newRotation * (new Vector3(moveInput.x, 0, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= GetRunSpeed();
                movement = attributes.StatusAgent.IsRooted() | attributes.AnimationHandler.IsReloading() ? Vector3.zero : targetDirection;
            }

            if (attributes.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            Vector3 startPos = currentPosition.Value;
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

            if (Physics.CapsuleCast(currentPosition.Value, currentPosition.Value + bodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude * Time.fixedDeltaTime, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore))
            {
                bool collidersIgnoreEachOther = false;
                foreach (Collider c in attributes.NetworkCollider.Colliders)
                {
                    if (Physics.GetIgnoreCollision(playerHit.collider, c))
                    {
                        collidersIgnoreEachOther = true;
                        break;
                    }
                }

                if (!collidersIgnoreEachOther)
                {
                    Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - currentPosition.Value, Vector3.up);
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
                if (attributes.AnimationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip))
                {
                    rb.AddForce(movement - rb.velocity, ForceMode.VelocityChange);
                    evaluateForce = false;
                }
            }

            if (evaluateForce)
            {
                if (IsGrounded())
                {
                    rb.AddForce(new Vector3(movement.x, 0, movement.z) - new Vector3(rb.velocity.x, 0, rb.velocity.z), ForceMode.VelocityChange);
                    if (rb.velocity.y > 0 & Mathf.Approximately(stairMovement, 0)) // This is to prevent slope bounce
                    {
                        rb.AddForce(new Vector3(0, -rb.velocity.y, 0), ForceMode.VelocityChange);
                    }
                }
                else // Decelerate horizontal movement while airborne
                {
                    Vector3 counterForce = Vector3.Slerp(Vector3.zero, new Vector3(-rb.velocity.x, 0, -rb.velocity.z), airborneHorizontalDragMultiplier);
                    rb.AddForce(counterForce, ForceMode.VelocityChange);
                }
            }

            rb.AddForce(new Vector3(0, stairMovement, 0), ForceMode.VelocityChange);

            currentPosition.Value = rb.position;
            currentRotation.Value = rb.rotation;
        }

        private const float stairStepHeight = 0.01f;
        private const float maxStairStepHeight = 0.5f;

        private const float airborneHorizontalDragMultiplier = 0.1f;

        private float GetRunSpeed()
        {
            return Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount();
        }

        private float GetAnimatorSpeed()
        {
            return (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * (attributes.AnimationHandler.IsAtRest() ? 1 : (weaponHandler.IsInRecovery ? weaponHandler.CurrentActionClip.recoveryAnimationSpeed : weaponHandler.CurrentActionClip.animationSpeed));
        }

        void OnDodge()
        {
            Vector2 moveInput = GetPathMoveInput();
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1), transform.forward, Vector3.up);
            attributes.AnimationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }
    }
}